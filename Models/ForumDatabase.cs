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
        public const string TF_USERS = "tf_users";
        public const string TF_SETTINGS = "tf_settings";
        public const string TF_SECTIONS = "tf_sections";
        public const string TF_TOPICS = "tf_topics";
        public const string TF_POSTS = "tf_posts";

        public string[] TFS = new string[] { TF_USERS, TF_SETTINGS, TF_SECTIONS, TF_TOPICS, TF_POSTS };

        public string[] TFS_WITH_UIDS = new string[] { TF_USERS, TF_SECTIONS, TF_TOPICS, TF_POSTS };
        public MongoClient Client;

        public IMongoDatabase Database;

        public void InstallCollections()
        {
            ListCollectionsOptions lco = new ListCollectionsOptions();
            lco.Filter = Builders<BsonDocument>.Filter.Eq("name", TF_USERS);
            if (Database.ListCollectionsAsync(lco).Result.AnyAsync().Result)
            {
                // We have the database already, somehow. Trying to re-create them will error, so we'll do nothing for now.
                //return;
                // Actually, let's give an error. A big shiny one. That way we don't fight with an existing installation!
                throw new InvalidOperationException("Trying to install a forum to a database that already has a forum in it!");
            }
            // Create the base collections
            Database.CreateCollectionAsync(TF_USERS).Wait();
            Database.CreateCollectionAsync(TF_SETTINGS).Wait();
            Database.CreateCollectionAsync(TF_SECTIONS).Wait();
            Database.CreateCollectionAsync(TF_TOPICS).Wait();
            Database.CreateCollectionAsync(TF_POSTS).Wait();
            // Grab the collections
            IMongoCollection<BsonDocument> tf_users = Database.GetCollection<BsonDocument>(TF_USERS);
            IMongoCollection<BsonDocument> tf_settings = Database.GetCollection<BsonDocument>(TF_SETTINGS);
            IMongoCollection<BsonDocument> tf_sections = Database.GetCollection<BsonDocument>(TF_SECTIONS);
            IMongoCollection<BsonDocument> tf_topics = Database.GetCollection<BsonDocument>(TF_TOPICS);
            IMongoCollection<BsonDocument> tf_posts = Database.GetCollection<BsonDocument>(TF_POSTS);
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
            // 'uid' for sections.
            tf_sections.Indexes.CreateOneAsync(new IndexKeysDefinitionBuilder<BsonDocument>().Ascending(uidField), options);
            // 'uid' for topics.
            tf_topics.Indexes.CreateOneAsync(new IndexKeysDefinitionBuilder<BsonDocument>().Ascending(uidField), options);
            // 'uid' for posts.
            tf_posts.Indexes.CreateOneAsync(new IndexKeysDefinitionBuilder<BsonDocument>().Ascending(uidField), options);
        }

        public long getIDFor(string mode)
        {
            IMongoCollection<BsonDocument> tf_settings = Database.GetCollection<BsonDocument>(TF_SETTINGS);
            string id_target = "_internal.counter_ids." + mode;
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("name", id_target);
            UpdateDefinition<BsonDocument> ud = new UpdateDefinitionBuilder<BsonDocument>().Inc("value", (long)1);
            BsonDocument res = tf_settings.FindOneAndUpdateAsync(fd, ud).Result;
            return res["value"].AsInt64;
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
            user["active"] = false;
            user["activation_code"] = "<Unusable>?";
            user["register_date"] = ForumUtilities.DateNow();
            user["last_login_date"] = "Never";
            return user;
        }

        public BsonDocument GenerateNewUser(string uname, string pw, string email)
        {
            uname = uname.ToLowerInvariant();
            BsonDocument bd = CreateEmptyUserDocument();
            bd["username"] = uname;
            bd["password"] = ForumUtilities.Hash(pw, uname);
            bd["email"] = email;
            bd["uid"] = getIDFor(TF_USERS);
            bd["activation_code"] = ForumUtilities.GetRandomHex(32);
            return bd;
        }

        public void InstallDefaultUser(string pw)
        {
            IMongoCollection<BsonDocument> userbase = Database.GetCollection<BsonDocument>(TF_USERS);
            BsonDocument user = CreateEmptyUserDocument();
            user["uid"] = (long)0;
            user["username"] = "admin";
            user["display_name"] = "Administrator";
            user["password"] = ForumUtilities.Hash(pw, "admin");
            user["active"] = true;
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("uid", (long)0);
            UpdateOptions uo = new UpdateOptions() { IsUpsert = true };
            userbase.ReplaceOneAsync(fd, user, uo).Wait();
        }

        public void SetSetting(IMongoCollection<BsonDocument> settings, string setting, string value)
        {
            BsonDocument doc = new BsonDocument();
            doc["name"] = setting;
            doc["value"] = value;
        }

        public void InstallDefaultSettings()
        {
            IMongoCollection<BsonDocument> settings = Database.GetCollection<BsonDocument>(TF_SETTINGS);
            foreach (string str in TFS_WITH_UIDS)
            {
                BsonDocument doc = new BsonDocument();
                doc["name"] = "_internal.counter_ids." + str;
                doc["value"] = (long)100;
                settings.InsertOneAsync(doc);
            }
        }

        public void InstallAll(string admin_pw, string title)
        {
            InstallCollections();
            InstallDefaultSettings();
            InstallDefaultUser(admin_pw);
            // Common Configuration
            IMongoCollection<BsonDocument> settings = Database.GetCollection<BsonDocument>(TF_SETTINGS);
            SetSetting(settings, "title", title);
        }

        public ForumDatabase(string conStr, string db)
        {
            Client = new MongoClient(conStr);
            Database = Client.GetDatabase(db);
        }

        public Account GetAccount(string name)
        {
            IMongoCollection<BsonDocument> userbase = Database.GetCollection<BsonDocument>(TF_USERS);
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("name", name);
            ProjectionDefinition<BsonDocument> proj = Builders<BsonDocument>.Projection.Include("uid").Include("name");
            BsonDocument acc = userbase.Find(fd).Project(proj).FirstAsync().Result;
            if (acc == null)
            {
                return null;
            }
            return new Account(userbase, (string)acc["name"], (long)acc["uid"]);
        }

        public Account GetAccount(long uid)
        {
            IMongoCollection<BsonDocument> userbase = Database.GetCollection<BsonDocument>(TF_USERS);
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("uid", uid);
            ProjectionDefinition<BsonDocument> proj = Builders<BsonDocument>.Projection.Include("uid").Include("name");
            BsonDocument acc = userbase.Find(fd).Project(proj).FirstAsync().Result;
            if (acc == null)
            {
                return null;
            }
            return new Account(userbase, (string)acc["name"], (long)acc["uid"]);
        }
    }
}
