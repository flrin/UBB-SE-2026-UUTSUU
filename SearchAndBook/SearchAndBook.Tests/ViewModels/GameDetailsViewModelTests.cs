using Moq;
using SearchAndBook.Domain;
using SearchAndBook.Services;
using SearchAndBook.Shared;
using SearchAndBook.ViewModels;

namespace SearchAndBook.Tests.ViewModels;

public class GameDetailsViewModelTests
{
    [Fact]
    public void Constructor_ServiceReturnsData_SetsPropertiesCorrectly()
    {
        var (sut, _, messages) = CreateSut();

        Assert.False(sut.HasError);
        Assert.NotNull(sut.GameAndUserDetails);
        Assert.NotNull(sut.UnavailableTimeRanges);
        Assert.Empty(messages);
    }

    [Fact]
    public void Constructor_ServiceThrowsException_SetsErrorAndRaisesMessage()
    {
        var bookingService = new Mock<InterfaceBookingService>();
        bookingService.Setup(s => s.GetBookingInformationForSpecificGame(It.IsAny<int>()))
            .Throws(new Exception("boom"));

        var messages = new List<string>();
        var sut = new GameDetailsViewModel(bookingService.Object, 1);
        sut.OnMessageRequested += messages.Add;

        Assert.True(sut.HasError);
        Assert.Empty(sut.UnavailableTimeRanges);
    }

    [Fact]
    public void CheckGameAvailability_RangeIsNull_ReturnsFalse()
    {
        var (sut, _, _) = CreateSut();

        var result = sut.CheckGameAvailability(null!);

        Assert.False(result);
    }

    [Fact]
    public void CheckGameAvailability_ServiceReturnsTrue_ReturnsTrue()
    {
        var (sut, bookingService, _) = CreateSut();

        var range = CreateRange();
        bookingService.Setup(s => s.CheckGameAvailability(It.IsAny<int>(), range)).Returns(true);

        var result = sut.CheckGameAvailability(range);

        Assert.True(result);
    }

    [Fact]
    public void CheckGameAvailability_ServiceThrowsException_ReturnsFalseAndRaisesMessage()
    {
        var (sut, bookingService, messages) = CreateSut();

        var range = CreateRange();
        bookingService.Setup(s => s.CheckGameAvailability(It.IsAny<int>(), range))
            .Throws(new Exception("boom"));

        var result = sut.CheckGameAvailability(range);

        Assert.False(result);
        Assert.Single(messages);
    }

    [Fact]
    public void CalculatePrice_ValidRange_ReturnsCorrectTotal()
    {
        var (sut, bookingService, _) = CreateSut();

        var range = CreateRange();
        bookingService.Setup(s => s.CalculateTotalPriceForRentingASpecificGame(It.IsAny<decimal>(), range))
            .Returns(100);

        var result = sut.CalculatePrice(range);

        Assert.Equal(100, result);
        Assert.Equal(100, sut.TotalPrice);
    }

    [Fact]
    public void CalculatePrice_RangeIsNull_ReturnsZeroAndRaisesMessage()
    {
        var (sut, _, messages) = CreateSut();

        var result = sut.CalculatePrice(null!);

        Assert.Equal(0, result);
        Assert.Equal(0, sut.TotalPrice);
        Assert.Single(messages);
    }

    [Fact]
    public void CalculatePrice_ServiceThrowsException_ReturnsZeroAndRaisesMessage()
    {
        var (sut, bookingService, messages) = CreateSut();

        var range = CreateRange();
        bookingService.Setup(s => s.CalculateTotalPriceForRentingASpecificGame(It.IsAny<decimal>(), range))
            .Throws(new Exception("boom"));

        var result = sut.CalculatePrice(range);

        Assert.Equal(0, result);
        Assert.Equal(0, sut.TotalPrice);
        Assert.Single(messages);
    }

    [Fact]
    public void StartBooking_UserNotLoggedIn_RaisesMessage()
    {
        var (sut, _, messages) = CreateSut();

        SessionContext.GetInstance().Clear();

        sut.StartBooking(CreateRange());

        Assert.Single(messages);
        Assert.Contains("User not logged in", messages[0]);
    }

    [Fact]
    public void StartBooking_RangeIsNull_RaisesMessage()
    {
        var (sut, _, messages) = CreateSut();

        SessionContext.GetInstance().Populate(CreateUser());

        sut.StartBooking(null!);

        Assert.Single(messages);
    }

    [Fact]
    public void StartBooking_ValidRange_RaisesStartBookingEvent()
    {
        var (sut, _, _) = CreateSut();

        SessionContext.GetInstance().Populate(CreateUser());

        BookingDTO? dto = null;
        TimeRange? rangeResult = null;

        sut.OnStartBookingRequested += (d, r) =>
        {
            dto = d;
            rangeResult = r;
        };

        var range = CreateRange();
        sut.StartBooking(range);

        Assert.NotNull(dto);
        Assert.Equal(range, rangeResult);
    }

    [Fact]
    public void GoBack_Invoked_RaisesEvent()
    {
        var (sut, _, _) = CreateSut();

        bool called = false;
        sut.OnGoBackRequested += () => called = true;

        sut.GoBack();

        Assert.True(called);
    }

    [Fact]
    public void BookCommand_InvalidParameter_RaisesMessage()
    {
        var (sut, _, messages) = CreateSut();

        sut.BookCommand.Execute("invalid");

        Assert.Single(messages);
        Assert.Contains("Invalid booking interval", messages[0]);
    }

    [Fact]
    public void BookCommand_ValidParameter_StartsBooking()
    {
        var (sut, _, _) = CreateSut();

        SessionContext.GetInstance().Populate(CreateUser());

        bool called = false;
        sut.OnStartBookingRequested += (_, _) => called = true;

        var range = CreateRange();
        sut.BookCommand.Execute(range);

        Assert.True(called);
    }


    private static (GameDetailsViewModel Sut, Mock<InterfaceBookingService> BookingService, List<string> Messages)
        CreateSut()
    {
        var bookingService = new Mock<InterfaceBookingService>(MockBehavior.Loose);

        bookingService.Setup(s => s.GetBookingInformationForSpecificGame(It.IsAny<int>()))
            .Returns(CreateDto());

        bookingService.Setup(s => s.GetUnavailableTimeRanges(It.IsAny<int>()))
            .Returns(Array.Empty<TimeRange>());

        var sut = new GameDetailsViewModel(bookingService.Object, 1);

        var messages = new List<string>();
        sut.OnMessageRequested += messages.Add;

        return (sut, bookingService, messages);
    }

    private static BookingDTO CreateDto()
    {
        return new BookingDTO
        {
            GameId = 1,
            Name = "Catan",
            Price = 10,
            City = "Cluj",
            MinimumNrPlayers = 2,
            MaximumNumberPlayers = 4,
            Description = "desc",
            UserId = 1,
            DisplayName = "Owner"
        };
    }

    private static TimeRange CreateRange()
    {
        return new TimeRange(DateTime.Now, DateTime.Now.AddDays(1));
    }

    private static User CreateUser()
    {
        return new User
        {
            UserId = 1,
            Username = "user",
            DisplayName = "User",
            Email = "test@test.com",
            PasswordHash = "hash",
            City = "Cluj",
            Country = "RO"
        };
    }
}