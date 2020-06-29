using Hackerman_Server.DatabaseInterface;

namespace Hackerman_Server.Commands
{
    public class CdCommand : Command
    {
        private HackermanContext _database;
        public CdCommand(HackermanContext database) => _database = database;

        public override string DoCommand(string[] args)
        {
            throw new System.NotImplementedException();
        }
    }
}
