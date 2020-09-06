using ThunderFox.Domain;
using System;
using System.Collections.Generic;
using SQLite;

namespace ThunderFox
{
    class Program
    {
        static void Main(string[] args)
        {
            // Entire project is a port of SharpChrome by harmj0y (https://twitter.com/harmj0y)
            // (https://github.com/GhostPack/SharpDPAPI/tree/master/SharpChrome)
            // Thanks, harmj0y :)
            try
            {
                // Print logo
                Info.ShowLogo();

                // try to parse the command line arguments, show usage on failure and then bail
                var parsed = ArgumentParser.Parse(args);
                if (parsed.ParsedOk == false)
                    Info.ShowUsage();
                else
                {
                    // Try to execute the command using the arguments passed in

                    var commandName = args.Length != 0 ? args[0] : "";

                    var commandFound = new CommandCollection().ExecuteCommand(commandName, parsed.Arguments);

                    // show the usage if no commands were found for the command name
                    if (commandFound == false)
                        Info.ShowUsage();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("\r\n[!] Unhandled ThunderFox exception:\r\n");
                Console.WriteLine(e);
            }
        }
    }
}
