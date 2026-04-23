namespace SearchAndBook.Views;
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
/// Provides the user interface for confirming a booking, allowing date modification and final submission.
/// </summary>
public sealed partial class ConfirmBookingView : Page
{
    private const int MinimumSelectedDates = 1;
    private DateTime? modifySelectedStart;
    private DateTime? modifySelectedEnd;

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfirmBookingView"/> class.
    /// </summary>
    public ConfirmBookingView()
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

        if (eventArgs.Parameter is not (BookingDTO bookingDTO, TimeRange range))
        {
            return;
        }

        var gameRepository = new GamesRepository();
        var rentalRepository = new RentalsRepository();
        var userRepository = new UsersRepository();
        var service = new BookingService(gameRepository, rentalRepository, userRepository);
        var viewModel = new ConfirmBookingViewModel(service, bookingDTO, range);

        viewModel.OnGoBackRequested += () =>
        {
            if (this.Frame.CanGoBack)
            {
                this.Frame.GoBack();
            }
        };

        viewModel.OnConfirmBookingRequested += async () =>
        {
            var dialog = new ContentDialog
            {
                Title = "Success",
                Content = "Booking request was sent successfully!",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot,
            };
            await dialog.ShowAsync();
            this.Frame.Navigate(typeof(DiscoveryView));
        };

        this.DataContext = viewModel;
    }

    private void OnBackClicked(object sender, RoutedEventArgs eventArgs)
    {
        var viewModel = (ConfirmBookingViewModel)this.DataContext;
        viewModel.GoBack();
    }

    private async void OnModifyClicked(object sender, RoutedEventArgs eventArgs)
    {
        var viewModel = (ConfirmBookingViewModel)this.DataContext;

        var calendar = new CalendarView
        {
            SelectionMode = CalendarViewSelectionMode.Multiple,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinDate = DateTimeOffset.Now.Date,
            SelectedBorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Goldenrod),
            SelectedHoverBorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Goldenrod),
            SelectedPressedBorderBrush = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Goldenrod),
        };

        calendar.CalendarViewDayItemChanging += (calendarSender, calendarArgumets) =>
        {
            var date = calendarArgumets.Item.Date.DateTime;

            bool isUnavailable = viewModel.IsTimeRangeUnavailable(date);

            if (isUnavailable)
            {
                calendarArgumets.Item.IsBlackout = true;
                calendarArgumets.Item.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkRed);
                return;
            }

            if (this.modifySelectedStart.HasValue && this.modifySelectedEnd.HasValue &&
                date.Date >= this.modifySelectedStart.Value.Date && date.Date <= this.modifySelectedEnd.Value.Date)
            {
                calendarArgumets.Item.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Goldenrod);
                return;
            }

            calendarArgumets.Item.Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.DarkGreen);
        };

        calendar.SelectedDatesChanged += (calendarSender, calendarArguments) =>
        {
            var selectedDates = calendarSender.SelectedDates;
            if (selectedDates.Count > MinimumSelectedDates + 1)
            {
                var toKeep = new List<DateTimeOffset>
                    {
                        selectedDates[selectedDates.Count - 2],
                        selectedDates[selectedDates.Count - 1],
                    };
                calendarSender.SelectedDates.Clear();
                foreach (var date in toKeep)
                {
                    calendarSender.SelectedDates.Add(date);
                }
                return;
            }

            if (selectedDates.Count < MinimumSelectedDates)
            {
                this.modifySelectedStart = null;
                this.modifySelectedEnd = null;
                return;
            }

            var sorted = selectedDates
                .Select(date => date.DateTime)
                .OrderBy(date => date)
                .ToList();

            this.modifySelectedStart = sorted[0];
            this.modifySelectedEnd = sorted[sorted.Count - 1];

            // force redraw
            var temporaryOffset = 1;
            var minDate = calendarSender.MinDate;
            calendarSender.MinDate = DateTimeOffset.Now.Date.AddDays(temporaryOffset);
            calendarSender.MinDate = minDate;
        };

        calendar.SelectedDates.Add(viewModel.SelectedTimeRange.StartTime);
        if (viewModel.SelectedTimeRange.EndTime != viewModel.SelectedTimeRange.StartTime)
        {
            calendar.SelectedDates.Add(viewModel.SelectedTimeRange.EndTime);
        }

        var dialog = new ContentDialog
        {
            Title = "Modify dates",
            Content = calendar,
            PrimaryButtonText = "Confirm",
            CloseButtonText = "Cancel",
            XamlRoot = this.XamlRoot,
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var selectedDates = calendar.SelectedDates;
            if (selectedDates.Count < MinimumSelectedDates)
            {
                return;
            }

            var sorted = selectedDates
                .Select(date => date.DateTime)
                .OrderBy(date => date)
                .ToList();

            var newRange = new TimeRange(sorted[0], sorted[sorted.Count - 1]);
            viewModel.UpdateSelectedRange(newRange);
        }
    }

    private void OnConfirmClicked(object sender, RoutedEventArgs eventArgs)
    {
        var viewModel = (ConfirmBookingViewModel)this.DataContext;
        viewModel.ConfirmBooking();
    }

    private void OnMessageUserClicked(object sender, RoutedEventArgs eventArgs)
    {
        // to be connected later
    }
}