namespace SearchAndBook.Repositories.Sql
{
    /// <summary>
    /// Provides SQL query strings for retrieving and managing rental data in the database.
    /// </summary>
    /// <remarks>This class contains predefined SQL queries for common rental-related operations, such as
    /// fetching rentals by game or user, checking for overlapping rental periods, and retrieving unavailable date
    /// ranges. The queries are intended for use with parameterized commands to help prevent SQL injection. All members
    /// are constants and can be accessed without instantiating the class.</remarks>
    public static class RentalQueries
    {
        /// <summary>
        /// Represents the SQL query used to retrieve all rental records for a specified game by its unique identifier.
        /// </summary>
        /// <remarks>The query selects rental details from the Rentals table where the game ID matches the
        /// provided parameter. Results are ordered by the rental start date. This constant is intended for use with
        /// data access code that executes parameterized queries.</remarks>
        public const string GetRentalsByGameId = @"
        SELECT
            r.rental_id,
            r.game_id,
            r.renter_id,
            r.owner_id,
            r.start_date,
            r.end_date,
            r.total_price
        FROM dbo.Rentals r
        WHERE r.game_id = @GameId
        ORDER BY r.start_date;";

        /// <summary>
        /// Represents the SQL query used to retrieve unavailable rental periods for a specific game by its identifier.
        /// </summary>
        /// <remarks>The query selects the start and end dates of all rental periods associated with the
        /// specified game, ordered by the start date. This constant can be used when executing database commands that
        /// require information about when a game is unavailable due to existing rentals.</remarks>
        public const string GetUnavailablePeriodsByGameId = @"
        SELECT
            r.start_date,
            r.end_date
        FROM dbo.Rentals r
        WHERE r.game_id = @GameId
        ORDER BY r.start_date;";

        /// <summary>
        /// Represents a SQL query that checks for the existence of overlapping rental periods for a specified game.
        /// </summary>
        /// <remarks>The query returns 1 if there is at least one rental for the given game where the
        /// rental period overlaps with the requested date range. This can be used to prevent double-booking of the same
        /// game for overlapping time periods.</remarks>
        public const string HasOverlappingRental = @"
        SELECT TOP 1 1
        FROM dbo.Rentals r
        WHERE r.game_id = @GameId
          AND r.start_date < @RequestedEndDate
          AND r.end_date > @RequestedStartDate;";

        /// <summary>
        /// Represents the SQL query used to retrieve all rental records for a specific user, ordered by the rental
        /// start date in descending order.
        /// </summary>
        /// <remarks>This query selects rental details from the Rentals table where the renter's ID
        /// matches the specified user. The results include rental and game identifiers, user IDs for both renter and
        /// owner, rental period, and total price. The query expects a parameter named @UserId to be supplied with the
        /// user's identifier.</remarks>
        public const string GetRentalsForUser = @"
        SELECT
            r.rental_id,
            r.game_id,
            r.renter_id,
            r.owner_id,
            r.start_date,
            r.end_date,
            r.total_price
        FROM dbo.Rentals r
        WHERE r.renter_id = @UserId
        ORDER BY r.start_date DESC;";

        /// <summary>
        /// Represents the SQL query used to retrieve the start and end dates for a rental by its unique identifier.
        /// </summary>
        /// <remarks>The query expects a parameter named @RentalId, which should be set to the rental's
        /// unique identifier. This constant can be used when executing database commands to fetch the rental period for
        /// a specific rental record.</remarks>
        public const string GetRentalRangeById = @"
    SELECT
        r.start_date,
        r.end_date
    FROM dbo.Rentals r
    WHERE r.rental_id = @RentalId;";

        /// <summary>
        /// Represents the SQL query used to retrieve the start and end dates for all rental records from the Rentals
        /// table.
        /// </summary>
        /// <remarks>This constant can be used to execute a query that returns the rental periods for all
        /// entries in the database. The query selects the start and end dates from the dbo.Rentals table without
        /// filtering or joining other tables.</remarks>
        public const string GetAllRentalRanges = @"
    SELECT
        r.start_date,
        r.end_date
    FROM dbo.Rentals r;";
    }
}