using Moq;
using SearchAndBook.Domain;
using SearchAndBook.Repositories;
using SearchAndBook.Services;
using SearchAndBook.Shared;

namespace SearchAndBook.Tests.Services;

public class BookingServiceTests
{
    [Fact]
    public void GetGameDetails_WhenGameAndOwnerExist_ReturnsMappedBookingDto()
    {
        var sut = CreateSut(out var gamesRepository, out _, out var usersRepository);

        var game = CreateGame(1, 10, "Catan", 25m, 4, 2, "Classic board game");
        var owner = CreateUser(10, "Owner Name", "Cluj");

        gamesRepository.Setup(repository => repository.GetGameById(1)).Returns(game);
        usersRepository.Setup(repository => repository.GetGameById(10)).Returns(owner);

        var result = sut.GetGameDetails(1);

        Assert.Equal(game.GameId, result.GameId);
        Assert.Equal(game.NameOfTheGame, result.NameOfTheGame);
        Assert.Equal(game.GameImage, result.GameImage);
        Assert.Equal(game.PriceOfTheGame, result.PriceOfTheGame);
        Assert.Equal(owner.City, result.City);
        Assert.Equal(game.MinimumPlayerNumber, result.MinimumPlayerNumber);
        Assert.Equal(game.MaximumPlayerNumber, result.MaximumPlayerNumber);
        Assert.Equal(game.GameDescription, result.GameDescription);
        Assert.Equal(owner.UserId, result.UserId);
        Assert.Equal(owner.DisplayName, result.DisplayName);
        Assert.Equal(owner.IsSuspended, result.IsSuspended);
        Assert.Equal(owner.AvatarUrl, result.AvatarUrl);
        Assert.Equal(owner.CreatedAt, result.CreatedAt);
    }

    [Fact]
    public void GetGameDetails_WhenGameDoesNotExist_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(out var gamesRepository, out _, out _);

        gamesRepository.Setup(repository => repository.GetGameById(1)).Returns((Game?)null);

        var exception = Assert.Throws<InvalidOperationException>(() => sut.GetGameDetails(1));

        Assert.Equal("Failed to retrieve details for game 1.", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void GetGameDetails_WhenOwnerDoesNotExist_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(out var gamesRepository, out _, out var usersRepository);

        var game = CreateGame(1, 10, "Catan", 25m, 4, 2, "Classic board game");

        gamesRepository.Setup(repository => repository.GetGameById(1)).Returns(game);
        usersRepository.Setup(repository => repository.GetGameById(10)).Returns((User?)null);

        var exception = Assert.Throws<InvalidOperationException>(() => sut.GetGameDetails(1));

        Assert.Equal("Failed to retrieve details for game 1.", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void GetUnavailableRanges_WhenRepositoryReturnsRanges_ReturnsArray()
    {
        var sut = CreateSut(out _, out var rentalsRepository, out _);
        var ranges = new List<TimeRange>
        {
            new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 3)),
            new TimeRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 2))
        };

        rentalsRepository.Setup(repository => repository.GetUnavailableRanges(1)).Returns(ranges);

        var result = sut.GetUnavailableRanges(1);

        Assert.Equal(2, result.Length);
        Assert.Equal(ranges[0], result[0]);
        Assert.Equal(ranges[1], result[1]);
    }

    [Fact]
    public void GetUnavailableRanges_WhenRepositoryThrows_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(out _, out var rentalsRepository, out _);

        rentalsRepository.Setup(repository => repository.GetUnavailableRanges(1))
            .Throws(new Exception("boom"));

        var exception = Assert.Throws<InvalidOperationException>(() => sut.GetUnavailableRanges(1));

        Assert.Equal("Failed to retrieve unavailable time ranges for game 1.", exception.Message);
        Assert.IsType<Exception>(exception.InnerException);
    }

    [Fact]
    public void CheckAvailability_WhenRepositoryReturnsTrue_ReturnsTrue()
    {
        var sut = CreateSut(out _, out var rentalsRepository, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));

        rentalsRepository.Setup(repository => repository.CheckAvailability(range, 1)).Returns(true);

        var result = sut.CheckAvailability(1, range);

        Assert.True(result);
    }

    [Fact]
    public void CheckAvailability_WhenRepositoryReturnsFalse_ReturnsFalse()
    {
        var sut = CreateSut(out _, out var rentalsRepository, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));

        rentalsRepository.Setup(repository => repository.CheckAvailability(range, 1)).Returns(false);

        var result = sut.CheckAvailability(1, range);

        Assert.False(result);
    }

    [Fact]
    public void CheckAvailability_WhenRepositoryThrows_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(out _, out var rentalsRepository, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));

        rentalsRepository.Setup(repository => repository.CheckAvailability(range, 1))
            .Throws(new Exception("boom"));

        var exception = Assert.Throws<InvalidOperationException>(() => sut.CheckAvailability(1, range));

        Assert.Equal("Failed to check availability for game 1.", exception.Message);
        Assert.IsType<Exception>(exception.InnerException);
    }

    [Fact]
    public void CalculateTotalPrice_WhenRangeIsSameDay_ReturnsOneDayPrice()
    {
        var sut = CreateSut(out _, out _, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 1));

        var result = sut.CalculateTotalPrice(20m, range);

        Assert.Equal(20m, result);
    }

    [Fact]
    public void CalculateTotalPrice_WhenRangeSpansMultipleDays_ReturnsPriceTimesDays()
    {
        var sut = CreateSut(out _, out _, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 3));

        var result = sut.CalculateTotalPrice(20m, range);

        Assert.Equal(60m, result);
    }

    [Fact]
    public void CalculateNumberOfDays_WhenRangeIsSameDay_ReturnsOne()
    {
        var sut = CreateSut(out _, out _, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 1));

        var result = sut.CalculateNumberOfDays(range);

        Assert.Equal(1, result);
    }

    [Fact]
    public void CalculateNumberOfDays_WhenRangeSpansMultipleDays_ReturnsCorrectCount()
    {
        var sut = CreateSut(out _, out _, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 4));

        var result = sut.CalculateNumberOfDays(range);

        Assert.Equal(4, result);
    }

    private static BookingService CreateSut(
        out Mock<InterfaceGamesRepository> gamesRepository,
        out Mock<InterfaceRentalsRepository> rentalsRepository,
        out Mock<InterfaceUsersRepository> usersRepository)
    {
        gamesRepository = new Mock<InterfaceGamesRepository>(MockBehavior.Loose);
        rentalsRepository = new Mock<InterfaceRentalsRepository>(MockBehavior.Loose);
        usersRepository = new Mock<InterfaceUsersRepository>(MockBehavior.Loose);

        return new BookingService(
            gamesRepository.Object,
            rentalsRepository.Object,
            usersRepository.Object);
    }

    private static Game CreateGame(
        int gameId,
        int ownerId,
        string name,
        decimal price,
        int maximumPlayers,
        int minimumPlayers,
        string description)
    {
        return new Game
        {
            GameId = gameId,
            OwnerOfTheGameID = ownerId,
            NameOfTheGame = name,
            PriceOfTheGame = price,
            MaximumPlayerNumber = maximumPlayers,
            MinimumPlayerNumber = minimumPlayers,
            GameDescription = description,
            GameImage = new byte[] { 1, 2, 3 },
            IsActive = true
        };
    }

    private static User CreateUser(int userId, string displayName, string city)
    {
        return new User
        {
            UserId = userId,
            Username = "owner",
            DisplayName = displayName,
            Email = "owner@example.com",
            PasswordHash = "hash",
            City = city,
            Country = "RO",
            AvatarUrl = "https://example.com/avatar.png",
            CreatedAt = new DateTime(2025, 1, 1),
            IsSuspended = false
        };
    }
}