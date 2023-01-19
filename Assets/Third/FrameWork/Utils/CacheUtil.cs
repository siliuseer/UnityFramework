using System.IO;
using UnityEngine;

namespace siliu
{
    public static class CacheUtil
    {
        private static readonly string Dir = Application.persistentDataPath + "/LocalData/";
        private const string Key = "x0oEPuG3dwUIZzYr5u8sRmBrfZY34hMtr00k4pGF9c6UtseqO53ogzaAwOHP8yh3j5u66XI2L8RR";

        public static void Write(string name, byte[] data)
        {
            if (!Directory.Exists(Dir))
            {
                Directory.CreateDirectory(Dir);
            }

            var encrypt = XXTEA.Encrypt(data, Key);
            File.WriteAllBytes(Dir + name, encrypt);
        }

        public static byte[] Read(string name)
        {
            var path = Dir + name;
            if (!File.Exists(path))
            {
                return null;
            }

            var bytes = File.ReadAllBytes(path);
            var decrypt = XXTEA.Decrypt(bytes, Key);
            return decrypt;
        }
    }
}