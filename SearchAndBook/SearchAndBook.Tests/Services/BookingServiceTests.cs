using Moq;
using SearchAndBook.Domain;
using SearchAndBook.Repositories;
using SearchAndBook.Services;
using SearchAndBook.Shared;

namespace SearchAndBook.Tests.Services;

public class BookingServiceTests
{
    [Fact]
    public void GetBookingInformationForSpecificGame_GameAndOwnerExist_ReturnsMappedBookingDto()
    {
        var sut = CreateSut(out var gamesRepository, out _, out var usersRepository);

        var game = CreateGame(1, 10, "Catan", 25m, 4, 2, "Classic board game");
        var owner = CreateUser(10, "Owner Name", "Cluj");

        gamesRepository.Setup(repository => repository.GetGameById(1)).Returns(game);
        usersRepository.Setup(repository => repository.GetGameById(10)).Returns(owner);

        var result = sut.GetBookingInformationForSpecificGame(1);

        var expected = new BookingDTO
        {
            GameId = game.GameId,
            Name = game.Name,
            Image = game.Image,
            Price = game.Price,
            City = owner.City,
            MinimumNrPlayers = game.MinimumPlayerNumber,
            MaximumNumberPlayers = game.MaximumPlayerNumber,
            Description = game.Description,
            UserId = owner.UserId,
            DisplayName = owner.DisplayName,
            IsSuspended = owner.IsSuspended,
            AvatarUrl = owner.AvatarUrl,
            CreatedAt = owner.CreatedAt
        };

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetBookingInformationForSpecificGame_GameDoesNotExist_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(out var gamesRepository, out _, out _);

        gamesRepository.Setup(repository => repository.GetGameById(1)).Returns((Game?)null);

        var exception = Assert.Throws<InvalidOperationException>(() => sut.GetBookingInformationForSpecificGame(1));

        Assert.Equal("Failed to retrieve details for game 1.", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void GetBookingInformationForSpecificGame_OwnerDoesNotExist_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(out var gamesRepository, out _, out var usersRepository);

        var game = CreateGame(1, 10, "Catan", 25m, 4, 2, "Classic board game");

        gamesRepository.Setup(repository => repository.GetGameById(1)).Returns(game);
        usersRepository.Setup(repository => repository.GetGameById(10)).Returns((User?)null);

        var exception = Assert.Throws<InvalidOperationException>(() => sut.GetBookingInformationForSpecificGame(1));

        Assert.Equal("Failed to retrieve details for game 1.", exception.Message);
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    [Fact]
    public void GetUnavailableTimeRanges_RepositoryReturnsRanges_ReturnsArray()
    {
        var sut = CreateSut(out _, out var rentalsRepository, out _);
        var ranges = new List<TimeRange>
        {
            new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 3)),
            new TimeRange(new DateTime(2026, 2, 1), new DateTime(2026, 2, 2))
        };

        rentalsRepository.Setup(repository => repository.GetUnavailableTimeRanges(1)).Returns(ranges);

        var result = sut.GetUnavailableTimeRanges(1);

        Assert.Equal(2, result.Length);
        Assert.Equal(ranges[0], result[0]);
        Assert.Equal(ranges[1], result[1]);
    }

    [Fact]
    public void GetUnavailableTimeRanges_RepositoryThrows_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(out _, out var rentalsRepository, out _);

        rentalsRepository.Setup(repository => repository.GetUnavailableTimeRanges(1))
            .Throws(new Exception("boom"));

        var exception = Assert.Throws<InvalidOperationException>(() => sut.GetUnavailableTimeRanges(1));

        Assert.Equal("Failed to retrieve unavailable time ranges for game 1.", exception.Message);
        Assert.IsType<Exception>(exception.InnerException);
    }

    [Fact]
    public void CheckGameAvailability_RepositoryReturnsTrue_ReturnsTrue()
    {
        var sut = CreateSut(out _, out var rentalsRepository, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));

        rentalsRepository.Setup(repository => repository.CheckGameAvailability(range, 1)).Returns(true);

        var result = sut.CheckGameAvailability(1, range);

        Assert.True(result);
    }

    [Fact]
    public void CheckGameAvailability_RepositoryReturnsFalse_ReturnsFalse()
    {
        var sut = CreateSut(out _, out var rentalsRepository, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));

        rentalsRepository.Setup(repository => repository.CheckGameAvailability(range, 1)).Returns(false);

        var result = sut.CheckGameAvailability(1, range);

        Assert.False(result);
    }

    [Fact]
    public void CheckGameAvailability_RepositoryThrows_ThrowsInvalidOperationException()
    {
        var sut = CreateSut(out _, out var rentalsRepository, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));

        rentalsRepository.Setup(repository => repository.CheckGameAvailability(range, 1))
            .Throws(new Exception("boom"));

        var exception = Assert.Throws<InvalidOperationException>(() => sut.CheckGameAvailability(1, range));

        Assert.Equal("Failed to check availability for game 1.", exception.Message);
        Assert.IsType<Exception>(exception.InnerException);
    }

    [Fact]
    public void CalculateTotalPriceForRentingASpecificGame_RangeIsSameDay_ReturnsOneDayPrice()
    {
        var sut = CreateSut(out _, out _, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 1));

        var result = sut.CalculateTotalPriceForRentingASpecificGame(20m, range);

        Assert.Equal(20m, result);
    }

    [Fact]
    public void CalculateTotalPriceForRentingASpecificGame_RangeSpansMultipleDays_ReturnsPriceTimesDays()
    {
        var sut = CreateSut(out _, out _, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 3));

        var result = sut.CalculateTotalPriceForRentingASpecificGame(20m, range);

        Assert.Equal(60m, result);
    }

    [Fact]
    public void CalculateNumberOfDaysInAGivenTimeRange_RangeIsSameDay_ReturnsOne()
    {
        var sut = CreateSut(out _, out _, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 1));

        var result = sut.CalculateNumberOfDaysInAGivenTimeRange(range);

        Assert.Equal(1, result);
    }

    [Fact]
    public void CalculateNumberOfDaysInAGivenTimeRange_RangeSpansMultipleDays_ReturnsCorrectCount()
    {
        var sut = CreateSut(out _, out _, out _);
        var range = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 4));

        var result = sut.CalculateNumberOfDaysInAGivenTimeRange(range);

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
            OwnerId = ownerId,
            Name = name,
            Price= price,
            MaximumPlayerNumber = maximumPlayers,
            MinimumPlayerNumber = minimumPlayers,
            Description = description,
            Image = new byte[] { 1, 2, 3 },
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