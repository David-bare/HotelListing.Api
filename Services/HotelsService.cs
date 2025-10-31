using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Models.Extensions;
using HotelListing.Api.Common.Models.Filtering;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Contracts;
using HotelListing.Api.Domain;
using HotelListing.Api.DTOs.Hotel;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Services
{
    public class HotelsService(HotelListingDbContext context, ICountriesService countriesService, IMapper mapper) : IHotelsService
    {
        public async Task<Result<PagedResult<GetHotelDto>>> GetHotelsAsync(PaginationParameters paginationParameters, HotelFilterParameters filters)
        {
            // SELECT * FROM Hotels LEFT JOIN Countries ON Hotels.CountryId = Countries.CountryId
            var query = context.Hotels.AsQueryable();
            if (filters.CountryId.HasValue)
            {
                query = query.Where(q => q.CountryId == filters.CountryId);
            }

            if (filters.MinRating.HasValue)
                query = query.Where(h => h.Rating >= filters.MinRating.Value);

            if (filters.MaxRating.HasValue)
                query = query.Where(h => h.Rating <= filters.MaxRating.Value);

            if (filters.MinPrice.HasValue)
                query = query.Where(h => h.PerNightRate >= filters.MinPrice.Value);

            if (filters.MaxPrice.HasValue)
                query = query.Where(h => h.PerNightRate <= filters.MaxPrice.Value);

            if (!string.IsNullOrWhiteSpace(filters.Location))
                query = query.Where(h => h.Address.Contains(filters.Location));

            // generic search param
            if (!string.IsNullOrWhiteSpace(filters.Search))
                query = query.Where(h => h.Name.Contains(filters.Search) || 
                                    h.Address.Contains(filters.Search)    
                                    );

            query = filters.SortBy?.ToLower() switch
            {
                "name" => filters.SortDescending ?
                    query.OrderByDescending(h => h.Name) : query.OrderBy(h => h.Name),
                "rating" => filters.SortDescending ?
                    query.OrderByDescending(h => h.Rating) : query.OrderBy(h => h.Rating),
                "price" => filters.SortDescending ?
                    query.OrderByDescending(h => h.PerNightRate) : query.OrderBy(h => h.PerNightRate),
                _ => query.OrderBy(h => h.Name)
            };

            var hotels = await query
                .Include(q => q.Country)
                .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
                .ToPagedResultAsync(paginationParameters);

            return Result<PagedResult<GetHotelDto>>.Success(hotels);
        }

        public async Task<Result<GetHotelDto?>> GetHotelAsync(int id)
        {
            // SELECT * FROM Hotels LEFT JOIN Countries ON Hotels.CountryId = Countries.CountryId WHERE Hotels.Id = @Id
            var hotel = await context.Hotels
                .Where(h => h.Id == id)
                .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
                .FirstOrDefaultAsync();

            if (hotel is null)
            {
                return Result<GetHotelDto>.Failure(new Error(ErrorCodes.NotFound, $"Hotel '{id}' was not found."));
            }

            return Result<GetHotelDto>.Success(hotel);
        }

        public async Task<Result> UpdateHotelAsync(int id, UpdateHotelDto updateDto)
        {
            if (id != updateDto.Id)
            {
                return Result.BadRequest(new Error(ErrorCodes.Validation, "Id route value does not match payload Id."));
            }

            var hotel = await context.Hotels.FindAsync(id);

            if (hotel is null)
            {
                return Result.NotFound(new Error(ErrorCodes.NotFound, $"Hotel '{id}' was not found."));
            }

            var countryExists = await context.Countries.AnyAsync(c => c.CountryId == updateDto.CountryId);
            if (!countryExists)
            {
                return Result.NotFound(new Error(ErrorCodes.NotFound, $"Country '{updateDto.CountryId}' was not found."));
            }




            hotel.Name = updateDto.Name;
            hotel.Address = updateDto.Address;
            hotel.Rating = updateDto.Rating;
            hotel.CountryId = updateDto.CountryId;

            context.Hotels.Update(hotel);
            await context.SaveChangesAsync();

            return Result.Success();


        }

        public async Task<Result<GetHotelDto>> CreateHotelAsync(CreateHotelDto hotelDto)
        {
            var countryExists = await countriesService.CountryExistsAsync(hotelDto.CountryId);

            if (!countryExists)
            {
                return Result<GetHotelDto>.Failure(new Error(ErrorCodes.NotFound, $"Country '{hotelDto.CountryId}'" +
                    $"was not found."));
            }

            var duplicate = await HotelExistsAsync(hotelDto.Name, hotelDto.CountryId);
            if (duplicate)
            {
                return Result<GetHotelDto>.Failure(new Error(ErrorCodes.Conflict, $"Hotel '{hotelDto.Name}' already exists in the selected country."));
            }

            var hotel = mapper.Map<Hotel>(hotelDto);

            context.Hotels.Add(hotel);
            await context.SaveChangesAsync();

            var dto = await context.Hotels
                .Where(h => h.Id == hotel.Id)
                .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
                .FirstAsync();

            return Result<GetHotelDto>.Success(dto);
        }

        private async Task<bool> HotelExistsAsync(string name, int id)
        {
            return await context.Hotels.AnyAsync(e => e.Id == id && e.Country.Name == name);
        }

        public async Task DeleteHotelAsync(int id)
        {
            var hotel = await context.Hotels
                .Where(q => q.Id == id)
                .ExecuteDeleteAsync();
        }

        public async Task<bool> HotelExistsAsync(int id)
        {
            return await context.Hotels.AnyAsync(e => e.Id == id);
        }

        public async Task<bool> HotelExistsAsync(string name)
        {
            return await context.Hotels.AnyAsync(e => e.Name == name);
        }

        //Task<GetHotelDto> IHotelsService.CreateHotelAsync(CreateHotelDto createDto)
        //{
        //    throw new NotImplementedException();
        //}

        //Task<IEnumerable<GetHotelDto>> IHotelsService.GetHotelsAsync()
        //{
        //    throw new NotImplementedException();
        //}


        //Task<GetHotelDto?> IHotelsService.GetHotelAsync(int id)
        //{
        //    throw new NotImplementedException();
        //}

        //Task IHotelsService.UpdateHotelAsync(int id, UpdateHotelDto updateDto)
        //{
        //    throw new NotImplementedException();
        //}
    }

}
