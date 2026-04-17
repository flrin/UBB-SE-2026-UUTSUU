namespace SearchAndBook.Services
{
    using System;
    using SearchAndBook.Domain;
    using SearchAndBook.Repositories;
    using SearchAndBook.Shared;

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
        /// Initializes a new instance of the <see cref="BookingService"/> class.
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
                var game = this.gamesRepo.GetGameById(gameId);
                if (game == null)
                {
                    throw new InvalidOperationException($"Game with id {gameId} was not found.");
                }

                var owner = this.usersRepo.GetGameById(game.OwnerId);
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
                    CreatedAt = owner.CreatedAt,
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
                return this.rentalsRepo
                    .GetUnavailableRanges(gameId)
                    .ToArray();
            }
            catch (Exception exception)
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
                return this.rentalsRepo.CheckAvailability(timeRange, gameId);
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException($"Failed to check availability for game {gameId}.", exception);
            }
        }

        /// <summary>
        /// Calculates the total price for renting a game based on the daily price and the duration of the rental time range.
        /// </summary>
        /// <param name="price">The daily renting price.</param>
        /// <param name="timeRange">The total rental time range.</param>
        /// <returns>The total calculated price as a decimal value.</returns>
        public decimal CalculateTotalPrice(decimal price, TimeRange timeRange)
        {
            const int MaximumDayNumberForDefault = 0;
            const int DefaultDayNumber = 1;

            int days = (timeRange.EndTime - timeRange.StartTime).Days + 1;

            if (days <= MaximumDayNumberForDefault)
            {
                days = DefaultDayNumber;
            }

            return days * price;
        }

        /// <summary>
        /// Calculates the number of days in a given time range, ensuring that it returns at least 1 day even if the end time is the same as or before the start time.
        /// </summary>
        /// <param name="selectedTimeRange">The selected rental time range.</param>
        /// <returns>The number of rental days.</returns>
        public int CalculateNumberOfDays(TimeRange selectedTimeRange)
        {
            int days = (selectedTimeRange.EndTime - selectedTimeRange.StartTime).Days + 1;
            return days <= 0 ? 1 : days;
        }
    }
}