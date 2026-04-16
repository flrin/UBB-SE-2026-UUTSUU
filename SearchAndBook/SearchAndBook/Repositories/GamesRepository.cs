namespace SearchAndBook.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using SearchAndBook.Domain;
using SearchAndBook.Repositories.Sql;
using SearchAndBook.Shared;

/// <summary>
/// Repository responsible for reading game/listing data from the database.
/// Important:
/// - This repository only reads data.
/// - It is used by the service layer, not directly by the UI.
/// How ADO.NET handles connections : 
/// - When you write using var connection = new SqlConnection(...) and call .Open(), Microsoft checks the pool, so the pool of connections is handled by .net
/// - If there is a free connection, it gives it to you.
/// - When your "using" block finishes, it calls .Close().
/// - Microsoft intercepts your .Close() command. It doesn't actually destroy the connection to the database. It just wipes the data clean and parks it back in the hidden pool for.  the next person to use.
/// </summary>
public class GamesRepository : InterfaceGamesRepository
{
    /// <summary>
    /// Represents the ID used for unauthenticated users.
    /// </summary>
    public const int AnonimousUserId = -1;

    // Used to convert game data to Game object
    private static Game MapGame(SqlDataReader reader)
    {
        return new Game
        {
            GameId = Convert.ToInt32(reader["game_id"]),
            Name = Convert.ToString(reader["name"]) ?? string.Empty,
            Price = Convert.ToDecimal(reader["price"]),
            MinimumPlayerNumber = Convert.ToInt32(reader["minimum_player_number"]),
            MaximumPlayerNumber = Convert.ToInt32(reader["maximum_player_number"]),
            Description = Convert.ToString(reader["description"]) ?? string.Empty,
            Image = reader["image"] == DBNull.Value ? null : (byte[])reader["image"],
            IsActive = Convert.ToBoolean(reader["is_active"]),
            OwnerId = Convert.ToInt32(reader["owner_id"]),
        };
    }

    /// Gets a single game by its database id.
    /// <param name="id">The unique id of the game.</param>
    /// <remarks>
    /// Use this when you already know the exact game id and need full game details.
    public Game? GetGameById(int id)
    {
        try
        {
            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();

            using var command = new SqlCommand(GameQueries.GetGameById, connection);
            command.Parameters.AddWithValue("@GameId", id);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return MapGame(reader);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Gets all active games that are visible in the system.
    /// </summary>
   
    public List<Game> GetAllGames()
    {
        try
        {
            return GetAllActiveGames(AnonimousUserId);
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Gets games that match the provided filter criteria.
    /// </summary>
    /// <param name="filter">
    /// Object containing user-entered search/filter values.
    /// All fields may be empty/null.
    /// </param>
    /// <returns>A list of games matching the filter.</returns>
    /// <remarks>
    /// Use this for:
    /// - search page
    /// - filter panel
    /// - search + filters combined
    /// Behavior:
    /// - null/empty fields are ignored
    /// - only active games are returned
    /// - user's own games are excluded if UserId is provided
    /// - if an availability range is provided, only games available in that range are returned
    /// </remarks>
    public List<Game> GetGamesByFilter(FilterCriteria filter)
    {
        try
        {
            var games = new List<Game>();

            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();

            using var command = new SqlCommand(GameQueries.SearchAvailableGamesWithFilters, connection);
            command.Parameters.AddWithValue("@Title", string.IsNullOrWhiteSpace(filter.Name) ? DBNull.Value : filter.Name);
            command.Parameters.AddWithValue("@City", string.IsNullOrWhiteSpace(filter.City) ? DBNull.Value : filter.City);
            command.Parameters.AddWithValue("@MaxPrice", filter.MaximumPrice.HasValue ? filter.MaximumPrice.Value : DBNull.Value);
            command.Parameters.AddWithValue("@PlayerCount", filter.PlayerCount.HasValue ? filter.PlayerCount.Value : DBNull.Value);
            command.Parameters.AddWithValue("@RequestedStartDate",filter.AvailabilityRange != null ? filter.AvailabilityRange.StartTime : DBNull.Value);
            command.Parameters.AddWithValue("@RequestedEndDate",filter.AvailabilityRange != null ? filter.AvailabilityRange.EndTime : DBNull.Value);
            command.Parameters.AddWithValue("@UserId", filter.UserId.HasValue ? filter.UserId.Value : -1);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                games.Add(MapGame(reader));
            }

            return games;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Gets games that are available starting today and continuing through tomorrow.
    /// </summary>
    /// <param name="userId">
    /// Current authenticated user id OR -1 for user not logged in. 
    /// Used to exclude the user's own games from the feed.
    /// </param>
    /// <returns>A list of games for the "Available Tonight" section.</returns>
    public List<Game> GetGamesForFeedAvailableTonight(int userId)
    {
        try
        {
            var games = new List<Game>();

            var todayDate = DateTime.Today;
            int daysAdded = 1;
            var tomorrowDate = todayDate.AddDays(daysAdded);

            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();

            using var command = new SqlCommand(GameQueries.GetAvailableGamesForDateRange, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@RequestedStartDate", todayDate);
            command.Parameters.AddWithValue("@RequestedEndDate", tomorrowDate);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                games.Add(MapGame(reader));
            }

            return games;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Gets all remaining active games that are not part of the "Available Tonight" section.
    /// </summary>
    /// <param name="userId">
    /// Current authenticated user id.
    /// Used to exclude the user's own games from the feed.
    /// </param>
    /// <returns>A list of games for the "Available Tonight" section.</returns>
    public List<Game> GetGamesForFeedOthers(int userId)
    {
        try
        {
            var games = new List<Game>();

            var todayDate = DateTime.Today;
            var tomorrowDate = todayDate.AddDays(1);

            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();

            using var command = new SqlCommand(GameQueries.GetOtherGamesFeedByUser, connection);
            command.Parameters.AddWithValue("@UserId", userId);
            command.Parameters.AddWithValue("@RequestedStartDate", todayDate);
            command.Parameters.AddWithValue("@RequestedEndDate", tomorrowDate);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                games.Add(MapGame(reader));
            }

            return games;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// Gets all active games from the database.
    /// <param name="userId">
    /// Current authenticated user id OR -1 for user not logged in. 
    /// Used to exclude the user's own games from the results.
    /// </param>
    /// <returns>A list of all active games.</returns>
    private List<Game> GetAllActiveGames(int userId)
    {
        try
        {
            var games = new List<Game>();

            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();

            using var command = new SqlCommand(GameQueries.GetAllActiveGamesWithOwner, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                games.Add(MapGame(reader));
            }

            return games;
        }
        catch (Exception)
        {
            throw;
        }
    }
}