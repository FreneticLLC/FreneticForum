using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TestForum.Models
{
    public class ForumDatabase
    {
        public MongoClient Client;

        public IMongoDatabase Database;

        public void InstallCollections()
        {
            // Create the base collections
            Database.CreateCollectionAsync("tf_users").Wait();
            Database.CreateCollectionAsync("tf_settings").Wait();
            Database.CreateCollectionAsync("tf_sections").Wait();
            Database.CreateCollectionAsync("tf_topics").Wait();
            Database.CreateCollectionAsync("tf_posts").Wait();
            // Grab the collections
            IMongoCollection<BsonDocument> tf_users = Database.GetCollection<BsonDocument>("tf_users");
            IMongoCollection<BsonDocument> tf_settings = Database.GetCollection<BsonDocument>("tf_settings");
            IMongoCollection<BsonDocument> tf_sections = Database.GetCollection<BsonDocument>("tf_sections");
            IMongoCollection<BsonDocument> tf_topics = Database.GetCollection<BsonDocument>("tf_topics");
            IMongoCollection<BsonDocument> tf_posts = Database.GetCollection<BsonDocument>("tf_posts");
            // Ensure their 'Indexes'
            CreateIndexOptions options = new CreateIndexOptions() { Unique = true };
            // 'uid' for users.
            FieldDefinition<BsonDocument, long> uidField = "uid";
            tf_users.Indexes.CreateOneAsync(new IndexKeysDefinitionBuilder<BsonDocument>().Ascending(uidField), options);
            // 'username' for users.
            StringFieldDefinition<BsonDocument> usernameField = new StringFieldDefinition<BsonDocument>("username");
            tf_users.Indexes.CreateOneAsync(new IndexKeysDefinitionBuilder<BsonDocument>().Ascending(usernameField), options);
            // 'name' for settings.
            StringFieldDefinition<BsonDocument> nameField = new StringFieldDefinition<BsonDocument>("name");
            tf_settings.Indexes.CreateOneAsync(new IndexKeysDefinitionBuilder<BsonDocument>().Ascending(nameField), options);
            // 'name' for sections.
            tf_sections.Indexes.CreateOneAsync(new IndexKeysDefinitionBuilder<BsonDocument>().Ascending(nameField), options);
            // 'uid' for topics.
            tf_topics.Indexes.CreateOneAsync(new IndexKeysDefinitionBuilder<BsonDocument>().Ascending(uidField), options);
            // 'uid' for posts.
            tf_posts.Indexes.CreateOneAsync(new IndexKeysDefinitionBuilder<BsonDocument>().Ascending(uidField), options);
        }

        public BsonDocument CreateEmptyUserDocument()
        {
            BsonDocument user = new BsonDocument();
            user["uid"] = (long)-1;
            user["username"] = "example";
            user["password"] = "This Won't Be Usable";
            user["email"] = "example@example.com";
            user["display_name"] = "Example";
            user["banned"] = false;
            user["banned_until"] = "";
            user["ban_reason"] = "";
            return user;
        }

        public void InstallDefaultUser(string pw)
        {
            IMongoCollection<BsonDocument> userbase = Database.GetCollection<BsonDocument>("tf_users");
            BsonDocument user = CreateEmptyUserDocument();
            user["uid"] = (long)0;
            user["username"] = "admin";
            user["display_name"] = "Administrator";
            user["password"] = ForumUtilities.Hash(pw, user["username"].AsString); // TODO: Hash!
            userbase.InsertOneAsync(user).Wait();
        }

        public void InstallDefaultSettings()
        {
            IMongoCollection<BsonDocument> settings = Database.GetCollection<BsonDocument>("tf_settings");
        }

        public ForumDatabase(string conStr, string db)
        {
            Client = new MongoClient(conStr);
            Database = Client.GetDatabase(db);
        }
    }
}
