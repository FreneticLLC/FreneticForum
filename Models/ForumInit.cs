using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace TestForum.Models
{
    public class ForumInit
    {
        // -------------- EDIT BELOW ---------------- //
        public const string CONFIG_FILE_LOCATION = "C:/testforum/testforum.cfg";
        // -------------- EDIT ABOVE ---------------- //

        public static void SaveNewConfig(string dbpath, string dbname)
        {
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

        public ForumInit()
        {
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
