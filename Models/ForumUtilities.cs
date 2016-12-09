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

        public static string GetRandomHex(int len = 64)
        {
            byte[] data = new byte[len];
            rng.GetBytes(data);
            return BitConverter.ToString(data).Replace("-", "");
        }
    }
}
