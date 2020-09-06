using System.Collections.Generic;

namespace ThunderFox.Commands
{
    public interface ICommand
    {
        void Execute(Dictionary<string, string> arguments);
    }
}