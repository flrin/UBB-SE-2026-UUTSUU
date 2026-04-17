using System.Collections.Generic;
using SearchAndBook.Domain;

namespace SearchAndBook.Repositories
{
    public interface InterfaceRentalsRepository : IRepository<TimeRange>
    {
        /// <summary>
        /// Retrieves unavailable rental time rentaltimeranges for a specific game.
        /// </summary>
        /// <param name="gameId">The game identifier.</param>
        /// <returns>A list of time rentaltimeranges when the game is unavailable.</returns>
        List<TimeRange> GetUnavailableTimeRanges(int gameId);
        /// <summary>
        /// Checks if a game is available for a specified time range.
        /// </summary>
        /// <param name="range">The requested rental time range.</param>
        /// <param name="gameId">The game identifier.</param>
        /// <returns>True if the game is available; otherwise, false.</returns>
        bool CheckGameAvailability(TimeRange range, int gameId);
    }
}
