package api

import (
	"context"
	"time"

	"go.mongodb.org/mongo-driver/bson"
	"go.mongodb.org/mongo-driver/mongo"
	"go.mongodb.org/mongo-driver/mongo/options"
)

type ModStore interface {
	ListMods(context.Context) ([]registryMod, error)
	GetMod(context.Context, string) (registryMod, bool, error)
	UpsertMod(context.Context, registryMod) error
}

type MongoModStore struct {
	client     *mongo.Client
	collection *mongo.Collection
}

func NewMongoModStore(ctx context.Context, connectionString, databaseName string) (*MongoModStore, error) {
	client, err := mongo.Connect(ctx, options.Client().ApplyURI(connectionString))
	if err != nil {
		return nil, err
	}
	if err := client.Ping(ctx, nil); err != nil {
		return nil, err
	}
	return &MongoModStore{
		client:     client,
		collection: client.Database(databaseName).Collection("mods"),
	}, nil
}

func (s *MongoModStore) ListMods(ctx context.Context) ([]registryMod, error) {
	ctx, cancel := context.WithTimeout(ctx, 5*time.Second)
	defer cancel()

	cursor, err := s.collection.Find(ctx, bson.M{})
	if err != nil {
		return nil, err
	}
	defer cursor.Close(ctx)

	var mods []registryMod
	if err := cursor.All(ctx, &mods); err != nil {
		return nil, err
	}
	return mods, nil
}

func (s *MongoModStore) GetMod(ctx context.Context, id string) (registryMod, bool, error) {
	ctx, cancel := context.WithTimeout(ctx, 5*time.Second)
	defer cancel()

	var mod registryMod
	err := s.collection.FindOne(ctx, bson.M{"id": id}).Decode(&mod)
	if err == mongo.ErrNoDocuments {
		return registryMod{}, false, nil
	}
	if err != nil {
		return registryMod{}, false, err
	}
	return mod, true, nil
}

func (s *MongoModStore) UpsertMod(ctx context.Context, mod registryMod) error {
	ctx, cancel := context.WithTimeout(ctx, 5*time.Second)
	defer cancel()

	_, err := s.collection.UpdateOne(ctx, bson.M{"id": mod.ID}, bson.M{"$set": mod}, options.Update().SetUpsert(true))
	return err
}
