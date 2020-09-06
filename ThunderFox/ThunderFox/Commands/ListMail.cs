using System;
using System.Collections.Generic;
using System.IO;

namespace ThunderFox.Commands
{
    public class ListMail : ICommand
    {
        public static string CommandName => "listmail";

        public void Execute(Dictionary<string, string> arguments)
        {
            Console.WriteLine("\n[*] Command: Thunderbird List Mail");
            arguments.Remove("listmail");

            string displayFormat = "csv";
            string bodyRegex = "";        // regex to search for through email body

            if (arguments.ContainsKey("/format"))
            {
                displayFormat = arguments["/format"];
            }

            if (arguments.ContainsKey("/search"))
            {
                bodyRegex = arguments["/search"];
            }

            if (arguments.ContainsKey("/target"))
            {
                string target = arguments["/target"].Trim('"').Trim('\'');
                string mailDB = target + @"\global-messages-db.sqlite";

                if (File.Exists(mailDB))
                {
                    Console.WriteLine("\n[i] Listing mail from '{0}'", mailDB);
                    Thunderbird.ExtractMailList(mailDB, displayFormat, bodyRegex);
                }
                else
                {
                    Console.WriteLine("\r\n[X] '{0}' is not a valid Thunderbird database.\n", mailDB);
                }
            }
            else if (!arguments.ContainsKey("/target"))
            {
                // Enumerate Thunderbird installation directories
                // Adapted from SharpWeb (https://github.com/djhohnstein/SharpWeb)
                string userName = Environment.GetEnvironmentVariable("USERNAME");
                Console.WriteLine("\n[*] Checking for Thunderbird installation as current user ({0})", userName);
                
                string userThunderbirdBasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    + @"\Thunderbird\Profiles";

                if (System.IO.Directory.Exists(userThunderbirdBasePath))
                {
                    string[] directories = Directory.GetDirectories(userThunderbirdBasePath);
                    foreach (string directory in directories)
                    {
                        string thundermailDB = String.Format("{0}\\{1}", directory, "global-messages-db.sqlite");
                        if (File.Exists(thundermailDB))
                        {
                            Console.WriteLine("\n[i] Listing mail from '{0}'", thundermailDB);
                            Thunderbird.ExtractMailList(thundermailDB, displayFormat, bodyRegex);
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