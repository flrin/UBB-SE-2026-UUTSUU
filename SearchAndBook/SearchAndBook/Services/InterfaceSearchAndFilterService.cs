using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SearchAndBook.Shared;

namespace SearchAndBook.Services
{
    /// <summary>
    /// Provides search and filtering capabilities for games.
    /// </summary>
    public interface InterfaceSearchAndFilterService
    {
        /// <summary>
        /// Searches for games based on the provided filter criteria.
        /// </summary>
        /// <param name="filter">The criteria to filter the games.</param>
        /// <returns>An array of games matching the filter criteria.</returns>

        /// <exception cref="InvalidOperationException">Thrown when search fails.</exception>
        GameDTO[] SearchGamesByFilter(FilterCriteria filter);

        /// <summary>
        /// Retrieves a feed of games available tonight for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>An array of games available tonight.</returns>

        /// <exception cref="InvalidOperationException">Thrown when feed retrieval fails.</exception>
        GameDTO[] GetGamesFeedAvailableTonightByUser(int userId);

        /// <summary>
        /// Retrieves a feed of other games for a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <returns>An array of other games.</returns>

        /// <exception cref="InvalidOperationException">Thrown when feed retrieval fails.</exception>
        GameDTO[] GetOtherGamesFeedByUser(int userId);

        /// <exception cref="InvalidOperationException">Thrown when filtering fails.</exception>
        GameDTO[] ApplyFilters(GameDTO[] games, FilterCriteria filter);

        (List<GameDTO> availableTonight, List<GameDTO> others, int totalAvailableGamesCount)
        GetDiscoveryFeedPaged(int userId, int page, int pageSize);

        bool IsValidDateRange(DateTime? start, DateTime? end);

        bool IsValidPlayersCount(int? players);

        void UpdateFilterFromUI(FilterCriteria filter,double selectedMaxPrice,double selectedMinPlayers,DateTime? startDate,DateTime? endDate);
    }
}
