using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FreneticForum.Models
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
            FindOneAndUpdateOptions<BsonDocument> uo = new FindOneAndUpdateOptions<BsonDocument>() { IsUpsert = true };
            BsonDocument res = tf_settings.FindOneAndUpdateAsync(fd, ud, uo).Result;
            return res["value"].AsInt64;
        }

        public long getViewCounter(string page)
        {
            IMongoCollection<BsonDocument> tf_settings = Database.GetCollection<BsonDocument>(TF_SETTINGS);
            string id_target = "count_views." + page;
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("name", id_target);
            UpdateDefinition<BsonDocument> ud = new UpdateDefinitionBuilder<BsonDocument>().Inc("value", (long)1);
            FindOneAndUpdateOptions<BsonDocument> uo = new FindOneAndUpdateOptions<BsonDocument>() { IsUpsert = true };
            BsonDocument res = tf_settings.FindOneAndUpdateAsync(fd, ud, uo).Result;
            if (res == null)
            {
                return 0;
            }
            return res["value"].AsInt64;
        }

        public BsonDocument CreateEmptyUserDocument()
        {
            BsonDocument user = new BsonDocument();
            user[Account.UID] = (long)-1;
            user[Account.USERNAME] = "example";
            user[Account.PASSWORD] = "This Won't Be Usable";
            user[Account.EMAIL] = "example@example.com";
            user[Account.DISPLAY_NAME] = "Example";
            user[Account.BANNED] = false;
            user[Account.BANNED_UNTIL] = "";
            user[Account.BAN_REASON] = "";
            user[Account.ACTIVE] = false;
            user[Account.ACTIVATION_CODE] = "<Unusable>?";
            user[Account.REGISTER_DATE] = ForumUtilities.DateNow();
            user[Account.LAST_LOGIN_DATE] = "Never";
            return user;
        }

        public BsonDocument GenerateNewUser(string uname, string pw, string email)
        {
            uname = uname.ToLowerInvariant();
            BsonDocument bd = CreateEmptyUserDocument();
            bd[Account.USERNAME] = uname;
            bd[Account.PASSWORD] = ForumUtilities.Hash(pw, uname);
            bd[Account.EMAIL] = email;
            bd[Account.UID] = getIDFor(TF_USERS);
            bd[Account.ACTIVATION_CODE] = ForumUtilities.GetRandomHex(32);
            return bd;
        }

        public void InstallDefaultUser(string pw)
        {
            IMongoCollection<BsonDocument> userbase = Database.GetCollection<BsonDocument>(TF_USERS);
            BsonDocument user = CreateEmptyUserDocument();
            user[Account.UID] = (long)0;
            user[Account.USERNAME] = "admin";
            user[Account.DISPLAY_NAME] = "Administrator";
            user[Account.PASSWORD] = ForumUtilities.Hash(pw, "admin");
            user[Account.ACTIVE] = true;
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(Account.UID, (long)0);
            UpdateOptions uo = new UpdateOptions() { IsUpsert = true };
            userbase.ReplaceOneAsync(fd, user, uo).Wait();
        }

        public void SetSetting(IMongoCollection<BsonDocument> settings, string setting, long value)
        {
            setting = setting.ToLowerInvariant();
            BsonDocument doc = new BsonDocument();
            doc["name"] = setting;
            doc["value"] = value;
            UpdateOptions uo = new UpdateOptions() { IsUpsert = true };
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("name", setting);
            settings.ReplaceOneAsync(fd, doc, uo).Wait();
        }

        public void SetSetting(IMongoCollection<BsonDocument> settings, string setting, string value)
        {
            setting = setting.ToLowerInvariant();
            BsonDocument doc = new BsonDocument();
            doc["name"] = setting;
            doc["value"] = value;
            UpdateOptions uo = new UpdateOptions() { IsUpsert = true };
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("name", setting);
            settings.ReplaceOneAsync(fd, doc, uo).Wait();
        }

        public string GetSetting(string setting, string def)
        {
            setting = setting.ToLowerInvariant();
            IMongoCollection<BsonDocument> settings = Database.GetCollection<BsonDocument>(TF_SETTINGS);
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("name", setting);
            BsonDocument bsd = settings.Find(fd).FirstOrDefaultAsync().Result;
            if (bsd == null)
            {
                return def;
            }
            return bsd["value"].AsString;
        }

        public long GetLongSetting(string setting, long def)
        {
            setting = setting.ToLowerInvariant();
            IMongoCollection<BsonDocument> settings = Database.GetCollection<BsonDocument>(TF_SETTINGS);
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("name", setting);
            BsonDocument bsd = settings.Find(fd).FirstOrDefaultAsync().Result;
            if (bsd == null)
            {
                return def;
            }
            return bsd["value"].AsInt64;
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

        public void InstallAll(string admin_pw, string title, string mainurl)
        {
            InstallCollections();
            InstallDefaultSettings();
            InstallDefaultUser(admin_pw);
            // Common Configuration
            IMongoCollection<BsonDocument> settings = Database.GetCollection<BsonDocument>(TF_SETTINGS);
            SetSetting(settings, "title", title);
            SetSetting(settings, "main_url", mainurl);
        }

        public ForumDatabase(string conStr, string db)
        {
            Client = new MongoClient(conStr);
            Database = Client.GetDatabase(db);
        }

        public Account GetAccount(string name)
        {
            IMongoCollection<BsonDocument> userbase = Database.GetCollection<BsonDocument>(TF_USERS);
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(Account.USERNAME, name);
            ProjectionDefinition<BsonDocument> proj = Builders<BsonDocument>.Projection.Include(Account.UID).Include(Account.USERNAME);
            BsonDocument acc = userbase.Find(fd).Project(proj).FirstOrDefaultAsync().Result;
            if (acc == null)
            {
                return null;
            }
            return new Account(userbase, (string)acc[Account.USERNAME], (long)acc[Account.UID]);
        }

        public Account GetAccount(long uid)
        {
            IMongoCollection<BsonDocument> userbase = Database.GetCollection<BsonDocument>(TF_USERS);
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(Account.UID, uid);
            ProjectionDefinition<BsonDocument> proj = Builders<BsonDocument>.Projection.Include(Account.UID).Include(Account.USERNAME);
            BsonDocument acc = userbase.Find(fd).Project(proj).FirstOrDefaultAsync().Result;
            if (acc == null)
            {
                return null;
            }
            return new Account(userbase, (string)acc[Account.USERNAME], (long)acc[Account.UID]);
        }
    }
}
