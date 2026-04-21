using SearchAndBook.Domain;
using SearchAndBook.Shared;

namespace SearchAndBook.Tests.Shared;

public class FilterCriteriaTests
{
    [Fact]
    public void Reset_WhenCalled_ClearsAllFilteringState()
    {
        var criteria = new FilterCriteria
        {
            Name = "Catan",
            City = "Cluj",
            AvailabilityRange = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2)),
            MaximumPrice = 50m,
            PlayerCount = 4,
            SortOption = SortOption.PriceAscending,
            UserId = 7,
        };

        criteria.Reset();

        Assert.Null(criteria.Name);
        Assert.Null(criteria.City);
        Assert.Null(criteria.AvailabilityRange);
        Assert.Null(criteria.MaximumPrice);
        Assert.Null(criteria.PlayerCount);
        Assert.Equal(SortOption.None, criteria.SortOption);
        Assert.Null(criteria.UserId);
    }

    [Fact]
    public void HasValidAvailabilityRange_WhenRangeIsNull_ReturnsTrue()
    {
        var criteria = new FilterCriteria();

        var result = criteria.HasValidAvailabilityRange();

        Assert.True(result);
    }

    [Fact]
    public void HasValidAvailabilityRange_WhenRangeIsAscending_ReturnsTrue()
    {
        var criteria = new FilterCriteria
        {
            AvailabilityRange = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2)),
        };

        var result = criteria.HasValidAvailabilityRange();

        Assert.True(result);
    }

    [Fact]
    public void HasValidAvailabilityRange_WhenRangeIsEqual_ReturnsFalse()
    {
        var criteria = new FilterCriteria
        {
            AvailabilityRange = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2)),
        };

        criteria.AvailabilityRange = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 1).AddMilliseconds(1));
        criteria.AvailabilityRange.EndTime = criteria.AvailabilityRange.StartTime;

        var result = criteria.HasValidAvailabilityRange();

        Assert.False(result);
    }

    [Fact]
    public void HasValidAvailabilityRange_WhenRangeIsDescending_ReturnsFalse()
    {
        var criteria = new FilterCriteria
        {
            AvailabilityRange = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2)),
        };

        criteria.AvailabilityRange = new TimeRange(new DateTime(2026, 1, 1), new DateTime(2026, 1, 2));
        criteria.AvailabilityRange.StartTime = new DateTime(2026, 1, 3);
        criteria.AvailabilityRange.EndTime = new DateTime(2026, 1, 2);

        var result = criteria.HasValidAvailabilityRange();

        Assert.False(result);
    }
}
