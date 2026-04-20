using Moq;
using SearchAndBook.Domain;
using SearchAndBook.Services;
using SearchAndBook.Shared;
using SearchAndBook.ViewModels;

namespace SearchAndBook.Tests.ViewModels;

public class FilteredSearchViewModelTests
{
    [Fact]
    public void LoadSearchResults_WithValidFilter_ReturnsResultsFromSearchService()
    {
        var (sut, searchService, _, errors) = CreateSut();
        var results = new[]
        {
            CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
            CreateGameDto(2, "Azul", 15m, "Iasi", 4, 2),
        };

        searchService.Setup(service => service.SearchGamesByFilter(It.IsAny<FilterCriteria>())).Returns(results);

        sut.LoadSearchResults(new FilterCriteria());

        Assert.Empty(errors);
        Assert.Equal(results, sut.BaseResults);
        Assert.Equal(results, sut.DisplayedResults);
        Assert.Equal(2, sut.VisibleGames.Count);
        Assert.False(sut.HasNoResults);
    }

    [Fact]
    public void LoadSearchResults_WhenServiceThrowsException_RaisesErrorAndClearsResults()
    {
        var (sut, searchService, _, errors) = CreateSut();
        searchService.Setup(service => service.SearchGamesByFilter(It.IsAny<FilterCriteria>())).Throws(new Exception("boom"));

        sut.LoadSearchResults(new FilterCriteria());

        Assert.Single(errors);
        Assert.Contains("Could not load search results.", errors[0]);
        Assert.Empty(sut.BaseResults);
        Assert.Empty(sut.DisplayedResults);
        Assert.Empty(sut.VisibleGames);
        Assert.True(sut.HasNoResults);
    }

    [Fact]
    public void LoadDiscoveryResults_WithValidResults_ReturnsProvidedResults()
    {
        var (sut, _, _, errors) = CreateSut();
        var results = new[] { CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2) };

        sut.LoadDiscoveryResults(results);

        Assert.Empty(errors);
        Assert.Equal(results, sut.BaseResults);
        Assert.Equal(results, sut.DisplayedResults);
        Assert.Single(sut.VisibleGames);
        Assert.False(sut.HasNoResults);
    }

    [Fact]
    public void ApplyFilters_WithValidDateRange_DelegatesToSearchService()
    {
        var (sut, searchService, _, errors) = CreateSut();
        var results = new[] { CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2) };
        sut.LoadDiscoveryResults(new[] { CreateGameDto(2, "Azul", 15m, "Iasi", 4, 2) });
        sut.CurrentFilter.AvailabilityRange = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
        searchService.Setup(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>())).Returns(results);

        sut.ApplyFilters();

        Assert.Empty(errors);
        Assert.Equal(results, sut.DisplayedResults);
        Assert.Single(sut.VisibleGames);
        searchService.Verify(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()), Times.Once);
    }

    [Fact]
    public void ApplyFilters_WithInvalidDateRange_RaisesErrorAndSkipsSearch()
    {
        var (sut, searchService, _, errors) = CreateSut();
        sut.LoadDiscoveryResults(new[] { CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2) });
        sut.CurrentFilter.AvailabilityRange = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
        searchService.Setup(service => service.IsValidDateRange(It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).Returns(false);

        sut.ApplyFilters();

        Assert.Single(errors);
        Assert.Contains("Please select a valid date range.", errors[0]);
        searchService.Verify(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()), Times.Never);
    }

    [Fact]
    public void ApplySelectedUiFilters_WithValidValues_UpdatesFilterAndAppliesResults()
    {
        var (sut, searchService, _, errors) = CreateSut();
        var results = new[] { CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2) };
        searchService.Setup(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>())).Returns(results);

        sut.SelectedMaximumPrice = 12.5;
        sut.SelectedMinimumPlayers = 3;
        sut.SelectedStartDate = new DateTimeOffset(new DateTime(2026, 1, 1));
        sut.SelectedEndDate = new DateTimeOffset(new DateTime(2026, 1, 2));

        sut.ApplySelectedUiFilters();

        Assert.Empty(errors);
        Assert.Equal(12.5m, sut.CurrentFilter.MaximumPrice);
        Assert.Equal(3, sut.CurrentFilter.PlayerCount);
        Assert.NotNull(sut.CurrentFilter.AvailabilityRange);
        Assert.Equal(results, sut.DisplayedResults);
        searchService.Verify(service => service.UpdateFilterFromUI(It.IsAny<FilterCriteria>(), 12.5, 3, It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Once);
        searchService.Verify(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()), Times.Once);
    }

    [Fact]
    public void ApplySelectedUiFilters_WithInvalidPlayers_RaisesValidationError()
    {
        var (sut, searchService, _, errors) = CreateSut();
        searchService.Setup(service => service.IsValidPlayersCount(It.IsAny<int?>())).Returns(false);

        sut.SelectedMinimumPlayers = -1;
        sut.ApplySelectedUiFilters();

        Assert.Single(errors);
        Assert.Contains("Please enter valid filter values.", errors[0]);
        searchService.Verify(service => service.UpdateFilterFromUI(It.IsAny<FilterCriteria>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<DateTime?>(), It.IsAny<DateTime?>()), Times.Never);
        searchService.Verify(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()), Times.Never);
    }

    [Fact]
    public void RemoveNameFilter_WhenCalled_ClearsNameAndReappliesFilters()
    {
        var (sut, searchService, _, errors) = CreateSut();
        sut.CurrentFilter.Name = "Catan";

        sut.RemoveNameFilter();

        Assert.Empty(errors);
        Assert.Null(sut.CurrentFilter.Name);
        searchService.Verify(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()), Times.Once);
    }

    [Fact]
    public void RemoveCityFilter_WhenCalled_ClearsCityAndReappliesFilters()
    {
        var (sut, searchService, _, errors) = CreateSut();
        sut.CurrentFilter.City = "Cluj";

        sut.RemoveCityFilter();

        Assert.Empty(errors);
        Assert.Null(sut.CurrentFilter.City);
        searchService.Verify(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()), Times.Once);
    }

    [Fact]
    public void RemovePriceFilter_WhenCalled_ClearsPriceAndReappliesFilters()
    {
        var (sut, searchService, _, errors) = CreateSut();
        sut.CurrentFilter.MaximumPrice = 12m;

        sut.RemovePriceFilter();

        Assert.Empty(errors);
        Assert.Null(sut.CurrentFilter.MaximumPrice);
        searchService.Verify(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()), Times.Once);
    }

    [Fact]
    public void RemovePlayersFilter_WhenCalled_ClearsPlayerCountAndReappliesFilters()
    {
        var (sut, searchService, _, errors) = CreateSut();
        sut.CurrentFilter.PlayerCount = 3;

        sut.RemovePlayersFilter();

        Assert.Empty(errors);
        Assert.Null(sut.CurrentFilter.PlayerCount);
        searchService.Verify(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()), Times.Once);
    }

    [Fact]
    public void RemoveDateFilter_WhenCalled_ClearsAvailabilityRangeAndReappliesFilters()
    {
        var (sut, searchService, _, errors) = CreateSut();
        sut.CurrentFilter.AvailabilityRange = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));

        sut.RemoveDateFilter();

        Assert.Empty(errors);
        Assert.Null(sut.CurrentFilter.AvailabilityRange);
        searchService.Verify(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()), Times.Once);
    }

    [Fact]
    public void SetPriceAscendingSort_WhenCalled_SetsSortOptionAndReappliesFilters()
    {
        var (sut, searchService, _, errors) = CreateSut();

        sut.SetPriceAscendingSort();

        Assert.Empty(errors);
        Assert.Equal(SortOption.PriceAscending, sut.CurrentFilter.SortOption);
        searchService.Verify(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()), Times.Once);
    }

    [Fact]
    public void SetPriceDescendingSort_WhenCalled_SetsSortOptionAndReappliesFilters()
    {
        var (sut, searchService, _, errors) = CreateSut();

        sut.SetPriceDescendingSort();

        Assert.Empty(errors);
        Assert.Equal(SortOption.PriceDescending, sut.CurrentFilter.SortOption);
        searchService.Verify(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()), Times.Once);
    }

    [Fact]
    public void ClearSorting_WhenCalled_ResetsSortOptionAndReappliesFilters()
    {
        var (sut, searchService, _, errors) = CreateSut();
        sut.CurrentFilter.SortOption = SortOption.PriceDescending;

        sut.ClearSorting();

        Assert.Empty(errors);
        Assert.Equal(SortOption.None, sut.CurrentFilter.SortOption);
        searchService.Verify(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()), Times.Once);
    }

    [Fact]
    public void ClearAllFilters_WhenCalled_ResetsSelectionsAndRestoresBaseResults()
    {
        var (sut, _, _, errors) = CreateSut();
        var baseResults = new[]
        {
            CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
            CreateGameDto(2, "Azul", 15m, "Iasi", 4, 2),
        };
        sut.LoadDiscoveryResults(baseResults);
        sut.CurrentFilter.Name = "Catan";
        sut.CurrentFilter.City = "Cluj";
        sut.CurrentFilter.AvailabilityRange = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
        sut.CurrentFilter.MaximumPrice = 12m;
        sut.CurrentFilter.PlayerCount = 3;
        sut.CurrentFilter.SortOption = SortOption.PriceAscending;
        sut.SelectedMaximumPrice = 12;
        sut.SelectedMinimumPlayers = 3;
        sut.SelectedStartDate = new DateTimeOffset(new DateTime(2026, 1, 1));
        sut.SelectedEndDate = new DateTimeOffset(new DateTime(2026, 1, 2));
        sut.CitySearchText = "Cluj";
        sut.LocationError = "error";

        sut.ClearAllFilters();

        Assert.Empty(errors);
        Assert.Null(sut.CurrentFilter.Name);
        Assert.Equal(string.Empty, sut.CurrentFilter.City);
        Assert.Null(sut.CurrentFilter.AvailabilityRange);
        Assert.Null(sut.CurrentFilter.MaximumPrice);
        Assert.Null(sut.CurrentFilter.PlayerCount);
        Assert.Equal(SortOption.None, sut.CurrentFilter.SortOption);
        Assert.Equal(0, sut.SelectedMaximumPrice);
        Assert.Equal(0, sut.SelectedMinimumPlayers);
        Assert.Null(sut.SelectedStartDate);
        Assert.Null(sut.SelectedEndDate);
        Assert.Equal(string.Empty, sut.CitySearchText);
        Assert.Equal(string.Empty, sut.LocationError);
        Assert.Equal(baseResults, sut.DisplayedResults);
        Assert.Equal(baseResults, sut.VisibleGames);
    }

    [Fact]
    public void ApplySortOnly_WithClosestToMeWithoutCity_ShowsLocationError()
    {
        var (sut, searchService, _, errors) = CreateSut();

        sut.SelectedSortOption = "Closest to me";

        Assert.Empty(errors);
        Assert.Equal("Please enter a city to measure from.", sut.LocationError);
        Assert.Null(sut.SelectedSortOption);
        searchService.Verify(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()), Times.Never);
        searchService.Verify(service => service.SearchGamesByFilter(It.IsAny<FilterCriteria>()), Times.Never);
    }

    [Fact]
    public void SearchGamesByFilter_WithValidSelectedDates_LoadsResults()
    {
        var (sut, searchService, _, errors) = CreateSut();
        var results = new[] { CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2) };
        searchService.Setup(service => service.SearchGamesByFilter(It.IsAny<FilterCriteria>())).Returns(results);
        sut.SelectedStartDate = new DateTimeOffset(new DateTime(2026, 1, 1));
        sut.SelectedEndDate = new DateTimeOffset(new DateTime(2026, 1, 2));

        sut.SearchGamesByFilter(new FilterCriteria());

        Assert.Empty(errors);
        Assert.Equal(results, sut.BaseResults);
        Assert.Equal(results, sut.DisplayedResults);
        searchService.Verify(service => service.SearchGamesByFilter(It.IsAny<FilterCriteria>()), Times.Once);
    }

    [Fact]
    public void SearchGamesByFilter_WithInvalidSelectedDates_RaisesError()
    {
        var (sut, searchService, _, errors) = CreateSut();
        sut.SelectedStartDate = new DateTimeOffset(new DateTime(2026, 1, 2));
        sut.SelectedEndDate = new DateTimeOffset(new DateTime(2026, 1, 1));
        searchService.Setup(service => service.IsValidDateRange(It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).Returns(false);

        sut.SearchGamesByFilter(new FilterCriteria());

        Assert.Single(errors);
        Assert.Contains("Please select a valid date range.", errors[0]);
        searchService.Verify(service => service.SearchGamesByFilter(It.IsAny<FilterCriteria>()), Times.Never);
    }

    [Fact]
    public void Initialize_WithExistingFilter_CopiesStateAndSearches()
    {
        var (sut, searchService, geoService, errors) = CreateSut();
        var results = new[] { CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2) };
        searchService.Setup(service => service.SearchGamesByFilter(It.IsAny<FilterCriteria>())).Returns(results);
        geoService.Setup(service => service.GetCitySuggestions("Cl")).Returns(new List<string> { "Cluj", "Cluj-Napoca" });
        var filter = new FilterCriteria
        {
            City = "Cl",
            AvailabilityRange = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2)),
        };

        sut.Initialize(filter);

        Assert.Empty(errors);
        Assert.Equal("Cl", sut.CitySearchText);
        Assert.NotNull(sut.SelectedStartDate);
        Assert.NotNull(sut.SelectedEndDate);
        Assert.Equal(results, sut.DisplayedResults);
        searchService.Verify(service => service.SearchGamesByFilter(filter), Times.Once);
    }

    [Fact]
    public void CitySearchText_WithTwoCharacters_LoadsSuggestions()
    {
        var (sut, _, geoService, errors) = CreateSut();
        geoService.Setup(service => service.GetCitySuggestions("Cl")).Returns(new List<string> { "Cluj", "Cluj-Napoca" });

        sut.CitySearchText = "Cl";

        Assert.Empty(errors);
        Assert.Equal("Cl", sut.CurrentFilter.City);
        Assert.Equal(new[] { "Cluj", "Cluj-Napoca" }, sut.CitySuggestions);
        geoService.Verify(service => service.GetCitySuggestions("Cl"), Times.Once);
    }

    private static (FilteredSearchViewModel Sut, Mock<InterfaceSearchAndFilterService> SearchService, Mock<InterfaceGeographicalService> GeoService, List<string> Errors) CreateSut()
    {
        var searchService = new Mock<InterfaceSearchAndFilterService>(MockBehavior.Loose);
        searchService.Setup(service => service.IsValidDateRange(It.IsAny<DateTime?>(), It.IsAny<DateTime?>())).Returns(true);
        searchService.Setup(service => service.IsValidPlayersCount(It.IsAny<int?>())).Returns(true);
        searchService.Setup(service => service.SearchGamesByFilter(It.IsAny<FilterCriteria>())).Returns(Array.Empty<GameDTO>());
        searchService.Setup(service => service.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>())).Returns(Array.Empty<GameDTO>());
        searchService.Setup(service => service.UpdateFilterFromUI(
                It.IsAny<FilterCriteria>(),
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<DateTime?>(),
                It.IsAny<DateTime?>()))
            .Callback<FilterCriteria, double, double, DateTime?, DateTime?>((filter, maxPrice, players, start, end) =>
            {
                filter.MaximumPrice = maxPrice > 0 ? (decimal?)maxPrice : null;
                filter.PlayerCount = players > 0 ? (int?)players : null;
                filter.AvailabilityRange = start.HasValue && end.HasValue ? new TimeRange(start.Value, end.Value) : null;
            });

        var geoService = new Mock<InterfaceGeographicalService>(MockBehavior.Loose);
        geoService.Setup(service => service.GetCitySuggestions(It.IsAny<string>())).Returns(new List<string>());
        geoService.Setup(service => service.GetCityDetails(It.IsAny<string>())).Returns((false, string.Empty, 0, 0));

        var sut = new FilteredSearchViewModel(searchService.Object, geoService.Object);
        var errors = new List<string>();
        sut.OnErrorOccurred += errors.Add;

        return (sut, searchService, geoService, errors);
    }

    private static GameDTO CreateGameDto(int id, string name, decimal price, string city, int maximumPlayers, int minimumPlayers)
    {
        return new GameDTO
        {
            GameId = id,
            Name = name,
            Price = price,
            City = city,
            MaximumPlayerNumber = maximumPlayers,
            MinimumPlayerNumber = minimumPlayers,
        };
    }

    [Fact]
    public void ApplyFilters_WithAvailableGames_ShowsOnlyAvailableGames()
    {
        var (sut, searchService, _, errors) = CreateSut();

        var baseGames = new[]
        {
        CreateGameDto(1, "Game1", 10m, "Cluj", 4, 2),
        CreateGameDto(2, "Game2", 10m, "Cluj", 4, 2),
    };

        var filtered = new[]
        {
        baseGames[0]
    };

        sut.LoadDiscoveryResults(baseGames);

        searchService.Setup(s => s.ApplyFilters(It.IsAny<GameDTO[]>(), It.IsAny<FilterCriteria>()))
            .Returns(filtered);

        sut.ApplyFilters();

        Assert.Empty(errors);
        Assert.Single(sut.VisibleGames);
        Assert.Equal(1, sut.VisibleGames.First().GameId);
    }

    [Fact]
    public void LoadSearchResults_WithDuplicateGameListings_AllowsMultipleListings()
    {
        var (sut, searchService, _, errors) = CreateSut();

        var results = new[]
        {
        CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
        CreateGameDto(2, "Catan", 25m, "Cluj", 4, 2),
    };

        searchService.Setup(s => s.SearchGamesByFilter(It.IsAny<FilterCriteria>()))
            .Returns(results);

        sut.LoadSearchResults(new FilterCriteria());

        Assert.Empty(errors);
        Assert.Equal(2, sut.VisibleGames.Count);
    }

    [Fact]
    public void LoadSearchResults_WithManyResults_ShowsSubset()
    {
        var (sut, searchService, _, errors) = CreateSut();

        var results = Enumerable.Range(1, 50)
            .Select(i => CreateGameDto(i, "Game", 10m, "Cluj", 4, 2))
            .ToArray();

        searchService.Setup(s => s.SearchGamesByFilter(It.IsAny<FilterCriteria>()))
            .Returns(results);

        sut.LoadSearchResults(new FilterCriteria());

        Assert.Empty(errors);
        Assert.True(sut.VisibleGames.Count <= results.Length);
    }

    [Fact]
    public void GamesShown_WhenNavigating_ContainsValidGameIds()
    {
        var (sut, searchService, _, errors) = CreateSut();

        var results = new[]
        {
        CreateGameDto(10, "Catan", 20m, "Cluj", 4, 2),
    };

        searchService.Setup(s => s.SearchGamesByFilter(It.IsAny<FilterCriteria>()))
            .Returns(results);

        sut.LoadSearchResults(new FilterCriteria());

        Assert.Empty(errors);
        Assert.True(sut.VisibleGames.First().GameId > 0);
    }

    [Fact]
    public void LoadSearchResults_WhenCalled_DoesNotRequireUserAuthentication()
    {
        var (sut, searchService, _, errors) = CreateSut();

        searchService.Setup(s => s.SearchGamesByFilter(It.IsAny<FilterCriteria>()))
            .Returns(Array.Empty<GameDTO>());

        sut.LoadSearchResults(new FilterCriteria());

        Assert.Empty(errors);
        Assert.NotNull(sut.VisibleGames);
    }

    [Fact]
    public void SelectGame_WhenCalled_RaisesNavigationEvent()
    {
        var (sut, _, _, errors) = CreateSut();

        int? selectedId = null;
        sut.OnGameSelectedRequest += id => selectedId = id;

        var game = CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2);

        sut.VisibleGames.Add(game);

        sut.SelectGame(game.GameId);

        Assert.Empty(errors);
        Assert.Equal(1, selectedId);
    }

    [Fact]
    public void NoResultsMessage_WhenNoGames_ShowsMessage()
    {
        var (sut, _, _, _) = CreateSut();

        sut.LoadDiscoveryResults(Array.Empty<GameDTO>());

        Assert.True(sut.HasNoResults);
        Assert.NotEmpty(sut.NoResultsMessage);
    }
}
