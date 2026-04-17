namespace SearchAndBook.Services
{
    using SearchAndBook.Domain;
    using SearchAndBook.Shared;

    /// <summary>
    /// Booking service operations.
    /// </summary>
    public interface InterfaceBookingService
    {
        /// <summary>
        /// Gets game details.
        /// </summary>
        /// <param name="gameId">Game id.</param>
        /// <returns>Game details.</returns>
        BookingDTO GetGameDetails(int gameId);

        /// <summary>
        /// Gets unavailable time ranges.
        /// </summary>
        /// <param name="gameId">Game id.</param>
        /// <returns>Unavailable ranges.</returns>
        TimeRange[] GetUnavailableRanges(int gameId);

        /// <summary>
        /// Checks if game is available.
        /// </summary>
        /// <param name="gameId">Game id.</param>
        /// <param name="range">Time range.</param>
        /// <returns>True if available.</returns>
        bool CheckAvailability(int gameId, TimeRange range);

        /// <summary>
        /// Calculates total price.
        /// </summary>
        /// <param name="price">Price per day.</param>
        /// <param name="timeRange">Time range.</param>
        /// <returns>Total price.</returns>
        decimal CalculateTotalPrice(decimal price, TimeRange timeRange);

        /// <summary>
        /// Calculates number of days.
        /// </summary>
        /// <param name="selectedTimeRange">Time range.</param>
        /// <returns>Number of days.</returns>
        int CalculateNumberOfDays(TimeRange selectedTimeRange);
    }
}