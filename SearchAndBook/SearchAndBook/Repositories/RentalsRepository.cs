namespace SearchAndBook.Repositories;
using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using SearchAndBook.Domain;
using SearchAndBook.Repositories.Sql;
using SearchAndBook.Shared;

// How ADO.NET handles connections :
// - When you write using var connection = new SqlConnection(...) and call .Open(), Microsoft
// checks the pool, so the pool of connections is handled by .net
// - If there is a free connection, it gives it to you.
// - When your "using" block finishes, it calls .Close().
// - Microsoft intercepts your .Close() command. It doesn't actually destroy the connection
// to the database. It just wipes the data clean and parks it back in the hidden pool for
// the next person to use.

/// <summary>
/// Repository for managing rental data.
/// </summary>
public class RentalsRepository : InterfaceRentalsRepository
{
    /// <summary>
    /// Retrieves a rental time range by its unique identifier.
    /// </summary>
    /// <param name="id">The rental identifier.</param>
    /// <returns>The rental time range if isfound; otherwise, null.</returns>
    public TimeRange? GetGameById(int id)
    {
        try
        {
            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();

            using var command = new SqlCommand(RentalQueries.GetRentalRangeById, connection);
            command.Parameters.AddWithValue("@RentalId", id);

            using var reader = command.ExecuteReader();

            if (!reader.Read())
            {
                return null;
            }

            return new TimeRange(
                Convert.ToDateTime(reader["start_date"]),
                Convert.ToDateTime(reader["end_date"]));
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Retrieves all rental time rentaltimeranges.
    /// </summary>
    /// <returns>A list of all rental time rentaltimeranges.</returns>
    public List<TimeRange> GetAll()
    {
        try
        {
            var rentalTimeRanges = new List<TimeRange>();

            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();

            using var command = new SqlCommand(RentalQueries.GetAllRentalRanges, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                rentalTimeRanges.Add(new TimeRange(
                    Convert.ToDateTime(reader["start_date"]),
                    Convert.ToDateTime(reader["end_date"])));
            }

            return rentalTimeRanges;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Retrieves unavailable rental time rentaltimeranges for a specific game.
    /// </summary>
    /// <param name="gameId">The game identifier.</param>
    /// <returns>A list of time rentaltimeranges when the game is unavailable.</returns>
    public List<TimeRange> GetUnavailableTimeRanges(int gameId)
    {
        try
        {
            var rentalTimeRanges = new List<TimeRange>();

            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();

            using var command = new SqlCommand(RentalQueries.GetUnavailablePeriodsByGameId, connection);
            command.Parameters.AddWithValue("@GameId", gameId);

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var start = Convert.ToDateTime(reader["start_date"]);
                var end = Convert.ToDateTime(reader["end_date"]);

                rentalTimeRanges.Add(new TimeRange(start, end));
            }

            return rentalTimeRanges;
        }
        catch (Exception)
        {
            throw;
        }
    }

    /// <summary>
    /// Checks if a game is available for a specified time range.
    /// </summary>
    /// <param name="range">The requested rental time range.</param>
    /// <param name="gameId">The game identifier.</param>
    /// <returns>True if the game is available; otherwise, false.</returns>
    public bool CheckGameAvailability(TimeRange range, int gameId)
    {
        try
        {
            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();

            using var command = new SqlCommand(RentalQueries.HasOverlappingRental, connection);
            command.Parameters.AddWithValue("@GameId", gameId);
            command.Parameters.AddWithValue("@RequestedStartDate", range.StartTime);
            command.Parameters.AddWithValue("@RequestedEndDate", range.EndTime);

            var result = command.ExecuteScalar();

            return result == null;
        }
        catch (Exception)
        {
            throw;
        }
    }
}