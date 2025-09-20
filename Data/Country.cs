using System.ComponentModel.DataAnnotations;

namespace HotelListing.Api.Data;

public class Country // one to many hotels, hence the reason for a list as the navigation property
{
    [Key]
    public int CountryId { get; set; }
    public required string  Name { get; set; }
    public required string ShortName { get; set; }
    public IList<Hotel> Hotels { get; set; } = []; // navigation property
}