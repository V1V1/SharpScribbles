using System;
using System.Collections.Generic;
using System.IO;

namespace ThunderFox.Commands
{
    public class Cookies : ICommand
    {
        public static string CommandName => "cookies";

        public void Execute(Dictionary<string, string> arguments)
        {
            Console.WriteLine("\n[*] Command: Firefox cookies");
            arguments.Remove("cookies");

            string displayFormat = "csv";
            string urlRegex = "";        // ID used to specify the email we're reading from Thunderbird

            if (arguments.ContainsKey("/format"))
            {
                displayFormat = arguments["/format"];
            }

            if (arguments.ContainsKey("/url"))
            {
                urlRegex = arguments["/url"];
            }

            if (arguments.ContainsKey("/target"))
            {
                string target = arguments["/target"].Trim('"').Trim('\'');
                string cookiesDB = target + @"\cookies.sqlite";

                if (File.Exists(cookiesDB))
                {
                    Console.WriteLine("\n[i] Reading cookies from '{0}'", cookiesDB);
                    Firefox.ExtractCookies(cookiesDB, displayFormat, urlRegex);
                }
                else
                {
                    Console.WriteLine("\r\n[X] '{0}' is not a valid Firefox database.\n", cookiesDB);
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
                        string firefoxcookiesDB = String.Format("{0}\\{1}", directory, "cookies.sqlite");
                        if (File.Exists(firefoxcookiesDB))
                        {
                            Console.WriteLine("\n[i] Reading cookies from '{0}'", firefoxcookiesDB);
                            Firefox.ExtractCookies(firefoxcookiesDB, displayFormat, urlRegex);
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