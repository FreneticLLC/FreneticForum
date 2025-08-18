using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FreneticUtilities.FreneticExtensions;
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

        public string[] TFS = [TF_USERS, TF_SETTINGS, TF_SECTIONS, TF_TOPICS, TF_POSTS];

        public string[] TFS_WITH_UIDS = [TF_USERS, TF_SECTIONS, TF_TOPICS, TF_POSTS];
        
        public MongoClient Client;

        public IMongoDatabase Database;

        public void InstallCollections()
        {
            ListCollectionsOptions lco = new()
            {
                Filter = Builders<BsonDocument>.Filter.Eq("name", TF_USERS)
            };
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
            CreateIndexOptions options = new() { Unique = true };
            // 'uid' for users.
            FieldDefinition<BsonDocument, long> uidField = "uid";
            tf_users.Indexes.CreateOneAsync(new IndexKeysDefinitionBuilder<BsonDocument>().Ascending(uidField), options);
            // 'username' for users.
            StringFieldDefinition<BsonDocument> usernameField = new("username");
            tf_users.Indexes.CreateOneAsync(new IndexKeysDefinitionBuilder<BsonDocument>().Ascending(usernameField), options);
            // 'name' for settings.
            StringFieldDefinition<BsonDocument> nameField = new("name");
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

        public long GetIDFor(string mode)
        {
            IMongoCollection<BsonDocument> tf_settings = Database.GetCollection<BsonDocument>(TF_SETTINGS);
            string id_target = "_internal.counter_ids." + mode;
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("name", id_target);
            UpdateDefinition<BsonDocument> ud = new UpdateDefinitionBuilder<BsonDocument>().Inc("value", (long)1);
            FindOneAndUpdateOptions<BsonDocument> uo = new() { IsUpsert = true };
            BsonDocument res = tf_settings.FindOneAndUpdateAsync(fd, ud, uo).Result;
            return res["value"].AsInt64;
        }

        public long GetViewCounter(string page)
        {
            IMongoCollection<BsonDocument> tf_settings = Database.GetCollection<BsonDocument>(TF_SETTINGS);
            string id_target = "count_views." + page;
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("name", id_target);
            UpdateDefinition<BsonDocument> ud = new UpdateDefinitionBuilder<BsonDocument>().Inc("value", (long)1);
            FindOneAndUpdateOptions<BsonDocument> uo = new() { IsUpsert = true };
            BsonDocument res = tf_settings.FindOneAndUpdateAsync(fd, ud, uo).Result;
            if (res == null)
            {
                return 0;
            }
            return res["value"].AsInt64;
        }

        public BsonDocument CreateEmptyUserDocument()
        {
            BsonDocument user = new()
            {
                [Account.UID] = (long)-1,
                [Account.USERNAME] = "example",
                [Account.PASSWORD] = "This Won't Be Usable",
                [Account.EMAIL] = "example@example.com",
                [Account.DISPLAY_NAME] = "Example",
                [Account.BANNED] = false,
                [Account.BANNED_UNTIL] = "",
                [Account.BAN_REASON] = "",
                [Account.ACTIVE] = false,
                [Account.ACTIVATION_CODE] = "<Unusable>?",
                [Account.REGISTER_DATE] = ForumUtilities.DateNow(),
                [Account.LAST_LOGIN_DATE] = "Never",
                [Account.USES_TFA] = false,
                [Account.TFA_BACKUPS] = "",
                [Account.TFA_INTERNAL] = "",
                [Account.ACCOUNT_TYPE] = Account.AT_INCOMPLETE,
                [Account.ROLES] = new BsonArray(Array.Empty<BsonValue>())
            };
            return user;
        }

        public BsonDocument GenerateNewUser(string uname, string pw, string email)
        {
            uname = uname.ToLowerFast();
            BsonDocument bd = CreateEmptyUserDocument();
            bd[Account.USERNAME] = uname;
            bd[Account.PASSWORD] = ForumUtilities.Hash(pw, uname);
            bd[Account.EMAIL] = email;
            bd[Account.UID] = GetIDFor(TF_USERS);
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
            user[Account.ACCOUNT_TYPE] = Account.AT_VALID;
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(Account.UID, (long)0);
            ReplaceOptions uo = new() { IsUpsert = true };
            userbase.ReplaceOneAsync(fd, user, uo).Wait();
        }

        public void SetSetting(IMongoCollection<BsonDocument> settings, string setting, long value)
        {
            setting = setting.ToLowerFast();
            BsonDocument doc = new()
            {
                ["name"] = setting,
                ["value"] = value
            };
            ReplaceOptions uo = new() { IsUpsert = true };
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("name", setting);
            settings.ReplaceOneAsync(fd, doc, uo).Wait();
        }

        public void SetSetting(IMongoCollection<BsonDocument> settings, string setting, string value)
        {
            setting = setting.ToLowerFast();
            BsonDocument doc = new()
            {
                ["name"] = setting,
                ["value"] = value
            };
            ReplaceOptions uo = new() { IsUpsert = true };
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("name", setting);
            settings.ReplaceOneAsync(fd, doc, uo).Wait();
        }

        public string GetSetting(string setting, string def)
        {
            setting = setting.ToLowerFast();
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
            setting = setting.ToLowerFast();
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
                BsonDocument doc = new()
                {
                    ["name"] = "_internal.counter_ids." + str,
                    ["value"] = (long)100
                };
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
            return new Account(userbase, (string)acc[Account.USERNAME], (long)acc[Account.UID]) { FData = this };
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
            return new Account(userbase, (string)acc[Account.USERNAME], (long)acc[Account.UID]) { FData = this };
        }
    }
}
