namespace SearchAndBook.Shared
{
    using SearchAndBook.Domain;

    /// <summary>
    /// Provides a context for managing the current user's session state, including authentication status and user
    /// identification.
    /// </summary>
    /// <remarks>This class implements a singleton pattern to ensure a single session context instance is used
    /// throughout the application. Use the GetInstance method to access the current session context. The session
    /// context tracks whether a user is logged in and stores the user's identifier. This class is not
    /// thread-safe.</remarks>
    public class SessionContext
    {
        private const int UnregisteredUserID = -1;
        private static SessionContext? instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="SessionContext"/> class with default values.
        /// </summary>
        /// <remarks>This private constructor sets the user as unregistered and not logged in. It is
        /// intended for internal use to establish a default session state.</remarks>
        private SessionContext()
        {
            this.UserId = UnregisteredUserID;
            this.IsLoggedIn = false;
        }

        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the user is currently authenticated.
        /// </summary>
        public bool IsLoggedIn { get; set; }

        /// <summary>
        /// Retrieves the singleton instance of the SessionContext class.
        /// </summary>
        /// <remarks>This method implements the singleton pattern to ensure that only one instance of
        /// SessionContext exists throughout the application's lifetime. Subsequent calls return the same
        /// instance.</remarks>
        /// <returns>The single, shared instance of SessionContext.</returns>
        public static SessionContext GetInstance()
        {
            if (instance == null)
            {
                instance = new SessionContext();
            }

            return instance;
        }

        /// <summary>
        /// Populates the current instance with values from the specified user.
        /// </summary>
        /// <param name="user">The user whose information is used to populate the current instance. If null, no changes are made.</param>
        public void Populate(User user)
        {
            if (user != null)
            {
                this.UserId = user.UserId;
                this.IsLoggedIn = true;
            }
        }

        /// <summary>
        /// Resets the user state to indicate that no user is currently logged in.
        /// </summary>
        /// <remarks>Call this method to clear all authentication information and revert the user to an
        /// unregistered state. After calling this method, the user will be considered logged out.</remarks>
        public void Clear()
        {
            this.UserId = UnregisteredUserID;
            this.IsLoggedIn = false;
        }
    }
}