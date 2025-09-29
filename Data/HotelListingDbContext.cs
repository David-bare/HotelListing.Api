using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Collections.Generic;

namespace HotelListing.Api.Data;

public class HotelListingDbContext(DbContextOptions<HotelListingDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{

    // DbSet is the C# datatype that represents a table 
    public DbSet<Country> Countries { get; set; } // Create a table called Countries based on the Country entity class
    public DbSet<Hotel> Hotels { get; set; }
    public DbSet<ApiKey> ApiKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApiKey>(b =>
        {
            b.HasIndex(k => k.Key).IsUnique();
        });

    }


}



public class HotelListingDbContextFactory : IDesignTimeDbContextFactory<HotelListingDbContext>
{
    public HotelListingDbContext CreateDbContext(string[] args)
    {
        // load configuration (from appsettings.json)
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<HotelListingDbContext>();

        // pull connection string
        var connectionString = configuration.GetConnectionString("HotelListingDbConnectString");
        optionsBuilder.UseSqlServer(connectionString);

        return new HotelListingDbContext(optionsBuilder.Options);
    }
}
