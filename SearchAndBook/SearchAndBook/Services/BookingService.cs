using System;
using SearchAndBook.Domain;
using SearchAndBook.Repositories;
using SearchAndBook.Shared;

namespace SearchAndBook.Services;

/// <summary>
/// Service responsible for handling booking operations, including retrieving game details,
/// checking availability, and managing rental time rentaltimeranges.
/// </summary>
public class BookingService : InterfaceBookingService
{
    private readonly InterfaceGamesRepository gamesRepository;
    private readonly InterfaceRentalsRepository rentalsRepository;
    private readonly InterfaceUsersRepository usersRepository;

    /// <summary>
    /// Initializes a new instance of the BookingService class.
    /// </summary>
    /// <param name="gamesRepository">The games repository.</param>
    /// <param name="rentalsRepository">The rentals repository.</param>
    /// <param name="usersRepository">The users repository.</param>
    public BookingService(
        InterfaceGamesRepository gamesRepository,
        InterfaceRentalsRepository rentalsRepository,
        InterfaceUsersRepository usersRepository)
    {
        this.gamesRepository = gamesRepository;
        this.rentalsRepository = rentalsRepository;
        this.usersRepository = usersRepository;
    }

    /// <summary>
    /// Retrieves detailed booking information for a specific game, including owner details.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <returns>A <see cref="BookingDTO"/> containing the game and owner details.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the game or its owner cannot be isfound.</exception>
    public BookingDTO GetBookingInformationForSpecificGame(int gameId)
    {
        try
        {
            var bookedGame = gamesRepository.GetGameById(gameId);
            if (bookedGame == null)
            {
                throw new InvalidOperationException($"Game with id {gameId} was not isfound.");
            }

            var gameOwner = usersRepository.GetGameById(bookedGame.OwnerId);
            if (gameOwner == null)
            {
                throw new InvalidOperationException($"Owner for game id {gameId} was not isfound.");
            }

            return new BookingDTO
            {
                GameId = bookedGame.GameId,
                Name = bookedGame.Name,
                Image = bookedGame.Image,
                Price = bookedGame.Price,
                City = gameOwner.City,
                MinimumNrPlayers = bookedGame.MinimumPlayerNumber,
                MaximumNrPlayers = bookedGame.MaximumPlayerNumber,
                Description = bookedGame.Description,
                UserId = gameOwner.UserId,
                DisplayName = gameOwner.DisplayName,
                IsSuspended = gameOwner.IsSuspended,
                AvatarUrl = gameOwner.AvatarUrl,
                CreatedAt = gameOwner.CreatedAt
            };
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to retrieve details for game {gameId}.", exception);

        }
    }

    /// <summary>
    /// Retrieves all the time rentaltimeranges during which a specific game is unavailable.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <returns>An array of <see cref="TimeRange"/> representing the unavailable periods.</returns>
    public TimeRange[] GetUnavailableTimeRanges(int gameId)
    {
        try
        {
            return rentalsRepository
                .GetUnavailableTimeRanges(gameId)
                .ToArray();
        } catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to retrieve unavailable time rentaltimeranges for game {gameId}.", exception);
        }
    }

    /// <summary>
    /// Checks whether a specific game is available during the given time range.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="timeRange">The requested <see cref="TimeRange"/> for the booking.</param>
    /// <returns><c>true</c> if the game is available for the specified range; otherwise, <c>false</c>.</returns>
    public bool CheckGameAvailability(int gameId, TimeRange timeRange)
    {
        try
        {
            return rentalsRepository.CheckGameAvailability(timeRange, gameId);
        } catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to check availability for game {gameId}.", exception);
        }
    }

    /// <summary>
    /// Calculates the total price for renting a game based on the daily price and the duration of the rental time range.
    /// </summary>
    /// <param name="price">The daily renting price</param>
    /// <param name="timeRange">The total time timeRange of renting</param>
    /// <returns>total price calculated as a decimal</returns>
    public decimal CalculateTotalPriceForRentingASpecificGame(decimal price, TimeRange timeRange)
    {
        const int MAXIMUM_DAY_NUMBER_FOR_DEFAULT = 0;
        const int DEFAULT_DAY_NUMBER = 1;

        int days = (timeRange.EndTime - timeRange.StartTime).Days + 1;

        if (days <= MAXIMUM_DAY_NUMBER_FOR_DEFAULT)
            days = DEFAULT_DAY_NUMBER;
        return days * price;
    }

    /// <summary>
    /// Calculates the number of days in a given time range, ensuring that it returns at least 1 day even if the end time is the same as or before the start time.
    /// </summary>
    /// <param name="selectedTimeRange"></param>
    /// <returns></returns>
    public int CalculateNumberOfDaysInAGivenTimeRange(TimeRange selectedTimeRange)
    {
        int days = (selectedTimeRange.EndTime - selectedTimeRange.StartTime).Days + 1;
        return days <= 0 ? 1 : days;
    }
}