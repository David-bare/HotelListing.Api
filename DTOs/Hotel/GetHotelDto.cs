namespace HotelListing.Api.DTOs.Hotel;

//public record GetHotelDto (
//     int Id,
//    string Name,
//    string Address,
//    double Rating,
//    int CountryId,
//    string Country
//    );


public class GetHotelDto 
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Rating { get; set; } 
    public int CountryId { get; set; } 
    public string Country { get; set; } = string.Empty;
}

public record GetHotelSlimDto (
    int Id,
    string Name,
    string Address,
    double Rating
    );