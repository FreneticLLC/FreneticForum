using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Security.Cryptography;

namespace TestForum.Models
{
    public static class ForumUtilities
    {
        public static Encoding Enc = new UTF8Encoding(false);

        public static SHA512 sha = SHA512.Create();

        public static string HashV2(string input, string uname, string salt)
        {
            byte[] dat = GetPbkdf2Bytes("a3550^^&w0fa" + input + "sa!?()ssacnk68" + uname + "as@=+)__2", Enc.GetBytes(salt), 100 * 1000, 64);
            return "v2:" + salt + ":" + BitConverter.ToString(dat).Replace("-", "");
        }

        public static bool SlowEquals(byte[] a, byte[] b)
        {
            var diff = (uint)a.Length ^ (uint)b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }

        private static byte[] GetPbkdf2Bytes(string password, byte[] salt, int iterations, int outputBytes)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, salt);
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
