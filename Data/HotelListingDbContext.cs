using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace HotelListing.Api.Data
{
    public class HotelListingDbContext : DbContext
    {
        public HotelListingDbContext(DbContextOptions<HotelListingDbContext> options) : base(options)
        {
            
        }

        // DbSet is the C# datatype that represents a table 
        public DbSet<Country> Countries { get; set; } // Create a table called Countries based on the Country entity class
        public DbSet<Hotel> Hotels { get; set; }

    }
}
