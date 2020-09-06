using System;
using System.Collections.Generic;
using System.IO;

namespace ThunderFox.Commands
{
    public class Contacts : ICommand
    {
        public static string CommandName => "contacts";

        public void Execute(Dictionary<string, string> arguments)
        {
            Console.WriteLine("\n[*] Command: Thunderbird Contacts");
            arguments.Remove("contacts");

            string displayFormat = "csv";

            if (arguments.ContainsKey("/target"))
            {
                string target = arguments["/target"].Trim('"').Trim('\'');
                string contactsDB = target + @"\global-messages-db.sqlite";

                if (File.Exists(contactsDB))
                {
                    Console.WriteLine("\n[i] Reading contacts from '{0}'", contactsDB);
                    Thunderbird.ExtractContacts(contactsDB, displayFormat);
                }
                else
                {
                    Console.WriteLine("\r\n[X] '{0}' is not a valid Thunderbird database.\n", contactsDB);
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
                        string thunderContactsDB = String.Format("{0}\\{1}", directory, "global-messages-db.sqlite");
                        if (File.Exists(thunderContactsDB))
                        {
                            Console.WriteLine("\n[i] Reading contacts from '{0}'", thunderContactsDB);
                            Thunderbird.ExtractContacts(thunderContactsDB, displayFormat);
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