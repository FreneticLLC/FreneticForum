using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FreneticForum.Models
{
    public class Account
    {
        public const string LAST_LOGIN_DATE = "last_login_date";
        public const string REGISTER_DATE = "register_date";
        public const string ACTIVATION_CODE = "activation_code";
        public const string ACTIVE = "active";
        public const string BAN_REASON = "ban_reason";
        public const string BANNED_UNTIL = "banned_until";
        public const string BANNED = "banned";
        public const string DISPLAY_NAME = "display_name";
        public const string EMAIL = "email";
        public const string PASSWORD = "password";
        public const string USERNAME = "username";
        public const string UID = "uid";
        public const string WEBSESS_CODES = "websess_codes";

        
        public string UserName;

        public long UserID;

        IMongoCollection<BsonDocument> UserBase;

        public Account(IMongoCollection<BsonDocument> ub, string uname, long uid)
        {
            UserBase = ub;
            UserName = uname;
            UserID = uid;
        }

        public BsonDocument Projected(params string[] vals)
        {
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(UID, UserID);
            ProjectionDefinition<BsonDocument> proj = Builders<BsonDocument>.Projection.Include(vals[0]);
            for (int i = 1; i < vals.Length; i++)
            {
                proj = proj.Include(vals[i]);
            }
            return UserBase.Find(fd).Project(proj).FirstOrDefaultAsync().Result;
        }

        public bool TrySession(string sess)
        {
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(UID, UserID);
            FilterDefinition<BsonDocument> fd_valids = Builders<BsonDocument>.Filter.AnyEq(WEBSESS_CODES, sess);
            FilterDefinition<BsonDocument> both = fd & fd_valids;
            return UserBase.Find(both).CountAsync().Result > 0;
        }

        public string GenerateSession()
        {
            string sess = ForumUtilities.GetRandomHex(32);
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(UID, UserID);
            UpdateDefinition<BsonDocument> ud = Builders<BsonDocument>.Update.AddToSet(WEBSESS_CODES, sess);
            FindOneAndUpdateOptions<BsonDocument> foauo = new FindOneAndUpdateOptions<BsonDocument>();
            foauo.IsUpsert = true;
            UserBase.FindOneAndUpdate(fd, ud, foauo);
            return sess;
        }

        public void ClearSessions()
        {
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(UID, UserID);
            UpdateDefinition<BsonDocument> ud = Builders<BsonDocument>.Update.Unset(WEBSESS_CODES);
            FindOneAndUpdateOptions<BsonDocument> foauo = new FindOneAndUpdateOptions<BsonDocument>();
            foauo.IsUpsert = true;
            UserBase.FindOneAndUpdate(fd, ud, foauo);
        }

        public LoginResult CanLogin(string pw, string tfa)
        {
            BsonDocument acc = Projected(PASSWORD, BANNED);
            if (acc == null)
            {
                return LoginResult.MISSING;
            }
            if (acc[BANNED].AsBoolean)
            {
                return LoginResult.BANNED;
            }
            string opass = acc[PASSWORD].AsString;
            string[] dat = opass.Split(':');
            if (dat[0] == "v2")
            {
                string hash = ForumUtilities.HashV2(pw, UserName, dat[1]);
                byte[] b1 = ForumUtilities.Enc.GetBytes(hash);
                byte[] b2 = ForumUtilities.Enc.GetBytes(opass);
                if (!ForumUtilities.SlowEquals(b1, b2))
                {
                    return LoginResult.BAD_PASSWORD;
                }
            }
            else
            {
                // TODO: Error? "Invalid password for " + UserName + ": unrecognized version, they will be unable to login without a reset!"
                return LoginResult.BAD_PASSWORD;
            }
            // TODO: TFA Check
            return LoginResult.ALLOWED;
        }
    }

    public enum LoginResult
    {
        ALLOWED = 0,
        BANNED = 1,
        BAD_PASSWORD = 2,
        MISSING = 3,
        BAD_TFA = 4
    }
}
