namespace SearchAndBook.Shared
{
    /// <summary>
    /// Provides configuration settings for connecting to the application's database.
    /// </summary>
    internal class DatabaseConfig
    {
        /// <summary>
        /// Represents the connection string used to connect to the mock BoardGamesRent database on the local SQL Server
        /// instance.
        /// </summary>
        /// <remarks>This connection string is intended for development and testing scenarios using the
        /// localdb instance of SQL Server. It enables trusted connections and accepts self-signed server certificates.
        /// Do not use this connection string in production environments.</remarks>
        public const string ConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=BoardGamesRentMockDb;Trusted_Connection=True;TrustServerCertificate=True;";
    }
}
