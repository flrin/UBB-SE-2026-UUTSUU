namespace SearchAndBook.Domain
{
    using System;

    /// <summary>
    /// Represents a game with associated metadata, ownership, and configuration details.
    /// </summary>
    /// <remarks>The Game class encapsulates information about a game, including its name, description, player
    /// limits, price, and status. It is typically used to store and transfer game-related data within an
    /// application.</remarks>
    public class Game
    {
        /// <summary>
        /// Gets or sets the unique identifier for the game.
        /// </summary>
        public int GameId { get; set; }

        /// <summary>
        /// Gets or sets the unique identifier of the owner associated with this entity.
        /// </summary>
        public int OwnerId { get; set; }

        /// <summary>
        /// Gets or sets the name associated with this instance.
        /// </summary>
        required public string Name { get; set; }

        /// <summary>
        /// Gets or sets the price associated with the item.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of players allowed.
        /// </summary>
        public int MaximumPlayerNumber { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of players required to start the game.
        /// </summary>
        public int MinimumPlayerNumber { get; set; }

        /// <summary>
        /// Gets or sets the description associated with this instance.
        /// </summary>
        required public string Description { get; set; }

        /// <summary>
        /// Gets or sets the image data as a byte array.
        /// </summary>
        public byte[]? Image { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the object is active.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
