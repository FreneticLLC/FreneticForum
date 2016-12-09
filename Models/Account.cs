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

        public LoginResult CanLogin(string pw)
        {
            BsonDocument acc = Projected(PASSWORD, BANNED);
            if (acc == null)
            {
                return LoginResult.MISSING;
            }
            if ((bool)acc[BANNED])
            {
                return LoginResult.BANNED;
            }
            string hash = ForumUtilities.Hash(pw, UserName);
            byte[] b1 = ForumUtilities.Enc.GetBytes(hash);
            byte[] b2 = ForumUtilities.Enc.GetBytes((string)acc[PASSWORD]);
            if (!ForumUtilities.SlowEquals(b1, b2))
            {
                return LoginResult.BAD_PASSWORD;
            }
            return LoginResult.ALLOWED;
        }
    }

    public enum LoginResult
    {
        ALLOWED = 0,
        BANNED = 1,
        BAD_PASSWORD = 2,
        MISSING = 3
    }
}
