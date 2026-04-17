using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using SearchAndBook.CommandHandler;
using SearchAndBook.Domain;
using SearchAndBook.Services;
using SearchAndBook.Shared;
using SearchAndBook.Utils;

namespace SearchAndBook.ViewModels
{
    public class FilteredSearchViewModel : INotifyPropertyChanged
    {
        private readonly InterfaceSearchAndFilterService _searchService;
        private readonly InterfaceGeographicalService _geographicalService;

        private const int ItemsPerPage = 10;
        private const int MinimumCharactersForCitySearch = 2;
        private const int FirstPage = 1;
        private const int MinimumPlayers = 0;
        private const double DefaultMaxPrice = 0;
        private const int MinimumPageNumber = 1;

        public DateTimeOffset Today => DateTimeOffset.Now.Date;
        public DateTimeOffset Tomorrow => DateTimeOffset.Now.Date.AddDays(1);

        public FilterCriteria CurrentFilter { get; set; }
        public GameDTO[] BaseResults { get; private set; }
        public GameDTO[] DisplayedResults { get; private set; }
        public bool HasNoResults { get; private set; }
        public string NoResultsMessage => HasNoResults
            ? "No games found matching your criteria. Try adjusting your filters or search terms."
            : "";

        public List<GameDTO> Games { get; set; } = new();

        private GameDTO? _selectedGame;
        public GameDTO? SelectedGame
        {
            get => _selectedGame;
            set
            {
                if (_selectedGame != value)
                {
                    _selectedGame = value;
                    OnPropertyChanged(nameof(SelectedGame));

                    if (_selectedGame != null)
                    {
                        try
                        {
                            SelectGame(_selectedGame.GameId);
                        }
                        catch (Exception ex)
                        {
                            RaiseError($"Could not select the game. {ex.Message}");
                        }
                        finally
                        {
                            _selectedGame = null;
                            OnPropertyChanged(nameof(SelectedGame));
                        }
                    }
                }
            }
        }

        public ObservableCollection<GameDTO> GamesShown { get; set; } = new();

        private readonly Dictionary<int, BitmapImage?> _gameImages = new();
        public Dictionary<int, BitmapImage?> GameImages => _gameImages;

        private BitmapImage? _selectedGameImage;
        public BitmapImage? SelectedGameImage
        {
            get => _selectedGameImage;
            set
            {
                _selectedGameImage = value;
                OnPropertyChanged(nameof(SelectedGameImage));
            }
        }

        private int _currentPage = FirstPage;
        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
            }
        }

        public int TotalPages
        {
            get
            {
                if (Games == null || Games.Count == 0)
                    return FirstPage;
                return (int)Math.Ceiling((double)Games.Count / ItemsPerPage);
            }
        }

        private double _selectedMaximumPrice;
        public double SelectedMaximumPrice
        {
            get => _selectedMaximumPrice;
            set
            {
                _selectedMaximumPrice = value;
                OnPropertyChanged(nameof(SelectedMaximumPrice));
            }
        }

        private double _selectedMinimumPlayers;
        public double SelectedMinimumPlayers
        {
            get => _selectedMinimumPlayers;
            set
            {
                _selectedMinimumPlayers = value;
                OnPropertyChanged(nameof(SelectedMinimumPlayers));
            }
        }

        public DateTimeOffset MinEndDate => SelectedStartDate.HasValue
            ? SelectedStartDate.Value.AddDays(1)
            : Today;

        private DateTimeOffset? _selectedStartDate;
        public DateTimeOffset? SelectedStartDate
        {
            get => _selectedStartDate;
            set
            {
                _selectedStartDate = value;
                OnPropertyChanged(nameof(SelectedStartDate));
                OnPropertyChanged(nameof(MinEndDate));

                if (SelectedEndDate.HasValue && value.HasValue && SelectedEndDate.Value <= value.Value)
                {
                    SelectedEndDate = null;
                }
            }
        }

        public DateTimeOffset MinStartDate => Today;

        private DateTimeOffset? _selectedEndDate;
        public DateTimeOffset? SelectedEndDate
        {
            get => _selectedEndDate;
            set
            {
                _selectedEndDate = value;
                OnPropertyChanged(nameof(SelectedEndDate));
            }
        }

        private string? _selectedSortOption;
        public string? SelectedSortOption
        {
            get => _selectedSortOption;
            set
            {
                if (_selectedSortOption != value)
                {
                    _selectedSortOption = value;
                    OnPropertyChanged(nameof(SelectedSortOption));
                    ApplySortOnly();
                }
            }
        }

        private string _locationError = string.Empty;
        public string LocationError
        {
            get => _locationError;
            set
            {
                _locationError = value;
                OnPropertyChanged(nameof(LocationError));
            }
        }

        public ICommand SearchCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PreviousPageCommand { get; }
        public ICommand SelectGameCommand { get; }
        public ICommand ApplySelectedUiFiltersCommand { get; }
        public ICommand ClearFiltersCommand { get; }
        public ICommand GoBackCommand { get; }

        public event Action<string>? OnErrorOccurred;
        public event Action<int>? OnGameSelectedRequest;
        public event Action? OnGoBackRequest;
        public event PropertyChangedEventHandler? PropertyChanged;

        public FilteredSearchViewModel(InterfaceSearchAndFilterService searchService, InterfaceGeographicalService geographicalService)
        {
            _geographicalService = geographicalService ?? throw new ArgumentNullException(nameof(geographicalService));
            _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));

            CurrentFilter = new FilterCriteria();
            BaseResults = Array.Empty<GameDTO>();
            DisplayedResults = Array.Empty<GameDTO>();
            HasNoResults = false;

            SelectedMaximumPrice = DefaultMaxPrice;
            SelectedStartDate = null;
            SelectedEndDate = null;

            SearchCommand = new RelayCommand(_ => SearchGamesByFilter(CurrentFilter));
            NextPageCommand = new RelayCommand(_ => NextPage());
            PreviousPageCommand = new RelayCommand(_ => PreviousPage());
            GoBackCommand = new RelayCommand(_ => GoBack());

            SelectGameCommand = new RelayCommand(obj =>
            {
                try
                {
                    if (obj is GameDTO game)
                    {
                        if (GameImages.TryGetValue(game.GameId, out var image))
                        {
                            SelectedGameImage = image;
                        }
                        else
                        {
                            SelectedGameImage = null;
                        }

                        SelectGame(game.GameId);
                    }
                }
                catch (Exception ex)
                {
                    RaiseError($"Could not open game details. {ex.Message}");
                }
            });

            ApplySelectedUiFiltersCommand = new RelayCommand(_ => ApplySelectedUiFilters());
            ClearFiltersCommand = new RelayCommand(_ => ClearAllFilters());
        }

        public void Initialize(FilterCriteria initialFilter)
        {
            try
            {
                if (initialFilter == null)
                    throw new ArgumentNullException(nameof(initialFilter));

                CurrentFilter = initialFilter;
                CitySearchText = CurrentFilter.City ?? string.Empty;

                if (CurrentFilter.AvailabilityRange != null)
                {
                    SelectedStartDate = new DateTimeOffset(CurrentFilter.AvailabilityRange.StartTime);
                    SelectedEndDate = new DateTimeOffset(CurrentFilter.AvailabilityRange.EndTime);
                }
                else
                {
                    SelectedStartDate = null;
                    SelectedEndDate = null;
                }

                SearchGamesByFilter(CurrentFilter);
            }
            catch (Exception ex)
            {
                RaiseError($"Could not initialize search results. {ex.Message}");
            }
        }

        public void LoadSearchResults(FilterCriteria searchCriteria)
        {
            try
            {
                BaseResults = _searchService.SearchGamesByFilter(searchCriteria) ?? Array.Empty<GameDTO>();
                DisplayedResults = BaseResults;
                Games = DisplayedResults.ToList();
                CurrentPage = FirstPage;
                OnPropertyChanged(nameof(TotalPages));
                RefreshPage();
                UpdateNoResultsState();
            }
            catch (Exception ex)
            {
                BaseResults = Array.Empty<GameDTO>();
                DisplayedResults = Array.Empty<GameDTO>();
                Games = new List<GameDTO>();
                RefreshPage();
                UpdateNoResultsState();
                RaiseError($"Could not load search results. {ex.Message}");
            }
        }

        public void LoadDiscoveryResutls(GameDTO[] discoveryResults)
        {
            try
            {
                BaseResults = discoveryResults ?? Array.Empty<GameDTO>();
                DisplayedResults = BaseResults;
                Games = DisplayedResults.ToList();
                CurrentPage = FirstPage;
                OnPropertyChanged(nameof(TotalPages));
                RefreshPage();
                UpdateNoResultsState();
            }
            catch (Exception ex)
            {
                BaseResults = Array.Empty<GameDTO>();
                DisplayedResults = Array.Empty<GameDTO>();
                Games = new List<GameDTO>();
                RefreshPage();
                UpdateNoResultsState();
                RaiseError($"Could not load discovery results. {ex.Message}");
            }
        }

        public void ApplyFilters()
        {
            try
            {
                if (!_searchService.IsValidDateRange(
                    CurrentFilter.AvailabilityRange?.StartTime,
                    CurrentFilter.AvailabilityRange?.EndTime))
                {
                    RaiseError("Please select a valid date range.");
                    return;
                }

                DisplayedResults = _searchService.ApplyFilters(BaseResults, CurrentFilter) ?? Array.Empty<GameDTO>();
                Games = DisplayedResults.ToList();
                CurrentPage = FirstPage;
                OnPropertyChanged(nameof(TotalPages));
                RefreshPage();
                UpdateNoResultsState();
            }
            catch (Exception ex)
            {
                RaiseError($"Could not apply filters. {ex.Message}");
            }
        }

        public void ApplySelectedUiFilters()
        {
            try
            {
                if (!_searchService.IsValidPlayersCount((int?)SelectedMinimumPlayers) ||
                    !_searchService.IsValidDateRange(
                        SelectedStartDate?.DateTime,
                        SelectedEndDate?.DateTime))
                {
                    RaiseError("Please enter valid filter values.");
                    return;
                }

                _searchService.UpdateFilterFromUI(
                    CurrentFilter,
                    SelectedMaximumPrice,
                    SelectedMinimumPlayers,
                    SelectedStartDate?.DateTime,
                    SelectedEndDate?.DateTime
                );

                ApplyFilters();
            }
            catch (Exception ex)
            {
                RaiseError($"Could not apply selected filters. {ex.Message}");
            }
        }

        public void RemoveNameFilter()
        {
            try
            {
                CurrentFilter.Name = null;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                RaiseError($"Could not remove name filter. {ex.Message}");
            }
        }

        public void RemoveCityFilter()
        {
            try
            {
                CurrentFilter.City = null;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                RaiseError($"Could not remove city filter. {ex.Message}");
            }
        }

        public void RemovePriceFilter()
        {
            try
            {
                CurrentFilter.MaximumPrice = null;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                RaiseError($"Could not remove price filter. {ex.Message}");
            }
        }

        public void RemovePlayersFilter()
        {
            try
            {
                CurrentFilter.PlayerCount = null;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                RaiseError($"Could not remove players filter. {ex.Message}");
            }
        }

        public void RemoveDateFilter()
        {
            try
            {
                CurrentFilter.AvailabilityRange = null;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                RaiseError($"Could not remove date filter. {ex.Message}");
            }
        }

        public void SetPriceAscendingSort()
        {
            try
            {
                CurrentFilter.SortOption = SortOption.PriceAscending;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                RaiseError($"Could not sort by ascending price. {ex.Message}");
            }
        }

        public void SetPriceDescendingSort()
        {
            try
            {
                CurrentFilter.SortOption = SortOption.PriceDescending;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                RaiseError($"Could not sort by descending price. {ex.Message}");
            }
        }

        public void ClearSorting()
        {
            try
            {
                CurrentFilter.SortOption = SortOption.None;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                RaiseError($"Could not clear sorting. {ex.Message}");
            }
        }

        public void ClearAllFilters()
        {
            try
            {
                CurrentFilter.Reset();
                SelectedMaximumPrice = DefaultMaxPrice;
                SelectedMinimumPlayers = MinimumPlayers;
                SelectedSortOption = null;
                SelectedStartDate = null;
                SelectedEndDate = null;
                CitySearchText = string.Empty;
                LocationError = string.Empty;

                DisplayedResults = BaseResults;
                Games = DisplayedResults.ToList();
                CurrentPage = FirstPage;
                OnPropertyChanged(nameof(TotalPages));
                RefreshPage();
                UpdateNoResultsState();
            }
            catch (Exception ex)
            {
                RaiseError($"Could not clear filters. {ex.Message}");
            }
        }

        public void ApplySortOnly()
        {
            try
            {
                if (SelectedSortOption == "Closest to me" && string.IsNullOrWhiteSpace(CitySearchText))
                {
                    LocationError = "Please enter a city to measure from.";
                    _selectedSortOption = null;
                    OnPropertyChanged(nameof(SelectedSortOption));
                    return;
                }

                LocationError = string.Empty;

                CurrentFilter.SortOption = SelectedSortOption switch
                {
                    "Price: lowest to highest" => SortOption.PriceAscending,
                    "Price: highest to lowest" => SortOption.PriceDescending,
                    "Closest to me" => SortOption.Location,
                    _ => SortOption.None
                };

                if (CurrentFilter.SortOption == SortOption.Location)
                {
                    SearchGamesByFilter(CurrentFilter);
                }
                else
                {
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Could not apply sorting. {ex.Message}");
            }
        }

        private void UpdateNoResultsState()
        {
            HasNoResults = DisplayedResults.Length == 0;
            OnPropertyChanged(nameof(HasNoResults));
            OnPropertyChanged(nameof(NoResultsMessage));
        }

        public void SelectGame(int gameId)
        {
            try
            {
                OnGameSelectedRequest?.Invoke(gameId);
            }
            catch (Exception ex)
            {
                RaiseError($"Could not navigate to game details. {ex.Message}");
            }
        }

        public void SearchGamesByFilter(FilterCriteria criteria)
        {
            try
            {
                if (criteria == null)
                    throw new ArgumentNullException(nameof(criteria));

                if (!_searchService.IsValidDateRange(
                    SelectedStartDate?.DateTime,
                    SelectedEndDate?.DateTime))
                {
                    RaiseError("Please select a valid date range.");
                    return;
                }

                Games = _searchService.SearchGamesByFilter(criteria)?.ToList() ?? new List<GameDTO>();
                DisplayedResults = Games.ToArray();
                BaseResults = DisplayedResults;
                CurrentPage = FirstPage;
                OnPropertyChanged(nameof(TotalPages));
                RefreshPage();
                UpdateNoResultsState();
            }
            catch (Exception ex)
            {
                Games = new List<GameDTO>();
                DisplayedResults = Array.Empty<GameDTO>();
                BaseResults = Array.Empty<GameDTO>();
                RefreshPage();
                UpdateNoResultsState();
                RaiseError($"Search failed. {ex.Message}");
            }
        }

        public void NextPage()
        {
            try
            {
                if (CurrentPage * ItemsPerPage < Games.Count)
                {
                    CurrentPage++;
                    RefreshPage();
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Could not go to next page. {ex.Message}");
            }
        }

        public void PreviousPage()
        {
            try
            {
                if (CurrentPage > MinimumPageNumber)
                {
                    CurrentPage--;
                    RefreshPage();
                }
            }
            catch (Exception ex)
            {
                RaiseError($"Could not go to previous page. {ex.Message}");
            }
        }

        private async void RefreshPage()
        {
            try
            {
                GamesShown.Clear();

                var pageListings = Games
                    .Skip((CurrentPage - 1) * ItemsPerPage)
                    .Take(ItemsPerPage)
                    .ToList();

                foreach (var game in pageListings)
                {
                    GamesShown.Add(game);
                }

                foreach (var game in pageListings)
                {
                    if (game.Image != null && game.GameImage == null)
                    {
                        await LoadGameImage(game);
                    }
                }

                OnPropertyChanged(nameof(GamesShown));
            }
            catch (Exception ex)
            {
                RaiseError($"Could not refresh the page. {ex.Message}");
            }
        }

        private void GoBack()
        {
            try
            {
                OnGoBackRequest?.Invoke();
            }
            catch (Exception ex)
            {
                RaiseError($"Could not go back. {ex.Message}");
            }
        }

        private async Task LoadGameImage(GameDTO game)
        {
            try
            {
                game.GameImage = await GameImage.ToBitmapImage(game.Image);
                _gameImages[game.GameId] = game.GameImage;
            }
            catch
            {
                game.GameImage = null;
                _gameImages[game.GameId] = null;
            }
        }

        protected void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<string> CitySuggestions { get; } = new();

        private string _citySearchText = string.Empty;
        public string CitySearchText
        {
            get => _citySearchText;
            set
            {
                if (_citySearchText != value)
                {
                    _citySearchText = value;
                    OnPropertyChanged(nameof(CitySearchText));
                    CurrentFilter.City = value;
                    UpdateCitySuggestions(value);
                }
            }
        }

        private void UpdateCitySuggestions(string input)
        {
            try
            {
                CitySuggestions.Clear();

                if (!string.IsNullOrWhiteSpace(input) && input.Length >= MinimumCharactersForCitySearch)
                {
                    var matches = _geographicalService.GetCitySuggestions(input);
                    foreach (var match in matches)
                    {
                        CitySuggestions.Add(match);
                    }
                }
            }
            catch (Exception ex)
            {
                CitySuggestions.Clear();
                RaiseError($"Could not load city suggestions. {ex.Message}");
            }
        }

        private void RaiseError(string message)
        {
            OnErrorOccurred?.Invoke(message);
        }
    }
}