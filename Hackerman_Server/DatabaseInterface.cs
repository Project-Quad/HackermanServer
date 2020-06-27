using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace Hackerman_Server.DatabaseInterface
{
    public class HackermanContext : DbContext
    {
        public DbSet<Computer> Computers { get; set; }
        public DbSet<Player> Players { get; set; }

        public HackermanContext(DbContextOptions options) : base(options) {}
        
        public static void ConfigureServices(IServiceCollection services, Configuration config)
        {
            services.AddDbContextPool<HackermanContext>(options => options
                .UseMySql($"Server={config.dbHost};Port={config.dbPort};Database={config.dbName};Username={config.dbUser};Password={config.dbPass}", mySqlOptions => mySqlOptions
                    .ServerVersion(new Version(10,4,8), ServerType.MySql)
                ));
        }
    }

    public class Computer
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string IP { get; set; }
    }
    public class Player
    {
        public int ID { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int HomeComp { get; set; }
        public bool Admin { get; set; }
    }
}