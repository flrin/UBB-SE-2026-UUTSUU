using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SearchAndBook.Services
{
    public interface InterfaceGeographicalService
    {
        // Initializes the service by reading the text file
        Task InitializeAsync();

        // Gets the coordinates and official name of a single city
        (bool found, string name, double lat, double lon) GetCityDetails(string cityName);

        // Calculates the distance between two cities, returns null if either is missing
        double? GetDistanceBetweenCities(string city1, string city2);

        public List<string> GetCitySuggestions(string partialName);

    }
}