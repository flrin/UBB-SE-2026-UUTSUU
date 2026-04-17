using SearchAndBook.CommandHandler;
using SearchAndBook.Domain;
using SearchAndBook.Services;
using SearchAndBook.Shared;
using SearchAndBook.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SearchAndBook.ViewModels
{
    public class DiscoveryViewModel : INotifyPropertyChanged
    {
        private const int ItemsPerPage = 10;

        public event Action? OnPageChanged;

        private readonly InterfaceSearchAndFilterService _searchService;
        private readonly InterfaceGeographicalService _geographicalService;

        public List<GameDTO> AvailableTonightGames { get; set; } = new();

        public List<GameDTO> OtherAvailableGames { get; set; } = new();


        public bool IsEndDateEnabled => SelectedStartDate.HasValue;

        private bool _showOthersHeader;

        public bool ShowOthersHeader
        {
            get => this._showOthersHeader;
            set
            {
                _showOthersHeader = value;
                OnPropertyChanged(nameof(ShowOthersHeader));
            }
        }

        public FilterCriteria Filter { get; set; } = new();

        public DateTimeOffset MinStartDate => DateTimeOffset.Now.Date;

        public DateTimeOffset MinEndDate =>
        SelectedStartDate.HasValue
        ? new DateTimeOffset(SelectedStartDate.Value.Year, SelectedStartDate.Value.Month, 1, 0, 0, 0, TimeSpan.Zero)
        : DateTimeOffset.Now.Date;

        private DateTimeOffset? _selectedStartDate;

        public DateTimeOffset? SelectedStartDate
        {
            get => _selectedStartDate;
            set
            {
                var newValue = value?.Date;

                if (_selectedStartDate != newValue)
                {
                    _selectedStartDate = newValue;
                    OnPropertyChanged(nameof(SelectedStartDate));
                    OnPropertyChanged(nameof(MinEndDate));
                    OnPropertyChanged(nameof(IsEndDateEnabled));

                    if (_selectedStartDate.HasValue)
                    {
                        _selectedEndDate = _selectedStartDate.Value;
                    }
                    else
                    {
                        _selectedEndDate = null;
                    }

                    OnPropertyChanged(nameof(SelectedEndDate));
                }
            }
        }

        private DateTimeOffset? _selectedEndDate;

        public DateTimeOffset? SelectedEndDate
        {
            get => _selectedEndDate;
            set
            {
                var newValue = value?.Date;

                if (SelectedStartDate.HasValue && newValue.HasValue &&
                    newValue.Value < SelectedStartDate.Value)
                {
                    newValue = SelectedStartDate.Value.Date;
                }

                _selectedEndDate = newValue;
                OnPropertyChanged(nameof(SelectedEndDate));
            }
        }

        private int _currentPage = 1;

        public int CurrentPage
        {
            get => _currentPage;
            set
            {
                _currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
            }
        }

        private int _totalAvailableGamesCount;

        private int TotalGamesCount => _totalAvailableGamesCount;

        public int TotalPages
        {
            get
            {
                if (TotalGamesCount == 0)
                    return 1;
                return (int)Math.Ceiling((double)TotalGamesCount / ItemsPerPage);
            }
        }


        public ICommand NextPageCommand { get; }

        public ICommand PreviousPageCommand { get; }

        public ICommand SearchCommand { get; }

        public event Action<int>? OnGameSelectedRequest;

        public event Action<FilterCriteria>? OnSearchRequest;

        public event Action<string>? OnErrorOccurred;

        public event PropertyChangedEventHandler? PropertyChanged;

        public DiscoveryViewModel(InterfaceSearchAndFilterService searchService, InterfaceGeographicalService geographicalService)
        {
            _searchService = searchService;
            _geographicalService = geographicalService;

            _selectedStartDate = null;
            _selectedEndDate = null;

            NextPageCommand = new RelayCommand(_ => GoToNextPage());
            PreviousPageCommand = new RelayCommand(_ => GoToPreviousPage());
            SearchCommand = new RelayCommand(_ => SearchGamesByFilter(Filter));

            try
            {
                LoadPaginatedDiscoveryFeed();
            }
            catch (Exception exception)
            {
                OnErrorOccurred?.Invoke($"Could not load discovery feed. {exception.Message}");
            }
        }

        public string NoResultsMessage => TotalGamesCount == 0 ? "No games available." : "";

        /// <summary>
        /// Loads paginated discovery feed and updates UI properties.
        /// </summary>
        public async void LoadPaginatedDiscoveryFeed()
        {
            try
            {
                int currentUserId = SessionContext.GetInstance().UserId;

                var discoveryFeedResult = this._searchService.GetDiscoveryFeedPaged(currentUserId, this.CurrentPage, ItemsPerPage);

                this.AvailableTonightGames = discoveryFeedResult.availableTonight;
                this.OtherAvailableGames = discoveryFeedResult.others;
                this.ShowOthersHeader = this.OtherAvailableGames.Any();
                this._totalAvailableGamesCount = discoveryFeedResult.totalAvailableGamesCount;

                await this.LoadImagesForGames(AvailableTonightGames);
                await this.LoadImagesForGames(OtherAvailableGames);

                this.OnPropertyChanged(nameof(TotalPages));
                this.OnPropertyChanged(nameof(AvailableTonightGames));
                this.OnPropertyChanged(nameof(OtherAvailableGames));
                this.OnPropertyChanged(nameof(NoResultsMessage)); 
            }
            catch (Exception exception)
            {
                this.OnErrorOccurred?.Invoke($"Could not load discovery feed. {exception.Message}");
            }
        }

        /// <summary>
        /// Navigates to the next page in the discovery feed.
        /// </summary>
        public void GoToNextPage()
        {
            try
            {
                if (this.CurrentPage * ItemsPerPage < this.TotalGamesCount)
                {
                    this.CurrentPage++;
                    this.LoadPaginatedDiscoveryFeed();
                    this.OnPageChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
               this.OnErrorOccurred?.Invoke($"Could not go to next page. {ex.Message}");
            }
        }

        /// <summary>
        /// Navigates to the previous page in the discovery feed.
        /// </summary>
        public void GoToPreviousPage()
        {
            try
            {
                if (this.CurrentPage > 1)
                {
                    this.CurrentPage--;
                    this.LoadPaginatedDiscoveryFeed();
                    this.OnPageChanged?.Invoke();
                }
            }
            catch (Exception ex)
            {
                this.OnErrorOccurred?.Invoke($"Could not go to previous page. {ex.Message}");
            }
        }

        public void SearchGamesByFilter(FilterCriteria criteria)
        {
            try
            {
                if (!_searchService.IsValidDateRange(
                    SelectedStartDate?.DateTime,
                    SelectedEndDate?.DateTime))
                {
                    return;
                }

                Filter.Name = criteria.Name;
                Filter.City = criteria.City;
                Filter.SortOption = criteria.SortOption;
                Filter.MaximumPrice = criteria.MaximumPrice;
                Filter.PlayerCount = criteria.PlayerCount;
                Filter.UserId = SessionContext.GetInstance().UserId;

                UpdateAvailabilityRange();

                CurrentPage = 1;
                OnSearchRequest?.Invoke(Filter);
            }
            catch (Exception ex)
            {
                OnErrorOccurred?.Invoke($"Could not perform search. {ex.Message}");
            }
        }

        private async Task LoadImagesForGames(IEnumerable<GameDTO> gamesToLoadImagesFor)
        {
            try
            {
                foreach (var game in gamesToLoadImagesFor)
                {
                    if (game.Image != null && game.GameImage == null)
                    {
                        try
                        {
                            game.GameImage = await GameImage.ToBitmapImage(game.Image);
                        }
                        catch (Exception ex)
                        {
                            OnErrorOccurred?.Invoke($"Could not load an image for a game. {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred?.Invoke($"Could not load game images. {ex.Message}");
            }
        }

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
                    Filter.City = value;
                    UpdateCitySuggestions(value);
                }
            }
        }

        private const int MinimumCitySearchLength = 2;

        public ObservableCollection<string> CitySuggestions { get; } = new();

        private void UpdateCitySuggestions(string input)
        {
            try
            {
                CitySuggestions.Clear();

                if (!string.IsNullOrWhiteSpace(input) && input.Length >= MinimumCitySearchLength)
                {
                    var matches = _geographicalService.GetCitySuggestions(input);
                    foreach (var match in matches)
                        CitySuggestions.Add(match);
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred?.Invoke($"Could not load city suggestions. {ex.Message}");
            }
        }

        protected void OnPropertyChanged(string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateAvailabilityRange()
        {
            try
            {
                if (SelectedStartDate.HasValue &&
                    SelectedEndDate.HasValue &&
                    SelectedStartDate.Value <= SelectedEndDate.Value)
                {
                    Filter.AvailabilityRange = new TimeRange(
                        SelectedStartDate.Value.Date,
                        SelectedEndDate.Value.Date);
                }
                else
                {
                    Filter.AvailabilityRange = null;
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred?.Invoke($"Could not update availability range. {ex.Message}");
            }
        }
    }
}