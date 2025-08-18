using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Driver;
using Google.Authenticator;
using FreneticUtilities.FreneticExtensions;

namespace FreneticForum.Models
{
    public class Account(IMongoCollection<BsonDocument> ub, string uname, long uid)
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
        public const string PREFIX_SESS_MASTER = "sess_master_";
        public const string PREFIX_ONE_USE_SESS = "sess_oneuse_";
        public const string USES_TFA = "uses_tfa";
        public const string TFA_INTERNAL = "tfa_internal";
        public const string TFA_BACKUPS = "tfa_backups";
        public const string ACCOUNT_TYPE = "account_type";
        public const string ROLES = "roles";
        
        public string UserName = uname;

        public long UserID = uid;

        public const int AT_INCOMPLETE = 0;
        public const int AT_GUEST = 1;
        public const int AT_VALID = 2;

        public IMongoCollection<BsonDocument> UserBase = ub;

        public ForumDatabase FData;

        public bool IsValidSessType(string inp)
        {
            // TODO: Match against database list
            return ForumUtilities.ValidateUsername(inp) && inp.ToLowerFast() == inp;
        }

        public string GenerateSessMaster(string typ)
        {
            if (!IsValidSessType(typ))
            {
                return null;
            }
            string sess = ForumUtilities.GetRandomHex(32);
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(UID, UserID);
            UpdateDefinition<BsonDocument> ud = Builders<BsonDocument>.Update.AddToSet(PREFIX_SESS_MASTER + typ, sess);
            FindOneAndUpdateOptions<BsonDocument> foauo = new()
            {
                IsUpsert = true
            };
            UserBase.FindOneAndUpdate(fd, ud, foauo);
            return sess;
        }

        public bool TrySessMaster(string typ, string sess)
        {
            if (!IsValidSessType(typ))
            {
                return false;
            }
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(UID, UserID);
            FilterDefinition<BsonDocument> fd_valids = Builders<BsonDocument>.Filter.AnyEq(PREFIX_SESS_MASTER + typ, sess);
            FilterDefinition<BsonDocument> both = fd & fd_valids;
            return UserBase.CountDocumentsAsync(both).Result > 0;
        }

        public string GenerateOneUseSess(string typ)
        {
            if (!IsValidSessType(typ))
            {
                return null;
            }
            string sess = ForumUtilities.GetRandomHex(32);
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(UID, UserID);
            UpdateDefinition<BsonDocument> ud = Builders<BsonDocument>.Update.Set(PREFIX_ONE_USE_SESS + typ, sess + "/" + DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            FindOneAndUpdateOptions<BsonDocument> foauo = new()
            {
                IsUpsert = true
            };
            UserBase.FindOneAndUpdate(fd, ud, foauo);
            return sess;
        }

        public bool CheckOneUseSess(string typ, string val)
        {
            if (!IsValidSessType(typ))
            {
                return false;
            }
            BsonDocument bsd = Projected(PREFIX_ONE_USE_SESS + typ);
            if (bsd == null || !bsd.Contains(PREFIX_ONE_USE_SESS + typ))
            {
                return false;
            }
            string keydat = bsd[PREFIX_ONE_USE_SESS + typ].AsString;
            string[] split = keydat.Split('/');
            if (!long.TryParse(split[1], out long t))
            {
                return false;
            }
            bool passed = val == split[0] && DateTimeOffset.UtcNow.Subtract(DateTimeOffset.FromUnixTimeSeconds(t)).TotalSeconds < 60; // TODO: 60: Configurable?
            if (passed)
            {
                Update(Builders<BsonDocument>.Update.Set(PREFIX_ONE_USE_SESS + typ, ""));
            }
            return passed;
        }

        public int GetActType()
        {
            return Projected(ACCOUNT_TYPE)[ACCOUNT_TYPE].AsInt32;
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

        public SetupCode GenerateTFA()
        {
            TwoFactorAuthenticator tfao = new();
            string ihex = ForumUtilities.GetRandomB64(6);
            SetupCode sc = tfao.GenerateSetupCode("Frenetic LLC", UserName, ihex, 300, 300, true);
            Update(Builders<BsonDocument>.Update.Set(TFA_INTERNAL, ihex));
            GenerateBackups();
            return sc;
        }

        public void GenerateBackups()
        {
            StringBuilder res = new();
            for (int i = 0; i < 5; i++)
            {
                res.Append(ForumUtilities.GetRandomHex(8)).Append('|');
            }
            Update(Builders<BsonDocument>.Update.Set(TFA_BACKUPS, res.ToString()));
        }

        public string GetBackups()
        {
            return Projected(TFA_BACKUPS)[TFA_BACKUPS].AsString;
        }

        public void DisableTFA()
        {
            Update(Builders<BsonDocument>.Update.Set(USES_TFA, false).Set(TFA_INTERNAL, "").Set(TFA_BACKUPS, ""));
        }

        public void EnableTFA()
        {
            Update(Builders<BsonDocument>.Update.Set(USES_TFA, true));
        }

        public void Update(UpdateDefinition<BsonDocument> theUpdate)
        {
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(UID, UserID);
            FindOneAndUpdateOptions<BsonDocument> foauo = new()
            {
                IsUpsert = true
            };
            UserBase.FindOneAndUpdate(fd, theUpdate, foauo);
        }

        public bool TrySession(string sess)
        {
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(UID, UserID);
            FilterDefinition<BsonDocument> fd_valids = Builders<BsonDocument>.Filter.AnyEq(WEBSESS_CODES, sess);
            FilterDefinition<BsonDocument> both = fd & fd_valids;
            return UserBase.CountDocumentsAsync(both).Result > 0;
        }

        public string GenerateSession()
        {
            string sess = ForumUtilities.GetRandomHex(32);
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(UID, UserID);
            UpdateDefinition<BsonDocument> ud = Builders<BsonDocument>.Update.AddToSet(WEBSESS_CODES, sess);
            FindOneAndUpdateOptions<BsonDocument> foauo = new()
            {
                IsUpsert = true
            };
            UserBase.FindOneAndUpdate(fd, ud, foauo);
            return sess;
        }

        public void ClearSessions()
        {
            FilterDefinition<BsonDocument> fd = Builders<BsonDocument>.Filter.Eq(UID, UserID);
            UpdateDefinition<BsonDocument> ud = Builders<BsonDocument>.Update.Unset(WEBSESS_CODES);
            FindOneAndUpdateOptions<BsonDocument> foauo = new()
            {
                IsUpsert = true
            };
            UserBase.FindOneAndUpdate(fd, ud, foauo);
        }

        public bool UsesTFA()
        {
            return Projected(USES_TFA)[USES_TFA].AsBoolean;
        }

        public LoginResult CanLogin(string pw, string tfa, bool checkTFA = true)
        {
            BsonDocument acc = Projected(PASSWORD, BANNED, USES_TFA, ACCOUNT_TYPE);
            if (acc == null)
            {
                return LoginResult.MISSING;
            }
            if (acc[ACCOUNT_TYPE].AsInt32 != AT_VALID)
            {
                return LoginResult.NOT_COMPLETED_REGISTRATION;
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
                // TODO: Error log upload! "Invalid password for " + UserName + ": unrecognized version, they will be unable to login without a reset!"
                return LoginResult.BAD_PASSWORD;
            }
            if (checkTFA && acc[USES_TFA].AsBoolean)
            {
                if (!CheckTFA(tfa))
                {
                    return LoginResult.BAD_TFA;
                }
            }
            return LoginResult.ALLOWED;
        }

        public bool CheckTFA(string tfa)
        {
            BsonDocument acc = Projected(TFA_INTERNAL);
            TwoFactorAuthenticator tfao = new();
            return tfao.ValidateTwoFactorPIN(acc[TFA_INTERNAL].AsString, tfa);
        }
    }

    public enum LoginResult
    {
        ALLOWED = 0,
        BANNED = 1,
        BAD_PASSWORD = 2,
        MISSING = 3,
        BAD_TFA = 4,
        NOT_COMPLETED_REGISTRATION = 5
    }
}
