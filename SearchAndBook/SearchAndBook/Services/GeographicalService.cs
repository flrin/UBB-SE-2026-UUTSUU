using SearchAndBook.Domain;
using SearchAndBook.Utils;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace SearchAndBook.Services
{
    public class GeographicalService : InterfaceGeographicalService
    {
        private readonly Dictionary<string, City> _cityLookupByNormalizedName = new();

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

        public GeographicalService() {}

        public static async Task<GeographicalService> LoadFromFileAsync()
        {
            var service = new GeographicalService();
            await service.LoadCitiesFromFileAsync();
            return service;
        }

        public async Task LoadCitiesFromFileAsync()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/RO.txt"));

            var lines = await FileIO.ReadLinesAsync(file);

            foreach (var line in lines)
            {
                var columns = line.Split('\t');

                if (columns.Length < MinimumRequiredColumns) continue;

                var featureClass = columns[ColumnIndexFeatureClass];
                if (featureClass != FeatureClassPopulatedPlace && featureClass != FeatureClassCapitalCity) continue;

                long.TryParse(columns[ColumnIndexPopulation], out var population);
                if (population < MinimumCityPopulation) continue;

                var primaryCityName = columns[ColumnIndexName];
                var asciiCityName = columns[ColumnIndexAsciiName];
                var alternateCityNames = columns[ColumnIndexAlternateNames];

                if (!double.TryParse(columns[ColumnIndexLatitude], NumberStyles.Any, CultureInfo.InvariantCulture, out var latitude)) continue;
                if (!double.TryParse(columns[ColumnIndexLongitude], NumberStyles.Any, CultureInfo.InvariantCulture, out var longitude)) continue;

                var city = new City
                {
                    MainName = primaryCityName,
                    Latitude = latitude,
                    Longitude = longitude,
                    Names = new List<string>(),
                };

                AddCityAlias(city, primaryCityName);
                AddCityAlias(city, asciiCityName);

                if (asciiCityName.Trim().Equals("Bucuresti", StringComparison.OrdinalIgnoreCase))
                {
                    AddCityAlias(city, "Bucharest");
                }

                if (!string.IsNullOrWhiteSpace(alternateCityNames))
                {
                    foreach (var alternate in alternateCityNames.Split(','))
                    {
                        AddCityAlias(city, alternate);
                    }
                }
            }
        }

        private void AddCityAlias(City city, string originalCityName)
        {
            var normalizedcityname = NormalizeCityName(originalCityName);

            if (string.IsNullOrWhiteSpace(normalizedcityname))
                return;

            city.Names.Add(normalizedcityname);

            if (!_cityLookupByNormalizedName.ContainsKey(normalizedcityname))
            {
                _cityLookupByNormalizedName[normalizedcityname] = city;
            }
        }

        public (bool isFound, string cityName, double latitude, double longitude) GetCityDetails(string cityName)
        {
            var normalizedCityName = NormalizeCityName(cityName);

            if (_cityLookupByNormalizedName.TryGetValue(normalizedCityName, out var city))
            {
                return (true, city.MainName, city.Latitude, city.Longitude);
            }

            return (false, EmptyCityName, DefaultCoordinateValue, DefaultCoordinateValue);
        }

        private string NormalizeCityName(string city)
        {
            if (string.IsNullOrWhiteSpace(city))
                return string.Empty;

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

        public double? GetDistanceBetweenCities(string originCityName, string destinationCityName)
        {
            var originCityDetails = GetCityDetails(originCityName);
            var destinationCityDetails = GetCityDetails(destinationCityName);

            if (!originCityDetails.isFound || !destinationCityDetails.isFound)
            {
                return null;
            }

            return GeographicDistance.CalculateDistance(
                originCityDetails.latitude, originCityDetails.longitude,
                destinationCityDetails.latitude, destinationCityDetails.longitude);
        }


        public List<string> GetCitySuggestions(string partialName)
        {
            if (string.IsNullOrWhiteSpace(partialName))
                return new List<string>();

            var normalizedPartialName = NormalizeCityName(partialName);

            return _cityLookupByNormalizedName
                .Where(cityLookupEntry => cityLookupEntry.Key.Contains(normalizedPartialName))
                .Select(cityLookupEntry => cityLookupEntry.Value.MainName)
                .Distinct()
                .Take(MaximumCitySuggestions)
                .ToList();
        }
    }
}
