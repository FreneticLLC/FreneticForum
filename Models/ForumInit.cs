using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MongoDB.Bson;
using MongoDB.Driver;
using FreneticUtilities.FreneticExtensions;

namespace FreneticForum.Models
{
    public class ForumInit
    {
        // ----------------------------- EDIT BELOW ------------------------- //
        public const string CONFIG_FILE_FOLDER_LOCATION = "./config/";
        // ----------------------------- EDIT ABOVE ------------------------- //

        public const string CONFIG_FILE_LOCATION = CONFIG_FILE_FOLDER_LOCATION + "FreneticForum.cfg";

        public static void SaveNewConfig(string dbpath, string dbname)
        {
            Directory.CreateDirectory(CONFIG_FILE_FOLDER_LOCATION);
            File.WriteAllText(CONFIG_FILE_LOCATION, "database_path: " + dbpath + "\ndatabase_db: " + dbname + "\n");
        }

        public ForumDatabase Database;

        public Dictionary<string, string> Config;

        public Dictionary<string, string> GetConfig()
        {
            if (!File.Exists(CONFIG_FILE_LOCATION))
            {
                return null;
            }
            string[] data = File.ReadAllText(CONFIG_FILE_LOCATION).Replace("\r", "\n").Split('\n');
            Dictionary<string, string> toret = [];
            foreach (string str in data)
            {
                string fstr = str.Trim();
                if (fstr.StartsWithFast('#'))
                {
                    continue;
                }
                string[] dat = fstr.Split([':'], 2);
                if (dat.Length == 2)
                {
                    toret[dat[0].ToLowerFast()] = dat[1].Trim();
                }
            }
            return toret;
        }

        public HttpRequest Request;

        public HttpResponse Response;

        public Account User = null;

        public bool IsGuest()
        {
            return User == null || User.GetActType() != Account.AT_VALID;
        }

        public LoginResult AttemptLogin(string username, string password, string tfa)
        {
            Account acc = Database.GetAccount(username);
            if (acc == null)
            {
                return LoginResult.MISSING;
            }
            LoginResult res = acc.CanLogin(password, tfa);
            if (res != LoginResult.ALLOWED)
            {
                return res;
            }
            acc.Update(Builders<BsonDocument>.Update.Set(Account.LAST_LOGIN_DATE, ForumUtilities.DateNow()));
            CookieOptions co_uid = new()
            {
                HttpOnly = true, // NOTE: Microsoft HttpOnly documentation appears to be backwards?
                Expires = DateTimeOffset.Now.AddYears(1)
            };
            Response.Cookies.Append("session_uid", acc.UserID.ToString(), co_uid);
            Response.Cookies.Append("session_val", acc.GenerateSession(), co_uid);
            return LoginResult.ALLOWED;
        }

        public Account TrySession(long uid, string sess)
        {
            Account acc = Database.GetAccount(uid);
            if (acc == null)
            {
                return null;
            }
            if (!acc.TrySession(sess))
            {
                return null;
            }
            return acc;
        }

        public ForumInit(HttpRequest htr, HttpResponse hres)
        {
            Request = htr;
            Response = hres;
            Config = GetConfig();
            if (Config == null || Config.Count == 0)
            {
                throw new InitFailedException();
            }
            else
            {
                Database = new ForumDatabase(Config["database_path"], Config["database_db"]);
            }
            if (Request.Cookies.ContainsKey("session_uid") && Request.Cookies.ContainsKey("session_val"))
            {
                string suid = Request.Cookies["session_uid"];
                string sval = Request.Cookies["session_val"];
                if (long.TryParse(suid, out long t))
                {
                    User = TrySession(t, sval);
                }
            }
            if (User != null && Request.Method == "POST" && Request.Form.ContainsKey("mode") && Request.Form["mode"].ToString() == "LOGOUT_NOW")
            {
                User.ClearSessions(); // TODO: Remove current session only. Also, a profile button to clear-all.
                Response.Cookies.Delete("session_uid");
                Response.Cookies.Delete("session_val");
                Response.Redirect("/");
                throw new NoProcessException();
            }
        }
    }
}
