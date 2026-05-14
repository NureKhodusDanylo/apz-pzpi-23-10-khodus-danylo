using Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System.IO;

public class MyDbContextFactory : IDesignTimeDbContextFactory<MyDbContext>
{
    public MyDbContext CreateDbContext(string[] args)
    {
        string basePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "../RobDeliveryAPI"));

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddUserSecrets("d59dff06-6a26-4e35-be1d-771f4cf795d2")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<MyDbContext>();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException("Could not find a connection string named 'DefaultConnection'. Check your user secrets and appsettings.json.");
        }

        optionsBuilder.UseSqlite(connectionString);

        return new MyDbContext(optionsBuilder.Options);
    }
}