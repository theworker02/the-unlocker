package client

import (
	"bytes"
	"encoding/json"
	"fmt"
	"net/http"
	"strings"
	"time"
)

type Client struct {
	baseURL    string
	token      string
	apiKey     string
	httpClient *http.Client
}

type Option func(*Client)

func WithBearerToken(token string) Option {
	return func(client *Client) {
		client.token = token
	}
}

func WithAPIKey(apiKey string) Option {
	return func(client *Client) {
		client.apiKey = apiKey
	}
}

func New(baseURL string, options ...Option) *Client {
	client := &Client{
		baseURL: strings.TrimRight(baseURL, "/"),
		httpClient: &http.Client{
			Timeout: 15 * time.Second,
		},
	}
	for _, option := range options {
		option(client)
	}
	return client
}

func (c *Client) Health() (map[string]any, error) {
	var result map[string]any
	err := c.send(http.MethodGet, "/api/v1/health", nil, &result)
	return result, err
}

func (c *Client) Mods() ([]RegistryMod, error) {
	var result []RegistryMod
	err := c.send(http.MethodGet, "/api/v1/mods", nil, &result)
	return result, err
}

func (c *Client) Mod(id string) (RegistryMod, error) {
	var result RegistryMod
	err := c.send(http.MethodGet, "/api/v1/mods/"+id, nil, &result)
	return result, err
}

func (c *Client) CreateJob(jobType string, payload any) (map[string]any, error) {
	var result map[string]any
	err := c.send(http.MethodPost, "/api/v1/jobs/"+jobType, payload, &result)
	return result, err
}

func (c *Client) send(method, path string, payload any, result any) error {
	var body *bytes.Reader
	if payload == nil {
		body = bytes.NewReader(nil)
	} else {
		data, err := json.Marshal(payload)
		if err != nil {
			return err
		}
		body = bytes.NewReader(data)
	}

	request, err := http.NewRequest(method, c.baseURL+path, body)
	if err != nil {
		return err
	}
	request.Header.Set("Accept", "application/json")
	if payload != nil {
		request.Header.Set("Content-Type", "application/json")
	}
	if c.token != "" {
		request.Header.Set("Authorization", "Bearer "+c.token)
	}
	if c.apiKey != "" {
		request.Header.Set("X-Api-Key", c.apiKey)
	}

	response, err := c.httpClient.Do(request)
	if err != nil {
		return err
	}
	defer response.Body.Close()

	if response.StatusCode >= 400 {
		return fmt.Errorf("theunlocker api returned %s", response.Status)
	}

	return json.NewDecoder(response.Body).Decode(result)
}

type RegistryMod struct {
	ID          string       `json:"id"`
	Name        string       `json:"name"`
	Author      string       `json:"author"`
	Description string       `json:"description"`
	Status      string       `json:"status"`
	GameID      string       `json:"gameId"`
	TrustLevel  string       `json:"trustLevel"`
	Tags        []string     `json:"tags"`
	Permissions []string     `json:"permissions"`
	Versions    []ModVersion `json:"versions"`
}

type ModVersion struct {
	Version     string `json:"version"`
	DownloadURL string `json:"downloadUrl"`
	SHA256      string `json:"sha256"`
	Changelog   string `json:"changelog"`
	CreatedAt   string `json:"createdAt"`
}
