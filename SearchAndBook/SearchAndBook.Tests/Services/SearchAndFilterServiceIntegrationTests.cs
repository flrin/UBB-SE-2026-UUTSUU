using SearchAndBook.Domain;
using SearchAndBook.Repositories;
using SearchAndBook.Services;
using SearchAndBook.Shared;

namespace SearchAndBook.Tests.Services;

[Trait("Category", "Integration")]
public class SearchAndFilterServiceIntegrationTests
{
    [Fact]
    public void ApplyFilters_EndToEnd_AppliesMultipleCriteria()
    {
        var rentalsRepository = new InMemoryRentalsRepository();
        var service = CreateSut(rentalsRepository, new InMemoryGeographicalService());
        var games = new[]
        {
            CreateGameDto(1, "Catan", 20m, "Cluj", 4, 2),
            CreateGameDto(2, "Azul", 30m, "Cluj", 4, 2),
            CreateGameDto(3, "Carcassonne", 15m, "Brasov", 2, 2),
        };
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));

        rentalsRepository.SetAvailability(1, true);
        rentalsRepository.SetAvailability(2, false);
        rentalsRepository.SetAvailability(3, true);

        var result = service.ApplyFilters(
            games,
            new FilterCriteria
            {
                Name = "cat",
                MaximumPrice = 25m,
                PlayerCount = 3,
                City = "cluj",
                AvailabilityRange = range,
            });

        Assert.Single(result);
        Assert.Equal(1, result[0].GameId);
    }

    [Fact]
    public void SearchGamesByFilter_EndToEnd_ReturnsLocationOrderedResults()
    {
        var gamesRepository = new InMemoryGamesRepository(
            new[]
            {
                CreateGame(1, 1, "Catan", 20m, 4, 2),
                CreateGame(2, 2, "Azul", 15m, 4, 2),
                CreateGame(3, 1, "Root", 10m, 4, 2),
            });

        var usersRepository = new InMemoryUsersRepository(
            new[]
            {
                CreateUser(1, "Paris"),
                CreateUser(2, "Lyon"),
            });

        var geographicalService = new InMemoryGeographicalService();
        geographicalService.SetCity("Brussels", 0, 0);
        geographicalService.SetCity("Paris", 0, 1);
        geographicalService.SetCity("Lyon", 0, 2);

        var service = new SearchAndFilterService(gamesRepository, usersRepository, new InMemoryRentalsRepository(), geographicalService);

        var result = service.SearchGamesByFilter(
            new FilterCriteria
            {
                City = "Brussels",
                SortOption = SortOption.Location,
            });

        Assert.Equal(new[] { 1, 3, 2 }, result.Select(game => game.GameId));
        Assert.All(result, game => Assert.Contains(game.City, new[] { "Paris", "Lyon" }));
    }

    private static SearchAndFilterService CreateSut(IInMemoryRentalsRepository rentalsRepository, InMemoryGeographicalService geographicalService)
    {
        return new SearchAndFilterService(
            new InMemoryGamesRepository(Array.Empty<Game>()),
            new InMemoryUsersRepository(Array.Empty<User>()),
            rentalsRepository,
            geographicalService);
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

    private interface IInMemoryRentalsRepository : InterfaceRentalsRepository
    {
        void SetAvailability(int gameId, bool isAvailable);
    }

    private sealed class InMemoryGamesRepository : InterfaceGamesRepository
    {
        private readonly List<Game> _games;

        public InMemoryGamesRepository(IEnumerable<Game> games)
        {
            _games = games.ToList();
        }

        public List<Game> GetGamesByFilter(FilterCriteria filter)
        {
            return _games.ToList();
        }

        public List<Game> GetGamesForFeedAvailableTonight(int userId)
        {
            return new List<Game>();
        }

        public List<Game> GetGamesForFeedOthers(int userId)
        {
            return new List<Game>();
        }

        public Game? GetGameById(int id)
        {
            return _games.FirstOrDefault(game => game.GameId == id);
        }

        public List<Game> GetAllGames()
        {
            return _games.ToList();
        }
    }

    private sealed class InMemoryUsersRepository : InterfaceUsersRepository
    {
        private readonly Dictionary<int, User> _users;

        public InMemoryUsersRepository(IEnumerable<User> users)
        {
            _users = users.ToDictionary(user => user.UserId);
        }

        public User? GetGameById(int id)
        {
            return _users.TryGetValue(id, out var user) ? user : null;
        }

        public List<User> GetAllGames()
        {
            return _users.Values.ToList();
        }
    }

    private sealed class InMemoryRentalsRepository : IInMemoryRentalsRepository
    {
        private readonly Dictionary<int, bool> _availability = new();

        public void SetAvailability(int gameId, bool isAvailable)
        {
            _availability[gameId] = isAvailable;
        }

        public bool CheckAvailability(TimeRange range, int gameId)
        {
            return !_availability.TryGetValue(gameId, out var isAvailable) || isAvailable;
        }

        public List<TimeRange> GetUnavailableRanges(int gameId)
        {
            return new List<TimeRange>();
        }

        public TimeRange? GetGameById(int id)
        {
            return null;
        }

        public List<TimeRange> GetAllGames()
        {
            return new List<TimeRange>();
        }
    }

    private sealed class InMemoryGeographicalService : InterfaceGeographicalService
    {
        private readonly Dictionary<string, (double lat, double lon)> _cities = new(StringComparer.OrdinalIgnoreCase);

        public void SetCity(string name, double lat, double lon)
        {
            _cities[name] = (lat, lon);
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public (bool found, string name, double lat, double lon) GetCityDetails(string cityName)
        {
            if (_cities.TryGetValue(cityName, out var coordinates))
            {
                return (true, cityName, coordinates.lat, coordinates.lon);
            }

            return (false, string.Empty, 0, 0);
        }

        public double? GetDistanceBetweenCities(string city1, string city2)
        {
            var first = GetCityDetails(city1);
            var second = GetCityDetails(city2);

            if (!first.found || !second.found)
            {
                return null;
            }

            return Math.Sqrt(Math.Pow(first.lat - second.lat, 2) + Math.Pow(first.lon - second.lon, 2));
        }

        public List<string> GetCitySuggestions(string partialName)
        {
            return _cities.Keys
                .Where(city => city.Contains(partialName, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }



        [Fact]
        public void GetDiscoveryFeedPaged_ExcludesInactiveAndBookedGames()
        {
            var rentalsRepository = new InMemoryRentalsRepository();

            var games = new List<Game>
            {
            CreateGame(1, 1, "ActiveAvailable", 10m, 4, 2),

            new Game
            {
                GameId = 2,
                OwnerId = 1,
                Name = "Inactive",
                Price = 10m,
                MaximumPlayerNumber = 4,
                MinimumPlayerNumber = 2,
                Description = "Test",
                IsActive = false
            },

            CreateGame(3, 1, "Booked", 10m, 4, 2)
             };
            rentalsRepository.SetAvailability(3, false);

            var service = new SearchAndFilterService(
                new InMemoryGamesRepository(games),
                new InMemoryUsersRepository(new[] { CreateUser(1, "Cluj") }),
                rentalsRepository,
                new InMemoryGeographicalService());

            var (available, others, total) = service.GetDiscoveryFeedPaged(1, 1, 10);

            var all = available.Concat(others).ToList();

            Assert.Single(all);
            Assert.Equal(1, all[0].GameId);
        }

        [Fact]
        public void GetDiscoveryFeedPaged_AvailableTonight_ContainsOnlyTodayAndTomorrow()
        {
            var rentalsRepository = new InMemoryRentalsRepository();

            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var future = today.AddDays(5);

            var games = new List<Game>
    {
        CreateGame(1, 1, "TodayGame", 10m, 4, 2),
        CreateGame(2, 1, "TomorrowGame", 10m, 4, 2),
        CreateGame(3, 1, "FutureGame", 10m, 4, 2)
    };
            rentalsRepository.SetAvailability(1, true); // today
            rentalsRepository.SetAvailability(2, true); // tomorrow
            rentalsRepository.SetAvailability(3, true); // future

            var service = new SearchAndFilterService(
                new InMemoryGamesRepository(games),
                new InMemoryUsersRepository(new[] { CreateUser(1, "Cluj") }),
                rentalsRepository,
                new InMemoryGeographicalService());

            var (availableTonight, others, total) =
                service.GetDiscoveryFeedPaged(1, 1, 10);

            var availableIds = availableTonight.Select(g => g.GameId).ToList();

            Assert.Contains(1, availableIds);
            Assert.Contains(2, availableIds);
            Assert.DoesNotContain(3, availableIds);
        }

        [Fact]
        public void GetDiscoveryFeedPaged_ReturnsCorrectPageSize()
        {
            var gamesRepository = new InMemoryGamesRepository(
                Enumerable.Range(1, 20)
                    .Select(i => CreateGame(i, 1, "Game", 10m, 4, 2))
                    .ToList());

            var usersRepository = new InMemoryUsersRepository(
                new[] { CreateUser(1, "Cluj") });

            var service = new SearchAndFilterService(
                gamesRepository,
                usersRepository,
                new InMemoryRentalsRepository(),
                new InMemoryGeographicalService());

            var (available, others, total) = service.GetDiscoveryFeedPaged(1, 2, 10);

            Assert.Equal(20, total);
            Assert.Equal(10, available.Count + others.Count);
        }

        [Fact]
        public void GetDiscoveryFeedPaged_PageOutOfBounds_ReturnsEmpty()
        {
            var gamesRepository = new InMemoryGamesRepository(
                Enumerable.Range(1, 20)
                    .Select(i => CreateGame(i, 1, "Game", 10m, 4, 2))
                    .ToList());

            var usersRepository = new InMemoryUsersRepository(
                new[] { CreateUser(1, "Cluj") });

            var service = new SearchAndFilterService(
                gamesRepository,
                usersRepository,
                new InMemoryRentalsRepository(),
                new InMemoryGeographicalService());

            var (available, others, total) = service.GetDiscoveryFeedPaged(1, 5, 10);

            Assert.Empty(available);
            Assert.Empty(others);
            Assert.Equal(20, total);
        }

        [Fact]
        public void GetDiscoveryFeedPaged_TotalEqualsSumOfAvailableAndOthers()
        {
            var gamesRepository = new InMemoryGamesRepository(
                Enumerable.Range(1, 10)
                    .Select(i => CreateGame(i, 1, "Game", 10m, 4, 2)));

            var usersRepository = new InMemoryUsersRepository(
                new[] { CreateUser(1, "Cluj") });

            var service = new SearchAndFilterService(
                gamesRepository,
                usersRepository,
                new InMemoryRentalsRepository(),
                new InMemoryGeographicalService());

            var (availableTonight, others, total) =
                service.GetDiscoveryFeedPaged(1, 1, 10);

            Assert.Equal(total, availableTonight.Count + others.Count);
        }

        [Fact]
        public void GetDiscoveryFeedPaged_NoGames_ReturnsEmpty()
        {
            var service = new SearchAndFilterService(
                new InMemoryGamesRepository(Array.Empty<Game>()),
                new InMemoryUsersRepository(Array.Empty<User>()),
                new InMemoryRentalsRepository(),
                new InMemoryGeographicalService());

            var (available, others, total) = service.GetDiscoveryFeedPaged(1, 1, 10);

            Assert.Empty(available);
            Assert.Empty(others);
            Assert.Equal(0, total);
        }
        [Fact]
        public void GetDiscoveryFeedPaged_OthersContainsRemainingGames()
        {
            var rentalsRepository = new InMemoryRentalsRepository();

            var games = new List<Game>
            {
                CreateGame(1, 1, "Game1", 10m, 4, 2),
                CreateGame(2, 1, "Game2", 10m, 4, 2)
            };

            var service = new SearchAndFilterService(
                new InMemoryGamesRepository(games),
                new InMemoryUsersRepository(new[] { CreateUser(1, "Cluj") }),
                rentalsRepository,
                new InMemoryGeographicalService());

            var (available, others, _) = service.GetDiscoveryFeedPaged(1, 1, 10);

            var availableIds = available.Select(g => g.GameId);
            var othersIds = others.Select(g => g.GameId);
            Assert.False(availableIds.Intersect(othersIds).Any());
            Assert.Equal(games.Count, available.Count + others.Count);
        }

        [Fact]
        public void GetDiscoveryFeedPaged_DifferentPages_ReturnDifferentResults()
        {
            var games = Enumerable.Range(1, 20)
                .Select(i => CreateGame(i, 1, $"Game{i}", 10m, 4, 2));

            var service = new SearchAndFilterService(
                new InMemoryGamesRepository(games),
                new InMemoryUsersRepository(new[] { CreateUser(1, "Cluj") }),
                new InMemoryRentalsRepository(),
                new InMemoryGeographicalService());

            var page1 = service.GetDiscoveryFeedPaged(1, 1, 10);
            var page2 = service.GetDiscoveryFeedPaged(1, 2, 10);

            var ids1 = page1.availableTonight.Concat(page1.others).Select(g => g.GameId);
            var ids2 = page2.availableTonight.Concat(page2.others).Select(g => g.GameId);

            Assert.NotEqual(ids1.First(), ids2.First());
        }

        [Fact]
        public void GetDiscoveryFeedPaged_DoesNotRequireAuthentication()
        {
            var service = new SearchAndFilterService(
                new InMemoryGamesRepository(Array.Empty<Game>()),
                new InMemoryUsersRepository(Array.Empty<User>()),
                new InMemoryRentalsRepository(),
                new InMemoryGeographicalService());

            var result = service.GetDiscoveryFeedPaged(-1, 1, 10);

            Assert.NotNull(result);
        }
    }
}
