using System.Collections.Generic;
using Hackerman_Server.Commands;
using Hackerman_Server.DatabaseInterface;
using static Hackerman_Server.Networking;

namespace Hackerman_Server
{
    public class CommandHandler
    {
        private Dictionary<string, Command> _commandList = new Dictionary<string, Command>();
        private HackermanContext _database;

        public CommandHandler(HackermanContext database)
        {
            _database = database;
            _commandList.Add("cd", new CdCommand(database));
        }
        
        public void ProcessCommand(UserConnection conn, string command)
        {
            string[] commandArgs = command.Split(' ');
            string returnMsg = _commandList[commandArgs[0]].DoCommand(commandArgs);

            Send(conn, returnMsg);
        }
    }
}
