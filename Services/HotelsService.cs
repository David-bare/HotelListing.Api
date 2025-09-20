using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.Api.Constants;
using HotelListing.Api.Contracts;
using HotelListing.Api.Data;
using HotelListing.Api.DTOs.Country;
using HotelListing.Api.DTOs.Hotel;
using HotelListing.Api.Results;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Services
{
    public class HotelsService(HotelListingDbContext context, ICountriesService countriesService, IMapper mapper) : IHotelsService
    {
        public async Task<Result<IEnumerable<GetHotelDto>>> GetHotelsAsync()
        {
            // SELECT * FROM Hotels LEFT JOIN Countries ON Hotels.CountryId = Countries.CountryId
            var hotels = await context.Hotels
                .ProjectTo<GetHotelDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            return Result<IEnumerable<GetHotelDto>>.Success(hotels);
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
