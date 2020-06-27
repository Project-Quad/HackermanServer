using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Hackerman_Server.DatabaseInterface;
using System.Runtime.InteropServices.ComTypes;

namespace Hackerman_Server
{
    class Program
    {
        private const string _fileName = @"config.json";

        static void Main(string[] args)
        {
            Configuration config;
            if (!File.Exists(_fileName))
                File.WriteAllText(_fileName,
                    JsonSerializer.Serialize(new Configuration(),
                        options: new JsonSerializerOptions() {WriteIndented = true}));
            config = JsonSerializer.Deserialize<Configuration>(File.ReadAllText(_fileName));
            Console.WriteLine($"PogServer V1 Starting on {config.bindAddr}:{config.bindPort}");
            var serviceCollection = new ServiceCollection();
            HackermanContext.ConfigureServices(serviceCollection, config);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            var database = serviceProvider.GetRequiredService<DatabaseInterface.HackermanContext>();
            foreach (var com in database.Computers)
                Console.WriteLine($"{com.ID} {com.Name} {com.IP}");
            foreach (var plr in database.Players)
                Console.WriteLine($"{plr.ID} {plr.Username} {plr.Password} {plr.HomeComp} {plr.Admin}");
            var net = new Networking(config, database);
            net.StartServer();
            //database.SaveChanges();
            Console.ReadLine();
        }
    }
}
