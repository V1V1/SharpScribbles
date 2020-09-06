using System;
using System.Collections.Generic;
using System.IO;

namespace ThunderFox.Commands
{
    public class History : ICommand
    {
        public static string CommandName => "history";

        public void Execute(Dictionary<string, string> arguments)
        {
            Console.WriteLine("\n[*] Command: Firefox history");
            arguments.Remove("history");

            string displayFormat = "csv";

            if (arguments.ContainsKey("/target"))
            {
                string target = arguments["/target"].Trim('"').Trim('\'');
                string historyDB = target + @"\places.sqlite";

                if (File.Exists(historyDB))
                {
                    Console.WriteLine("\n[i] Reading history from '{0}'", historyDB);
                    Firefox.ExtractHistory(historyDB, displayFormat);
                }
                else
                {
                    Console.WriteLine("\r\n[X] '{0}' is not a valid Firefox database.\n", historyDB);
                }
            }
            else if (!arguments.ContainsKey("/target"))
            {
                // Enumerate Firefox installation directories
                // Adapted from SharpWeb (https://github.com/djhohnstein/SharpWeb)
                string userName = Environment.GetEnvironmentVariable("USERNAME");
                Console.WriteLine("\n[*] Checking for Firefox installation as current user ({0})", userName);

                string userFirefoxBasePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
                    + @"\Mozilla\Firefox\Profiles";

                if (System.IO.Directory.Exists(userFirefoxBasePath))
                {
                    string[] directories = Directory.GetDirectories(userFirefoxBasePath);
                    foreach (string directory in directories)
                    {
                        string firefoxhistoryDB = String.Format("{0}\\{1}", directory, "places.sqlite");
                        if (File.Exists(firefoxhistoryDB))
                        {
                            Console.WriteLine("\n[i] Reading history from '{0}'", firefoxhistoryDB);
                            Firefox.ExtractHistory(firefoxhistoryDB, displayFormat);
                        }
                    }
                }
                else if (!System.IO.Directory.Exists(userFirefoxBasePath))
                {
                    Console.WriteLine("\r\n[X] Firefox installation not found.\n");
                }
            }
        }
    }
}