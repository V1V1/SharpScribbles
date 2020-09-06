using System;
using System.Collections.Generic;
using System.IO;

namespace ThunderFox.Commands
{
    public class ReadMail : ICommand
    {
        public static string CommandName => "readmail";

        public void Execute(Dictionary<string, string> arguments)
        {
            Console.WriteLine("\n[*] Command: Thunderbird Read Mail");
            arguments.Remove("readmail");

            string displayFormat = "table";
            string emailID = "";        // ID used to specify the email we're reading from Thunderbird

            if (arguments.ContainsKey("/format"))
            {
                displayFormat = arguments["/format"];
            }

            if (arguments.ContainsKey("/id"))
            {
                emailID = arguments["/id"];
            }

            if (arguments.ContainsKey("/target"))
            {
                string target = arguments["/target"].Trim('"').Trim('\'');
                string mailDB = target + @"\global-messages-db.sqlite";

                if (File.Exists(mailDB))
                {
                    Console.WriteLine("\n[i] Reading mail from '{0}'", mailDB);
                    Thunderbird.ExtractSingleMail(mailDB, displayFormat, emailID);
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
                            Console.WriteLine("\n[i] Reading mail from '{0}'", thundermailDB);
                            Thunderbird.ExtractSingleMail(thundermailDB, displayFormat, emailID);
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