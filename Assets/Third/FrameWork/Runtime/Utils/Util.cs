using System.Security.Cryptography;
using System.Text;

namespace siliu
{
    public static class Util
    {
        public static string MD5 (string str) {
            var data = Encoding.UTF8.GetBytes(str);
            var bytes = new MD5CryptoServiceProvider().ComputeHash(data);
            var md5 = new StringBuilder();
            foreach (var t in bytes)
            {
                md5.Append(t.ToString("x2"));
            }

            return md5.ToString().ToLower();
        }
    }
}