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
    public class GeoService : InterfaceGeographicalService
    {
        private readonly Dictionary<string, City> _lookup = new();

        private const int MinPopulation = 5000;
        private const int MinColumns = 15;
        private const string FeatureClassSettlement = "P";
        private const string FeatureClassCapital = "PPLC";

        private const int ColName = 1;
        private const int ColAsciiName = 2;
        private const int ColAlternateNames = 3;
        private const int ColLatitude = 4;
        private const int ColLongitude = 5;
        private const int ColFeatureClass = 6;
        private const int ColPopulation = 14;

        public GeoService() {}

        public static async Task<GeoService> LoadAsync()
        {
            var service = new GeoService();
            await service.InitializeAsync();
            return service;
        }

        public async Task InitializeAsync()
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(
                new Uri("ms-appx:///Assets/RO.txt"));

            var lines = await FileIO.ReadLinesAsync(file);

            foreach (var line in lines)
            {
                var parts = line.Split('\t');

                if (parts.Length < MinColumns) continue;

                var featureClass = parts[ColFeatureClass];
                if (featureClass != FeatureClassSettlement && featureClass != FeatureClassCapital) continue;

                long.TryParse(parts[ColPopulation], out var pop);
                if (pop < MinPopulation) continue;

                var name = parts[ColName];
                var name2 = parts[ColAsciiName];
                var alternateNames = parts[ColAlternateNames];

                if (!double.TryParse(parts[ColLatitude], NumberStyles.Any, CultureInfo.InvariantCulture, out var lat)) continue;
                if (!double.TryParse(parts[ColLongitude], NumberStyles.Any, CultureInfo.InvariantCulture, out var lon)) continue;

                var city = new City
                {
                    MainName = name,
                    Latitude = lat,
                    Longitude = lon,
                    Names = new List<string>(),
                };

                AddCityName(city, name);
                AddCityName(city, name2);

                if (name2.Trim().Equals("Bucuresti", StringComparison.OrdinalIgnoreCase))
                {
                    AddCityName(city, "Bucharest");
                }

                if (!string.IsNullOrWhiteSpace(alternateNames))
                {
                    foreach (var alt in alternateNames.Split(','))
                    {
                        AddCityName(city, alt);
                    }
                }
            }
        }

        private void AddCityName(City city, string rawName)
        {
            var normalized = Normalize(rawName);

            if (string.IsNullOrWhiteSpace(normalized))
                return;

            city.Names.Add(normalized);

            if (!_lookup.ContainsKey(normalized))
            {
                _lookup[normalized] = city;
            }
        }

        public (bool found, string name, double lat, double lon) GetCityDetails(string cityName)
        {
            var key = Normalize(cityName);

            if (_lookup.TryGetValue(key, out var city))
            {
                return (true, city.MainName, city.Latitude, city.Longitude);
            }

            return (false, "", 0, 0);
        }

        private string Normalize(string city)
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

        public double? GetDistanceBetweenCities(string city1, string city2)
        {
            var firstCity = GetCityDetails(city1);
            var secondCity = GetCityDetails(city2);

            if (!firstCity.found || !secondCity.found)
            {
                return null;
            }

            return GeographicDistance.CalculateDistance(
                firstCity.lat, firstCity.lon,
                secondCity.lat, secondCity.lon);
        }

        public List<string> GetCitySuggestions(string partialName)
        {
            if (string.IsNullOrWhiteSpace(partialName))
                return new List<string>();

            var normalizedInput = Normalize(partialName);

            return _lookup
                .Where(kvp => kvp.Key.Contains(normalizedInput))
                .Select(kvp => kvp.Value.MainName)
                .Distinct()
                .Take(10)
                .ToList();
        }
    }
}
