namespace SearchAndBook.Views
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.UI.Xaml.Navigation;
    using SearchAndBook.Domain;
    using SearchAndBook.Repositories;
    using SearchAndBook.Services;
    using SearchAndBook.Shared;
    using SearchAndBook.ViewModels;

    /// <summary>
    /// Provides the user interface for viewing detailed information about a game and selecting rental dates.
    /// </summary>
    public sealed partial class GameDetailsView : Page
    {
        private DateTime? selectedDateStart;
        private DateTime? selectedDateEnd;

        /// <summary>
        /// Initializes a new instance of the <see cref="GameDetailsView"/> class.
        /// </summary>
        public GameDetailsView()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Invoked when the Page is loaded and becomes the current source of a parent Frame.
        /// </summary>
        /// <param name="eventArgs">Event data that can be examined by overriding code.</param>
        protected override void OnNavigatedTo(NavigationEventArgs eventArgs)
        {
            base.OnNavigatedTo(eventArgs);
            if (eventArgs.Parameter is not int gameId)
            {
                return;
            }

            var gameRepository = new GamesRepository();
            var rentalRepository = new RentalsRepository();
            var userRepository = new UsersRepository();
            var service = new BookingService(gameRepository, rentalRepository, userRepository);
            var viewModel = new GameDetailsViewModel(service, gameId);

            viewModel.OnGoBackRequested += () =>
            {
                if (this.Frame.CanGoBack)
                {
                    this.Frame.GoBack();
                }
            };

            viewModel.OnStartBookingRequested += (bookingDto, range) =>
            {
                this.Frame.Navigate(typeof(ConfirmBookingView), (bookingDto, range));
            };

            viewModel.OnMessageRequested += async message =>
            {
                var dialog = new ContentDialog
                {
                    Title = "Booking",
                    Content = message,
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot,
                };

                await dialog.ShowAsync();
            };

            this.DataContext = viewModel;
        }

        private void OnBackClicked(object sender, RoutedEventArgs eventArgs)
        {
            var viewModel = (GameDetailsViewModel)this.DataContext;
            viewModel.GoBack();
        }

        private async void OnBookClicked(object sender, RoutedEventArgs eventArgs)
        {
            var selectedDates = this.RentalCalendar.SelectedDates;
            if (selectedDates.Count == 0)
            {
                var dialog = new ContentDialog
                {
                    Title = "Invalid selection",
                    Content = "Please select at least one date.",
                    CloseButtonText = "OK",
                    XamlRoot = this.XamlRoot,
                };
                await dialog.ShowAsync();
                return;
            }

            var viewModel = (GameDetailsViewModel)this.DataContext;
            var sortedDates = selectedDates
               .Select(date => date.DateTime)
               .OrderBy(date => date)
               .ToList();
            var dateRange = new TimeRange(sortedDates[0], sortedDates[selectedDates.Count - 1]);
            viewModel.StartBooking(dateRange);
        }

        private void OnDatesChanged(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs eventArgs)
        {
            if (this.DataContext is not GameDetailsViewModel viewModel)
            {
                return;
            }

            var selectedDates = this.RentalCalendar.SelectedDates;

            if (selectedDates.Count > 2)
            {
                var datesToKeep = new List<DateTimeOffset>
                    {
                        selectedDates[selectedDates.Count - 2],
                        selectedDates[selectedDates.Count - 1],
                    };
                this.RentalCalendar.SelectedDates.Clear();
                foreach (var date in datesToKeep)
                {
                    this.RentalCalendar.SelectedDates.Add(date);
                }

                return;
            }

            if (selectedDates.Count < 1)
            {
                this.selectedDateStart = null;
                this.selectedDateEnd = null;
                this.ForceRedrawCalendar();
                return;
            }

            var sorted = selectedDates
                .Select(d => d.DateTime)
                .OrderBy(d => d)
                .ToList();

            this.selectedDateStart = sorted[0];
            this.selectedDateEnd = sorted[sorted.Count - 1];

            var range = new TimeRange(this.selectedDateStart.Value, this.selectedDateEnd.Value);
            viewModel.CalculatePrice(range);

            this.ForceRedrawCalendar();
        }

        private void ForceRedrawCalendar()
        {
            var currentDate = this.RentalCalendar.MinDate;
            this.RentalCalendar.MinDate = currentDate.AddDays(1);
            this.RentalCalendar.MinDate = currentDate;
        }

        private void OnDayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs eventArgs)
        {
            if (this.DataContext is not GameDetailsViewModel viewModel)
            {
                return;
            }

            var date = eventArgs.Item.Date.Date;
            var today = DateTimeOffset.Now.Date;

            if (date < today)
            {
                eventArgs.Item.IsBlackout = true;
                return;
            }

            bool isUnavailable = false;
            if (viewModel.UnavailableTimeRanges != null)
            {
                foreach (var range in viewModel.UnavailableTimeRanges)
                {
                    if (date >= range.StartTime.Date && date <= range.EndTime.Date)
                    {
                        isUnavailable = true;
                        break;
                    }
                }
            }

            if (isUnavailable)
            {
                eventArgs.Item.IsBlackout = true;
                eventArgs.Item.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                return;
            }

            if (this.selectedDateStart.HasValue && this.selectedDateEnd.HasValue &&
                date >= this.selectedDateStart.Value.Date && date <= this.selectedDateEnd.Value.Date)
            {
                eventArgs.Item.IsBlackout = false;
                eventArgs.Item.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Goldenrod);
                return;
            }

            eventArgs.Item.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkGreen);
        }
    }
}