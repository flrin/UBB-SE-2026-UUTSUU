namespace SearchAndBook.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines methods for loading, retrieving, and searching city data, as well as calculating distances between
    /// cities within a geographical context.
    /// </summary>
    public interface InterfaceGeographicalService
    {
        /// <summary>
        /// Asynchronously loads city data from a file into the application.
        /// </summary>
        /// <returns>A task that represents the asynchronous load operation.</returns>
        Task LoadCitiesFromFileAsync();

        /// <summary>
        /// Retrieves details for the specified city, including its name and geographic coordinates.
        /// </summary>
        /// <param name="cityName">The name of the city for which to retrieve details. Cannot be null or empty.</param>
        /// <returns>A tuple containing a Boolean value indicating whether the city was found, the city's name, its latitude, and
        /// its longitude. If the city is not found, the tuple's values are unspecified except for the Boolean flag,
        /// which is set to false.</returns>
        (bool isFound, string cityName, double latitude, double longitude) GetCityDetails(string cityName);

        /// <summary>
        /// Calculates the distance between two specified cities.
        /// </summary>
        /// <param name="originCity">The name of the origin city. Cannot be null or empty.</param>
        /// <param name="destinationCity">The name of the destination city. Cannot be null or empty.</param>
        /// <returns>The distance between the origin and destination cities, in kilometers. Returns null if the distance cannot
        /// be determined.</returns>
        double? GetDistanceBetweenCities(string originCity, string destinationCity);

        /// <summary>
        /// Gets a list of city names that match the provided partial name for autocomplete suggestions. The search is case-insensitive and returns cities that contain the partial name anywhere in their official name.
        /// </summary>
        /// <param name="partialName">The partial name to search for.</param>
        /// <returns>A list of city names that match the partial name.</returns>
        public List<string> GetCitySuggestions(string partialName);
    }
}