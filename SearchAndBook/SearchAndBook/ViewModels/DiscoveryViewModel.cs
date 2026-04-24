namespace SearchAndBook.ViewModels
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Input;
    using SearchAndBook.CommandHandler;
    using SearchAndBook.Domain;
    using SearchAndBook.Services;
    using SearchAndBook.Shared;
    using SearchAndBook.Utils;

    /// <summary>
    /// Provides the logic for discovering and filtering games, including pagination and search capabilities.
    /// </summary>
    public class DiscoveryViewModel : INotifyPropertyChanged
    {
        private const int MinimumCitySearchLength = 2;
        private const int ItemsPerPage = 10;
        private const int FirstDayOfMonth = 1;
        private const int MidnightHour = 0;
        private const int MidnightMinute = 0;
        private const int MidnightSecond = 0;
        private const int NoPagesAvailable = 0;
        private const int NoGamesAvailable = 0;
        private const int InitialPage = 1;
        private readonly InterfaceSearchAndFilterService searchAndFilterService;
        private readonly InterfaceGeographicalService geographicalService;
        private DateTimeOffset? selectedEndDate;
        private int currentPage = 1;
        private int totalAvailableGamesCount;
        private string citySearchText = string.Empty;
        private bool showOthersHeader;
        private DateTimeOffset? selectedStartDate;

        /// <summary>
        /// Initializes a new instance of the <see cref="DiscoveryViewModel"/> class.
        /// </summary>
        /// <param name="searchService">The service used for searching and filtering games.</param>
        /// <param name="geographicalService">The service used for geographical location suggestions.</param>
        public DiscoveryViewModel(InterfaceSearchAndFilterService searchService, InterfaceGeographicalService geographicalService)
        {
            this.searchAndFilterService = searchService;
            this.geographicalService = geographicalService;

            this.selectedStartDate = null;
            this.selectedEndDate = null;

            this.NextPageCommand = new RelayCommand(_ => this.GoToNextPage());
            this.PreviousPageCommand = new RelayCommand(_ => this.GoToPreviousPage());
            this.SearchCommand = new RelayCommand(_ => this.SearchGamesByFilter(this.Filter));

            try
            {
                this.LoadPaginatedDiscoveryFeed();
            }
            catch (Exception exception)
            {
                this.OnErrorOccurred?.Invoke($"Could not load discovery feed. {exception.Message}");
            }
        }

        /// <summary>
        /// Occurs when the current page of the discovery feed changes.
        /// </summary>
        public event Action? OnPageChanged;

        /// <summary>
        /// Occurs when a request is made to view the details of a specific game.
        /// </summary>
        public event Action<int>? OnGameSelectedRequest;

        /// <summary>
        /// Occurs when a search request is initiated with specific filter criteria.
        /// </summary>
        public event Action<FilterCriteria>? OnSearchRequest;

        /// <summary>
        /// Occurs when an error is encountered during discovery operations.
        /// </summary>
        public event Action<string>? OnErrorOccurred;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets the list of games available for rent tonight.
        /// </summary>
        public List<GameDTO> AvailableTonightGames { get; set; } = new ();

        /// <summary>
        /// Gets or sets the list of other available games that do not match the "tonight" criteria.
        /// </summary>
        public List<GameDTO> OtherAvailableGames { get; set; } = new ();

        /// <summary>
        /// Gets a value indicating whether the end date selection is enabled based on the start date.
        /// </summary>
        public bool IsEndDateEnabled => this.SelectedStartDate.HasValue;

        private int TotalGamesCount => this.totalAvailableGamesCount;

        /// <summary>
        /// Gets or sets a value indicating whether the header for "Other games" should be visible.
        /// </summary>
        public bool ShowOthersHeader
        {
            get => this.showOthersHeader;
            set
            {
                this.showOthersHeader = value;
                this.OnPropertyChanged(nameof(this.ShowOthersHeader));
            }
        }

        /// <summary>
        /// Gets or sets the filter criteria used for searching games.
        /// </summary>
        public FilterCriteria Filter { get; set; } = new ();

        /// <summary>
        /// Gets the minimum allowed start date for a search, typically the current date.
        /// </summary>
        public DateTimeOffset MinStartDate => DateTimeOffset.Now.Date;

        /// <summary>
        /// Gets the minimum allowed end date based on the currently selected start date.
        /// </summary>
        public DateTimeOffset MinEndDate =>
        this.SelectedStartDate.HasValue
        ? new DateTimeOffset(
            this.SelectedStartDate.Value.Year,
            this.SelectedStartDate.Value.Month,
            FirstDayOfMonth,
            MidnightHour,
            MidnightMinute,
            MidnightSecond,
            TimeSpan.Zero)
        : DateTimeOffset.Now.Date;

        /// <summary>
        /// Gets or sets the selected start date for the game search.
        /// </summary>
        public DateTimeOffset? SelectedStartDate
        {
            get => this.selectedStartDate;
            set
            {
                var newValue = value?.Date;

                if (this.selectedStartDate != newValue)
                {
                    this.selectedStartDate = newValue;
                    this.OnPropertyChanged(nameof(this.SelectedStartDate));
                    this.OnPropertyChanged(nameof(this.MinEndDate));
                    this.OnPropertyChanged(nameof(this.IsEndDateEnabled));

                    if (this.selectedStartDate.HasValue)
                    {
                        this.selectedEndDate = this.selectedStartDate.Value;
                    }
                    else
                    {
                        this.selectedEndDate = null;
                    }

                    this.OnPropertyChanged(nameof(this.SelectedEndDate));
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected end date for the game search.
        /// </summary>
        public DateTimeOffset? SelectedEndDate
        {
            get => this.selectedEndDate;
            set
            {
                var newValue = value?.Date;

                if (this.SelectedStartDate.HasValue && newValue.HasValue &&
                    newValue.Value < this.SelectedStartDate.Value)
                {
                    newValue = this.SelectedStartDate.Value.Date;
                }

                this.selectedEndDate = newValue;
                this.OnPropertyChanged(nameof(this.SelectedEndDate));
            }
        }

        /// <summary>
        /// Gets or sets the current page number in the discovery feed.
        /// </summary>
        public int CurrentPage
        {
            get => this.currentPage;
            set
            {
                this.currentPage = value;
                this.OnPropertyChanged(nameof(this.CurrentPage));
            }
        }

        /// <summary>
        /// Gets the total number of pages available based on the total games count.
        /// </summary>
        public int TotalPages
        {
            get
            {
                if (this.TotalGamesCount == 0)
                {
                    return NoPagesAvailable;
                }

                return (int)Math.Ceiling((double)this.TotalGamesCount / ItemsPerPage);
            }
        }

        /// <summary>
        /// Gets or sets the text used to search for games in a specific city.
        /// </summary>
        public string CitySearchText
        {
            get => this.citySearchText;
            set
            {
                if (this.citySearchText != value)
                {
                    this.citySearchText = value;
                    this.OnPropertyChanged(nameof(this.CitySearchText));
                    this.Filter.City = value;
                    this.UpdateCitySuggestions(value);
                }
            }
        }

        /// <summary>
        /// Gets the collection of city suggestions based on the current search text.
        /// </summary>
        public ObservableCollection<string> CitySuggestions { get; } = new ();

        /// <summary>
        /// Gets the command to navigate to the next page.
        /// </summary>
        public ICommand NextPageCommand { get; }

        /// <summary>
        /// Gets the command to navigate to the previous page.
        /// </summary>
        public ICommand PreviousPageCommand { get; }

        /// <summary>
        /// Gets the command to perform a search based on current filters.
        /// </summary>
        public ICommand SearchCommand { get; }

        /// <summary>
        /// Gets the message to be displayed when no games match the discovery or search criteria.
        /// </summary>
        public string NoResultsMessage => this.TotalGamesCount == NoGamesAvailable ? "No games available." : string.Empty;

        /// <summary>
        /// Loads paginated discovery feed and updates UI properties.
        /// </summary>
        public async void LoadPaginatedDiscoveryFeed()
        {
            try
            {
                int currentUserId = SessionContext.GetInstance().UserId;

                var discoveryFeedResult = this.searchAndFilterService.GetDiscoveryFeedPaged(currentUserId, this.CurrentPage, ItemsPerPage);

                this.AvailableTonightGames = discoveryFeedResult.availableTonight;
                this.OtherAvailableGames = discoveryFeedResult.others;
                this.ShowOthersHeader = this.OtherAvailableGames.Any();
                this.totalAvailableGamesCount = discoveryFeedResult.totalAvailableGamesCount;

                await this.LoadImagesForGames(this.AvailableTonightGames);
                await this.LoadImagesForGames(this.OtherAvailableGames);

                this.OnPropertyChanged(nameof(this.TotalPages));
                this.OnPropertyChanged(nameof(this.AvailableTonightGames));
                this.OnPropertyChanged(nameof(this.OtherAvailableGames));
                this.OnPropertyChanged(nameof(this.NoResultsMessage));
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

        /// <summary>
        /// Searches for games based on the provided filter criteria and updates the discovery feed.
        /// </summary>
        /// <param name="criteria">The filter criteria containing search parameters like name, city, and price.</param>
        public void SearchGamesByFilter(FilterCriteria criteria)
        {
            try
            {
                if (!this.searchAndFilterService.IsValidDateRange(
                    this.SelectedStartDate?.DateTime,
                    this.SelectedEndDate?.DateTime))
                {
                    return;
                }

                this.Filter.Name = criteria.Name;
                this.Filter.City = criteria.City;
                this.Filter.SortOption = criteria.SortOption;
                this.Filter.MaximumPrice = criteria.MaximumPrice;
                this.Filter.PlayerCount = criteria.PlayerCount;
                this.Filter.UserId = SessionContext.GetInstance().UserId;

                this.UpdateAvailabilityRange();

                this.CurrentPage = InitialPage;
                this.OnSearchRequest?.Invoke(this.Filter);
            }
            catch (Exception ex)
            {
                this.OnErrorOccurred?.Invoke($"Could not perform search. {ex.Message}");
            }
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed. This is optional because of CallerMemberName.</param>
        protected void OnPropertyChanged(string? propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void UpdateAvailabilityRange()
        {
            try
            {
                if (this.SelectedStartDate.HasValue &&
                    this.SelectedEndDate.HasValue &&
                    this.SelectedStartDate.Value <= this.SelectedEndDate.Value)
                {
                    this.Filter.AvailabilityRange = new TimeRange(
                        this.SelectedStartDate.Value.Date,
                        this.SelectedEndDate.Value.Date);
                }
                else
                {
                    this.Filter.AvailabilityRange = null;
                }
            }
            catch (Exception ex)
            {
                this.OnErrorOccurred?.Invoke($"Could not update availability range. {ex.Message}");
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
                            this.OnErrorOccurred?.Invoke($"Could not load an image for a game. {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                this.OnErrorOccurred?.Invoke($"Could not load game images. {ex.Message}");
            }
        }

        private void UpdateCitySuggestions(string input)
        {
            try
            {
                this.CitySuggestions.Clear();

                if (!string.IsNullOrWhiteSpace(input) && input.Length >= MinimumCitySearchLength)
                {
                    var matches = this.geographicalService.GetCitySuggestions(input);
                    foreach (var match in matches)
                    {
                        this.CitySuggestions.Add(match);
                    }
                }
            }
            catch (Exception ex)
            {
                this.OnErrorOccurred?.Invoke($"Could not load city suggestions. {ex.Message}");
            }
        }
    }
}