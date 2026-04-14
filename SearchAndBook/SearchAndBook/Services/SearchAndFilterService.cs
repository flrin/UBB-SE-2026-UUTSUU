namespace SearchAndBook.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using SearchAndBook.Domain;
    using SearchAndBook.Repositories;
    using SearchAndBook.Shared;
    using SearchAndBook.Utils;

    /// <summary>
    /// Service responsible for searching, filtering, and retrieving game feeds.
    /// </summary>
    internal class SearchAndFilterService : InterfaceSearchAndFilterService
    {
        private readonly InterfaceGamesRepository gamesRepository;
        private readonly InterfaceUsersRepository usersRepository;
        private readonly InterfaceRentalsRepository rentalsRepository;
        private readonly InterfaceGeographicalService geographicalService;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchAndFilterService"/> class.
        /// Initializes a new instance.
        /// </summary>
        /// <param name="gamesRepository">The repository for game data operations.</param>
        /// <param name="usersRepository">The repository for user data operations.</param>
        /// <param name="rentalsRepository">The repository for rental data operations.</param>
        /// <param name="geographicalService">The service for geographical and location-based calculations.</param>
        public SearchAndFilterService(InterfaceGamesRepository gamesRepository, InterfaceUsersRepository usersRepository, InterfaceRentalsRepository rentalsRepository, InterfaceGeographicalService geographicalService)
        {
            this.gamesRepository = gamesRepository;
            this.usersRepository = usersRepository;
            this.rentalsRepository = rentalsRepository;
            this.geographicalService = geographicalService;
        }

        /// <summary>
        /// Searches for games based on the provided filter criteria.
        /// </summary>
        /// <param name="filter">The criteria to filter games.</param>
        /// <returns>An array of <see cref="GameDTO"/> matching the criteria.</returns>
        public GameDTO[] SearchGamesByFilter(FilterCriteria filter)
        {
            try
            {
                string? originalCity = filter.City;
                if (filter.SortOption == SortOption.Location)
                {
                    filter.City = null;
                }

                var games = this.gamesRepository.GetGamesByFilter(filter);
                filter.City = originalCity;

                var gameResults = new List<GameDTO>();
                var ownerCacheById = new Dictionary<int, User>();

                foreach (var game in games)
                {
                    if (!ownerCacheById.ContainsKey(game.OwnerId))
                    {
                        ownerCacheById[game.OwnerId] = this.usersRepository.GetGameById(game.OwnerId);
                    }

                    var gameowner = ownerCacheById[game.OwnerId];

                    var gameDto = new GameDTO
                    {
                        GameId = game.GameId,
                        Name = game.Name,
                        Image = game.Image,
                        Price = game.Price,
                        City = gameowner != null ? gameowner.City : string.Empty,
                        MaximumPlayerNumber = game.MaximumPlayerNumber,
                        MinimumPlayerNumber = game.MinimumPlayerNumber,
                    };

                    gameResults.Add(gameDto);
                }

                GameDTO[] gameResultsAray = gameResults.ToArray();

                //// sorting by distance

                //// this is if we decide to only use this methode and remove the ApplyFilters method
                //// only runs this code if SortOption is set, so never from feed

                return this.ApplyFilters(gameResultsAray, filter);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to search for games.", ex);
            }
        }

        /// <summary>
        /// Retrieves a feed of games available tonight for the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user requesting the feed or null.</param>
        /// <returns>An array of <see cref="GameDTO"/> available tonight.</returns>
        public GameDTO[] GetGamesFeedAvailableTonightByUser(int userId)
        {
            try
            {
                var games = this.gamesRepository.GetGamesForFeedAvailableTonight(userId);
                return games.Select(game => this.MapToGameDTO(game, this.usersRepository.GetGameById(game.OwnerId))).ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve <<Available tonight>> feed.", ex);
            }
        }

        /// <summary>
        /// Retrieves a feed of other relevant games for the specified user.
        /// </summary>
        /// <param name="userId">The ID of the user requesting the feed or null.</param>
        /// <returns>An array of <see cref="GameDTO"/> representing other games.</returns>
        public GameDTO[] GetOtherGamesFeedByUser(int userId)
        {
            try
            {
                var games = this.gamesRepository.GetGamesForFeedOthers(userId);
                return games.Select(game => this.MapToGameDTO(game, this.usersRepository.GetGameById(game.OwnerId))).ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve <<Others>> feed.", ex);
            }
        }

        /// <summary>
        /// Filters and sorts an array of games based on the provided criteria, including name, price, player count, and location.
        /// </summary>
        /// <param name="sourceGames">The initial collection of games to be filtered.</param>
        /// <param name="filter">The criteria used for filtering and sorting the games.</param>
        /// <returns>An array of <see cref="GameDTO""")/>> objects that match the filter criteria.</returns>
        /// <exception cref="InvalidOperationException">Thrown when an error occurs during the filtering process.</exception>
        public GameDTO[] ApplyFilters(GameDTO[] sourceGames, FilterCriteria filter)
        {
            try
            {
                IEnumerable<GameDTO> filteredGames = sourceGames;

                if (!string.IsNullOrWhiteSpace(filter.Name))
                {
                    filteredGames = filteredGames.Where(game => game.Name.Contains(filter.Name, StringComparison.OrdinalIgnoreCase));
                }

                if (filter.MaximumPrice.HasValue)
                {
                    filteredGames = filteredGames.Where(game => game.Price <= filter.MaximumPrice.Value);
                }

                if (filter.PlayerCount.HasValue)
                {
                    filteredGames = filteredGames.Where(game => game.MaximumPlayerNumber >= filter.PlayerCount.Value);
                }

                if (!string.IsNullOrWhiteSpace(filter.City) && filter.SortOption != SortOption.Location)
                {
                    filteredGames = filteredGames.Where(game =>
                        !string.IsNullOrWhiteSpace(game.City) &&
                        game.City.Contains(filter.City, StringComparison.OrdinalIgnoreCase));
                }

                switch (filter.SortOption)
                {
                    case SortOption.PriceAscending:
                        filteredGames = filteredGames.OrderBy(game => game.Price);
                        break;

                    case SortOption.PriceDescending:
                        filteredGames = filteredGames.OrderByDescending(game => game.Price);
                        break;

                    case SortOption.Location:
                        if (!string.IsNullOrWhiteSpace(filter.City))
                        {
                            var userCity = this.geographicalService.GetCityDetails(filter.City);
                            if (userCity.found)
                            {
                                var distanceCache = new Dictionary<string, double?>();

                                filteredGames = filteredGames.OrderBy(g =>
                                {
                                    if (string.IsNullOrWhiteSpace(g.City))
                                    {
                                        return double.MaxValue;
                                    }

                                    if (!distanceCache.TryGetValue(g.City, out double? distance))
                                    {
                                        var gameCity = this.geographicalService.GetCityDetails(g.City);
                                        distance = gameCity.found
                                            ? GeographicDistance.CalculateDistance(userCity.lat, userCity.lon, gameCity.lat, gameCity.lon)
                                            : null;

                                        distanceCache[g.City] = distance;
                                    }

                                    return distance ?? double.MaxValue;
                                });
                            }
                        }

                        break;

                    case SortOption.None:
                    default:
                        break;
                }

                if (filter.AvailabilityRange != null)
                {
                    filteredGames = filteredGames.Where(game =>
                        this.rentalsRepository.CheckAvailability(filter.AvailabilityRange, game.GameId));
                }

                return filteredGames.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to apply filters.", ex);
            }
        }

        /// <summary>
        /// Retrieves a paginated discovery feed, splitting games into those available tonight and others.
        /// </summary>
        /// <param name="userId">The ID of the user for whom the feed is generated.</param>
        /// <param name="page">The current page number (1-based).</param>
        /// <param name="pageSize">The number of items to include per page.</param>
        /// <returns>A tuple containing available games for tonight, other available games, and the total count of games.</returns>
        public (List<GameDTO> availableTonight, List<GameDTO> others, int totalAvailableGamesCount)
            GetDiscoveryFeedPaged(int userId, int page, int pageSize)
         {
                var availableTonightGames = this.GetGamesFeedAvailableTonightByUser(userId).ToList();
                var otherAvailableGames = this.GetOtherGamesFeedByUser(userId).ToList();

                var allDescoveryFeedGames = availableTonightGames.Concat(otherAvailableGames).ToList();
                var totalAvailableGamesCount = allDescoveryFeedGames.Count;

                var paginatedGames = allDescoveryFeedGames
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                var pagedAvailableTonightGames = paginatedGames
                    .Where(g => availableTonightGames.Any(a => a.GameId == g.GameId))
                    .ToList();

                var pagedOtherGames = paginatedGames
                    .Where(g => otherAvailableGames.Any(o => o.GameId == g.GameId))
                    .ToList();

                return (pagedAvailableTonightGames, pagedOtherGames, totalAvailableGamesCount);
         }

        /// <summary>
        /// Validates if a given date range is logical (start date is before or equal to end date).
        /// </summary>
        /// <param name="start">The start date of the range.</param>
        /// <param name="end">The end date of the range.</param>
        /// <returns>True if the range is valid or both dates are null; false if only one date is provided or start is after end.</returns>
        public bool IsValidDateRange(DateTime? start, DateTime? end)
        {
            if (!start.HasValue && !end.HasValue)
            {
                return true;
            }

            if (!start.HasValue || !end.HasValue)
            {
                return false;
            }

            return start.Value <= end.Value;
        }

        /// <summary>
        /// checks if the number of players if valid.
        /// </summary>
        /// <param name="players">number of players.</param>
        /// <returns>true if the value is valid, false otherwise.</returns>
        public bool IsValidPlayersCount(int? players)
        {
            if (!players.HasValue)
            {
                return true;
            }

            return players.Value >= 0;
        }

        /// <summary>
        /// Updates the filter criteria object with values provided from the user interface.
        /// </summary>
        /// <param name="filter">The filter object to be updated.</param>
        /// <param name="selectedMaxPrice">The maximum price selected by the user.</param>
        /// <param name="selectedMinPlayers">The minimum number of players selected by the user.</param>
        /// <param name="startDate">The start date for availability.</param>
        /// <param name="endDate">The end date for availability.</param>
        public void UpdateFilterFromUI(FilterCriteria filter, double selectedMaxPrice, double selectedMinPlayers, DateTime? startDate, DateTime? endDate)
        {
            // price
            filter.MaximumPrice = selectedMaxPrice > 0
                ? (decimal?)selectedMaxPrice
                : null;

            // players
            filter.PlayerCount = selectedMinPlayers > 0
                ? (int?)selectedMinPlayers
                : null;

            // date
            if (this.IsValidDateRange(startDate, endDate))
            {
                if (startDate.HasValue && endDate.HasValue)
                {
                    filter.AvailabilityRange = new TimeRange(
                        startDate.Value,
                        endDate.Value);
                }
                else
                {
                    filter.AvailabilityRange = null;
                }
            }
            else
            {
                filter.AvailabilityRange = null;
            }
        }

        /// <summary>
        /// Maps a <see cref="Game""")/>> entity and its owner's information to a <see cref="GameDTO""")/>>.
        /// </summary>
        /// <param name="game">The game entity to map.</param>
        /// <param name="owner">The user who owns the game.</param>
        /// <returns>A data transfer object representing the game.</returns>
        private GameDTO MapToGameDTO(Game game, User? owner)
        {
            return new GameDTO
            {
                GameId = game.GameId,
                Name = game.Name,
                Image = game.Image,
                Price = game.Price,
                City = owner?.City ?? string.Empty,
                MaximumPlayerNumber = game.MaximumPlayerNumber,
                MinimumPlayerNumber = game.MinimumPlayerNumber,
            };
        }
    }
}
