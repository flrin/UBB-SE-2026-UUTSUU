namespace SearchAndBook.Services
{
    using System;
    using System.Collections.Generic;
    using SearchAndBook.Shared;

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

        /// <summary>
        /// Applies multiple filters and sorting logic to a collection of games.
        /// </summary>
        /// <param name="games">The collection of games to filter.</param>
        /// <param name="filter">The filtering and sorting criteria.</param>
        /// <returns>A filtered and ordered array of games.</returns>
        /// <exception cref="InvalidOperationException">Thrown when filtering fails.</exception>
        GameDTO[] ApplyFilters(GameDTO[] games, FilterCriteria filter);

        /// <summary>
        /// Retrieves a paginated discovery feed categorized into games available tonight and others.
        /// </summary>
        /// <param name="userId">The ID of the user requesting the feed.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="pageSize">The number of games per page.</param>
        /// <returns>A tuple containing categorized games and the total count of available items.</returns>
        (List<GameDTO> availableTonight, List<GameDTO> others, int totalAvailableGamesCount)
        GetDiscoveryFeedPaged(int userId, int page, int pageSize);

        /// <summary>
        /// Checks if the provided start and end dates form a valid chronological range.
        /// </summary>
        /// <param name="start">The start date.</param>
        /// <param name="end">The end date.</param>
        /// <returns>True if the range is valid, false otherwise.</returns>
        bool IsValidDateRange(DateTime? start, DateTime? end);

        /// <summary>
        /// Verifies if the player count is a non-negative value.
        /// </summary>
        /// <param name="players">The number of players to check.</param>
        /// <returns>True if valid or null, false otherwise.</returns>
        bool IsValidPlayersCount(int? players);

        /// <summary>
        /// Updates the filter criteria object using raw values from the UI components.
        /// </summary>
        /// <param name="filter">The filter object to update.</param>
        /// <param name="selectedMaxPrice">The maximum price chosen in the UI.</param>
        /// <param name="selectedMinimumPlayerCount">The minimum players chosen in the UI.</param>
        /// <param name="selectedStartDate">The selected start date.</param>
        /// <param name="selectedEndDate">The selected end date.</param>
        void UpdateFilterFromUI(FilterCriteria filter, double selectedMaxPrice, double selectedMinimumPlayerCount, DateTime? selectedStartDate, DateTime? selectedEndDate);
    }
}
