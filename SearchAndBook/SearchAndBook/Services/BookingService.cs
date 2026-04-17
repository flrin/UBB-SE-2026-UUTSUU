using System;
using SearchAndBook.Domain;
using SearchAndBook.Repositories;
using SearchAndBook.Shared;

namespace SearchAndBook.Services;

/// <summary>
/// Service responsible for handling booking operations, including retrieving game details,
/// checking availability, and managing rental time ranges.
/// </summary>
public class BookingService : InterfaceBookingService
{
    private readonly InterfaceGamesRepository gamesRepo;
    private readonly InterfaceRentalsRepository rentalsRepo;
    private readonly InterfaceUsersRepository usersRepo;

    /// <summary>
    /// Initializes a new instance of the BookingService class.
    /// </summary>
    /// <param name="gamesRepo">The games repository.</param>
    /// <param name="rentalsRepo">The rentals repository.</param>
    /// <param name="usersRepo">The users repository.</param>
    public BookingService(
        InterfaceGamesRepository gamesRepo,
        InterfaceRentalsRepository rentalsRepo,
        InterfaceUsersRepository usersRepo)
    {
        this.gamesRepo = gamesRepo;
        this.rentalsRepo = rentalsRepo;
        this.usersRepo = usersRepo;
    }

    /// <summary>
    /// Retrieves detailed booking information for a specific game, including owner details.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <returns>A <see cref="BookingDTO"/> containing the game and owner details.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the game or its owner cannot be found.</exception>
    public BookingDTO GetGameDetails(int gameId)
    {
        try
        {
            var game = gamesRepo.GetGameById(gameId);
            if (game == null)
            {
                throw new InvalidOperationException($"Game with id {gameId} was not found.");
            }

            var owner = usersRepo.GetGameById(game.OwnerId);
            if (owner == null)
            {
                throw new InvalidOperationException($"Owner for game id {gameId} was not found.");
            }

            return new BookingDTO
            {
                GameId = game.GameId,
                Name = game.Name,
                Image = game.Image,
                Price = game.Price,
                City = owner.City,
                MinimumNrPlayers = game.MinimumPlayerNumber,
                MaximumNrPlayers = game.MaximumPlayerNumber,
                Description = game.Description,
                UserId = owner.UserId,
                DisplayName = owner.DisplayName,
                IsSuspended = owner.IsSuspended,
                AvatarUrl = owner.AvatarUrl,
                CreatedAt = owner.CreatedAt
            };
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to retrieve details for game {gameId}.", exception);

        }
    }

    /// <summary>
    /// Retrieves all the time ranges during which a specific game is unavailable.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <returns>An array of <see cref="TimeRange"/> representing the unavailable periods.</returns>
    public TimeRange[] GetUnavailableRanges(int gameId)
    {
        try
        {
            return rentalsRepo
                .GetUnavailableRanges(gameId)
                .ToArray();
        } catch (Exception exception)
        {
            throw new InvalidOperationException($"Failed to retrieve unavailable time ranges for game {gameId}.", exception);
        }
    }

    /// <summary>
    /// Checks whether a specific game is available during the given time range.
    /// </summary>
    /// <param name="gameId">The unique identifier of the game.</param>
    /// <param name="timeRange">The requested <see cref="TimeRange"/> for the booking.</param>
    /// <returns><c>true</c> if the game is available for the specified range; otherwise, <c>false</c>.</returns>
    public bool CheckAvailability(int gameId, TimeRange timeRange)
    {
        try
        {
            return rentalsRepo.CheckAvailability(timeRange, gameId);
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
    public decimal CalculateTotalPrice(decimal price, TimeRange timeRange)
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
    public int CalculateNumberOfDays(TimeRange selectedTimeRange)
    {
        int days = (selectedTimeRange.EndTime - selectedTimeRange.StartTime).Days + 1;
        return days <= 0 ? 1 : days;
    }
}