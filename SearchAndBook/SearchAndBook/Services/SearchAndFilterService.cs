using SearchAndBook.Domain;
using SearchAndBook.Repositories;
using SearchAndBook.Shared;
using SearchAndBook.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SearchAndBook.Services
{
    /// <summary>
    /// Service responsible for searching, filtering, and retrieving game feeds.
    /// </summary>
    internal class SearchAndFilterService : InterfaceSearchAndFilterService
    {
        private readonly InterfaceGamesRepository gamesRepository;
        private readonly InterfaceUsersRepository usersRepository;
        private readonly InterfaceRentalsRepository rentalsRepository;
        private readonly InterfaceGeographicalService _geographicalService;

        public SearchAndFilterService(InterfaceGamesRepository gamesRepository, InterfaceUsersRepository usersRepository, InterfaceRentalsRepository rentalsRepository, InterfaceGeographicalService geographicalService)
        {
            this.gamesRepository = gamesRepository;
            this.usersRepository = usersRepository;
            this.rentalsRepository = rentalsRepository;
            this._geographicalService = geographicalService;
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

                var games = gamesRepository.GetGamesByFilter(filter);
                filter.City = originalCity;

                var gameResults = new List<GameDTO>();
                var ownerCacheById = new Dictionary<int, User>();

                foreach (var game in games)
                {
                    if (!ownerCacheById.ContainsKey(game.OwnerId))
                    {
                        ownerCacheById[game.OwnerId] = usersRepository.GetGameById(game.OwnerId);
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
                        MinimumPlayerNumber = game.MinimumPlayerNumber
                    };

                    gameResults.Add(gameDto);
                }

                GameDTO[] gameResultsAray = gameResults.ToArray();

                //// sorting by distance

                //// this is if we decide to only use this methode and remove the ApplyFilters method
                //// only runs this code if SortOption is set, so never from feed


                return ApplyFilters(gameResultsAray, filter);
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
                var result = new List<GameDTO>();
                // aici am moficat, nu am mai duplicat codul din functia MapToGameDTO 

                foreach (var game in games)
                {
                    var user = this.usersRepository.GetGameById(game.OwnerId);

                    if (user != null)
                    {
                        var dto = MapToGameDTO(game, user);
                        result.Add(dto);
                    }
                }

                return result.ToArray();
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
                var result = new List<GameDTO>();
                foreach (var game in games)
                {
                    var user = this.usersRepository.GetGameById(game.OwnerId);

                    if (user == null)
                        continue;

                    var dto = MapToGameDTO(game, user);
                    result.Add(dto);
                }

                return result.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to retrieve <<Others>> feed.", ex);
            }
        }

        // pentru ca in codul curent avem in functiile GetGamesFeedAvailableTonightByUser si GetOtherGamesFeedByUser cu cod duplicat
        private static GameDTO MapToGameDTO(Game game, User? owner)
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
                            var userCity = _geographicalService.GetCityDetails(filter.City);
                            if (userCity.found)
                            {
                                var distanceCache = new Dictionary<string, double?>();

                                filteredGames = filteredGames.OrderBy(g =>
                                {
                                    if (string.IsNullOrWhiteSpace(g.City)) return double.MaxValue;

                                    if (!distanceCache.TryGetValue(g.City, out double? distance))
                                    {
                                        var gameCity = _geographicalService.GetCityDetails(g.City);
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
                        rentalsRepository.CheckAvailability(filter.AvailabilityRange, game.GameId));
                }
                return filteredGames.ToArray();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to apply filters.", ex);
            }
        }


        public (List<GameDTO> availableTonight, List<GameDTO> others, int totalAvailableGamesCount)
         GetDiscoveryFeedPaged(int userId, int page, int pageSize)
         {
                var availableTonightGames = GetGamesFeedAvailableTonightByUser(userId).ToList();
                var otherAvailableGames = GetOtherGamesFeedByUser(userId).ToList();

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


        public bool IsValidDateRange(DateTime? start, DateTime? end)
        {
            if (!start.HasValue && !end.HasValue)
                return true;

            if (!start.HasValue || !end.HasValue)
                return false;

            return start.Value <= end.Value;
        }

        public bool IsValidPlayersCount(int? players)
        {
            if (!players.HasValue)
                return true;

            return players.Value >= 0;
        }


        public void UpdateFilterFromUI(FilterCriteria filter,double selectedMaxPrice,double selectedMinPlayers,DateTime? startDate,DateTime? endDate)
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
            if (IsValidDateRange(startDate, endDate))
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
    }
}
