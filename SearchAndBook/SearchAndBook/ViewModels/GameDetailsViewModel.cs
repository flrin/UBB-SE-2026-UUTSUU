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
    /// Provides details for a specific game, including pricing, availability, and booking commands.
    /// </summary>
    public class GameDetailsViewModel : INotifyPropertyChanged
    {
        private const long UnregisteredUserID = -1;
        private const long StartOfStreamPosition = 0;
        private const decimal DefaultTotalPrice = 0;
        private readonly InterfaceBookingService bookingService;
        private bool hasError;
        private decimal totalPrice;
        private BitmapImage? gameImage;
        private string? ownerImageUrl;
        private BookingDTO gameAndUserDetail;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDetailsViewModel"/> class.
        /// </summary>
        /// <param name="bookingService">The service used for booking operations and data retrieval.</param>
        /// <param name="gameId">The unique identifier of the game to display details for.</param>
        public GameDetailsViewModel(InterfaceBookingService bookingService, int gameId)
        {
            this.bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
            this.gameAndUserDetail = this.bookingService.GetBookingInformationForSpecificGame(gameId);
            try
            {
                this.GameAndUserDetails = this.bookingService.GetBookingInformationForSpecificGame(gameId);
                this.UnavailableTimeRanges = this.bookingService.GetUnavailableTimeRanges(gameId) ?? Array.Empty<TimeRange>();
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

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Occurs when a navigation back request is made.
        /// </summary>
        public event Action? OnGoBackRequested;

        /// <summary>
        /// Occurs when the user requests to start the booking process for a specific game and time range.
        /// </summary>
        public event Action<BookingDTO, TimeRange>? OnStartBookingRequested;

        /// <summary>
        /// Occurs when a message or notification needs to be displayed to the user.
        /// </summary>
        public event Action<string>? OnMessageRequested;

        /// <summary>
        /// Gets the current date.
        /// </summary>
        public DateTimeOffset Today => DateTimeOffset.Now.Date;

        /// <summary>
        /// Gets the combined details of the game and its owner.
        /// </summary>
        public BookingDTO GameAndUserDetails
        {
            get => this.gameAndUserDetail;
            private set
            {
                this.gameAndUserDetail = value;
                this.OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets a value indicating whether the view model is in an error state.
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
        /// Gets the total calculated price for the current selection.
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
        /// Gets the image of the game.
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
        /// Gets the URL of the owner's profile image.
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
        /// Gets the array of time ranges when the game is already booked.
        /// </summary>
        public TimeRange[] UnavailableTimeRanges { get; private set; } = Array.Empty<TimeRange>();

        /// <summary>
        /// Gets the command to navigate back to the previous view.
        /// </summary>
        public ICommand GoBackCommand => new RelayCommand(_ => this.GoBack());

        /// <summary>
        /// Gets the command to initiate the booking process for the selected game.
        /// </summary>
        public ICommand BookCommand => new RelayCommand(commandParameter =>
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

        /// <summary>
        /// Gets the command to initiate the booking process for the selected game.
        /// </summary>
        public ICommand ChatWithOwnerCommand => new RelayCommand(_ => { /* later */ });

        /// <summary>
        /// Checks if the game is available for a given time range.
        /// </summary>
        /// <param name="timeRange">The period to check for availability.</param>
        /// <returns>True if available; otherwise, false.</returns>
        public bool CheckGameAvailability(TimeRange timeRange)
        {
            try
            {
                if (timeRange == null)
                {
                    return false;
                }

                return this.bookingService.CheckGameAvailability(this.GameAndUserDetails.GameId, timeRange);
            }
            catch (Exception exception)
            {
                this.OnMessageRequested?.Invoke($"Could not check availability. {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Calculates the price for a specific booking duration.
        /// </summary>
        /// <param name="timeRange">The booking duration.</param>
        /// <returns>The total calculated price.</returns>
        public decimal CalculatePrice(TimeRange timeRange)
        {
            try
            {
                if (timeRange == null)
                {
                    throw new ArgumentNullException(nameof(timeRange));
                }

                this.TotalPrice = this.bookingService.CalculateTotalPriceForRentingASpecificGame(this.GameAndUserDetails.Price, timeRange);
                return this.TotalPrice;
            }
            catch (Exception exception)
            {
                this.OnMessageRequested?.Invoke($"Could not calculate price. {exception.Message}");
                this.TotalPrice = DefaultTotalPrice;
                return DefaultTotalPrice;
            }
        }

        /// <summary>
        /// Initiates the booking flow for the selected game.
        /// </summary>
        /// <param name="timeRange">The chosen time range for the reservation.</param>
        public void StartBooking(TimeRange timeRange)
        {
            try
            {
                if (SessionContext.GetInstance().UserId == UnregisteredUserID)
                {
                    this.OnMessageRequested?.Invoke("User not logged in. Please log in first");

                    // TODO login
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
        /// Triggers the go-back navigation logic.
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

        private void OnPropertyChanged([CallerMemberName] string? name = null)
           => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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