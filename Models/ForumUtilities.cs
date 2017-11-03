using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Html;

namespace FreneticForum.Models
{
    public static class ForumUtilities
    {
        public static HtmlString SECTION_SEPARATOR = new HtmlString("</div><div class=\"section\">");

        public static HtmlString BULLET = new HtmlString("&#9899;");

        public static HtmlString CHECKMARK = new HtmlString("&#10003;");

        public static HtmlString BIG_X = new HtmlString("&#10008;");

        public static bool IsSafePassword(string input)
        {
            if (input.Contains(' ') || input.Contains('\'') || input.Contains('\"'))
            {
                return false;
            }
            if (input.Length < 10)
            {
                return false;
            }
            return true;
        }

        public static string Pad2Z(int input)
        {
            if (input < 10)
            {
                return "0" + input;
            }
            return input.ToString();
        }

        public static string DateNow()
        {
            DateTime dt = DateTime.Now;
            DateTime utc = dt.ToUniversalTime();
            TimeSpan rel = dt.Subtract(utc);
            string timezone = rel.TotalHours < 0 ? rel.TotalHours.ToString(): ("+" + rel.TotalHours);
            return dt.Year + "/" + Pad2Z(dt.Month) + "/" + Pad2Z(dt.Day) + " " + Pad2Z(dt.Hour) + ":" + Pad2Z(dt.Minute) + ":" + Pad2Z(dt.Second) + " UTC" + timezone + ":00";
        }

        public static Encoding Enc = new UTF8Encoding(false);

        public static SHA512 sha = SHA512.Create();

        public static string HashV2(string input, string uname, string salt)
        {
            byte[] dat = GetPbkdf2Bytes("a3550^^&w0fa" + input + "sa!?()ssacnk68" + uname + "as@=+)__2", Enc.GetBytes(salt), 100 * 1000, 64);
            return "v2:" + salt + ":" + BitConverter.ToString(dat).Replace("-", "");
        }

        public static bool SlowEquals(byte[] a, byte[] b)
        {
            uint diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }

        private static byte[] GetPbkdf2Bytes(string password, byte[] salt, int iterations, int outputBytes)
        {
            Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, salt);
            pbkdf2.IterationCount = iterations;
            return pbkdf2.GetBytes(outputBytes);
        }

        public static string Hash(string input, string uname)
        {
            return HashV2(input, uname, GetRandomHex());
        }

        public static RandomNumberGenerator rng = RandomNumberGenerator.Create();
        public static MD5 md5 = MD5.Create();

        public static string GetRandomB64(int len = 32)
        {
            byte[] data = new byte[len];
            rng.GetBytes(data);
            return Convert.ToBase64String(data);
        }

        public static string GetRandomHex(int len = 64)
        {
            byte[] data = new byte[len];
            rng.GetBytes(data);
            return BitConverter.ToString(data).Replace("-", "");
        }
        
        /// <summary>
        /// Validates a username as correctly formatted.
        /// </summary>
        /// <param name="str">The username to validate.</param>
        /// <returns>Whether the username is valid.</returns>
        public static bool ValidateUsername(string str)
        {
            if (str == null)
            {
                return false;
            }
            // Length = 4-15
            if (str.Length < 4 || str.Length > 15)
            {
                return false;
            }
            // Starts A-Z
            if (!(str[0] >= 'a' && str[0] <= 'z') && !(str[0] >= 'A' && str[0] <= 'Z'))
            {
                return false;
            }
            // All symbols are A-Z, 0-9, _
            for (int i = 0; i < str.Length; i++)
            {
                if (!(str[i] >= 'a' && str[i] <= 'z') && !(str[i] >= 'A' && str[i] <= 'Z')
                    && !(str[i] >= '0' && str[i] <= '9') && !(str[i] == '_'))
                {
                    return false;
                }
            }
            // Valid if all tests above passed
            return true;
        }
    }
}
