use anyhow::Result;
use axum::{routing::get, Json, Router};
use redis::AsyncCommands;
use serde::{Deserialize, Serialize};
use sha2::{Digest, Sha256};
use std::{env, net::SocketAddr, path::Path, time::Duration};
use tokio::time::sleep;

#[derive(Debug, Serialize)]
struct HealthResponse {
    status: &'static str,
    service: &'static str,
    redis: String,
}

#[derive(Debug, Deserialize, Serialize)]
struct WorkerJob {
    id: Option<String>,
    #[serde(rename = "type")]
    job_type: Option<String>,
    #[serde(rename = "packagePath")]
    package_path: Option<String>,
}

#[derive(Debug, Serialize)]
struct ScanReport {
    job_id: String,
    package_path: String,
    sha256: String,
    status: String,
}

#[tokio::main]
async fn main() -> Result<()> {
    tracing_subscriber::fmt()
        .with_env_filter(tracing_subscriber::EnvFilter::from_default_env())
        .init();

    let redis_url = env::var("REDIS_URL").unwrap_or_else(|_| "redis://localhost:6379/0".to_string());
    let worker_redis_url = redis_url.clone();
    tokio::spawn(async move {
        if let Err(error) = worker_loop(worker_redis_url).await {
            tracing::error!(%error, "worker loop stopped");
        }
    });

    let app = Router::new().route("/health", get(move || health(redis_url.clone())));
    let address: SocketAddr = "0.0.0.0:7070".parse()?;
    let listener = tokio::net::TcpListener::bind(address).await?;
    tracing::info!("Rust worker listening on {address}");
    axum::serve(listener, app).await?;
    Ok(())
}

async fn health(redis_url: String) -> Json<HealthResponse> {
    let redis = match redis::Client::open(redis_url)
        .and_then(|client| client.get_connection())
        .and_then(|mut connection| redis::cmd("PING").query::<String>(&mut connection))
    {
        Ok(value) => value,
        Err(_) => "unavailable".to_string(),
    };

    Json(HealthResponse {
        status: "Healthy",
        service: "rust-worker",
        redis,
    })
}

async fn worker_loop(redis_url: String) -> Result<()> {
    let client = redis::Client::open(redis_url)?;
    loop {
        let mut connection = client.get_multiplexed_async_connection().await?;
        let job: Option<[String; 2]> = connection
            .brpop("theunlocker:jobs:package-scan", 2.0)
            .await?;

        if let Some([_, payload]) = job {
            process_job(&payload).await?;
        }

        sleep(Duration::from_millis(250)).await;
    }
}

async fn process_job(payload: &str) -> Result<()> {
    let job: WorkerJob = serde_json::from_str(payload)?;
    let job_id = job.id.unwrap_or_else(|| "unknown".to_string());
    let package_path = job.package_path.unwrap_or_default();
    let sha256 = if package_path.is_empty() || !Path::new(&package_path).exists() {
        "metadata-only".to_string()
    } else {
        hash_file(&package_path).await?
    };

    let report = ScanReport {
        job_id,
        package_path,
        sha256,
        status: "scanned".to_string(),
    };
    tracing::info!("{}", serde_json::to_string(&report)?);
    Ok(())
}

async fn hash_file(path: &str) -> Result<String> {
    let bytes = tokio::fs::read(path).await?;
    let mut hasher = Sha256::new();
    hasher.update(bytes);
    Ok(format!("{:X}", hasher.finalize()))
}
