using HotelListing.Api.Common.Models.Filtering;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Results;
using HotelListing.Api.DTOs.Hotel;

namespace HotelListing.Api.Contracts
{
    public interface IHotelsService
    {
        Task<bool> HotelExistsAsync(int id);
        Task<bool> HotelExistsAsync(string name);
        Task DeleteHotelAsync(int id);

        Task<Result<PagedResult<GetHotelDto>>> GetHotelsAsync(PaginationParameters paginationParameters, HotelFilterParameters filters);
        Task<Result<GetHotelDto?>> GetHotelAsync(int id);
        Task<Result> UpdateHotelAsync(int id, UpdateHotelDto updateDto);
        Task<Result<GetHotelDto>> CreateHotelAsync(CreateHotelDto hotelDto);
    }
}