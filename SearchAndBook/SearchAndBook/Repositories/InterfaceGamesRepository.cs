using System.Collections.Generic;
using SearchAndBook.Domain;
using SearchAndBook.Shared;

namespace SearchAndBook.Repositories
{
    /// <summary>
    /// Defines the repository operations for managing Game entities.
    /// </summary>
    public interface InterfaceGamesRepository : IRepository<Game>
    {
        /// <summary>
        /// Retrieves a list of games that match the specified filter criteria.
        /// </summary>
        /// <param name="filter">The criteria used to filter the games.</param>
        /// <returns>A list of games matching the filter.</returns>
        List<Game> GetGamesByFilter(FilterCriteria filter);

        /// <summary>
        /// Retrieves a list of games available tonight for the specified user's feed.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of games available tonight.</returns>
        List<Game> GetGamesForFeedAvailableTonight(int userId);

        /// <summary>
        /// Retrieves a list of other games for the specified user's feed.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <returns>A list of other games for the user's feed.</returns>
        List<Game> GetGamesForFeedOthers(int userId);
    }
}
