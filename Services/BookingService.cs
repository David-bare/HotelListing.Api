using AutoMapper;
using AutoMapper.QueryableExtensions;
using HotelListing.Api.Common.Constants;
using HotelListing.Api.Common.Results;
using HotelListing.Api.Contracts;
using HotelListing.Api.Domain;
using HotelListing.Api.Domain.Enums;
using HotelListing.Api.DTOs.Booking;
using Microsoft.EntityFrameworkCore;

namespace HotelListing.Api.Services
{
    public class BookingService(HotelListingDbContext context, IUsersService usersService, IMapper mapper) : IBookingService
    {
        public async Task<Result<IEnumerable<GetBookingDto>>> GetBookingForHotelAsync(int hotelId)
        {
            var hotelExists = await context.Hotels.AnyAsync(h => h.Id == hotelId);
            if (!hotelExists)
                return Result<IEnumerable<GetBookingDto>>.Failure(new Error(ErrorCodes.NotFound, $"Hotel '{hotelId}' was not found."));

            var bookings = await context.Bookings
                .Where(b => b.HotelId == hotelId)
                .OrderBy(b => b.CheckIn)
                .ProjectTo<GetBookingDto>(mapper.ConfigurationProvider)
                .ToListAsync();

            //.Select(b => new GetBookingDto
            //(
            //    b.Id,
            //    b.HotelId,
            //    b.Hotel!.Name,
            //    b.CheckIn,
            //    b.CheckOut,
            //    b.Guests,
            //    b.TotalPrice,
            //    b.Status.ToString(),
            //    b.CreatedAtUtc,
            //    b.UpdatedAtUtc
            //))

            return Result<IEnumerable<GetBookingDto>>.Success(bookings);
        }

        public async Task<Result<GetBookingDto>> CreateBookingAsync(CreateBookingDto dto)
        {
            var userId = usersService.UserId;


            bool overlaps = await IsOverlap(dto.HotelId, userId, dto.CheckIn, dto.CheckOut);


            if (overlaps)
                return Result<GetBookingDto>.Failure(new Error(ErrorCodes.Conflict, "The selected dates overlap with an existing booking."));

            if (dto.Guests <= 0)
                return Result<GetBookingDto>.Failure(new Error(ErrorCodes.Validation, "Guests must be at least 1"));

            var hotel = await context.Hotels
                .Where(h => h.Id == dto.HotelId)
                .FirstOrDefaultAsync();

            if (hotel is null)
                return Result<GetBookingDto>.Failure(new Error(ErrorCodes.NotFound, $"Hotel '{dto.HotelId}' was not found."));


            var nights = dto.CheckOut.DayNumber - dto.CheckIn.DayNumber;
            var totalPrice = hotel.PerNightRate * nights;
            //var booking = new Booking
            //{
            //    HotelId = dto.HotelId,
            //    UserId = userId,
            //    CheckIn = dto.CheckIn,
            //    CheckOut = dto.CheckOut,
            //    Guests = dto.Guests,
            //    TotalPrice = totalPrice,
            //    Status = BookingStatus.Pending
            //};

            var booking = mapper.Map<Booking>(dto);
            booking.UserId = userId;

            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            //var created = new GetBookingDto(
            //    booking.Id,
            //    hotel.Id,
            //    hotel.Name,
            //    dto.CheckIn,
            //    dto.CheckOut,
            //    dto.Guests,
            //    totalPrice,
            //    BookingStatus.Pending.ToString(),
            //    booking.CreatedAtUtc,
            //    booking.UpdatedAtUtc
            //    );

            var result = mapper.Map<GetBookingDto>(booking);

            return Result<GetBookingDto>.Success(result);

        }

        public async Task<Result<GetBookingDto>> UpdateBookingAsync(int hotelId, int bookingId, UpdateBookingDto dto)
        {
            var userId = usersService.UserId;

            //if (string.IsNullOrWhiteSpace(userId))
            //    return Result<GetBookingDto>.Failure(new Error(ErrorCodes.Validation, "User is required."));

            //if (nights <= 0)
            //    return Result<GetBookingDto>.Failure(new Error(ErrorCodes.Validation, "Check-out must be after check-in."));

            if (dto.Guests <= 0)
                return Result<GetBookingDto>.Failure(new Error(ErrorCodes.Validation, "Guests must be at least 1"));

            var overlaps = await context.Bookings.AnyAsync(
               b => b.HotelId == hotelId
               && b.Status != BookingStatus.Cancelled
               && dto.CheckIn < b.CheckOut
               && dto.CheckIn > b.CheckIn
               && b.UserId == userId
               );

            if (overlaps)
                return Result<GetBookingDto>.Failure(new Error(ErrorCodes.Conflict, "The selected dates overlap with an existing booking."));

            var booking = await context.Bookings
                .Include(b => b.Hotel)
                .FirstOrDefaultAsync(b =>
                    b.Id == bookingId
                    && b.HotelId == hotelId
                    && b.UserId == userId);

            if (booking is null)
                return Result<GetBookingDto>.Failure(new Error(ErrorCodes.NotFound, $"Booking '{bookingId}' was not found."));

            if (booking.Status == BookingStatus.Cancelled)
                return Result<GetBookingDto>.Failure(new Error(ErrorCodes.Conflict, "Cancelled bookings cannot be modified."));

            mapper.Map(dto, booking);
            var perNight = booking.Hotel!.PerNightRate;
            var nights = dto.CheckOut.DayNumber - dto.CheckIn.DayNumber;
            booking.CheckIn = dto.CheckIn;
            booking.CheckOut = dto.CheckOut;
            booking.Guests = dto.Guests;
            booking.TotalPrice = perNight * nights;
            booking.UpdatedAtUtc = DateTime.UtcNow;

            await context.SaveChangesAsync();

            //var updated = new GetBookingDto(
            //    booking.Id,
            //    booking.HotelId,
            //    booking.Hotel!.Name,
            //    booking.CheckIn,
            //    booking.CheckOut,
            //    booking.Guests,
            //    booking.TotalPrice,
            //    booking.Status.ToString(),
            //    booking.CreatedAtUtc,
            //    booking.UpdatedAtUtc
            //    );

            var updated = mapper.Map<GetBookingDto>(booking);
            return Result<GetBookingDto>.Success(updated);


        }

        public async Task<Result> CancelBookingAsync(int hotelId, int bookingId)
        {
            var userId = usersService.UserId;


            var booking = await context.Bookings
               .Include(b => b.Hotel)
               .FirstOrDefaultAsync(b =>
                   b.Id == bookingId
                   && b.HotelId == hotelId
                   && b.UserId == userId);

            if (booking is null)
                return Result.Failure(new Error(ErrorCodes.NotFound, $"Booking '{bookingId}' was not found."));

            if (booking.Status == BookingStatus.Cancelled)
                return Result.Failure(new Error(ErrorCodes.Conflict, "This booking has already been cancelled."));

            booking.Status = BookingStatus.Cancelled;
            booking.CreatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return Result.Success();
        }

        public async Task<Result> AdminCancelBookingAsync(int hotelId, int bookingId)
        {
            var userId = usersService.UserId;

            var isHotelAdminUser = await context.HotelAdmins
                .AnyAsync(q => q.UserId == userId && q.HotelId == hotelId);

            if (!isHotelAdminUser)
                return Result.Failure(new Error(ErrorCodes.Forbid, $"You are not an admin of the selected Hotel."));

            var booking = await context.Bookings
              .Include(b => b.Hotel)
              .FirstOrDefaultAsync(b =>
                  b.Id == bookingId
                  && b.HotelId == hotelId
                  );

            if (booking is null)
                return Result.Failure(new Error(ErrorCodes.NotFound, $"Booking '{bookingId}' was not found."));

            if (booking.Status == BookingStatus.Cancelled)
                return Result.Failure(new Error(ErrorCodes.Conflict, $"This booking has already been cancelled."));

            booking.Status = BookingStatus.Cancelled;
            booking.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return Result.Success();

        }

        public async Task<Result> AdminConfirmBookingAsync(int hotelId, int bookingId)
        {
            var userId = usersService.UserId;


            var isHotelAdminUser = await context.HotelAdmins
               .AnyAsync(q => q.UserId == userId && q.HotelId == hotelId);

            if (!isHotelAdminUser)
                return Result.Failure(new Error(ErrorCodes.Forbid, $"You are not an admin of the selected Hotel."));


            var booking = await context.Bookings
             .Include(b => b.Hotel)
             .FirstOrDefaultAsync(b =>
                 b.Id == bookingId
                 && b.HotelId == hotelId
                 );

            if (booking is null)
                return Result.Failure(new Error(ErrorCodes.NotFound, $"Booking '{bookingId}' was not found."));

            if (booking.Status == BookingStatus.Cancelled)
                return Result.Failure(new Error(ErrorCodes.Conflict, $"This booking has already been cancelled."));

            booking.Status = BookingStatus.Confirmed;
            booking.UpdatedAtUtc = DateTime.UtcNow;
            await context.SaveChangesAsync();

            return Result.Success();



        }

        public async Task<Result<IEnumerable<GetBookingDto>>> GetUserBookingsForHotelAsync(int hotelId)
        {
            var userId = usersService.UserId;

            var hotelExists = await context.Hotels.AnyAsync(h => h.Id == hotelId);

            if (!hotelExists)
                return Result<IEnumerable<GetBookingDto>>.Failure(new Error(ErrorCodes.NotFound, $"Hotel '{hotelId}' was not found."));

            var bookings = await context.Bookings
                .Where(b => b.HotelId == hotelId && b.UserId == userId)
                .OrderBy(b => b.CheckIn)
                .ProjectTo<GetBookingDto>(mapper.ConfigurationProvider)
                .ToListAsync();
            //.Select(b => new GetBookingDto(
            //    b.Id,
            //    b.HotelId,
            //    b.Hotel!.Name,
            //    b.CheckIn,
            //    b.CheckOut,
            //    b.Guests,
            //    b.TotalPrice,
            //    b.Status.ToString(),
            //    b.CreatedAtUtc,
            //    b.UpdatedAtUtc
            //    ))

            return Result<IEnumerable<GetBookingDto>>.Success(bookings);
        }

        public async Task<bool> IsOverlap(int hotelId, string userId, DateOnly checkIn, DateOnly checkOut, int? bookingId = null)
        {
            var overLap = context.Bookings.Where(
                    b => b.HotelId == hotelId
                    && b.Status != BookingStatus.Cancelled
                    && checkIn < b.CheckOut
                    && checkOut > b.CheckIn
                    && b.UserId == userId
                    ).AsQueryable();

            if (bookingId.HasValue)
            {
                overLap = overLap.Where(q => q.Id != bookingId.Value);
            }

            return await overLap.AnyAsync();
        }
    }
}
