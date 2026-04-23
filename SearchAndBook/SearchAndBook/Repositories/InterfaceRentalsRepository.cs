namespace SearchAndBook.Repositories
{
    using System.Collections.Generic;
    using SearchAndBook.Domain;

    /// <summary>
    /// Defines methods for querying and managing rental time ranges for games, including checking availability and
    /// retrieving unavailable periods.
    /// </summary>
    /// <remarks>This interface extends IRepository.
    /// <TimeRange> to provide domain-specific operations for game
    /// rentals. Implementations are expected to handle time range conflicts and availability checks according to the
    /// application's business rules.</remarks>
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
