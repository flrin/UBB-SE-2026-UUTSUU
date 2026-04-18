using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using SearchAndBook.Repositories;
using SearchAndBook.Services;
using SearchAndBook.Shared;
using SearchAndBook.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Notifications;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SearchAndBook.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FilteredSearchView : Page
    {
        public FilteredSearchView()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var criteria = e.Parameter as FilterCriteria ?? new FilterCriteria();
            var gamesRepository = new GamesRepository();
            var usersRepository = new UsersRepository();
            var rentalsRepository = new RentalsRepository();
            var geographicalService = App.GlobalGeoService!;
            var service = new SearchAndFilterService(gamesRepository, usersRepository, rentalsRepository, geographicalService);
            var viewModel = new FilteredSearchViewModel(service, geographicalService);
            viewModel.OnGameSelectedRequest += gameId =>
            {
                Frame.Navigate(typeof(GameDetailsView), gameId);
            };
            viewModel.OnGoBackRequest += () => Frame.Navigate(typeof(DiscoveryView));
            viewModel.Initialize(criteria);
            this.DataContext = viewModel;
        }
    }
}