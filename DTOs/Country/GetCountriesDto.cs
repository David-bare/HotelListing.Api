namespace HotelListing.Api.DTOs.Country;

//public record GetCountriesDto
//(
//    int Id,
//    string Name,
//    string ShortName
//);

public class GetCountriesDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string  ShortName { get; set; } = string.Empty;
}

