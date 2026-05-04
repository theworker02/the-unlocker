package main

import (
	"context"
	"log"
	"net/http"
	"os"
	"time"

	"github.com/theunlocker/theunlocker/backend/go-api/internal/api"
)

func main() {
	port := getenv("PORT", "8088")
	registryURL := getenv("REGISTRY_BASE_URL", "http://ruby-registry:4567")
	mongoURL := os.Getenv("MONGO_URL")
	mongoDatabase := getenv("MONGO_DATABASE", "theunlocker_registry")

	var store api.ModStore
	if mongoURL != "" {
		mongoStore, err := api.NewMongoModStore(context.Background(), mongoURL, mongoDatabase)
		if err != nil {
			log.Printf("mongo store unavailable, falling back to proxy/sample data: %v", err)
		} else {
			store = mongoStore
		}
	}

	server := api.NewServer(api.Options{
		RegistryBaseURL: registryURL,
		ModStore:        store,
		HTTPClient: &http.Client{
			Timeout: 10 * time.Second,
		},
		StartedAt: time.Now().UTC(),
	})

	log.Printf("theunlocker go api listening on :%s, registry=%s", port, registryURL)
	if err := http.ListenAndServe(":"+port, server.Router()); err != nil {
		log.Fatal(err)
	}
}

func getenv(key, fallback string) string {
	value := os.Getenv(key)
	if value == "" {
		return fallback
	}
	return value
}
