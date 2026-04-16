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

namespace SearchAndBook.ViewModels
{
    public class GameDetailsViewModel : INotifyPropertyChanged
    {
        private const long UNREGISTERED_USER_ID = -1;
        private const long START_OF_STREAM_POSTION = 0;

        public event PropertyChangedEventHandler? PropertyChanged;

        public event Action? OnGoBackRequested;

        public event Action<BookingDTO, TimeRange>? OnStartBookingRequested;

        public event Action<string>? OnMessageRequested;

        public DateTimeOffset Today => DateTimeOffset.Now.Date;

        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

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

        private bool _hasError;

        public bool HasError
        {
            get => _hasError;
            private set
            {
                _hasError = value;
                OnPropertyChanged();
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

        private string? _ownerImageUrl;

        public string? OwnerImageUrl
        {
            get => _ownerImageUrl;
            private set
            {
                _ownerImageUrl = value;
                OnPropertyChanged();
            }
        }

        private readonly IBookingService _bookingService;

        public TimeRange[] UnavailableTimeRanges { get; private set; } = Array.Empty<TimeRange>();

        public ICommand GoBackCommand => new RelayCommand(_ => GoBack());

        public ICommand BookCommand => new RelayCommand(commandParameter =>
        {
            try
            {
                if (commandParameter is TimeRange timeRange)
                {
                    StartBooking(timeRange);
                }
                else
                {
                    OnMessageRequested?.Invoke("Invalid booking interval selected.");
                }
            }
            catch (Exception exception)
            {
                OnMessageRequested?.Invoke($"Could not start booking. {exception.Message}");
            }
        });

        public ICommand ChatWithOwnerCommand => new RelayCommand(_ => { /* later */ });

        public GameDetailsViewModel(InterfaceBookingService bookingService, int gameId)
        {
            _bookingService = bookingService ?? throw new ArgumentNullException(nameof(bookingService));

            try
            {
                GameAndUserDetails = _bookingService.GetGameDetails(gameId);
                UnavailableTimeRanges = _bookingService.GetUnavailableRanges(gameId) ?? Array.Empty<TimeRange>();
                LoadGameImage();
                LoadOwnerImage();
                HasError = false;
            }
            catch (Exception exception)
            {
                HasError = true;
                UnavailableTimeRanges = Array.Empty<TimeRange>();
                OnMessageRequested?.Invoke($"Could not load game details. {exception.Message}");
            }
        }

        public bool CheckAvailability(TimeRange timeRange)
        {
            try
            {
                if (timeRange == null)
                    return false;

                return _bookingService.CheckAvailability(GameAndUserDetails.GameId, timeRange);
            }
            catch (Exception exception)
            {
                OnMessageRequested?.Invoke($"Could not check availability. {exception.Message}");
                return false;
            }
        }

        public decimal CalculatePrice(TimeRange timeRange)
        {
            try
            {
                if (timeRange == null)
                    throw new ArgumentNullException(nameof(timeRange));

                TotalPrice = _bookingService.CalculateTotalPrice(GameAndUserDetails.Price, timeRange);
                return TotalPrice;
            }
            catch (Exception exception)
            {
                OnMessageRequested?.Invoke($"Could not calculate price. {exception.Message}");
                TotalPrice = 0;
                return 0;
            }
        }

        private async void LoadGameImage()
        {
            try
            {
                if (GameAndUserDetails.Image == null || GameAndUserDetails.Image.Length == 0)
                {
                    GameImage = null;
                    return;
                }

                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(GameAndUserDetails.Image.AsBuffer());
                stream.Seek(START_OF_STREAM_POSTION);

                var bitmap = new BitmapImage();
                await bitmap.SetSourceAsync(stream);
                GameImage = bitmap;
            }
            catch (Exception exception)
            {
                GameImage = null;
                OnMessageRequested?.Invoke($"Could not load game image. {exception.Message}");
            }
        }

        private void LoadOwnerImage()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(GameAndUserDetails.AvatarUrl))
                {
                    OwnerImageUrl = null;
                    return;
                }

                OwnerImageUrl = GameAndUserDetails.AvatarUrl;
            }
            catch (Exception exception)
            {
                OwnerImageUrl = null;
                OnMessageRequested?.Invoke($"Could not load owner image. {exception.Message}");
            }
        }

        public void StartBooking(TimeRange timeRange)
        {
            try
            {
                if (SessionContext.GetInstance().UserId == UNREGISTERED_USER_ID)
                {
                    OnMessageRequested?.Invoke("User not logged in. Please log in first");

                    // TODO login

                    return;
                }

                if (timeRange == null)
                {
                    OnMessageRequested?.Invoke("Please select a valid booking timeRange.");
                    return;
                }

                OnStartBookingRequested?.Invoke(GameAndUserDetails, timeRange);
            }
            catch (Exception exception)
            {
                OnMessageRequested?.Invoke($"Could not continue to booking. {exception.Message}");
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
                OnMessageRequested?.Invoke($"Could not go back. {exception.Message}");
            }
        }
    }
}