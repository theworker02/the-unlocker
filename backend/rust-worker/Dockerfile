FROM rust:1.94-bookworm AS build
WORKDIR /src
COPY backend/rust-worker/Cargo.toml ./
COPY backend/rust-worker/src ./src
RUN cargo build --release

FROM debian:bookworm-slim
WORKDIR /app
COPY --from=build /src/target/release/theunlocker-rust-worker /usr/local/bin/theunlocker-rust-worker
EXPOSE 7070
CMD ["theunlocker-rust-worker"]
