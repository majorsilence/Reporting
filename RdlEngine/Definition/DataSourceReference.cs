

using System;
using System.Xml;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Majorsilence.Reporting.Rdl
{
    ///<summary>
    /// Utitlity class for reading and writing a DataSourceReference file
    ///</summary>
    public sealed class DataSourceReference
    {
        const int IV_SIZE = 16;
        const int KEY_SIZE = 32;
        /// <summary>
        /// Create the named file containing the encrypted input data
        /// </summary>
        /// <param name="filename">Output file name</param>
        /// <param name="indata">Input data to place in file encrypted.</param>
        /// <param name="pswd">Password phrase to use when encrypting</param>
        static public void Create(string filename, string indata, string pswd)
        {
            // create the salt
            byte[] salt = new byte[IV_SIZE];
            RandomNumberGenerator.Fill(salt);

            // create the key from the password phrase
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(pswd, salt, 10000, HashAlgorithmName.SHA256, KEY_SIZE);

            // Create an instance of the Aes class
            using (Aes aes = Aes.Create())
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(key, salt), CryptoStreamMode.Write))
            {
                byte[] ta = Encoding.UTF8.GetBytes(indata);
                cs.Write(ta, 0, ta.Length);
                cs.Close();

                using (FileStream fs = File.OpenWrite(filename))
                {
                    // Write the salt to the beginning of the file
                    fs.Write(salt, 0, salt.Length);
                    // Write the encrypted data
                    byte[] ba = ms.ToArray();
                    fs.Write(ba, 0, ba.Length);
                }
            }
        }

        /// <summary>
        /// Retrieve the string data from an encrypted file.   Retrieve assumes 
        /// that Create was used to create the file.
        /// </summary>
        /// <param name="filename">File name to retrieve encrypted data from.</param>
        /// <param name="pswd">Password phrase used when data was encrypted.</param>
        /// <returns>Unencrypted string contents of file</returns>
        static public string Retrieve(string filename, string pswd)
        {
            byte[] salt = new byte[IV_SIZE];
            byte[] enc;

            using (FileStream fs = File.OpenRead(filename))
            {
                fs.ReadExactly(salt, 0, salt.Length);
                enc = new byte[fs.Length - salt.Length];
                fs.ReadExactly(enc, 0, enc.Length);
            }

            // create the key from the password phrase
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(pswd, salt, 10000, HashAlgorithmName.SHA256, KEY_SIZE);

            // Create an instance of the Aes class
            using (Aes aes = Aes.Create())
            using (MemoryStream ms = new MemoryStream(enc))
            using (CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(key, salt), CryptoStreamMode.Read))
            using (MemoryStream ms2 = new MemoryStream())
            {
                byte[] ta = new byte[256];
                int count;
                while ((count = cs.Read(ta, 0, ta.Length)) > 0)
                {
                    ms2.Write(ta, 0, count);
                }
                return Encoding.UTF8.GetString(ms2.ToArray());
            }
        }
    }
}
