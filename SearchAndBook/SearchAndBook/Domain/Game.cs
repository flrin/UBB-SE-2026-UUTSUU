using System;

namespace SearchAndBook.Domain
{
    public class Game
    {
        public int GameId { get; set; }

        public int OwnerId { get; set; }

        public required string Name { get; set; }

        public decimal Price { get; set; }

        public int MaximumPlayerNumber { get; set; }

        public int MinimumPlayerNumber { get; set; }

        public required string Description { get; set; }

        public byte[]? Image {  get; set; }

        public bool IsActive { get; set; }
    }
}
