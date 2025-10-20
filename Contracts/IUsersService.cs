using HotelListing.Api.Common.Results;
using HotelListing.Api.DTOs.Auth;

namespace HotelListing.Api.Contracts;

public interface IUsersService
{
    string UserId { get; }

    Task<Result<string>> LoginAsync(LoginDto dto);
    Task<Result<RegisteredUserDto>> RegisterAsync(RegisterUserDto registerUserDto);
}