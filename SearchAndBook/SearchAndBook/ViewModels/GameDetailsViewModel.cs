namespace SearchAndBook.ViewModels
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Windows.Input;
    using Microsoft.UI.Xaml.Media.Imaging;
    using SearchAndBook.CommandHandler;
    using SearchAndBook.Domain;
    using SearchAndBook.Services;
    using SearchAndBook.Shared;
    using Windows.Storage.Streams;

    /// <summary>
    /// View model used for displaying a game's details and starting a booking.
    /// </summary>
    public class GameDetailsViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// Special id used for users that are not logged in.
        /// </summary>
        private const long UnregisteredUserId = -1;

        /// <summary>
        /// The start position for the image stream.
        /// </summary>
        private const long StartOfStreamPosition = 0;

        private readonly InterfaceBookingService bookingService;
        private BookingDTO gameAndUserDetails = null!;
        private bool hasError;
        private decimal totalPrice;
        private BitmapImage? gameImage;
        private string? ownerImageUrl;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDetailsViewModel"/> class.
        /// </summary>
        /// <param name="bookingService">The booking service.</param>
        /// <param name="gameId">The id of the game.</param>
        public GameDetailsViewModel(InterfaceBookingService bookingService, int gameId)
        {
            this.bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));

            this.GoBackCommand = new RelayCommand(_ => this.GoBack());

            this.BookCommand = new RelayCommand(commandParameter =>
            {
                try
                {
                    if (commandParameter is TimeRange timeRange)
                    {
                        this.StartBooking(timeRange);
                    }
                    else
                    {
                        this.OnMessageRequested?.Invoke("Invalid booking interval selected.");
                    }
                }
                catch (Exception exception)
                {
                    this.OnMessageRequested?.Invoke($"Could not start booking. {exception.Message}");
                }
            });

            this.ChatWithOwnerCommand = new RelayCommand(_ => { });

            try
            {
                this.GameAndUserDetails = this.bookingService.GetGameDetails(gameId);
                this.UnavailableTimeRanges = this.bookingService.GetUnavailableRanges(gameId) ?? Array.Empty<TimeRange>();
                this.LoadGameImage();
                this.LoadOwnerImage();
                this.HasError = false;
            }
            catch (Exception exception)
            {
                this.HasError = true;
                this.UnavailableTimeRanges = Array.Empty<TimeRange>();
                this.OnMessageRequested?.Invoke($"Could not load game details. {exception.Message}");
            }
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raised when the UI should go back to the previous page.
        /// </summary>
        public event Action? OnGoBackRequested;

        /// <summary>
        /// Raised when the booking flow should start.
        /// </summary>
        public event Action<BookingDTO, TimeRange>? OnStartBookingRequested;

        /// <summary>
        /// Raised when a message should be shown to the user.
        /// </summary>
        public event Action<string>? OnMessageRequested;

        /// <summary>
        /// Gets today's date.
        /// </summary>
        public DateTimeOffset Today => DateTimeOffset.Now.Date;

        /// <summary>
        /// Gets the game and owner details.
        /// </summary>
        public BookingDTO GameAndUserDetails
        {
            get => this.gameAndUserDetails;
            private set
            {
                this.gameAndUserDetails = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets a value indicating whether an error occurred.
        /// </summary>
        public bool HasError
        {
            get => this.hasError;
            private set
            {
                this.hasError = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the total calculated price.
        /// </summary>
        public decimal TotalPrice
        {
            get => this.totalPrice;
            private set
            {
                this.totalPrice = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the game image.
        /// </summary>
        public BitmapImage? GameImage
        {
            get => this.gameImage;
            private set
            {
                this.gameImage = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the owner's image url.
        /// </summary>
        public string? OwnerImageUrl
        {
            get => this.ownerImageUrl;
            private set
            {
                this.ownerImageUrl = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the unavailable booking intervals.
        /// </summary>
        public TimeRange[] UnavailableTimeRanges { get; private set; } = Array.Empty<TimeRange>();

        /// <summary>
        /// Gets the command used for going back.
        /// </summary>
        public ICommand GoBackCommand { get; }

        /// <summary>
        /// Gets the command used for starting a booking.
        /// </summary>
        public ICommand BookCommand { get; }

        /// <summary>
        /// Gets the command used for starting a chat with the owner.
        /// </summary>
        public ICommand ChatWithOwnerCommand { get; }

        /// <summary>
        /// Checks whether a selected time range is available.
        /// </summary>
        /// <param name="timeRange">The selected time range.</param>
        /// <returns>True if available, false otherwise.</returns>
        public bool CheckAvailability(TimeRange timeRange)
        {
            try
            {
                if (timeRange == null)
                {
                    return false;
                }

                return this.bookingService.CheckAvailability(this.GameAndUserDetails.GameId, timeRange);
            }
            catch (Exception exception)
            {
                this.OnMessageRequested?.Invoke($"Could not check availability. {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Calculates the booking price for a selected time range.
        /// </summary>
        /// <param name="timeRange">The selected time range.</param>
        /// <returns>The calculated total price.</returns>
        public decimal CalculatePrice(TimeRange timeRange)
        {
            try
            {
                if (timeRange == null)
                {
                    throw new ArgumentNullException(nameof(timeRange));
                }

                this.TotalPrice = this.bookingService.CalculateTotalPrice(this.GameAndUserDetails.Price, timeRange);
                return this.TotalPrice;
            }
            catch (Exception exception)
            {
                this.OnMessageRequested?.Invoke($"Could not calculate price. {exception.Message}");
                this.TotalPrice = 0;
                return 0;
            }
        }

        /// <summary>
        /// Starts the booking process.
        /// </summary>
        /// <param name="timeRange">The selected time range.</param>
        public void StartBooking(TimeRange timeRange)
        {
            try
            {
                if (SessionContext.GetInstance().UserId == UnregisteredUserId)
                {
                    this.OnMessageRequested?.Invoke("User not logged in. Please log in first");
                    return;
                }

                if (timeRange == null)
                {
                    this.OnMessageRequested?.Invoke("Please select a valid booking timeRange.");
                    return;
                }

                this.OnStartBookingRequested?.Invoke(this.GameAndUserDetails, timeRange);
            }
            catch (Exception exception)
            {
                this.OnMessageRequested?.Invoke($"Could not continue to booking. {exception.Message}");
            }
        }

        /// <summary>
        /// Requests navigation back.
        /// </summary>
        public void GoBack()
        {
            try
            {
                this.OnGoBackRequested?.Invoke();
            }
            catch (Exception exception)
            {
                this.OnMessageRequested?.Invoke($"Could not go back. {exception.Message}");
            }
        }

        /// <summary>
        /// Raises the property changed event.
        /// </summary>
        /// <param name="name">The property name.</param>
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private async void LoadGameImage()
        {
            try
            {
                if (this.GameAndUserDetails.Image == null || this.GameAndUserDetails.Image.Length == 0)
                {
                    this.GameImage = null;
                    return;
                }

                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(this.GameAndUserDetails.Image.AsBuffer());
                stream.Seek(StartOfStreamPosition);

                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                this.GameImage = bitmap;
            }
            catch (Exception exception)
            {
                this.GameImage = null;
                this.OnMessageRequested?.Invoke($"Could not load game image. {exception.Message}");
            }
        }

        private void LoadOwnerImage()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(this.GameAndUserDetails.AvatarUrl))
                {
                    this.OwnerImageUrl = null;
                    return;
                }

                this.OwnerImageUrl = this.GameAndUserDetails.AvatarUrl;
            }
            catch (Exception exception)
            {
                this.OwnerImageUrl = null;
                this.OnMessageRequested?.Invoke($"Could not load owner image. {exception.Message}");
            }
        }
    }
}