using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Models.Extensions;
using HotelListing.Api.Common.Models.Filtering;
using HotelListing.Api.Common.Models.Paging;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Contracts;
using HotelListing.Api.Domain;
using HotelListing.Api.DTOs.Country;
using HotelListing.Api.DTOs.Hotel;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.EntityFrameworkCore;


namespace HotelListing.Api.Services;

public class CountriesService(HotelListingDbContext context, IMapper mapper) : ICountriesService
{
    public async Task<Result<IEnumerable<GetCountriesDto>>> GetCountriesAsync(CountryFilterParameters filters)
    {
        var query = context.Countries.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim();
            query = query.Where(c => EF.Functions.Like(c.Name, $"%{term}%")
                || EF.Functions.Like(c.ShortName, $"%{term}%"));
        }


        var countries = await query
            .ProjectTo<GetCountriesDto>(mapper.ConfigurationProvider)
            .ToListAsync();

        return Result<IEnumerable<GetCountriesDto>>.Success(countries);
    }

    public async Task<Result<GetCountryDto>> GetCountryAsync(int id)
    {
        var country = await context.Countries
            .Where(q => q.CountryId == id)
            .ProjectTo<GetCountryDto>(mapper.ConfigurationProvider)
            .FirstOrDefaultAsync();

        return country is null
            ? Result<GetCountryDto>.Failure(new Error(ErrorCodes.NotFound, $"Country '{id}' was not found."))
            : Result<GetCountryDto>.Success(country);
    }

    public async Task<Result<GetCountryHotelDto>> GetCountryHotelsAsync(int countryId, PaginationParameters paginationParameters, CountryFilterParameters filters)
    {
        var exists = await CountryExistsAsync(countryId);
        if (!exists)
        {
            return Result< GetCountryHotelDto>.Failure(
                new Error(ErrorCodes.NotFound, $"Country '{countryId}' was not found."));
        }

        var countryName = await context.Countries
            .Where(q => q.CountryId == countryId)
            .Select(q => q.Name)
            .SingleAsync();

        var hotelsQuery = context.Hotels
            .Where(h => h.CountryId == countryId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var term = filters.Search.Trim();
            hotelsQuery = hotelsQuery.Where(h => EF.Functions.Like(h.Name, $"${term}"));
        }

        hotelsQuery = (filters.SortBy?.Trim().ToLowerInvariant()) switch
        {
            "name" => filters.SortDescending ? hotelsQuery.OrderByDescending(h => h.Name) :
                hotelsQuery.OrderBy(h => h.Name),
            "rating" => filters.SortDescending ? hotelsQuery.OrderByDescending(h => h.Rating) :
                hotelsQuery.OrderBy(h => h.Rating),
            _ => hotelsQuery.OrderBy(h => h.Name)
        };

        //var pagedHotels = await hotelsQuery
        //    .ProjectTo<GetHotelSlimDto>(mapper.ConfigurationProvider)
        //    .ToPagedResultAsync(paginationParameters);

        var pagedHotels = await hotelsQuery
        .ProjectTo<HotelListing.Api.DTOs.Hotel.GetHotelSlimDto>(mapper.ConfigurationProvider)
        .ToPagedResultAsync(paginationParameters);




        var result = new GetCountryHotelDto
        {
            Id = countryId,
            Name = countryName
            
        };

        // Hotels = pagedHotels

        return Result<GetCountryHotelDto>.Success(result);
    }

    public async Task<Result> UpdateCountryAsync(int id, UpdateCountryDto updateDto)
    {
        try
        {
            if (id != updateDto.Id)
            {
                return Result.BadRequest(new Error("Validation", "Id route value does not match payload Id."));
            }


            var country = await context.Countries.FindAsync(id);

            if (country is null)
            {
                return Result.NotFound(new Error("NotFound", $"Country '{id}' was not found."));
            }



            country.Name = updateDto.Name;
            country.ShortName = updateDto.ShortName;
            context.Countries.Update(country);
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure();
        }



    }

    public async Task<Result<GetCountryDto>> CreateCountryAsync(CreateCountryDto createDto)
    {
        try
        {
            var exists = await CountryExistsAsync(createDto.Name);
            if (exists)
            {
                return Result<GetCountryDto>.Failure(new Error("Conflict", $"Country with name '{createDto.Name}' already exists."));
            }


            var country = mapper.Map<Country>(createDto);

            context.Countries.Add(country);
            await context.SaveChangesAsync();

            var dto = await context.Countries
                .Where(c => c.CountryId == country.CountryId)
                .ProjectTo<GetCountryDto>(mapper.ConfigurationProvider)
                .FirstAsync();

            return Result<GetCountryDto>.Success(dto);
        }
        catch (Exception)
        {
            return Result<GetCountryDto>.Failure(new Error(ErrorCodes.Failure, "An unexpected error occurred" +
                "while creating the country."));
        }
    }

    public async Task<Result> DeleteCountryAsync(int id)
    {
        try
        {
            var country = await context.Countries.FindAsync(id);
            if (country is null)
            {
                return Result.Failure(new Error("NotFound", $"Country '{id}' was not found."));
            }

            context.Countries.Remove(country);
            await context.SaveChangesAsync();

            return Result.Success();
        }
        catch
        {
            return Result.Failure();
        }



    }

    public async Task<bool> CountryExistsAsync(int id)
    {
        return await context.Countries.AnyAsync(e => e.CountryId == id);
    }

    public async Task<bool> CountryExistsAsync(string name)
    {
        return await context.Countries.AnyAsync(e => e.Name == name);
    }

    public async Task<Result> PatchCountryAsync(int id, JsonPatchDocument<UpdateCountryDto> patchDoc)
    {
        var country = await context.Countries.FindAsync(id);
        if (country is null)
        {
            return Result.NotFound(new Error(ErrorCodes.NotFound, $"Country '{id}' was not found."));
        }

        var countryDto = mapper.Map<UpdateCountryDto>(country);
        patchDoc.ApplyTo(countryDto);

        if (countryDto.Id != id)
        {
            return Result.BadRequest(new Error(ErrorCodes.Validation, "Cannot modify the Id field."));
        }

        var duplicateExists = await context.Countries.AnyAsync(
            c => c.Name.ToLower().Trim() == countryDto.Name.ToLower().Trim()
            && c.CountryId != id);

        if (duplicateExists) 
        {
            return Result.Failure(new Error(ErrorCodes.Conflict,
                $"Country with name '{countryDto.Name}' already exists."));
        }

        mapper.Map(countryDto, country);
        await context.SaveChangesAsync();

        return Result.Success();

    }
}
