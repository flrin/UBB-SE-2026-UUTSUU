namespace SearchAndBook.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.SqlClient;

/// <summary>
/// Provides methods for seeding the database with initial game image data.
/// </summary>
/// <remarks>This static class is intended for use during application setup or development to populate the
/// database with image data for predefined games. It is not intended for use in production environments. All methods
/// are static and thread safety is not guaranteed.</remarks>
public static class DatabaseSeeder
{
    /// <summary>
    /// Seeds the database with image data for predefined games by updating the image column for each game that does not
    /// already have an image.
    /// </summary>
    /// <remarks>This method locates image files in the 'Assets/SeedImages' directory relative to the project
    /// root and updates the corresponding records in the 'Games' table where the image is currently null. Only games
    /// with matching image files and without existing images are affected. The method must be run with appropriate
    /// database access and file system permissions.</remarks>
    public static void SeedGameImages()
    {
        string basePath = AppContext.BaseDirectory;
        string projectRoot = FindProjectRoot(basePath);

        string imageFolder = Path.Combine(projectRoot, "Assets", "SeedImages");

        var imageMap = new Dictionary<int, string>
        {
            { 1, "catan.png" },
            { 2, "monopoly.jpg" },
            { 3, "carcassonne.jpg" },
            { 4, "terraforming_mars.png" },
            { 5, "TicketToRide.jpg" },
            { 6, "Pandemic.jpg" },
            { 7, "7Wonders.png" },
            { 8, "Azul.jpg" },
            { 9, "Dixit.jpg" },
            { 10, "Splendor.jpg" },
            { 11, "Codenames.jpg" },
            { 12, "Risk.jpg" },
            { 13, "Dominion.jpg" },
            { 14, "LoveLetter.jpg" },
            { 15, "Scythe.jpg" },
            { 16, "Wingspan.jpg" },
            { 17, "Gloomhaven.jpg" },
            { 18, "BrassBirmingham.jpg" },
            { 19, "Root.jpg" },
            { 20, "terraforming_mars.png" },
            { 21, "ArkNova.jpg" },
            { 22, "Everdell.jpg" },
            { 23, "TheCrew.jpg" },
            { 24, "Hanabi.jpg" },
            { 25, "Agricola.jpg" },
            { 26, "Patchwork.jpg" },
            { 27, "carcassonne.jpg" },
            { 28, "Uno.jpg" },
            { 29, "ExplodingKittens.png" },
            { 30, "Bang!.png" },
            { 31, "KingOfTokyo.jpg" },
            { 32, "SheriffOfNottingham.jpg" },
            { 33, "Mysterium.jpg" },
            { 34, "Clank!.jpg" },
        };

        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        connection.Open();

        foreach (var pair in imageMap)
        {
            int gameId = pair.Key;
            string filePath = Path.Combine(imageFolder, pair.Value);

            if (!File.Exists(filePath))
            {
                continue;
            }

            byte[] imageBytes = File.ReadAllBytes(filePath);

            using var command = new SqlCommand(
                @"
                UPDATE dbo.Games
                SET image = @Image
                WHERE game_id = @GameId
                  AND image IS NULL;", connection);

            command.Parameters.AddWithValue("@Image", imageBytes);
            command.Parameters.AddWithValue("@GameId", gameId);

            command.ExecuteNonQuery();
        }
    }

    private static string FindProjectRoot(string startPath)
    {
        DirectoryInfo? dir = new DirectoryInfo(startPath);

        while (dir != null)
        {
            bool hasCsproj = dir.GetFiles("*.csproj").Length > 0;
            if (hasCsproj)
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate project root.");
    }
}