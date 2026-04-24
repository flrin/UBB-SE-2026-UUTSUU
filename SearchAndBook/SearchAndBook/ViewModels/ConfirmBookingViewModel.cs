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
    /// Represents the view model for confirming a booking, providing booking details, availability checks, and commands
    /// for booking confirmation workflows.
    /// </summary>
    /// <remarks>This view model exposes properties and events to support the booking confirmation process in
    /// a UI, including selected time range, total price calculation, and image loading for the game and owner. It
    /// manages state changes and notifies the UI of updates via property change notifications. Error handling is
    /// performed through the OnErrorOccurred event. This class is intended for use in scenarios where users review and
    /// confirm booking details before finalizing a reservation.</remarks>
    internal class ConfirmBookingViewModel : INotifyPropertyChanged
    {
        private const long StartOfStreamPosition = 0;
        private const int MinimumBookingDayCount = 1;
        private const decimal DefaultTotalPrice = 0;
        private readonly InterfaceBookingService bookingService;
        private BookingDTO gameAndUserDetail;
        private TimeRange selectedTimeRange;
        private decimal totalPrice;
        private BitmapImage? ownerImage;
        private BitmapImage? gameImage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfirmBookingViewModel"/> class.
        /// </summary>
        /// <param name="bookingService">The service used to manage bookings.</param>
        /// <param name="gameAndUserDetails">The details of the game and the user.</param>
        /// <param name="selectedTimeRange">The time range selected for the booking.</param>
        public ConfirmBookingViewModel(InterfaceBookingService bookingService, BookingDTO gameAndUserDetails, TimeRange selectedTimeRange)
        {
            this.bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
            this.gameAndUserDetail = gameAndUserDetails ?? throw new ArgumentNullException(nameof(gameAndUserDetails));
            this.selectedTimeRange = selectedTimeRange ?? throw new ArgumentNullException(nameof(selectedTimeRange));

            try
            {
                this.bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
                this.GameAndUserDetails = gameAndUserDetails ?? throw new ArgumentNullException(nameof(gameAndUserDetails));
                this.SelectedTimeRange = selectedTimeRange ?? throw new ArgumentNullException(nameof(selectedTimeRange));

                this.UnavailableTimeRanges = this.bookingService.GetUnavailableTimeRanges(this.GameAndUserDetails.GameId) ?? Array.Empty<TimeRange>();
                this.TotalPrice = this.CalculatePrice();
                this.LoadImages();
            }
            catch (Exception exception)
            {
                this.RaiseError($"Could not initialize booking confirmation. {exception.Message}");
                this.UnavailableTimeRanges = Array.Empty<TimeRange>();
                this.TotalPrice = DefaultTotalPrice;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? name = null)
           => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        /// <summary>
        /// Occurs when a request to navigate back to the previous screen is made.
        /// </summary>
        public event Action? OnGoBackRequested;

        /// <summary>
        /// Occurs when the user confirms the booking process.
        /// </summary>
        public event Action? OnConfirmBookingRequested;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <remarks>This event is typically raised by classes that implement the INotifyPropertyChanged
        /// interface to notify clients, such as data-binding frameworks, that a property value has changed.</remarks>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Occurs when an error is encountered, providing a message that describes the error.
        /// </summary>
        /// <remarks>Subscribers can use this event to handle or log errors as they occur. The event
        /// provides a string containing details about the error condition.</remarks>
        public event Action<string>? OnErrorOccurred;

        /// <summary>
        /// Gets the currently selected time range for the booking.
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
        /// Gets the total price calculated for the selected booking duration.
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
        /// Gets the formatted start date string of the selected time range.
        /// </summary>
        public string StartDate => this.SelectedTimeRange?.StartTime.ToString("dd MMM yyyy") ?? "-";

        /// <summary>
        /// Gets the formatted end date string of the selected time range.
        /// </summary>
        public string EndDate => this.SelectedTimeRange?.EndTime.ToString("dd MMM yyyy") ?? "-";

        /// <summary>
        /// Gets the combined details of the game and the associated user for the current booking.
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
        /// Gets the profile image of the game owner.
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
        /// Gets the display image of the game being booked.
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
        /// Gets the collection of time ranges during which the resource is unavailable.
        /// </summary>
        public TimeRange[] UnavailableTimeRanges { get; private set; } = Array.Empty<TimeRange>();

        /// <summary>
        /// Gets the number of days in the currently selected time range.
        /// </summary>
        /// <remarks>If no time range is selected or an error occurs during calculation, the minimum
        /// booking day count is returned.</remarks>
        public int NumberOfDays
        {
            get
            {
                try
                {
                    if (this.SelectedTimeRange == null)
                    {
                        return MinimumBookingDayCount;
                    }

                    return this.bookingService.CalculateNumberOfDaysInAGivenTimeRange(this.SelectedTimeRange);
                }
                catch
                {
                    return MinimumBookingDayCount;
                }
            }
        }

        /// <summary>
        /// Checks whether a game is available for booking within the specified time range.
        /// </summary>
        /// <remarks>If an error occurs while checking availability or if the time range is null, the
        /// method returns false.</remarks>
        /// <param name="timeRange">The time range for which to check game availability. Cannot be null.</param>
        /// <returns>true if the game is available during the specified time range; otherwise, false.</returns>
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
                this.RaiseError($"Could not check availability. {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// Confirms the current booking by invoking the confirmation request event.
        /// </summary>
        /// <remarks>If an error occurs during the confirmation process, the method raises an error with a
        /// descriptive message. This method does not throw exceptions to the caller.</remarks>
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
        /// Attempts to navigate to the previous state or page in the navigation history.
        /// </summary>
        /// <remarks>If an error occurs during the navigation process, the error is handled internally and
        /// an error message is raised. This method does not throw exceptions to the caller.</remarks>
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
        /// Calculates the total price for renting the selected game over the specified time range.
        /// </summary>
        /// <remarks>If an error occurs during price calculation, the method sets the total price to a
        /// default value and raises an error notification.</remarks>
        /// <returns>The total price for the rental. Returns a default price if the calculation fails.</returns>
        public decimal CalculatePrice()
        {
            try
            {
                return this.bookingService.CalculateTotalPriceForRentingASpecificGame(this.GameAndUserDetails.Price, this.SelectedTimeRange);
            }
            catch (Exception exception)
            {
                this.RaiseError($"Could not calculate price. {exception.Message}");
                this.TotalPrice = DefaultTotalPrice;
                return DefaultTotalPrice;
            }
        }

        /// <summary>
        /// Updates the currently selected time range and recalculates the total price based on the new selection.
        /// </summary>
        /// <remarks>This method updates related properties and notifies listeners of property changes. If
        /// an error occurs during the update, an error is raised instead of throwing an exception.</remarks>
        /// <param name="newTimeRange">The new time range to select. Cannot be null.</param>
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
                this.RaiseError($"Could not update selected timeRange. {exception.Message}");
            }
        }

        /// <summary>
        /// Determines whether the specified date falls within any unavailable time range.
        /// </summary>
        /// <remarks>This method checks all defined unavailable time ranges and returns true if the date
        /// matches any range. If no unavailable time ranges are defined, the method returns false.</remarks>
        /// <param name="date">The date to check for availability. Only the date component is considered; the time component is ignored.</param>
        /// <returns>true if the specified date is within an unavailable time range; otherwise, false.</returns>
        internal bool IsTimeRangeUnavailable(DateTime date)
        {
            bool isUnavailable = false;
            if (this.UnavailableTimeRanges != null)
            {
                foreach (var timeRange in this.UnavailableTimeRanges)
                {
                    if (date >= timeRange.StartTime.Date && date <= timeRange.EndTime.Date)
                    {
                        isUnavailable = true;
                        break;
                    }
                }
            }

            return isUnavailable;
        }

        private void RaiseError(string message)
        {
            this.OnErrorOccurred?.Invoke(message);
        }

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
    }
}