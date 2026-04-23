namespace SearchAndBook.Services
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using SearchAndBook.Domain;
    using SearchAndBook.Utils;
    using Windows.Storage;

    /// <summary>
    /// GeographicalService is responsible for loading city data from a text file, providing details about cities, calculating distances between cities, and offering city name suggestions based on partial input. It processes the city data to create a lookup for efficient retrieval of city information and handles normalization of city names to improve search accuracy.
    /// </summary>
    public class GeographicalService : InterfaceGeographicalService
    {
        private const int MinimumCityPopulation = 5000;
        private const int MinimumRequiredColumns = 15;
        private const string FeatureClassPopulatedPlace = "P";
        private const string FeatureClassCapitalCity = "PPLC";
        private const double DefaultCoordinateValue = 0;
        private const string EmptyCityName = "";
        private const int MaximumCitySuggestions = 10;

        private const int ColumnIndexName = 1;
        private const int ColumnIndexAsciiName = 2;
        private const int ColumnIndexAlternateNames = 3;
        private const int ColumnIndexLatitude = 4;
        private const int ColumnIndexLongitude = 5;
        private const int ColumnIndexFeatureClass = 6;
        private const int ColumnIndexPopulation = 14;
        private readonly Dictionary<string, City> cityLookupByNormalizedName = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="GeographicalService"/> class.
        /// </summary>
        public GeographicalService()
        {
        }

        /// <summary>
        /// Asynchronously creates a new instance of the GeographicalService class and loads city data from a file.
        /// </summary>
        /// <remarks>This method initializes a new GeographicalService and loads city data before
        /// returning the instance. The file source and loading behavior are determined by the implementation of
        /// LoadCitiesFromFileAsync. Callers should await the returned task to ensure the data is loaded before using
        /// the service.</remarks>
        /// <returns>A task that represents the asynchronous operation. The task result contains a GeographicalService instance
        /// with city data loaded from the file.</returns>
        public static async Task<GeographicalService> LoadFromFileAsync()
        {
            var service = new GeographicalService();
            await service.LoadCitiesFromFileAsync();
            return service;
        }

        /// <summary>
        /// Asynchronously loads city data from a bundled text file and adds qualifying cities to the collection.
        /// </summary>
        /// <remarks>Only cities classified as populated places or capital cities with a population above
        /// the minimum threshold are included. The method reads from a file located at 'Assets/RO.txt' within the
        /// application package. City aliases are added for each city, including alternate names and special handling
        /// for the capital city. This method does not clear existing cities before loading new ones.</remarks>
        /// <returns>A task that represents the asynchronous load operation.</returns>
        public async Task LoadCitiesFromFileAsync()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/RO.txt"));

            var lines = await FileIO.ReadLinesAsync(file);

            foreach (var line in lines)
            {
                var columns = line.Split('\t');

                if (columns.Length < MinimumRequiredColumns)
                {
                    continue;
                }

                var featureClass = columns[ColumnIndexFeatureClass];
                if (featureClass != FeatureClassPopulatedPlace && featureClass != FeatureClassCapitalCity)
                {
                    continue;
                }

                long.TryParse(columns[ColumnIndexPopulation], out var population);
                if (population < MinimumCityPopulation)
                {
                    continue;
                }

                var primaryCityName = columns[ColumnIndexName];
                var asciiCityName = columns[ColumnIndexAsciiName];
                var alternateCityNames = columns[ColumnIndexAlternateNames];

                if (!double.TryParse(columns[ColumnIndexLatitude], NumberStyles.Any, CultureInfo.InvariantCulture, out var latitude))
                {
                    continue;
                }

                if (!double.TryParse(columns[ColumnIndexLongitude], NumberStyles.Any, CultureInfo.InvariantCulture, out var longitude))
                {
                    continue;
                }

                var city = new City
                {
                    MainName = primaryCityName,
                    Latitude = latitude,
                    Longitude = longitude,
                    Names = new List<string>(),
                };

                this.AddCityAlias(city, primaryCityName);
                this.AddCityAlias(city, asciiCityName);

                if (asciiCityName.Trim().Equals("Bucuresti", StringComparison.OrdinalIgnoreCase))
                {
                    this.AddCityAlias(city, "Bucharest");
                }

                if (!string.IsNullOrWhiteSpace(alternateCityNames))
                {
                    foreach (var alternate in alternateCityNames.Split(','))
                    {
                        this.AddCityAlias(city, alternate);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves details for a specified city, including its name and geographic coordinates.
        /// </summary>
        /// <param name="cityName">The name of the city to look up. The search is case-insensitive and may normalize the input for matching.
        /// Cannot be null.</param>
        /// <returns>A tuple containing a value indicating whether the city was found, the city's main name, and its latitude and
        /// longitude. If the city is not found, returns (<see langword="false"/>, an empty string, and default
        /// coordinate values).</returns>
        public (bool isFound, string cityName, double latitude, double longitude) GetCityDetails(string cityName)
        {
            var normalizedCityName = this.NormalizeCityName(cityName);

            if (this.cityLookupByNormalizedName.TryGetValue(normalizedCityName, out var city))
            {
                return (true, city.MainName, city.Latitude, city.Longitude);
            }

            return (false, EmptyCityName, DefaultCoordinateValue, DefaultCoordinateValue);
        }

        /// <summary>
        /// Calculates the geographic distance between two cities, specified by their names.
        /// </summary>
        /// <param name="originCityName">The name of the origin city. Cannot be null or empty.</param>
        /// <param name="destinationCityName">The name of the destination city. Cannot be null or empty.</param>
        /// <returns>The distance in kilometers between the origin and destination cities, or null if either city cannot be
        /// found.</returns>
        public double? GetDistanceBetweenCities(string originCityName, string destinationCityName)
        {
            var originCityDetails = this.GetCityDetails(originCityName);
            var destinationCityDetails = this.GetCityDetails(destinationCityName);

            if (!originCityDetails.isFound || !destinationCityDetails.isFound)
            {
                return null;
            }

            return GeographicDistance.CalculateDistance(
                originCityDetails.latitude,
                originCityDetails.longitude,
                destinationCityDetails.latitude,
                destinationCityDetails.longitude);
        }

        /// <summary>
        /// Returns a list of city name suggestions that match the specified partial city name.
        /// </summary>
        /// <remarks>The number of suggestions returned is limited by the MaximumCitySuggestions value.
        /// The search is case-insensitive and matches any city name containing the normalized partial name.</remarks>
        /// <param name="partialName">The partial city name to search for. Cannot be null, empty, or consist only of white-space characters.</param>
        /// <returns>A list of city names that contain the specified partial name. Returns an empty list if no matches are found
        /// or if the input is null or white space.</returns>
        public List<string> GetCitySuggestions(string partialName)
        {
            if (string.IsNullOrWhiteSpace(partialName))
            {
                return new List<string>();
            }

            var normalizedPartialName = this.NormalizeCityName(partialName);

            return this.cityLookupByNormalizedName
                .Where(cityLookupEntry => cityLookupEntry.Key.Contains(normalizedPartialName))
                .Select(cityLookupEntry => cityLookupEntry.Value.MainName)
                .Distinct()
                .Take(MaximumCitySuggestions)
                .ToList();
        }

        private void AddCityAlias(City city, string originalCityName)
        {
            var normalizedcityname = this.NormalizeCityName(originalCityName);

            if (string.IsNullOrWhiteSpace(normalizedcityname))
            {
                return;
            }

            city.Names.Add(normalizedcityname);

            if (!this.cityLookupByNormalizedName.ContainsKey(normalizedcityname))
            {
                this.cityLookupByNormalizedName[normalizedcityname] = city;
            }
        }

        private string NormalizeCityName(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
            {
                return string.Empty;
            }

            return city
                .Trim()
                .ToLower()
                .Replace("-", " ")
                .Replace("ă", "a")
                .Replace("â", "a")
                .Replace("î", "i")
                .Replace("ș", "s")
                .Replace("ţ", "t")
                .Replace("ț", "t");
        }
    }
}