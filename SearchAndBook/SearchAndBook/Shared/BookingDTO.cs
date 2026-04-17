namespace SearchAndBook.Shared
{
    using System;

    /// <summary>
    /// Data transfer object used for displaying booking-related game and owner details.
    /// </summary>
    public class BookingDTO
    {
        /// <summary>
        /// Gets or sets the unique identifier of the game.
        /// </summary>
        public int GameId { get; set; }

        /// <summary>
        /// Gets or sets the name of the game.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the image of the game.
        /// </summary>
        public byte[]? Image { get; set; }

        /// <summary>
        /// Gets or sets the rental price per day.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the city where the game is located.
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the minimum number of players.
        /// </summary>
        public int MinimumNrPlayers { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of players.
        /// </summary>
        public int MaximumNrPlayers { get; set; }

        /// <summary>
        /// Gets or sets the description of the game.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the owner user id.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the display name of the owner.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the owner is suspended.
        /// </summary>
        public bool IsSuspended { get; set; }

        /// <summary>
        /// Gets or sets the avatar URL of the owner.
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Gets or sets the account creation date of the owner.
        /// </summary>
        public DateTime CreatedAt { get; set; }
    }
}