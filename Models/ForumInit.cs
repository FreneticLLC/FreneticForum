using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;

namespace FreneticForum.Models
{
    public class ForumInit
    {
        // ----------------------------- EDIT BELOW ------------------------ //
        public const string CONFIG_FILE_FOLDER_LOCATION = "./config/";
        // ----------------------------- EDIT ABOVE ------------------------- //

        public const string CONFIG_FILE_LOCATION = CONFIG_FILE_FOLDER_LOCATION + "FreneticForum.cfg";

        public static void SaveNewConfig(string dbpath, string dbname)
        {
            Directory.CreateDirectory(CONFIG_FILE_FOLDER_LOCATION);
            File.WriteAllText(CONFIG_FILE_LOCATION, "database_path: " + dbpath + "\ndatabase_db: " + dbname + "\n");
        }

        public static void SaveTestConfig()
        {
            Directory.CreateDirectory(CONFIG_FILE_FOLDER_LOCATION);
            File.WriteAllText(CONFIG_FILE_LOCATION, "\n");
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
            Dictionary<string, string> toret = new Dictionary<string, string>();
            foreach (string str in data)
            {
                string fstr = str.Trim();
                if (fstr.StartsWith("#"))
                {
                    continue;
                }
                string[] dat = fstr.Split(new char[] { ':' }, 2);
                if (dat.Length == 2)
                {
                    toret[dat[0].ToLowerInvariant()] = dat[1].Trim();
                }
            }
            return toret;
        }

        public HttpRequest Request;

        public Account User = null;

        public ForumInit(HttpRequest htr)
        {
            Request = htr;
            Config = GetConfig();
            if (Config == null || Config.Count == 0)
            {
                throw new InitFailedException();
            }
            else
            {
                Database = new ForumDatabase(Config["database_path"], Config["database_db"]);
            }
        }
    }
}
