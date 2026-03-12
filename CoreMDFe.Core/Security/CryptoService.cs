using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CoreMDFe.Core.Security
{
    public static class CryptoService
    {
        // ====================================================================
        // POOR MAN'S OBFUSCATION
        // Quebramos a chave e o vetor de inicialização (IV) em pedaços.
        // Isso impede que ferramentas automáticas achem a chave lendo as strings do .exe
        // ====================================================================
        private static byte[] GetKey()
        {
            string p1 = "C0r3MDF3_";
            string p2 = "S3cur3K3y!";
            string p3 = "2026@#";
            string p4 = "8901237";
            return Encoding.UTF8.GetBytes(p1 + p2 + p3 + p4); // Exatamente 32 bytes (AES-256)
        }

        private static byte[] GetIV()
        {
            string p1 = "C0r3_";
            string p2 = "1n1tV3ct0r!";
            return Encoding.UTF8.GetBytes(p1 + p2); // Exatamente 16 bytes
        }

        private static readonly byte[] Key = GetKey();
        private static readonly byte[] IV = GetIV();

        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText)) return string.Empty;

            using Aes aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
            using (var sw = new StreamWriter(cs))
            {
                sw.Write(plainText);
            }
            return Convert.ToBase64String(ms.ToArray());
        }

        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText)) return string.Empty;

            try
            {
                using Aes aes = Aes.Create();
                aes.Key = Key;
                aes.IV = IV;

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
            catch
            {
                // Fallback de Segurança: Se tentar descriptografar e falhar 
                // (ex: lendo um banco de dados antigo que tinha a senha sem criptografia),
                // ele devolve a própria string em texto limpo para não quebrar o sistema!
                return cipherText;
            }
        }
    }
}