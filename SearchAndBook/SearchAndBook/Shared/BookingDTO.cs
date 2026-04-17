using System;

namespace SearchAndBook.Shared;

public class BookingDTO
{
    public int GameId { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte[]? Image { get; set; }
    public decimal Price { get; set; }
    public string City { get; set; } = string.Empty;
    public int MinimumNrPlayers { get; set; }
    public int MaximumNrPlayers { get; set; }
    public string Description {  get; set; } = string.Empty;

    public int UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsSuspended { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}