namespace SearchAndBook.Domain
{
    using System;

    /// <summary>
    /// Represents a user account with profile, authentication, and contact information.
    /// </summary>
    /// <remarks>The User class encapsulates identifying, authentication, and contact details for an
    /// individual user. It is typically used to store and transfer user-related data within applications that require
    /// user management, such as authentication systems or user profile features.</remarks>
    public class User
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets the username associated with the current user or account.
        /// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the display name associated with the object.
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the email address associated with the user.
        /// </summary>
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the hashed representation of the user's password.
        /// </summary>
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the phone number associated with the entity.
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Gets or sets the URL of the user's avatar image.
        /// </summary>
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the current object is suspended.
        /// </summary>
        public bool IsSuspended { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the entity was created.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the date and time when the entity was last updated.
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Gets or sets the name of the street for the address.
        /// </summary>
        public string? StreetName { get; set; }

        /// <summary>
        /// Gets or sets the street number component of the address.
        /// </summary>
        public string? StreetNumber { get; set; }

        /// <summary>
        /// Gets or sets the name of the city associated with the entity.
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the country associated with the entity.
        /// </summary>
        public string Country { get; set; } = string.Empty;
    }
}
