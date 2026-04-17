using SearchAndBook.Domain;

namespace SearchAndBook.Shared
{
    public class SessionContext
    {
        private const int UNREGISTERED_USER_ID = -1;
        private static SessionContext? _instance;

        public int UserId { get; set; }
        public bool IsLoggedIn { get; set; }

        private SessionContext()
        {
            UserId = UNREGISTERED_USER_ID;
            IsLoggedIn = false;
        }

        public static SessionContext GetInstance()
        {
            if (_instance == null)
            {
                _instance = new SessionContext();
            }
            return _instance;
        }

        public void Populate(User user)
        {
            if (user != null)
            {
                UserId = user.UserId;
                IsLoggedIn = true;
            }
        }

        public void Clear()
        {
            UserId = UNREGISTERED_USER_ID;
            IsLoggedIn = false;
        }
    }
}