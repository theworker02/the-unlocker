FROM ruby:3.3-alpine

WORKDIR /app
RUN apk add --no-cache build-base
COPY backend/ruby-registry/Gemfile ./
RUN bundle install
COPY backend/ruby-registry ./

EXPOSE 4567
CMD ["bundle", "exec", "puma", "-p", "4567", "config.ru"]
