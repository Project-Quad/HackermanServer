using System;

namespace Hackerman_Server
{
    [Serializable]
    public class Configuration
    {
        public string bindAddr { get; set; } = "";
        public int bindPort { get; set; } = 0;
        public string dbHost { get; set; } = "";
        public int dbPort { get; set; } = 0;
        public string dbUser { get; set; } = "";
        public string dbPass { get; set; } = "";
        public string dbName { get; set; } = "";
    }
}
