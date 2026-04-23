namespace SearchAndBook.Repositories.Sql
{
    /// <summary>
    /// Provides predefined SQL query strings for retrieving user-related data from the database.
    /// </summary>
    /// <remarks>This class contains constant SQL queries intended for use with ADO.NET or similar data access
    /// technologies. The queries are designed to select user information based on various criteria, such as user ID,
    /// username, or game ownership. All queries assume the presence of a 'Users' table in the 'dbo' schema and may
    /// require parameterization to prevent SQL injection. This class is static and cannot be instantiated.</remarks>
    public static class UserQueries
    {
        /// <summary>
        /// Represents the SQL query used to retrieve a user's details by their unique identifier.
        /// </summary>
        /// <remarks>The query selects all user fields from the Users table where the user ID matches the
        /// specified parameter. The parameterized query helps prevent SQL injection when used with a parameterized
        /// command.</remarks>
        public const string GetUserById = @"
        SELECT
            u.user_id,
            u.username,
            u.display_name,
            u.email,
            u.password_hash,
            u.phone_number,
            u.avatar_url,
            u.is_suspended,
            u.created_at,
            u.updated_at,
            u.street_name,
            u.street_number,
            u.city,
            u.country
        FROM dbo.Users u
        WHERE u.user_id = @UserId;";

        /// <summary>
        /// Represents the SQL query used to retrieve the current user's details by username.
        /// </summary>
        /// <remarks>The query selects all user fields from the Users table where the username matches the
        /// specified parameter. The parameterized query helps prevent SQL injection when used with a parameterized
        /// command.</remarks>
        public const string GetCurrentUserByUsername = @"
        SELECT
            u.user_id,
            u.username,
            u.display_name,
            u.email,
            u.password_hash,
            u.phone_number,
            u.avatar_url,
            u.is_suspended,
            u.created_at,
            u.updated_at,
            u.street_name,
            u.street_number,
            u.city,
            u.country
        FROM dbo.Users u
        WHERE u.username = @Username;";

        /// <summary>
        /// Represents the SQL query used to retrieve the owner of a game by the game's unique identifier.
        /// </summary>
        /// <remarks>The query selects user details for the owner of a game by joining the Users and Games
        /// tables on the owner's user ID. The parameter @GameId must be supplied with the target game's identifier when
        /// executing this query.</remarks>
        public const string GetGameOwnerByGameId = @"
        SELECT
            u.user_id,
            u.username,
            u.display_name,
            u.email,
            u.password_hash,
            u.phone_number,
            u.avatar_url,
            u.is_suspended,
            u.created_at,
            u.updated_at,
            u.street_name,
            u.street_number,
            u.city,
            u.country
        FROM dbo.Users u
        INNER JOIN dbo.Games g ON g.owner_id = u.user_id
        WHERE g.game_id = @GameId;";

        /// <summary>
        /// Represents the SQL query used to retrieve all user records and their associated details from the database.
        /// </summary>
        /// <remarks>The query selects all columns relevant to user information, including contact details
        /// and account status. It is intended for use when a complete list of users is required, such as for
        /// administrative dashboards or user management features.</remarks>
        public const string GetAllUsers = @"
        SELECT
            u.user_id,
            u.username,
            u.display_name,
            u.email,
            u.password_hash,
            u.phone_number,
            u.avatar_url,
            u.is_suspended,
            u.created_at,
            u.updated_at,
            u.street_name,
            u.street_number,
            u.city,
            u.country
        FROM dbo.Users u;";
    }
}