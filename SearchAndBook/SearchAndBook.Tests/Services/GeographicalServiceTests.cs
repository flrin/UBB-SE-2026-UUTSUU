using SearchAndBook.Domain;
using SearchAndBook.Services;
using System.Reflection;

namespace SearchAndBook.Tests.Services;

public class GeographicalServiceTests
{

    [Fact]
    public void GetCityDetails_CityExists_ReturnsTrueWithCorrectCoordinates()
    {
        var sut = CreateSutWithCities(
            CreateCity("Cluj-Napoca", 46.7712, 23.6236, normalizedKey: "cluj napoca"));

        var result = sut.GetCityDetails("Cluj-Napoca");

        Assert.True(result.isFound);
        Assert.Equal("Cluj-Napoca", result.cityName);
        Assert.Equal(46.7712, result.latitude);
        Assert.Equal(23.6236, result.longitude);
    }

    [Fact]
    public void GetCityDetails_CityDoesNotExist_ReturnsFalseWithEmptyValues()
    {
        var sut = CreateSutWithCities();

        var result = sut.GetCityDetails("NonExistentCity");

        Assert.False(result.isFound);
        Assert.Equal("", result.cityName);
        Assert.Equal(0, result.latitude);
        Assert.Equal(0, result.longitude);
    }

    [Fact]
    public void GetCityDetails_CityNameWithDiacritics_NormalizesAndFindsCity()
    {
        var sut = CreateSutWithCities(
            CreateCity("Timișoara", 45.7489, 21.2087, normalizedKey: "timisoara"));

        var result = sut.GetCityDetails("Timișoara");

        Assert.True(result.isFound);
        Assert.Equal("Timișoara", result.cityName);
    }

    [Fact]
    public void GetCityDetails_CityNameWithHyphen_NormalizesHyphenToSpaceAndFindsCity()
    {
        var sut = CreateSutWithCities(
            CreateCity("Cluj-Napoca", 46.7712, 23.6236, normalizedKey: "cluj napoca"));

        var result = sut.GetCityDetails("Cluj-Napoca");

        Assert.True(result.isFound);
        Assert.Equal("Cluj-Napoca", result.cityName);
    }

    [Fact]
    public void GetCityDetails_CityNameWithMixedCase_NormalizesAndFindsCity()
    {
        var sut = CreateSutWithCities(
            CreateCity("Brașov", 45.6427, 25.5887, normalizedKey: "brasov"));

        var result = sut.GetCityDetails("BRASOV");

        Assert.True(result.isFound);
        Assert.Equal("Brașov", result.cityName);
    }

    [Fact]
    public void GetCityDetails_CityNameWithLeadingAndTrailingWhitespace_NormalizesAndFindsCity()
    {
        var sut = CreateSutWithCities(
            CreateCity("Iași", 47.1585, 27.6014, normalizedKey: "iasi"));

        var result = sut.GetCityDetails("  Iași  ");

        Assert.True(result.isFound);
        Assert.Equal("Iași", result.cityName);
    }

    [Fact]
    public void GetDistanceBetweenCities_BothCitiesExist_ReturnsPositiveDistance()
    {
        var sut = CreateSutWithCities(
            CreateCity("Cluj-Napoca", 46.7712, 23.6236, normalizedKey: "cluj napoca"),
            CreateCity("București", 44.4268, 26.1025, normalizedKey: "bucuresti"));

        var result = sut.GetDistanceBetweenCities("Cluj-Napoca", "București");

        Assert.NotNull(result);
        Assert.True(result > 0);
    }

    [Fact]
    public void GetDistanceBetweenCities_SameCity_ReturnsZeroOrNearZero()
    {
        var sut = CreateSutWithCities(
            CreateCity("Cluj-Napoca", 46.7712, 23.6236, normalizedKey: "cluj napoca"));

        var result = sut.GetDistanceBetweenCities("Cluj-Napoca", "Cluj-Napoca");

        Assert.NotNull(result);
        Assert.Equal(0, result!.Value, precision: 5);
    }

    [Fact]
    public void GetDistanceBetweenCities_OriginCityNotFound_ReturnsNull()
    {
        var sut = CreateSutWithCities(
            CreateCity("București", 44.4268, 26.1025, normalizedKey: "bucuresti"));

        var result = sut.GetDistanceBetweenCities("UnknownCity", "București");

        Assert.Null(result);
    }

    [Fact]
    public void GetDistanceBetweenCities_DestinationCityNotFound_ReturnsNull()
    {
        var sut = CreateSutWithCities(
            CreateCity("Cluj-Napoca", 46.7712, 23.6236, normalizedKey: "cluj napoca"));

        var result = sut.GetDistanceBetweenCities("Cluj-Napoca", "UnknownCity");

        Assert.Null(result);
    }

    [Fact]
    public void GetDistanceBetweenCities_BothCitiesNotFound_ReturnsNull()
    {
        var sut = CreateSutWithCities();

        var result = sut.GetDistanceBetweenCities("UnknownA", "UnknownB");

        Assert.Null(result);
    }


    [Fact]
    public void GetCitySuggestions_EmptyString_ReturnsEmptyList()
    {
        var sut = CreateSutWithCities(
            CreateCity("Cluj-Napoca", 46.7712, 23.6236, normalizedKey: "cluj napoca"));

        var result = sut.GetCitySuggestions("");

        Assert.Empty(result);
    }

    [Fact]
    public void GetCitySuggestions_WhitespaceOnly_ReturnsEmptyList()
    {
        var sut = CreateSutWithCities(
            CreateCity("Cluj-Napoca", 46.7712, 23.6236, normalizedKey: "cluj napoca"));

        var result = sut.GetCitySuggestions("   ");

        Assert.Empty(result);
    }

    [Fact]
    public void GetCitySuggestions_MatchingPartialName_ReturnsMatchingCityNames()
    {
        var sut = CreateSutWithCities(
            CreateCity("Cluj-Napoca", 46.7712, 23.6236, normalizedKey: "cluj napoca"),
            CreateCity("Cluj-Mănăștur", 46.7600, 23.5700, normalizedKey: "cluj manastur"));

        var result = sut.GetCitySuggestions("cluj");

        Assert.Equal(2, result.Count);
        Assert.Contains("Cluj-Napoca", result);
        Assert.Contains("Cluj-Mănăștur", result);
    }

    [Fact]
    public void GetCitySuggestions_NoMatchingCities_ReturnsEmptyList()
    {
        var sut = CreateSutWithCities(
            CreateCity("Cluj-Napoca", 46.7712, 23.6236, normalizedKey: "cluj napoca"));

        var result = sut.GetCitySuggestions("xyz");

        Assert.Empty(result);
    }

    [Fact]
    public void GetCitySuggestions_MoreThanTenMatches_ReturnsOnlyTen()
    {
        var cities = Enumerable.Range(1, 15)
            .Select(i => CreateCity($"City{i}", 0, 0, normalizedKey: $"city{i}"))
            .ToArray();

        var sut = CreateSutWithCities(cities);

        var result = sut.GetCitySuggestions("city");

        Assert.Equal(10, result.Count);
    }

    [Fact]
    public void GetCitySuggestions_MultipleLookupKeysPointToSameCity_ReturnsCityOnlyOnce()
    {
        var city = CreateCity("Cluj-Napoca", 46.7712, 23.6236);
        var sut = CreateSutWithCities();

        InjectCities(sut, new Dictionary<string, City>
        {
            ["cluj napoca"] = city,
            ["klausenburg"]  = city
        });

        var result = sut.GetCitySuggestions("u"); 

        Assert.Single(result);
        Assert.Equal("Cluj-Napoca", result[0]);
    }
    private static GeographicalService CreateSutWithCities(params City[] cities)
    {
        var sut = new GeographicalService();

        var lookup = new Dictionary<string, City>();
        foreach (var city in cities)
        {
            var key = city.Names.FirstOrDefault() ?? city.MainName.ToLower();
            lookup[key] = city;
        }

        InjectCities(sut, lookup);
        return sut;
    }

    private static void InjectCities(GeographicalService service, Dictionary<string, City> lookup)
    {
        var field = typeof(GeographicalService)
            .GetField("_cityLookupByNormalizedName", BindingFlags.NonPublic | BindingFlags.Instance)!;

        var existing = (Dictionary<string, City>)field.GetValue(service)!;
        foreach (var kvp in lookup)
            existing[kvp.Key] = kvp.Value;
    }

    private static City CreateCity(
        string mainName,
        double latitude,
        double longitude,
        string? normalizedKey = null)
    {
        var key = normalizedKey ?? mainName.ToLower();
        return new City
        {
            MainName = mainName,
            Latitude = latitude,
            Longitude = longitude,
            Names = new List<string> { key }
        };
    }
}
