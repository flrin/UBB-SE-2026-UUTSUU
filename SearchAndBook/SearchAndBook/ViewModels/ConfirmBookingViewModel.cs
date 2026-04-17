namespace SearchAndBook.ViewModels
{
    using System;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices.WindowsRuntime;
    using Microsoft.UI.Xaml.Media.Imaging;
    using SearchAndBook.Domain;
    using SearchAndBook.Services;
    using SearchAndBook.Shared;
    using Windows.Storage.Streams;

    /// <summary>
    /// View model used for confirming a booking.
    /// </summary>
    internal class ConfirmBookingViewModel : INotifyPropertyChanged
    {
        private const long StartOfStreamPosition = 0;

        private readonly InterfaceBookingService bookingService;
        private BookingDTO gameAndUserDetails;
        private TimeRange selectedTimeRange;
        private decimal totalPrice;
        private BitmapImage? ownerImage;
        private BitmapImage? gameImage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmBookingViewModel"/> class.
        /// </summary>
        /// <param name="bookingService">The booking service.</param>
        /// <param name="gameAndUserDetails">The selected game and owner details.</param>
        /// <param name="selectedTimeRange">The selected booking interval.</param>
        public ConfirmBookingViewModel(
            InterfaceBookingService bookingService,
            BookingDTO gameAndUserDetails,
            TimeRange selectedTimeRange)
        {
            try
            {
                this.bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
                this.gameAndUserDetails = gameAndUserDetails ?? throw new ArgumentNullException(nameof(gameAndUserDetails));
                this.selectedTimeRange = selectedTimeRange ?? throw new ArgumentNullException(nameof(selectedTimeRange));

                this.UnavailableTimeRanges = this.bookingService.GetUnavailableRanges(this.GameAndUserDetails.GameId) ?? Array.Empty<TimeRange>();
                this.TotalPrice = this.CalculatePrice();
                this.LoadImages();
            }
            catch (Exception exception)
            {
                this.RaiseError($"Could not initialize booking confirmation. {exception.Message}");
                this.UnavailableTimeRanges = Array.Empty<TimeRange>();
                this.TotalPrice = 0;
            }
        }

        /// <summary>
        /// Raised when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raised when an error occurs.
        /// </summary>
        public event Action<string>? OnErrorOccurred;

        /// <summary>
        /// Raised when the user wants to go back.
        /// </summary>
        public event Action? OnGoBackRequested;

        /// <summary>
        /// Raised when the user confirms the booking.
        /// </summary>
        public event Action? OnConfirmBookingRequested;

        /// <summary>
        /// Gets the selected game and owner details.
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
        /// Gets the unavailable time ranges for the current game.
        /// </summary>
        public TimeRange[] UnavailableTimeRanges { get; private set; } = Array.Empty<TimeRange>();

        /// <summary>
        /// Gets the currently selected time range.
        /// </summary>
        public TimeRange SelectedTimeRange
        {
            get => this.selectedTimeRange;
            private set
            {
                this.selectedTimeRange = value;
                this.OnPropertyChanged();
                this.OnPropertyChanged(nameof(this.NumberOfDays));
                this.OnPropertyChanged(nameof(this.StartDate));
                this.OnPropertyChanged(nameof(this.EndDate));
            }
        }

        /// <summary>
        /// Gets the total booking price.
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
        /// Gets the formatted start date.
        /// </summary>
        public string StartDate => this.SelectedTimeRange.StartTime.ToString("dd MMM yyyy");

        /// <summary>
        /// Gets the formatted end date.
        /// </summary>
        public string EndDate => this.SelectedTimeRange.EndTime.ToString("dd MMM yyyy");

        /// <summary>
        /// Gets the owner image.
        /// </summary>
        public BitmapImage? OwnerImage
        {
            get => this.ownerImage;
            private set
            {
                this.ownerImage = value;
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
        /// Gets the number of selected booking days.
        /// </summary>
        public int NumberOfDays
        {
            get
            {
                try
                {
                    return this.bookingService.CalculateNumberOfDays(this.SelectedTimeRange);
                }
                catch
                {
                    return 1;
                }
            }
        }

        /// <summary>
        /// Checks if a time range is available.
        /// </summary>
        /// <param name="timeRange">The interval to check.</param>
        /// <returns><c>true</c> if available; otherwise, <c>false</c>.</returns>
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
                this.RaiseError($"Could not check availability. {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Confirms the booking.
        /// </summary>
        public void ConfirmBooking()
        {
            try
            {
                this.OnConfirmBookingRequested?.Invoke();
            }
            catch (Exception exception)
            {
                this.RaiseError($"Could not confirm booking. {exception.Message}");
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
                this.RaiseError($"Could not go back. {exception.Message}");
            }
        }

        /// <summary>
        /// Calculates the total booking price.
        /// </summary>
        /// <returns>The total price.</returns>
        public decimal CalculatePrice()
        {
            try
            {
                return this.bookingService.CalculateTotalPrice(this.GameAndUserDetails.Price, this.SelectedTimeRange);
            }
            catch (Exception exception)
            {
                this.RaiseError($"Could not calculate price. {exception.Message}");
                this.TotalPrice = 0;
                return 0;
            }
        }

        /// <summary>
        /// Updates the selected booking interval.
        /// </summary>
        /// <param name="newTimeRange">The new time range.</param>
        public void UpdateSelectedRange(TimeRange newTimeRange)
        {
            try
            {
                if (newTimeRange == null)
                {
                    throw new ArgumentNullException(nameof(newTimeRange));
                }

                this.SelectedTimeRange = newTimeRange;
                this.TotalPrice = this.CalculatePrice();
                this.OnPropertyChanged(nameof(this.NumberOfDays));
                this.OnPropertyChanged(nameof(this.StartDate));
                this.OnPropertyChanged(nameof(this.EndDate));
                this.OnPropertyChanged(nameof(this.TotalPrice));
            }
            catch (Exception exception)
            {
                this.RaiseError($"Could not update selected time range. {exception.Message}");
            }
        }

        /// <summary>
        /// Checks whether a date is inside an unavailable time range.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns><c>true</c> if the date is unavailable; otherwise, <c>false</c>.</returns>
        internal bool IsTimeRangeUnavailable(DateTime date)
        {
            if (this.UnavailableTimeRanges != null)
            {
                foreach (var timeRange in this.UnavailableTimeRanges)
                {
                    if (date >= timeRange.StartTime.Date && date <= timeRange.EndTime.Date)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Raises the property changed event.
        /// </summary>
        /// <param name="name">The property name.</param>
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        /// <summary>
        /// Loads the game and owner images.
        /// </summary>
        private async void LoadImages()
        {
            try
            {
                if (this.GameAndUserDetails.Image != null && this.GameAndUserDetails.Image.Length > 0)
                {
                    using var stream = new InMemoryRandomAccessStream();
                    await stream.WriteAsync(this.GameAndUserDetails.Image.AsBuffer());
                    stream.Seek(StartOfStreamPosition);

                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream);
                    this.GameImage = bitmap;
                }
                else
                {
                    this.GameImage = null;
                }

                if (!string.IsNullOrEmpty(this.GameAndUserDetails.AvatarUrl))
                {
                    this.OwnerImage = new BitmapImage(new Uri(this.GameAndUserDetails.AvatarUrl));
                }
                else
                {
                    this.OwnerImage = null;
                }
            }
            catch (Exception exception)
            {
                this.GameImage = null;
                this.OwnerImage = null;
                this.RaiseError($"Could not load images. {exception.Message}");
            }
        }

        /// <summary>
        /// Raises an error event.
        /// </summary>
        /// <param name="message">The error message.</param>
        private void RaiseError(string message)
        {
            this.OnErrorOccurred?.Invoke(message);
        }
    }
}