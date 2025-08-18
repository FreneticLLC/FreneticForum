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
using FreneticUtilities.FreneticDataSyntax;

namespace FreneticForum.Models
{
    public class ForumInit
    {
        // ----------------------------- EDIT BELOW ------------------------- //
        public const string CONFIG_FILE_FOLDER_LOCATION = "./config/";
        // ----------------------------- EDIT ABOVE ------------------------- //

        public const string CONFIG_FILE_LOCATION = CONFIG_FILE_FOLDER_LOCATION + "FreneticForum.fds";

        public static void SaveNewConfig(string dbpath, string dbname)
        {
            Directory.CreateDirectory(CONFIG_FILE_FOLDER_LOCATION);
            Config = new FDSSection();
            File.WriteAllText(CONFIG_FILE_LOCATION, "database_path: " + dbpath + "\ndatabase_db: " + dbname + "\n");
        }

        public static ForumDatabase Database;

        public static FDSSection Config;

        public static void LoadConfig()
        {
            Config = new();
            if (File.Exists(CONFIG_FILE_LOCATION))
            {
                Config = FDSUtility.ReadFile(CONFIG_FILE_LOCATION);
            }
        }

        public HttpRequest Request;

        public HttpResponse Response;

        public Account User = null;

        public bool IsGuest()
        {
            return User == null || User.GetActType() != Account.AT_VALID;
        }

        public RegisterResult AttemptRegister(string username, string password, string password2, string validationCode, string email)
        {
            if (password != password2)
            {
                return RegisterResult.MISMATCHED_PASSWORDS;
            }
            if (password.Length < 8 || password.Length > 128)
            {
                return RegisterResult.BAD_PASSWORD;
            }
            if (!email.Contains('@') || !email.Contains('.'))
            {
                return RegisterResult.BAD_EMAIL;
            }
            if (!ForumUtilities.ValidateUsername(username))
            {
                return RegisterResult.BAD_USERNAME;
            }
            if (validationCode != Config.GetString("validation_code", ""))
            {
                return RegisterResult.BAD_VALIDATION;
            }
            Account acc = Database.GetAccount(username);
            if (acc is not null)
            {
                return RegisterResult.USERNAME_TAKEN;
            }
            BsonDocument userData = Database.GenerateNewUser(username, password, email);
            Database.RegisterAccount(userData);
            return RegisterResult.ACCEPTED;
        }

        public LoginResult AttemptLogin(string username, string password, string tfa)
        {
            Account acc = Database.GetAccount(username);
            if (acc is null)
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
                HttpOnly = true,
                Expires = DateTimeOffset.Now.AddYears(1)
            };
            Response.Cookies.Append("session_uid", acc.UserID.ToString(), co_uid);
            Response.Cookies.Append("session_val", acc.GenerateSession(), co_uid);
            return LoginResult.ALLOWED;
        }

        public Account TrySession(long uid, string sess)
        {
            Account acc = Database.GetAccount(uid);
            if (acc is null)
            {
                return null;
            }
            if (!acc.TrySession(sess))
            {
                return null;
            }
            return acc;
        }

        static ForumInit()
        {
            LoadConfig();
            Init();
        }

        public static void Init()
        {
            if (Config is not null && Config.HasKey("database_path"))
            {
                Database = new ForumDatabase(Config.GetString("database_path"), Config.GetString("database_db"));
            }
        }

        public ForumInit(HttpRequest htr, HttpResponse hres)
        {
            if (Database is null)
            {
                Init();
                if (Database is null)
                {
                    throw new InitFailedException();
                }
            }
            Request = htr;
            Response = hres;
            if (Request.Cookies.ContainsKey("session_uid") && Request.Cookies.ContainsKey("session_val"))
            {
                string suid = Request.Cookies["session_uid"];
                string sval = Request.Cookies["session_val"];
                if (long.TryParse(suid, out long t))
                {
                    User = TrySession(t, sval);
                }
            }
            if (User is not null && Request.Method == "POST" && Request.Form.ContainsKey("mode") && Request.Form["mode"].ToString() == "LOGOUT_NOW")
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
