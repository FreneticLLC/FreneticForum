using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace TestForum.Models
{
    public class Account
    {
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
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq("uid", UserID);
            ProjectionDefinition<BsonDocument> proj = Builders<BsonDocument>.Projection.Include(vals[0]);
            for (int i = 1; i < vals.Length; i++)
            {
                proj = proj.Include(vals[i]);
            }
            return UserBase.Find(fd).Project(proj).FirstAsync().Result;
        }

        public LoginResult CanLogin(string pw)
        {
            BsonDocument acc = Projected("password", "banned");
            if (acc == null)
            {
                return LoginResult.MISSING;
            }
            if ((bool)acc["banned"])
            {
                return LoginResult.BANNED;
            }
            string hash = ForumUtilities.Hash(pw, UserName);
            byte[] b1 = ForumUtilities.Enc.GetBytes(hash);
            byte[] b2 = ForumUtilities.Enc.GetBytes((string)acc["password"]);
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
