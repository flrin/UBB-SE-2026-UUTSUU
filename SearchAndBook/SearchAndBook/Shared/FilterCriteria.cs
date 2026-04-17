using SearchAndBook.Domain;

namespace SearchAndBook.Shared;

public class FilterCriteria
{
    public string? Name { get; set; }
    public string? City { get; set; }
    public TimeRange? AvailabilityRange { get; set; }
    public decimal? MaximumPrice { get; set; }
    public int? PlayerCount { get; set; }
    public SortOption SortOption { get; set; }
    public int? UserId { get; set; }

    public FilterCriteria()
    {
        Reset();
    }

    public void Reset()
    {
        Name = null;
        City = null;
        AvailabilityRange = null;
        MaximumPrice = null;
        PlayerCount = null;
        SortOption = SortOption.None;
        UserId = null;
    }

    public bool HasValidAvailabilityRange()
    {
        if (AvailabilityRange == null)
        {
            return true;
        }

        return AvailabilityRange.StartTime < AvailabilityRange.EndTime;
    }
}