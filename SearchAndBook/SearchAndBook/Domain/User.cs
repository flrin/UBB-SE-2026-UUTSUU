using System;

namespace SearchAndBook.Domain
{
    public class User
    {
        public int UserId { get; set; }

        public string Username { get; set; } = "";

        public string DisplayName { get; set; } = "";

        public string Email { get; set; } = "";

        public string PasswordHash { get; set; } = "";

        public string? PhoneNumber { get; set; }

        public string? AvatarUrl { get; set; }

        public bool IsSuspended { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public string? StreetName { get; set; }

        public string? StreetNumber { get; set; }

        public string City { get; set; } = "";

        public string Country { get; set; } = "";
    }
}
