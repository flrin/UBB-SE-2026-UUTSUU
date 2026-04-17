using Moq;
using SearchAndBook.Domain;
using SearchAndBook.Repositories;
using SearchAndBook.Services;
using SearchAndBook.Shared;

namespace SearchAndBook.Tests.Services;

public class SearchAndFilterServiceTests
{
    [Fact]
    public void ApplyFilters_NameFilter_ReturnsCaseInsensitiveMatches()
    {
        var sut = CreateSut(out _, out _, out _, out _);
        var games = new[]
        {
            CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
            CreateGameDto(2, "Azul", 15m, "Iasi", 4, 2),
        };

        var result = sut.ApplyFilters(games, new FilterCriteria { Name = "cat" });

        Assert.Single(result);
        Assert.Equal(1, result[0].GameId);
    }

    [Fact]
    public void ApplyFilters_MaximumPriceFilter_ReturnsGamesWithinBudget()
    {
        var sut = CreateSut(out _, out _, out _, out _);
        var games = new[]
        {
            CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
            CreateGameDto(2, "Azul", 15m, "Iasi", 4, 2),
        };

        var result = sut.ApplyFilters(games, new FilterCriteria { MaximumPrice = 15m });

        Assert.Single(result);
        Assert.Equal(2, result[0].GameId);
    }

    [Fact]
    public void ApplyFilters_PlayerCountFilter_ReturnsGamesWithEnoughPlayers()
    {
        var sut = CreateSut(out _, out _, out _, out _);
        var games = new[]
        {
            CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
            CreateGameDto(2, "Azul", 15m, "Iasi", 3, 2),
        };

        var result = sut.ApplyFilters(games, new FilterCriteria { PlayerCount = 4 });

        Assert.Single(result);
        Assert.Equal(1, result[0].GameId);
    }

    [Fact]
    public void ApplyFilters_CityFilter_ReturnsGamesFromMatchingCity()
    {
        var sut = CreateSut(out _, out _, out _, out _);
        var games = new[]
        {
            CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
            CreateGameDto(2, "Azul", 15m, "Iasi", 4, 2),
        };

        var result = sut.ApplyFilters(games, new FilterCriteria { City = "cluj" });

        Assert.Single(result);
        Assert.Equal(1, result[0].GameId);
    }

    [Fact]
    public void ApplyFilters_PriceAscendingSort_ReturnsCheapestFirst()
    {
        var sut = CreateSut(out _, out _, out _, out _);
        var games = new[]
        {
            CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
            CreateGameDto(2, "Azul", 15m, "Iasi", 4, 2),
            CreateGameDto(3, "Root", 10m, "Brasov", 4, 2),
        };

        var result = sut.ApplyFilters(games, new FilterCriteria { SortOption = SortOption.PriceAscending });

        Assert.Equal(new[] { 3, 2, 1 }, result.Select(game => game.GameId));
    }

    [Fact]
    public void ApplyFilters_PriceDescendingSort_ReturnsMostExpensiveFirst()
    {
        var sut = CreateSut(out _, out _, out _, out _);
        var games = new[]
        {
            CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
            CreateGameDto(2, "Azul", 15m, "Iasi", 4, 2),
            CreateGameDto(3, "Root", 10m, "Brasov", 4, 2),
        };

        var result = sut.ApplyFilters(games, new FilterCriteria { SortOption = SortOption.PriceDescending });

        Assert.Equal(new[] { 1, 2, 3 }, result.Select(game => game.GameId));
    }

    [Fact]
    public void ApplyFilters_LocationSort_OrdersByDistanceAndCachesCityLookups()
    {
        var sut = CreateSut(out _, out _, out _, out var geoService);
        var games = new[]
        {
            CreateGameDto(1, "Catan", 20m, "Paris", 4, 2),
            CreateGameDto(2, "Azul", 15m, "Lyon", 4, 2),
            CreateGameDto(3, "Root", 10m, "Paris", 4, 2),
        };

        geoService.Setup(service => service.GetCityDetails("Brussels")).Returns((true, "Brussels", 0, 0));
        geoService.Setup(service => service.GetCityDetails("Paris")).Returns((true, "Paris", 0, 1));
        geoService.Setup(service => service.GetCityDetails("Lyon")).Returns((true, "Lyon", 0, 2));

        var result = sut.ApplyFilters(
            games,
            new FilterCriteria { City = "Brussels", SortOption = SortOption.Location });

        Assert.Equal(new[] { 1, 3, 2 }, result.Select(game => game.GameId));
        geoService.Verify(service => service.GetCityDetails("Brussels"), Times.Once);
        geoService.Verify(service => service.GetCityDetails("Paris"), Times.Once);
        geoService.Verify(service => service.GetCityDetails("Lyon"), Times.Once);
    }

    [Fact]
    public void ApplyFilters_LocationSortWithUnknownCity_KeepsOriginalOrder()
    {
        var sut = CreateSut(out _, out _, out _, out var geoService);
        var games = new[]
        {
            CreateGameDto(1, "Catan", 20m, "Paris", 4, 2),
            CreateGameDto(2, "Azul", 15m, "Lyon", 4, 2),
        };

        geoService.Setup(service => service.GetCityDetails("Unknown")).Returns((false, string.Empty, 0, 0));

        var result = sut.ApplyFilters(
            games,
            new FilterCriteria { City = "Unknown", SortOption = SortOption.Location });

        Assert.Equal(new[] { 1, 2 }, result.Select(game => game.GameId));
        geoService.Verify(service => service.GetCityDetails("Unknown"), Times.Once);
        geoService.Verify(service => service.GetCityDetails("Paris"), Times.Never);
        geoService.Verify(service => service.GetCityDetails("Lyon"), Times.Never);
    }

    [Fact]
    public void ApplyFilters_AvailabilityRangeFilter_UsesRentalsRepository()
    {
        var sut = CreateSut(out _, out _, out var rentalsRepository, out _);
        var games = new[]
        {
            CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
            CreateGameDto(2, "Azul", 15m, "Iasi", 4, 2),
        };
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));

        rentalsRepository.Setup(repository => repository.CheckAvailability(range, 1)).Returns(true);
        rentalsRepository.Setup(repository => repository.CheckAvailability(range, 2)).Returns(false);

        var result = sut.ApplyFilters(games, new FilterCriteria { AvailabilityRange = range });

        Assert.Single(result);
        Assert.Equal(1, result[0].GameId);
    }

    [Fact]
    public void SearchGamesByFilter_LocationSort_ClearsCityForRepositoryAndRestoresItAfterwards()
    {
        var sut = CreateSut(out var gamesRepository, out var usersRepository, out _, out var geoService);
        var filter = new FilterCriteria { City = "Brussels", SortOption = SortOption.Location };
        var owner = CreateUser(1, "Paris");
        var games = new List<Game>
        {
            CreateGame(1, 1, "Catan", 20m, 4, 2),
            CreateGame(2, 1, "Azul", 15m, 4, 2),
        };

        gamesRepository.Setup(repository => repository.GetGamesByFilter(It.IsAny<FilterCriteria>()))
            .Callback<FilterCriteria>(criteria => Assert.Null(criteria.City))
            .Returns(games);
        usersRepository.Setup(repository => repository.GetGameById(1)).Returns(owner);
        geoService.Setup(service => service.GetCityDetails("Brussels")).Returns((true, "Brussels", 0, 0));
        geoService.Setup(service => service.GetCityDetails("Paris")).Returns((true, "Paris", 0, 1));

        var result = sut.SearchGamesByFilter(filter);

        Assert.Equal("Brussels", filter.City);
        Assert.Equal(2, result.Length);
        Assert.All(result, game => Assert.Equal("Paris", game.City));
        usersRepository.Verify(repository => repository.GetGameById(1), Times.Once);
    }

    [Fact]
    public void SearchGamesByFilter_WhenRepositoryFails_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(out var gamesRepository, out _, out _, out _);
        gamesRepository.Setup(repository => repository.GetGamesByFilter(It.IsAny<FilterCriteria>()))
            .Throws(new Exception("boom"));

        var exception = Assert.Throws<InvalidOperationException>(() => sut.SearchGamesByFilter(new FilterCriteria()));

        Assert.Equal("Failed to search for games.", exception.Message);
        Assert.IsType<Exception>(exception.InnerException);
    }

    [Theory]
    [MemberData(nameof(DateRangeData))]
    public void IsValidDateRange_ReturnsExpectedResult(DateTime? start, DateTime? end, bool expected)
    {
        var sut = CreateSut(out _, out _, out _, out _);

        var result = sut.IsValidDateRange(start, end);

        Assert.Equal(expected, result);
    }

    public static IEnumerable<object?[]> DateRangeData()
    {
        yield return new object?[] { null, null, true };
        yield return new object?[] { new DateTime(2026, 1, 1), null, false };
        yield return new object?[] { null, new DateTime(2026, 1, 1), false };
        yield return new object?[] { new DateTime(2026, 1, 1), new DateTime(2026, 1, 1), true };
        yield return new object?[] { new DateTime(2026, 1, 1), new DateTime(2026, 1, 2), true };
    }

    [Theory]
    [MemberData(nameof(PlayerCountData))]
    public void IsValidPlayersCount_ReturnsExpectedResult(int? players, bool expected)
    {
        var sut = CreateSut(out _, out _, out _, out _);

        var result = sut.IsValidPlayersCount(players);

        Assert.Equal(expected, result);
    }

    public static IEnumerable<object?[]> PlayerCountData()
    {
        yield return new object?[] { null, true };
        yield return new object?[] { -1, false };
        yield return new object?[] { 0, true };
        yield return new object?[] { 1, true };
    }

    [Fact]
    public void UpdateFilterFromUI_ValidInput_PopulatesFilterValues()
    {
        var sut = CreateSut(out _, out _, out _, out _);
        var filter = new FilterCriteria();
        var startDate = new DateTime(2026, 1, 1);
        var endDate = new DateTime(2026, 1, 2);

        sut.UpdateFilterFromUI(filter, 19.99, 3, startDate, endDate);

        Assert.Equal(19.99m, filter.MaximumPrice);
        Assert.Equal(3, filter.PlayerCount);
        Assert.NotNull(filter.AvailabilityRange);
        Assert.Equal(startDate, filter.AvailabilityRange!.StartTime);
        Assert.Equal(endDate, filter.AvailabilityRange.EndTime);
    }

    [Fact]
    public void UpdateFilterFromUI_InvalidDateRange_ClearsAvailabilityRange()
    {
        var sut = CreateSut(out _, out _, out _, out _);
        var filter = new FilterCriteria();

        sut.UpdateFilterFromUI(filter, 0, 0, new DateTime(2026, 1, 2), new DateTime(2026, 1, 1));

        Assert.Null(filter.MaximumPrice);
        Assert.Null(filter.PlayerCount);
        Assert.Null(filter.AvailabilityRange);
    }

    private static SearchAndFilterService CreateSut(
        out Mock<InterfaceGamesRepository> gamesRepository,
        out Mock<InterfaceUsersRepository> usersRepository,
        out Mock<InterfaceRentalsRepository> rentalsRepository,
        out Mock<InterfaceGeographicalService> geographicalService)
    {
        gamesRepository = new Mock<InterfaceGamesRepository>(MockBehavior.Loose);
        usersRepository = new Mock<InterfaceUsersRepository>(MockBehavior.Loose);
        rentalsRepository = new Mock<InterfaceRentalsRepository>(MockBehavior.Loose);
        geographicalService = new Mock<InterfaceGeographicalService>(MockBehavior.Loose);

        geographicalService
            .Setup(service => service.GetCityDetails(It.IsAny<string>()))
            .Returns((false, string.Empty, 0, 0));

        rentalsRepository
            .Setup(repository => repository.CheckAvailability(It.IsAny<TimeRange>(), It.IsAny<int>()))
            .Returns(true);

        usersRepository
            .Setup(repository => repository.GetGameById(It.IsAny<int>()))
            .Returns(new User { City = string.Empty });

        return new SearchAndFilterService(
            gamesRepository.Object,
            usersRepository.Object,
            rentalsRepository.Object,
            geographicalService.Object);
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

    private static Game CreateGame(int id, int ownerId, string name, decimal price, int maximumPlayers, int minimumPlayers)
    {
        return new Game
        {
            GameId = id,
            OwnerId = ownerId,
            Name = name,
            Price = price,
            MaximumPlayerNumber = maximumPlayers,
            MinimumPlayerNumber = minimumPlayers,
            Description = "Description",
        };
    }

    private static User CreateUser(int id, string city)
    {
        return new User
        {
            UserId = id,
            Username = "user",
            DisplayName = "User",
            Email = "user@example.com",
            PasswordHash = "hash",
            City = city,
            Country = "RO",
        };
    }

    [Fact]
    public void ApplyFilters_NoResults_ReturnsEmpty()
    {
        var sut = CreateSut(out _, out _, out _, out _);

        var games = new[]
        {
        CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
    };

        var result = sut.ApplyFilters(games, new FilterCriteria { Name = "zzz" });

        Assert.Empty(result);
    }

    [Fact]
    public void ApplyFilters_AllowsMultipleListingsForSameGame()
    {
        var sut = CreateSut(out _, out _, out _, out _);

        var games = new[]
        {
        CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
        CreateGameDto(2, "Catan", 25m, "Cluj", 4, 2),
    };

        var result = sut.ApplyFilters(games, new FilterCriteria { Name = "cat" });

        Assert.Equal(2, result.Length);
    }

    [Fact]
    public void ApplyFilters_PartialMatch_ReturnsMatchingGames()
    {
        var sut = CreateSut(out _, out _, out _, out _);

        var games = new[]
        {
        CreateGameDto(1, "Monopoly", 20m, "Cluj", 4, 2),
        CreateGameDto(2, "Chess", 10m, "Cluj", 2, 2),
    };

        var result = sut.ApplyFilters(games, new FilterCriteria { Name = "poly" });

        Assert.Single(result);
        Assert.Equal(1, result[0].GameId);
    }

    [Fact]
    public void ApplyFilters_AllCriteriaMustMatch()
    {
        var sut = CreateSut(out _, out _, out _, out _);

        var games = new[]
        {
        CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
        CreateGameDto(2, "Catan", 50m, "Cluj", 4, 2),
    };

        var result = sut.ApplyFilters(games, new FilterCriteria
        {
            Name = "cat",
            MaximumPrice = 25m
        });

        Assert.Single(result);
        Assert.Equal(1, result[0].GameId);
    }

    [Fact]
    public void ApplyFilters_ReturnsOnlyAvailableGames()
    {
        var sut = CreateSut(out _, out _, out var rentalsRepo, out _);

        var range = new TimeRange(DateTime.Now, DateTime.Now.AddDays(1));

        rentalsRepo.Setup(r => r.CheckAvailability(range, 1)).Returns(true);
        rentalsRepo.Setup(r => r.CheckAvailability(range, 2)).Returns(false);

        var games = new[]
        {
        CreateGameDto(1, "Game1", 10m, "Cluj", 4, 2),
        CreateGameDto(2, "Game2", 10m, "Cluj", 4, 2),
    };

        var result = sut.ApplyFilters(games, new FilterCriteria { AvailabilityRange = range });

        Assert.Single(result);
        Assert.Equal(1, result[0].GameId);
    }

    [Fact]
    public void SearchGamesByFilter_ResultContainsGameId()
    {
        var sut = CreateSut(out var gamesRepo, out var usersRepo, out _, out _);

        gamesRepo.Setup(r => r.GetGamesByFilter(It.IsAny<FilterCriteria>()))
            .Returns(new List<Game> { CreateGame(1, 1, "Catan", 20m, 4, 2) });

        usersRepo.Setup(r => r.GetGameById(1)).Returns(CreateUser(1, "Cluj"));

        var result = sut.SearchGamesByFilter(new FilterCriteria());

        Assert.True(result[0].GameId > 0);
    }

    [Fact]
    public void SearchGamesByFilter_DoesNotRequireAuthentication()
    {
        var sut = CreateSut(out var gamesRepo, out _, out _, out _);

        gamesRepo.Setup(r => r.GetGamesByFilter(It.IsAny<FilterCriteria>()))
            .Returns(new List<Game>());

        var result = sut.SearchGamesByFilter(new FilterCriteria());

        Assert.NotNull(result);
    }

    [Fact]
    public void SearchGamesByFilter_Called_ReturnsResults()
    {
        var sut = CreateSut(out var gamesRepo, out var usersRepo, out _, out _);

        gamesRepo.Setup(r => r.GetGamesByFilter(It.IsAny<FilterCriteria>()))
            .Returns(new List<Game> { CreateGame(1, 1, "Catan", 20m, 4, 2) });

        usersRepo.Setup(r => r.GetGameById(1)).Returns(CreateUser(1, "Cluj"));

        var result = sut.SearchGamesByFilter(new FilterCriteria());

        Assert.Single(result);
    }

    [Fact]
    public void SearchGamesByFilter_ResultContainsRequiredFields()
    {
        var sut = CreateSut(out var gamesRepo, out var usersRepo, out _, out _);

        gamesRepo.Setup(r => r.GetGamesByFilter(It.IsAny<FilterCriteria>()))
            .Returns(new List<Game>
            {
            CreateGame(1, 1, "Catan", 20m, 4, 2)
            });

        usersRepo.Setup(r => r.GetGameById(1)).Returns(CreateUser(1, "Cluj"));

        var result = sut.SearchGamesByFilter(new FilterCriteria());

        var game = result.First();

        Assert.NotNull(game.Name);
        Assert.True(game.Price > 0);
        Assert.NotNull(game.City);
        Assert.True(game.MaximumPlayerNumber > 0);
    }




}
