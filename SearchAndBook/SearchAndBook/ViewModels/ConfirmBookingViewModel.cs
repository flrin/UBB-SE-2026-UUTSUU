using Microsoft.UI.Xaml.Media.Imaging;
using SearchAndBook.Domain;
using SearchAndBook.Services;
using SearchAndBook.Shared;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Storage.Streams;

namespace SearchAndBook.ViewModels
{
    internal class ConfirmBookingViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action<string>? OnErrorOccurred;

        private const long START_OF_STREAM_POSTION = 0;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly InterfaceBookingService BookingService;

        private BookingDTO _gameAndUserDetails;
        public BookingDTO GameAndUserDetails
        {
            get => _gameAndUserDetails;
            private set
            {
                _gameAndUserDetails = value;
                OnPropertyChanged();
            }
        }

        public TimeRange[] UnavailableTimeRanges { get; private set; } = Array.Empty<TimeRange>();

        private TimeRange _selectedTimeRange;
        public TimeRange SelectedTimeRange
        {
            get => _selectedTimeRange;
            private set
            {
                _selectedTimeRange = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(NumberOfDays));
                OnPropertyChanged(nameof(StartDate));
                OnPropertyChanged(nameof(EndDate));
            }
        }

        private decimal _totalPrice;
        public decimal TotalPrice
        {
            get => _totalPrice;
            private set
            {
                _totalPrice = value;
                OnPropertyChanged();
            }
        }

        public string StartDate => SelectedTimeRange?.StartTime.ToString("dd MMM yyyy") ?? "-";
        public string EndDate => SelectedTimeRange?.EndTime.ToString("dd MMM yyyy") ?? "-";

        private BitmapImage? _ownerImage;
        public BitmapImage? OwnerImage
        {
            get => _ownerImage;
            private set
            {
                _ownerImage = value;
                OnPropertyChanged();
            }
        }

        private BitmapImage? _gameImage;
        public BitmapImage? GameImage
        {
            get => _gameImage;
            private set
            {
                _gameImage = value;
                OnPropertyChanged();
            }
        }

        public event Action? OnGoBackRequested;
        public event Action? OnConfirmBookingRequested;

        public ConfirmBookingViewModel(InterfaceBookingService bookingService, BookingDTO gameAndUserDetails, TimeRange selectedTimeRange)
        {
            try
            {
                BookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));
                GameAndUserDetails = gameAndUserDetails ?? throw new ArgumentNullException(nameof(gameAndUserDetails));
                SelectedTimeRange = selectedTimeRange ?? throw new ArgumentNullException(nameof(selectedTimeRange));

                UnavailableTimeRanges = BookingService.GetUnavailableRanges(GameAndUserDetails.GameId) ?? Array.Empty<TimeRange>();
                TotalPrice = CalculatePrice();
                LoadImages();
            }
            catch (Exception exception)
            {
                RaiseError($"Could not initialize booking confirmation. {exception.Message}");
                UnavailableTimeRanges = Array.Empty<TimeRange>();
                TotalPrice = 0;
            }
        }

        private async void LoadImages()
        {
            try
            {
                if (GameAndUserDetails.Image != null && GameAndUserDetails.Image.Length > 0)
                {
                    using var stream = new InMemoryRandomAccessStream();
                    await stream.WriteAsync(GameAndUserDetails.Image.AsBuffer());
                    stream.Seek(START_OF_STREAM_POSTION);
                    var bitmap = new BitmapImage();
                    await bitmap.SetSourceAsync(stream);
                    GameImage = bitmap;
                }
                else
                {
                    GameImage = null;
                }

                if (!string.IsNullOrEmpty(GameAndUserDetails.AvatarUrl))
                {
                    OwnerImage = new BitmapImage(new Uri(GameAndUserDetails.AvatarUrl));
                }
                else
                {
                    OwnerImage = null;
                }
            }
            catch (Exception exception)
            {
                GameImage = null;
                OwnerImage = null;
                RaiseError($"Could not load images. {exception.Message}");
            }
        }

        public int NumberOfDays
        {
            get
            {
                try
                {
                    if (SelectedTimeRange == null)
                        return 1;

                    return BookingService.CalculateNumberOfDays(SelectedTimeRange);
                }
                catch
                {
                    return 1;
                }
            }
        }

        public bool CheckAvailability(TimeRange timeRange)
        {
            try
            {
                if (timeRange == null)
                    return false;

                return BookingService.CheckAvailability(GameAndUserDetails.GameId, timeRange);
            }
            catch (Exception exception)
            {
                RaiseError($"Could not check availability. {exception.Message}");
                return false;
            }
        }

        public void ConfirmBooking()
        {
            try
            {
                OnConfirmBookingRequested?.Invoke();
            }
            catch (Exception exception)
            {
                RaiseError($"Could not confirm booking. {exception.Message}");
            }
        }

        public void GoBack()
        {
            try
            {
                OnGoBackRequested?.Invoke();
            }
            catch (Exception exception)
            {
                RaiseError($"Could not go back. {exception.Message}");
            }
        }

        public decimal CalculatePrice()
        {
            try
            {
                return BookingService.CalculateTotalPrice(GameAndUserDetails.Price, SelectedTimeRange);
            }
            catch (Exception exception)
            {
                RaiseError($"Could not calculate price. {exception.Message}");
                TotalPrice = 0;
                return 0;
            }
        }

        public void UpdateSelectedRange(TimeRange newTimeRange)
        {
            try
            {
                if (newTimeRange == null)
                    throw new ArgumentNullException(nameof(newTimeRange));

                SelectedTimeRange = newTimeRange;
                TotalPrice = CalculatePrice();
                OnPropertyChanged(nameof(NumberOfDays));
                OnPropertyChanged(nameof(StartDate));
                OnPropertyChanged(nameof(EndDate));
                OnPropertyChanged(nameof(TotalPrice));
            }
            catch (Exception exception)
            {
                RaiseError($"Could not update selected timeRange. {exception.Message}");
            }
        }

        private void RaiseError(string message)
        {
            OnErrorOccurred?.Invoke(message);
        }

        internal bool IsTimeRangeUnavailable(DateTime date)
        {
            bool isUnavailable = false;
            if (UnavailableTimeRanges != null)
            {
                foreach (var timeRange in UnavailableTimeRanges)
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
    }
}