using System;
using System.Collections.Generic;
using ThunderFox.Commands;

namespace ThunderFox.Domain
{
    // Adapted from SharpChrome (https://github.com/GhostPack/SharpDPAPI/tree/master/SharpChrome)
    public class CommandCollection
    {
        private readonly Dictionary<string, Func<ICommand>> _availableCommands = new Dictionary<string, Func<ICommand>>();

        // How To Add A New Command:
        //  1. Create your command class in the Commands Folder
        //      a. That class must have a CommandName static property that has the Command's name
        //              and must also Implement the ICommand interface
        //      b. Put the code that does the work into the Execute() method
        //  2. Add an entry to the _availableCommands dictionary in the Constructor below.

        public CommandCollection()
        {
            // Thunderbird commands
            _availableCommands.Add(Contacts.CommandName, () => new Contacts());
            _availableCommands.Add(ListMail.CommandName, () => new ListMail());
            _availableCommands.Add(ReadMail.CommandName, () => new ReadMail());

            // Firefox commands
            _availableCommands.Add(History.CommandName, () => new History());
            _availableCommands.Add(Cookies.CommandName, () => new Cookies());

            // Mozilla creds
            _availableCommands.Add(Creds.CommandName, () => new Creds());
        }

        public bool ExecuteCommand(string commandName, Dictionary<string, string> arguments)
        {
            bool commandWasFound;

            if (string.IsNullOrEmpty(commandName) || _availableCommands.ContainsKey(commandName) == false)
                commandWasFound = false;
            else
            {
                // Create the command object 
                var command = _availableCommands[commandName].Invoke();

                // and execute it with the arguments from the command line
                command.Execute(arguments);

                commandWasFound = true;
            }

            return commandWasFound;
        }
    }
}