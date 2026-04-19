namespace SearchAndBook.Shared;
using SearchAndBook.Domain;

/// <summary>
/// Represents the criteria used for searching and filtering games.
/// </summary>
public class FilterCriteria
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilterCriteria"/> class.
    /// Initializes a new instance of the <see cref="FilterCriteria""")/>> class and resets it to default values.
    /// </summary>
    public FilterCriteria()
    {
        this.Reset();
    }

    /// <summary>
    /// Gets or sets the name or part of the name of the game to search for.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the city where the game or owner is located.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// Gets or sets the time range during which the game should be available.
    /// </summary>
    public TimeRange? AvailabilityRange { get; set; }

    /// <summary>
    /// Gets or sets the maximum price limit for the game rental.
    /// </summary>
    public decimal? MaximumPrice { get; set; }

    /// <summary>
    /// Gets or sets the required player count for the game.
    /// </summary>
    public int? PlayerCount { get; set; }

    /// <summary>
    /// Gets or sets the sorting preference for the search results.
    /// </summary>
    public SortOption SortOption { get; set; }

    /// <summary>
    /// Gets or sets the ID of the user performing the search (optional).
    /// </summary>
    public int? UserId { get; set; }

    /// <summary>
    /// Resets all filter properties to their default null or initial values.
    /// </summary>
    public void Reset()
    {
        this.Name = null;
        this.City = null;
        this.AvailabilityRange = null;
        this.MaximumPrice = null;
        this.PlayerCount = null;
        this.SortOption = SortOption.None;
        this.UserId = null;
    }

    /// <summary>
    /// Checks if the current availability range is logically valid (start time is before end time).
    /// </summary>
    /// <returns>True if the range is valid or null; otherwise, false.</returns>
    public bool HasValidAvailabilityRange()
    {
        if (this.AvailabilityRange == null)
        {
            return true;
        }

        return this.AvailabilityRange.StartTime < this.AvailabilityRange.EndTime;
    }
}
