namespace SearchAndBook.Repositories.Sql
{
    /// <summary>
    /// Provides SQL query strings for retrieving and searching game and owner data from the database.
    /// </summary>
    /// <remarks>This static class contains predefined SQL queries used to access game information, including details
    /// about games, their owners, and availability based on various filters. The queries are intended for use with
    /// parameterized commands to prevent SQL injection and should be executed using appropriate data access methods. All
    /// queries assume the presence of the corresponding tables and columns in the database schema.</remarks>
    public static class GameQueries
    {
        /// <summary>
        /// Represents the SQL query used to retrieve a game's details by its unique identifier.
        /// </summary>
        /// <remarks>The query selects all columns relevant to a game from the Games table where the game ID
        /// matches the specified parameter. The parameter '@GameId' must be provided when executing this query.</remarks>
        public const string GetGameById = @"
        SELECT
            g.game_id,
            g.name,
            g.price,
            g.minimum_player_number,
            g.maximum_player_number,
            g.description,
            g.image,
            g.is_active,
            g.owner_id
        FROM dbo.Games g
        WHERE g.game_id = @GameId;";

        /// <summary>
        /// Represents the SQL query used to retrieve a game's details along with its owner's information by game
        /// identifier.
        /// </summary>
        /// <remarks>The query joins the Games and Users tables to return both game and owner fields. It
        /// is parameterized and expects a value for @GameId. The result includes all columns necessary to fully
        /// describe the game and its owner.</remarks>
        public const string GetGameWithOwnerById = @"
        SELECT
            g.game_id,
            g.name,
            g.price,
            g.minimum_player_number,
            g.maximum_player_number,
            g.description,
            g.image,
            g.is_active,
            g.owner_id,

            u.user_id,
            u.username,
            u.display_name,
            u.email,
            u.phone_number,
            u.avatar_url,
            u.is_suspended,
            u.created_at,
            u.updated_at,
            u.street_name,
            u.street_number,
            u.city,
            u.country
        FROM dbo.Games g
        INNER JOIN dbo.Users u ON g.owner_id = u.user_id
        WHERE g.game_id = @GameId;";

        /// <summary>
        /// Represents the SQL query used to retrieve all active games along with their owner information, excluding
        /// games owned by a specified user.
        /// </summary>
        /// <remarks>This query joins the Games and Users tables to return both game details and the
        /// corresponding owner's user information. The query filters out games that are inactive or owned by the user
        /// specified by the @UserId parameter.</remarks>
        public const string GetAllActiveGamesWithOwner = @"
        SELECT
            g.game_id,
            g.name,
            g.price,
            g.minimum_player_number,
            g.maximum_player_number,
            g.description,
            g.image,
            g.is_active,
            g.owner_id,

            u.user_id,
            u.username,
            u.display_name,
            u.email,
            u.phone_number,
            u.avatar_url,
            u.is_suspended,
            u.created_at,
            u.updated_at,
            u.street_name,
            u.street_number,
            u.city,
            u.country
        FROM dbo.Games g
        INNER JOIN dbo.Users u ON g.owner_id = u.user_id
        WHERE g.is_active = 1
            AND g.owner_id <> @UserId;";

        /// <summary>
        /// Represents the SQL query used to retrieve all active games that are available for rent within a specified
        /// date range and not owned by the requesting user.
        /// </summary>
        /// <remarks>The query joins the Games and Users tables to return both game and owner details. It
        /// excludes games that are already rented during the requested period or owned by the user making the request.
        /// Use parameterized values for @UserId, @RequestedStartDate, and @RequestedEndDate to avoid SQL injection and
        /// ensure correct filtering.</remarks>
        public const string GetAvailableGamesForDateRange = @"
        SELECT
            g.game_id,
            g.name,
            g.price,
            g.minimum_player_number,
            g.maximum_player_number,
            g.description,
            g.image,
            g.is_active,
            g.owner_id,

            u.user_id,
            u.username,
            u.display_name,
            u.email,
            u.phone_number,
            u.avatar_url,
            u.is_suspended,
            u.created_at,
            u.updated_at,
            u.street_name,
            u.street_number,
            u.city,
            u.country
        FROM dbo.Games g
        INNER JOIN dbo.Users u ON g.owner_id = u.user_id
        WHERE g.is_active = 1
            AND g.owner_id <> @UserId
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.Rentals r
              WHERE r.game_id = g.game_id
                AND r.start_date < @RequestedEndDate
                AND r.end_date > @RequestedStartDate
          );";

        /// <summary>
        /// Represents the SQL query used to search for available games that are active, not owned by the requesting
        /// user, and not currently rented during the specified date range.
        /// </summary>
        /// <remarks>The query filters games based on optional title and city parameters, and excludes
        /// games that are already rented within the requested period. It returns both game and owner user details for
        /// each matching game.</remarks>
        public const string SearchAvailableGames = @"
        SELECT
            g.game_id,
            g.name,
            g.price,
            g.minimum_player_number,
            g.maximum_player_number,
            g.description,
            g.image,
            g.is_active,
            g.owner_id,

            u.user_id,
            u.username,
            u.display_name,
            u.email,
            u.phone_number,
            u.avatar_url,
            u.is_suspended,
            u.created_at,
            u.updated_at,
            u.street_name,
            u.street_number,
            u.city,
            u.country
        FROM dbo.Games g
        INNER JOIN dbo.Users u ON g.owner_id = u.user_id
        WHERE g.is_active = 1
          AND g.owner_id <> @UserId
          AND (@Title IS NULL OR g.name LIKE '%' + @Title + '%')
          AND (@City IS NULL OR u.city = @City)
          AND NOT EXISTS
          (
              SELECT 1
              FROM dbo.Rentals r
              WHERE r.game_id = g.game_id
                AND r.start_date < @RequestedEndDate
                AND r.end_date > @RequestedStartDate
          );";

        /// <summary>
        /// Represents the SQL query used to search for available games with optional filters such as title, city,
        /// maximum price, player count, and requested rental dates.
        /// </summary>
        /// <remarks>This query returns game details along with their owners' user information, excluding
        /// games owned by the requesting user and games that are not active. It also filters out games that are already
        /// rented during the requested date range, if provided. The query is intended for use with parameterized SQL
        /// commands to prevent SQL injection and to allow flexible filtering.</remarks>
        public const string SearchAvailableGamesWithFilters = @"
        SELECT
            g.game_id,
            g.name,
            g.price,
            g.minimum_player_number,
            g.maximum_player_number,
            g.description,
            g.image,
            g.is_active,
            g.owner_id,

            u.user_id,
            u.username,
            u.display_name,
            u.email,
            u.phone_number,
            u.avatar_url,
            u.is_suspended,
            u.created_at,
            u.updated_at,
            u.street_name,
            u.street_number,
            u.city,
            u.country
        FROM dbo.Games g
        INNER JOIN dbo.Users u ON g.owner_id = u.user_id
        WHERE g.is_active = 1
          AND g.owner_id <> @UserId
          AND (@Title IS NULL OR g.name LIKE '%' + @Title + '%')
          AND (@City IS NULL OR u.city = @City)
          AND (@MaxPrice IS NULL OR g.price <= @MaxPrice)
          AND (@PlayerCount IS NULL OR @PlayerCount BETWEEN g.minimum_player_number AND g.maximum_player_number)
          AND (
            @RequestedStartDate IS NULL
            OR @RequestedEndDate IS NULL
            OR NOT EXISTS (
                SELECT 1
                FROM dbo.Rentals r
                WHERE r.game_id = g.game_id
                AND r.start_date < @RequestedEndDate
                AND r.end_date > @RequestedStartDate
            )
        );";

        /// <summary>
        /// Represents the SQL query used to retrieve all games owned by a specific user.
        /// </summary>
        /// <remarks>The query selects all columns relevant to a game from the Games table where the owner
        /// matches the specified user. The parameter @OwnerId must be provided when executing this query to filter
        /// results by user ownership.</remarks>
        public const string GetGamesOwnedByUser = @"
        SELECT
            g.game_id,
            g.name,
            g.price,
            g.minimum_player_number,
            g.maximum_player_number,
            g.description,
            g.image,
            g.is_active,
            g.owner_id
        FROM dbo.Games g
        WHERE g.owner_id = @OwnerId;";

        /// <summary>
        /// Represents the SQL query used to retrieve active games owned by other users that have overlapping rental
        /// periods with the specified date range.
        /// </summary>
        /// <remarks>This query selects games that are currently active, not owned by the requesting user,
        /// and have at least one rental that overlaps with the requested date range. The query expects parameters for
        /// the requesting user's ID, the requested start date, and the requested end date.</remarks>
        public const string GetOtherGamesFeedByUser = @"
        SELECT
            g.game_id,
            g.name,
            g.price,
            g.minimum_player_number,
            g.maximum_player_number,
            g.description,
            g.image,
            g.is_active,
            g.owner_id
        FROM dbo.Games g
        WHERE g.is_active = 1
          AND g.owner_id <> @UserId
          AND EXISTS 
          (
              SELECT 1
              FROM dbo.Rentals r
              WHERE r.game_id = g.game_id
                AND r.start_date < @RequestedEndDate
                AND r.end_date > @RequestedStartDate
          );";
    }
}
