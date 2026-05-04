FROM golang:1.23-alpine AS build
WORKDIR /src
COPY backend/go-api/go.mod backend/go-api/go.sum ./
RUN go mod download
COPY backend/go-api ./
RUN go build -o /out/theunlocker-go-api ./cmd/server

FROM alpine:3.21
WORKDIR /app
COPY --from=build /out/theunlocker-go-api /app/theunlocker-go-api
ENV PORT=8088
EXPOSE 8088
ENTRYPOINT ["/app/theunlocker-go-api"]
