using HotelListing.Api.DTOs.Hotel;
using HotelListing.Api.Results;

namespace HotelListing.Api.Contracts
{
    public interface IHotelsService
    {
        Task<bool> HotelExistsAsync(int id);
        Task<bool> HotelExistsAsync(string name);
        Task DeleteHotelAsync(int id);
       
        Task<Result<IEnumerable<GetHotelDto>>> GetHotelsAsync();
        Task<Result<GetHotelDto?>> GetHotelAsync(int id);
        Task<Result> UpdateHotelAsync(int id, UpdateHotelDto updateDto);
        Task<Result<GetHotelDto>> CreateHotelAsync(CreateHotelDto hotelDto);
    }
}