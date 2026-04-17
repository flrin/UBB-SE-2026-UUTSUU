using SearchAndBook.Domain;
using SearchAndBook.Shared;

namespace SearchAndBook.Services
{
    /// <summary>
    /// Defines the operations for managing and querying game bookings.
    /// </summary>
    public interface InterfaceBookingService
    {
        /// <summary>
        /// Retrieves the booking details for a specific game.
        /// </summary>
        /// <param name="gameId">The unique identifier of the game.</param>
        /// <returns>A <see cref="BookingDTO"/> containing the game details.</returns>
        
        /// <exception cref="InvalidOperationException">
        /// Thrown when the game or its owner cannot be found, or when retrieval fails.
        /// </exception>
        BookingDTO GetGameDetails(int gameId);

        /// <summary>
        /// Retrieves all unavailable time ranges for a specific game.
        /// </summary>
        /// <param name="gameId">The unique identifier of the game.</param>
        /// <returns>An array of <see cref="TimeRange"/> representing periods when the game is unavailable.</returns>

        
        /// <exception cref="InvalidOperationException">
        /// Thrown when retrieval of unavailable ranges fails.
        /// </exception>
        TimeRange[] GetUnavailableRanges(int gameId);

        /// <summary>
        /// Checks if a game is available for booking during a specified time range.
        /// </summary>
        /// <param name="gameId">The unique identifier of the game.</param>
        /// <param name="range">The time range to check for availability.</param>
        /// <returns><c>true</c> if the game is available during the specified range; otherwise, <c>false</c>.</returns>
        
        /// <exception cref="InvalidOperationException">
        /// Thrown when the availability check fails.
        /// </exception>
        bool CheckAvailability(int gameId, TimeRange range);
        decimal CalculateTotalPrice(decimal price, TimeRange timeRange);
        int CalculateNumberOfDays(TimeRange selectedTimeRange);
    }
}
