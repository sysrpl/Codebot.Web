using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Codebot.Web
{
    public static class Security
    {
        private static string secretKey;
        private static readonly Random random;
        private static readonly List<string> roles;

        static Security()
        {
            random = new Random();
            RandomSecretKey(32);
            roles = new List<string>
            {
                "anonymous",
                "admin"
            };
        }

        public static void AddRole(string role)
        {
            roles.Add(role);
        }

        public static IEnumerable<string> Roles
        {
            get
            {
                foreach (var r in roles)
                    yield return r;
            }
        }

        public static string RandomSecretKey(int length)
        {
            const string chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            var secret = new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
            SecretKey(secret);
            return secret;
        }

        public static void SecretKey(string key) => secretKey = key;

        public static string ComputeHash(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            var key = Encoding.UTF8.GetBytes(secretKey);
            using var hmac = new HMACSHA256(key);
            bytes = hmac.ComputeHash(bytes);
            return Convert.ToBase64String(bytes);
        }

        private const string cookieName = "security-credentials";

        public static string ReadUserName(HttpContext context)
        {
            var s = ReadCredentials(context);
            return string.IsNullOrEmpty(s) ? string.Empty : s.Split(':').FirstOrDefault();
        }

        public static string ReadCredentials(HttpContext context)
        {
            var s = context.Request.Cookies[cookieName];
            return string.IsNullOrEmpty(s) ? string.Empty : s;
        }

        public static void WriteCredentials(HttpContext context, IUser user, string salt)
        {
            string s = Credentials(user, salt);
            CookieOptions option = new CookieOptions { Expires = DateTime.Now.AddYears(1) };
            context.Response.Cookies.Append(cookieName, s, option);
        }

        public static void DeleteCredentials(HttpContext context) =>
            context.Response.Cookies.Delete(cookieName);

        public static string Credentials(IUser user, string salt) =>
            user.Name + ":" + ComputeHash(salt + user.Name + user.Hash);

        public static bool Match(IUser user, string salt, string credentials)
        {
            if (!user.Active)
                return false;
            var items = credentials.Split(':');
            if (items.Length != 2)
                return false;
            if (items[0] != user.Name)
                return false;
            return items[1] == ComputeHash(salt + user.Name + user.Hash);
        }
    }
}
