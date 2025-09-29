using HotelListing.Api.DTOs.Auth;
using HotelListing.Api.Results;

namespace HotelListing.Api.Contracts;

public interface IUsersService
{
    Task<Result<string>> LoginAsync(LoginDto dto);
    Task<Result<RegisteredUserDto>> RegisterAsync(RegisterUserDto registerUserDto);
}