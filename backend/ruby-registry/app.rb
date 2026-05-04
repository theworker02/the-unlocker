require "json"
require "net/http"
require "sinatra/base"
require "redis"
require "mongo"
require "securerandom"
require "bcrypt"
require "time"

class TheUnlockerRegistry < Sinatra::Base
  set :bind, "0.0.0.0"
  set :port, ENV.fetch("PORT", "4567")

  before do
    content_type :json
    headers "Access-Control-Allow-Origin" => "*"
  end

  options "*" do
    headers "Access-Control-Allow-Methods" => "GET,POST,OPTIONS"
    headers "Access-Control-Allow-Headers" => "Content-Type,X-Api-Key,Authorization"
    204
  end

  get "/health" do
    json(
      status: "Healthy",
      registry: "ruby",
      mongo: ping_mongo,
      redis: ping_redis,
      minio: ENV.fetch("MINIO_ENDPOINT", "not configured"),
      checkedAt: Time.now.utc.iso8601
    )
  end

  post "/auth/register" do
    body = parse_body
    email = body["email"].to_s.strip.downcase
    password = body["password"].to_s
    display_name = body["displayName"].to_s.strip

    halt 400, json(error: "email, displayName, and password are required") if email.empty? || display_name.empty? || password.length < 8
    halt 409, json(error: "account already exists") if mongo[:users].find("email" => email).first

    user = {
      "id" => SecureRandom.hex(16),
      "email" => email,
      "displayName" => display_name,
      "passwordHash" => hash_password(password),
      "onboardingComplete" => false,
      "trustedDevices" => [],
      "createdAt" => Time.now.utc,
      "updatedAt" => Time.now.utc
    }
    mongo[:users].insert_one(user)
    audit_login(user["id"], "register", true)
    json_session_for(user)
  end

  post "/auth/login" do
    body = parse_body
    email = body["email"].to_s.strip.downcase
    password = body["password"].to_s
    user = mongo[:users].find("email" => email).first
    unless user && verify_password(user["passwordHash"], password)
      audit_login(user ? user["id"] : "", "login", false)
      halt 401, json(error: "invalid email or password")
    end

    user["lastSignedInAt"] = Time.now.utc
    mongo[:users].update_one({ "id" => user["id"] }, { "$set" => { "lastSignedInAt" => user["lastSignedInAt"], "updatedAt" => Time.now.utc } })
    audit_login(user["id"], "login", true)
    json_session_for(user)
  end

  post "/auth/logout" do
    token = bearer_token
    mongo[:sessions].update_one({ "token" => token }, { "$set" => { "revokedAt" => Time.now.utc } }) if token
    json(ok: true)
  end

  post "/auth/refresh" do
    body = parse_body
    refresh_token = body["refreshToken"].to_s
    session = mongo[:sessions].find("refreshToken" => refresh_token, "refreshExpiresAt" => { "$gt" => Time.now.utc }, "revokedAt" => nil).first
    halt 401, json(error: "missing or expired refresh token") unless session

    user = mongo[:users].find("id" => session["userId"]).first
    halt 401, json(error: "user not found") unless user

    mongo[:sessions].update_one({ "token" => session["token"] }, { "$set" => { "revokedAt" => Time.now.utc } })
    json_session_for(user)
  end

  post "/auth/password-reset/request" do
    body = parse_body
    email = body["email"].to_s.strip.downcase
    user = mongo[:users].find("email" => email).first

    if user
      token = SecureRandom.hex(32)
      mongo[:password_resets].insert_one({
        "id" => SecureRandom.hex(16),
        "userId" => user["id"],
        "token" => token,
        "createdAt" => Time.now.utc,
        "expiresAt" => Time.now.utc + (60 * 30),
        "usedAt" => nil
      })
      audit_login(user["id"], "password-reset-request", true)
      json(ok: true, message: "password reset requested", devResetToken: token)
    else
      audit_login("", "password-reset-request", false)
      json(ok: true, message: "password reset requested")
    end
  end

  post "/auth/password-reset/confirm" do
    body = parse_body
    token = body["token"].to_s
    password = body["password"].to_s
    halt 400, json(error: "token and a new password of at least 8 characters are required") if token.empty? || password.length < 8

    reset = mongo[:password_resets].find("token" => token, "expiresAt" => { "$gt" => Time.now.utc }, "usedAt" => nil).first
    halt 401, json(error: "missing or expired reset token") unless reset

    mongo[:users].update_one({ "id" => reset["userId"] }, { "$set" => { "passwordHash" => hash_password(password), "updatedAt" => Time.now.utc } })
    mongo[:password_resets].update_one({ "token" => token }, { "$set" => { "usedAt" => Time.now.utc } })
    mongo[:sessions].update_many({ "userId" => reset["userId"], "revokedAt" => nil }, { "$set" => { "revokedAt" => Time.now.utc } })
    audit_login(reset["userId"], "password-reset-confirm", true)
    json(ok: true)
  end

  post "/auth/email-verification/request" do
    session = require_session
    user = mongo[:users].find("id" => session["userId"]).first
    halt 401, json(error: "user not found") unless user

    token = SecureRandom.hex(32)
    mongo[:email_verifications].insert_one({
      "id" => SecureRandom.hex(16),
      "userId" => user["id"],
      "email" => user["email"],
      "token" => token,
      "createdAt" => Time.now.utc,
      "expiresAt" => Time.now.utc + (60 * 60 * 24),
      "usedAt" => nil
    })
    json(ok: true, message: "email verification requested", devVerificationToken: token)
  end

  post "/auth/email-verification/confirm" do
    body = parse_body
    token = body["token"].to_s
    verification = mongo[:email_verifications].find("token" => token, "expiresAt" => { "$gt" => Time.now.utc }, "usedAt" => nil).first
    halt 401, json(error: "missing or expired verification token") unless verification

    mongo[:users].update_one({ "id" => verification["userId"] }, { "$set" => { "emailVerified" => true, "updatedAt" => Time.now.utc } })
    mongo[:email_verifications].update_one({ "token" => token }, { "$set" => { "usedAt" => Time.now.utc } })
    json(ok: true)
  end

  post "/auth/revoke" do
    session = require_session
    body = parse_body
    target_token = body["token"].to_s
    halt 400, json(error: "token is required") if target_token.empty?

    mongo[:sessions].update_one({ "userId" => session["userId"], "token" => target_token }, { "$set" => { "revokedAt" => Time.now.utc } })
    json(ok: true)
  end

  get "/auth/session" do
    session = current_session
    halt 401, json(error: "missing or expired session") unless session
    user = mongo[:users].find("id" => session["userId"]).first
    halt 401, json(error: "user not found") unless user
    json(session_response(user, session))
  end

  get "/account/settings" do
    session = require_session
    user = mongo[:users].find("id" => session["userId"]).first
    halt 401, json(error: "user not found") unless user
    json(public_user(user).merge(trustedDevices: user["trustedDevices"] || []))
  end

  get "/account/security" do
    session = require_session
    user = mongo[:users].find("id" => session["userId"]).first
    halt 401, json(error: "user not found") unless user

    sessions = mongo[:sessions].find("userId" => user["id"]).sort(createdAt: -1).limit(20).map do |item|
      {
        id: item["token"].to_s[0, 12],
        createdAt: item["createdAt"].respond_to?(:iso8601) ? item["createdAt"].iso8601 : item["createdAt"].to_s,
        expiresAt: item["expiresAt"].respond_to?(:iso8601) ? item["expiresAt"].iso8601 : item["expiresAt"].to_s,
        revoked: !!item["revokedAt"]
      }
    end

    audit = mongo[:login_audit].find("userId" => user["id"]).sort(createdAt: -1).limit(20).map do |item|
      {
        action: item["action"],
        success: !!item["success"],
        ip: item["ip"],
        userAgent: item["userAgent"],
        createdAt: item["createdAt"].respond_to?(:iso8601) ? item["createdAt"].iso8601 : item["createdAt"].to_s
      }
    end

    json(
      userId: user["id"],
      email: user["email"],
      emailVerified: !!user["emailVerified"],
      trustedDevices: user["trustedDevices"] || [],
      sessions: sessions,
      loginAudit: audit
    )
  end

  post "/account/settings" do
    session = require_session
    body = parse_body
    updates = {
      "displayName" => body["displayName"].to_s.strip,
      "registryUrl" => body["registryUrl"].to_s.strip,
      "primaryGame" => body["primaryGame"].to_s.strip,
      "updatedAt" => Time.now.utc
    }.reject { |_, value| value.respond_to?(:empty?) && value.empty? }

    if body["password"].to_s.length >= 8
      updates["passwordHash"] = hash_password(body["password"].to_s)
    end

    mongo[:users].update_one({ "id" => session["userId"] }, { "$set" => updates })
    user = mongo[:users].find("id" => session["userId"]).first
    json(session_response(user, session))
  end

  post "/onboarding" do
    session = require_session
    body = parse_body
    updates = {
      "onboardingComplete" => true,
      "role" => body["role"].to_s,
      "primaryGame" => body["primaryGame"].to_s,
      "registryUrl" => body["registryUrl"].to_s,
      "updatedAt" => Time.now.utc
    }
    mongo[:users].update_one({ "id" => session["userId"] }, { "$set" => updates })
    user = mongo[:users].find("id" => session["userId"]).first
    json(session_response(user, session))
  end

  get "/mods" do
    mods = load_mods
    mods = filter_mods(mods, params)
    json(mods)
  end

  get "/mods/:id" do
    mod = load_mods.find { |item| item["id"].casecmp?(params[:id]) }
    halt 404, json(error: "mod not found") unless mod
    json(mod)
  end

  post "/jobs/:type" do
    payload = parse_body.merge("id" => SecureRandom.hex(16), "type" => params[:type], "createdAt" => Time.now.utc.iso8601)
    redis.lpush("theunlocker:jobs:#{params[:type]}", JSON.generate(payload))
    status 202
    json(payload)
  end

  post "/mods" do
    body = parse_body
    halt 400, json(error: "id is required") if body["id"].to_s.strip.empty?
    collection = mongo[:mods]
    body["updatedAt"] = Time.now.utc
    collection.update_one({ "id" => body["id"] }, { "$set" => body }, upsert: true)
    status 201
    json(body)
  end

  post "/crash-reports" do
    report = parse_body.merge("id" => SecureRandom.hex(16), "submittedAt" => Time.now.utc.iso8601)
    mongo[:crash_reports].insert_one(report)
    redis.lpush("theunlocker:jobs:crash-triage", JSON.generate(report))
    status 201
    json(report)
  end

  private

  def json(value)
    JSON.pretty_generate(value)
  end

  def parse_body
    request.body.rewind if request.body.respond_to?(:rewind)
    body = request.body.read
    body.empty? ? {} : JSON.parse(body)
  rescue JSON::ParserError
    halt 400, json(error: "invalid json")
  end

  def filter_mods(mods, query)
    result = mods
    if query["q"] && !query["q"].empty?
      needle = query["q"].downcase
      result = result.select { |mod| [mod["id"], mod["name"], mod["description"]].compact.any? { |value| value.downcase.include?(needle) } }
    end
    result = result.select { |mod| mod["gameId"].to_s.casecmp?(query["game"]) } if query["game"] && !query["game"].empty?
    result = result.select { |mod| mod["trustLevel"].to_s.casecmp?(query["trust"]) } if query["trust"] && !query["trust"].empty?
    result
  end

  def json_session_for(user)
    session = {
      "token" => SecureRandom.hex(32),
      "refreshToken" => SecureRandom.hex(48),
      "userId" => user["id"],
      "createdAt" => Time.now.utc,
      "expiresAt" => Time.now.utc + (60 * 60 * 24),
      "refreshExpiresAt" => Time.now.utc + (60 * 60 * 24 * 30),
      "revokedAt" => nil
    }
    mongo[:sessions].insert_one(session)
    json(session_response(user, session))
  end

  def session_response(user, session)
    {
      token: session["token"],
      refreshToken: session["refreshToken"],
      expiresAt: session["expiresAt"].respond_to?(:iso8601) ? session["expiresAt"].iso8601 : session["expiresAt"].to_s,
      refreshExpiresAt: session["refreshExpiresAt"].respond_to?(:iso8601) ? session["refreshExpiresAt"].iso8601 : session["refreshExpiresAt"].to_s,
      user: public_user(user)
    }
  end

  def public_user(user)
    {
      id: user["id"],
      email: user["email"],
      displayName: user["displayName"],
      onboardingComplete: !!user["onboardingComplete"],
      role: user["role"].to_s,
      primaryGame: user["primaryGame"].to_s,
      registryUrl: user["registryUrl"].to_s,
      emailVerified: !!user["emailVerified"]
    }
  end

  def current_session
    token = bearer_token
    return nil unless token
    mongo[:sessions].find("token" => token, "expiresAt" => { "$gt" => Time.now.utc }, "revokedAt" => nil).first
  end

  def require_session
    session = current_session
    halt 401, json(error: "sign in required") unless session
    session
  end

  def bearer_token
    auth = request.env["HTTP_AUTHORIZATION"].to_s
    return nil unless auth.start_with?("Bearer ")
    auth.delete_prefix("Bearer ").strip
  end

  def hash_password(password)
    BCrypt::Password.create(password)
  end

  def verify_password(hash, password)
    BCrypt::Password.new(hash) == password
  rescue BCrypt::Errors::InvalidHash
    false
  end

  def audit_login(user_id, action, success)
    mongo[:login_audit].insert_one({
      "id" => SecureRandom.hex(16),
      "userId" => user_id,
      "action" => action,
      "success" => success,
      "ip" => request.ip,
      "userAgent" => request.user_agent.to_s,
      "createdAt" => Time.now.utc
    })
  end

  def load_mods
    docs = mongo[:mods].find.to_a
    return docs.map { |doc| normalize_mod(doc) } unless docs.empty?

    proxy_mods || [
      {
        "id" => "hello-world",
        "name" => "Hello World",
        "author" => "Sample Author",
        "description" => "Ruby registry fallback sample.",
        "status" => "Approved",
        "gameId" => "unity",
        "trustLevel" => "Trusted Publisher",
        "tags" => ["sample"],
        "permissions" => ["AddMenuItems"],
        "versions" => [{ "version" => "1.0.0", "downloadUrl" => "#", "sha256" => "", "changelog" => "Initial", "createdAt" => Time.now.utc.iso8601 }]
      }
    ]
  end

  def normalize_mod(doc)
    doc.delete("_id")
    doc["tags"] ||= []
    doc["permissions"] ||= []
    doc["versions"] ||= []
    doc
  end

  def proxy_mods
    url = ENV["DOTNET_REGISTRY_URL"]
    return nil unless url && !url.empty?

    response = Net::HTTP.get_response(URI("#{url}/mods"))
    return nil unless response.is_a?(Net::HTTPSuccess)
    JSON.parse(response.body)
  rescue StandardError
    nil
  end

  def mongo
    @mongo ||= Mongo::Client.new(ENV.fetch("MONGO_URL", "mongodb://localhost:27017/theunlocker_registry"))
  end

  def redis
    @redis ||= Redis.new(url: ENV.fetch("REDIS_URL", "redis://localhost:6379/0"))
  end

  def ping_mongo
    mongo.database.command(ping: 1)
    "ok"
  rescue StandardError
    "unavailable"
  end

  def ping_redis
    redis.ping
  rescue StandardError
    "unavailable"
  end
end
