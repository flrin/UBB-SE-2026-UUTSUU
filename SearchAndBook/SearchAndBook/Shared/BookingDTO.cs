namespace SearchAndBook.Shared;
using System;

/// <summary>
/// Represents the data transfer object for a booking, containing information about the booked game, user, and related
/// details.
/// </summary>
/// <remarks>This record is typically used to transfer booking data between application layers or over service
/// boundaries. It includes both game-specific and user-specific information relevant to a booking.</remarks>
public record BookingDTO
{
    /// <summary>
    /// Gets or sets the unique identifier for the game.
    /// </summary>
    public int GameId { get; set; }

    /// <summary>
    /// Gets or sets the name associated with the object.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the image data as a byte array.
    /// </summary>
    public byte[]? Image { get; set; }

    /// <summary>
    /// Gets or sets the price of the item.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the name of the city.
    /// </summary>
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the minimum number of players required to start the game.
    /// </summary>
    public int MinimumNrPlayers { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of players allowed.
    /// </summary>
    public int MaximumNumberPlayers { get; set; }

    /// <summary>
    /// Gets or sets the description associated with the current instance.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unique identifier for the user.
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Gets or sets the display name associated with the object.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the current object is suspended.
    /// </summary>
    public bool IsSuspended { get; set; }

    /// <summary>
    /// Gets or sets the URL of the user's avatar image.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}