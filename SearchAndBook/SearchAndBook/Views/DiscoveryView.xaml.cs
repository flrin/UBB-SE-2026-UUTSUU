using System;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using SearchAndBook.Repositories;
using SearchAndBook.Services;
using SearchAndBook.Shared;
using SearchAndBook.ViewModels;


namespace SearchAndBook.Views
{
    public sealed partial class DiscoveryView : Page
    {
        public DiscoveryView()
        {
            InitializeComponent();
        }

        public DiscoveryViewModel ViewModel { get; private set; }

        /// <summary>
        /// Called when the page is navigated to.
        /// </summary>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            var gamesRepository = new GamesRepository();
            var usersRepository = new UsersRepository();
            var rentalsRepository = new RentalsRepository();
            var geographicalService = App.GlobalGeoService!;

            var service = new SearchAndFilterService(
                gamesRepository,
                usersRepository,
                rentalsRepository,
                geographicalService);

            ViewModel = new DiscoveryViewModel(service, geographicalService);

            ViewModel.OnSearchRequest += HandleSearchRequest;
            ViewModel.OnGameSelectedRequest += gameId =>
            {
                Frame.Navigate(typeof(GameDetailsView), gameId);
            };

            ViewModel.OnPageChanged += () =>
            {
                MainScrollViewer.ScrollToVerticalOffset(0);
            };

            DataContext = ViewModel;
            StartDatePicker.Date = null;
            EndDatePicker.Date = null;
        }

        private void HandleSearchRequest(FilterCriteria filter)
        {
            Frame.Navigate(typeof(FilteredSearchView), filter);
        }

        private void Game_Click(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is GameDTO game)
            {
                Frame.Navigate(typeof(GameDetailsView), game.GameId);
            }
        }

        private void EndDatePicker_DayItemChanging(CalendarView sender, CalendarViewDayItemChangingEventArgs args)
        {
            if (ViewModel?.SelectedStartDate.HasValue == true)
            {
                var date = args.Item.Date.Date;
                var startDate = ViewModel.SelectedStartDate.Value.Date;

                if (date < startDate)
                {
                    args.Item.IsBlackout = true;
                }
            }
        }
    }
}