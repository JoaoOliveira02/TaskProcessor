using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using TaskProcessor.Domain.Entities;

namespace TaskProcessor.Infrastructure.Persistence;

public class MongoDbContext(IMongoClient client, string databaseName)
{
    private readonly IMongoDatabase _database = client.GetDatabase(databaseName);

    static MongoDbContext()
    {
        BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
    }

    public IMongoCollection<Job> Jobs => _database.GetCollection<Job>("jobs");
}