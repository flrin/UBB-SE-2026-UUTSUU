namespace SearchAndBook.ViewModels
{
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

    /// <summary>
    /// ViewModel for the filtered search page.
    /// Handles game search, filtering, sorting, pagination, and navigation.
    /// Implements <see cref="INotifyPropertyChanged"/> for two-way UI binding.
    /// </summary>
    public class FilteredSearchViewModel : INotifyPropertyChanged
    {
        /// <summary>Number of game cards displayed per page.</summary>
        private const int ItemsPerPage = 10;

        /// <summary>Minimum number of characters required before city auto-suggestions are triggered.</summary>
        private const int MinimumCharactersForCitySearch = 2;

        /// <summary>Default starting page index.</summary>
        private const int FirstPage = 1;

        /// <summary>Minimum allowed value for the players filter.</summary>
        private const int MinimumPlayers = 0;

        /// <summary>Default value for the maximum price filter (0 = no filter applied).</summary>
        private const double DefaultMaxPrice = 0;

        /// <summary>Lowest valid page number.</summary>
        private const int MinimumPageNumber = 1;
        private const int NoGavesAvailable = 0;

        private readonly InterfaceSearchAndFilterService searchService;
        private readonly InterfaceGeographicalService geographicalService;

        /// <summary>Cache of loaded BitmapImages keyed by game ID.</summary>
        private readonly Dictionary<int, BitmapImage?> gameImages = new ();

        private BitmapImage? selectedGameImage;
        private string citySearchText = string.Empty;
        private int currentPage = FirstPage;
        private GameDTO? selectedGame;
        private double selectedMaximumPrice;
        private double selectedMinimumPlayers;
        private DateTimeOffset? selectedStartDate;
        private DateTimeOffset? selectedEndDate;
        private string? selectedSortOption;
        private string locationError = string.Empty;

        /// <summary>Raised when an error occurs anywhere in the ViewModel. The string argument contains the user-facing message.</summary>
        public event Action<string>? OnErrorOccurred;

        /// <summary>Raised when the user selects a game and the UI should navigate to the game-details page. The int argument is the game ID.</summary>
        public event Action<int>? OnGameSelectedRequest;

        /// <summary>Raised when the user requests to navigate back to the previous screen.</summary>
        public event Action? OnGoBackRequest;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>Gets or sets the collection of games currently visible on the active page. Bound directly to the UI list/grid.</summary>
        public ObservableCollection<GameDTO> VisibleGames { get; set; } = new ();

        /// <summary>Gets the images.</summary>
        public Dictionary<int, BitmapImage?> GameImages => this.gameImages;

        /// <summary>Gets today's date as a <see cref="DateTimeOffset"/>, used as the minimum selectable date in date pickers.</summary>
        public DateTimeOffset Today => DateTimeOffset.Now.Date;

        /// <summary>Gets or sets the currently active filter criteria applied to the search and filter pipeline.</summary>
        public FilterCriteria CurrentFilter { get; set; }

        /// <summary>
        /// Gets the raw, unfiltered results returned by the last search call.
        /// Filters and sorts are applied on top of this array without re-querying the service.
        /// </summary>
        public GameDTO[] BaseResults { get; private set; }

        /// <summary>Gets the filtered (and optionally sorted) subset of <see cref="BaseResults"/> that is currently displayed.</summary>
        public GameDTO[] DisplayedResults { get; private set; }

        /// <summary>Gets a value indicating whether: <c>true</c> when <see cref="DisplayedResults"/> is empty; drives the "no results" UI state.</summary>
        public bool HasNoResults { get; private set; }

        /// <summary>
        /// Gets the human-readable message shown when no results are isfound.
        /// Returns an empty string when results exist.
        /// </summary>
        public string NoResultsMessage => this.HasNoResults
            ? "No games isfound matching your criteria. Try adjusting your filters or search terms."
            : string.Empty;

        /// <summary>Gets or sets the flat list of all games currently subject to pagination.</summary>
        public List<GameDTO> Games { get; set; } = new ();

        /// <summary>
        /// Gets or sets the game the user has just tapped/clicked.
        /// Setting this property triggers navigation to the game-details page and then resets itself to <c>null</c>.
        /// </summary>
        public GameDTO? SelectedGame
        {
            get => this.selectedGame;
            set
            {
                if (this.selectedGame != value)
                {
                    this.selectedGame = value;
                    this.OnPropertyChanged(nameof(this.SelectedGame));

                    if (this.selectedGame != null)
                    {
                        try
                        {
                            this.SelectGame(this.selectedGame.GameId);
                        }
                        catch (Exception ex)
                        {
                            this.RaiseError($"Could not select the game. {ex.Message}");
                        }
                        finally
                        {
                            this.selectedGame = null;
                            this.OnPropertyChanged(nameof(this.SelectedGame));
                        }
                    }
                }
            }
        }

        /// <summary>Gets or sets the bitmap image associated with the most recently selected game. Bound to an image preview control.</summary>
        public BitmapImage? SelectedGameImage
        {
            get => this.selectedGameImage;
            set
            {
                this.selectedGameImage = value;
                this.OnPropertyChanged(nameof(this.SelectedGameImage));
            }
        }

        /// <summary>Gets or sets the 1-based index of the page currently displayed.</summary>
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
        /// Gets the total number of pages for the current result set.
        /// Returns 1 when there are no games so that pagination controls remain in a valid state.
        /// </summary>
        public int TotalPages
        {
            get
            {
                if (this.Games == null || this.Games.Count == NoGavesAvailable)
                {
                    return FirstPage;
                }

                return (int)Math.Ceiling((double)this.Games.Count / ItemsPerPage);
            }
        }

        /// <summary>Gets or sets the upper price bound selected by the user in the filter panel. A value of 0 means "no maximum price filter".</summary>
        public double SelectedMaximumPrice
        {
            get => this.selectedMaximumPrice;
            set
            {
                this.selectedMaximumPrice = value;
                this.OnPropertyChanged(nameof(this.SelectedMaximumPrice));
            }
        }

        /// <summary>Gets or sets the minimum number of players selected by the user in the filter panel.</summary>
        public double SelectedMinimumPlayers
        {
            get => this.selectedMinimumPlayers;
            set
            {
                this.selectedMinimumPlayers = value;
                this.OnPropertyChanged(nameof(this.SelectedMinimumPlayers));
            }
        }

        /// <summary>
        /// Gets the earliest date the end-date picker will allow.
        /// Equals the day after <see cref="SelectedStartDate"/> when a start date is set; otherwise defaults to today.
        /// </summary>
        public DateTimeOffset MinimumEndDate => this.SelectedStartDate.HasValue
            ? this.SelectedStartDate.Value.AddDays(1)
            : this.Today;

        /// <summary>
        /// Gets or sets the availability window start date chosen by the user.
        /// Automatically clears <see cref="SelectedEndDate"/> if it would become earlier than or equal to the new start date.
        /// </summary>
        public DateTimeOffset? SelectedStartDate
        {
            get => this.selectedStartDate;
            set
            {
                this.selectedStartDate = value;
                this.OnPropertyChanged(nameof(this.SelectedStartDate));
                this.OnPropertyChanged(nameof(this.MinimumEndDate));

                if (this.SelectedEndDate.HasValue && value.HasValue && this.SelectedEndDate.Value <= value.Value)
                {
                    this.SelectedEndDate = null;
                }
            }
        }

        /// <summary>Gets the earliest date that can be selected as a start date (today).</summary>
        public DateTimeOffset MinimumStartDate => this.Today;

        /// <summary>Gets or sets the availability window end date chosen by the user.</summary>
        public DateTimeOffset? SelectedEndDate
        {
            get => this.selectedEndDate;
            set
            {
                this.selectedEndDate = value;
                this.OnPropertyChanged(nameof(this.SelectedEndDate));
            }
        }

        /// <summary>Gets or sets the sort option selected by the user (e.g. "Price: lowest to highest"). Changing this property immediately triggers <see cref="ApplySortOnly"/>.</summary>
        public string? SelectedSortOption
        {
            get => this.selectedSortOption;
            set
            {
                if (this.selectedSortOption != value)
                {
                    this.selectedSortOption = value;
                    this.OnPropertyChanged(nameof(this.SelectedSortOption));
                    this.ApplySortOnly();
                }
            }
        }

        /// <summary>Gets or sets the validation error message displayed near the location/city input. Empty string when there is no error.</summary>
        public string LocationError
        {
            get => this.locationError;
            set
            {
                this.locationError = value;
                this.OnPropertyChanged(nameof(this.LocationError));
            }
        }

        /// <summary>Gets the command that executes a search using <see cref="CurrentFilter"/>.</summary>
        public ICommand SearchCommand { get; }

        /// <summary>Gets the command that advances to the next page of results if one exists.</summary>
        public ICommand NextPageCommand { get; }

        /// <summary>Gets the command that returns to the previous page of results if one exists.</summary>
        public ICommand PreviousPageCommand { get; }

        /// <summary>Gets the command that navigates to the details page of the game passed as the command parameter (<see cref="GameDTO"/>).</summary>
        public ICommand SelectGameCommand { get; }

        /// <summary>Gets the command that reads the current UI filter controls and applies them to the displayed results.</summary>
        public ICommand ApplySelectedUiFiltersCommand { get; }

        /// <summary>Gets the command that resets all filters and sort options and restores the base result set.</summary>
        public ICommand ClearFiltersCommand { get; }

        /// <summary>Gets the command that raises <see cref="OnGoBackRequest"/> to trigger back-navigation.</summary>
        public ICommand GoBackCommand { get; }

        /// <summary>Gets observable list of city name suggestions shown in the autocomplete dropdown.</summary>
        public ObservableCollection<string> CitySuggestions { get; } = new ();

        /// <summary>
        /// Initializes a new instance of the <see cref="FilteredSearchViewModel"/> class.
        /// Initializes a new instance of <see cref="FilteredSearchViewModel"/>.
        /// </summary>
        /// <param name="searchService">Service responsible for searching and filtering games.</param>
        /// <param name="geographicalService">Service responsible for city suggestions and location-based features.</param>
        /// <exception cref="ArgumentNullException">Thrown when either service is <c>null</c>.</exception>
        public FilteredSearchViewModel(InterfaceSearchAndFilterService searchService, InterfaceGeographicalService geographicalService)
        {
            this.geographicalService = geographicalService ?? throw new ArgumentNullException(nameof(geographicalService));
            this.searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));

            this.CurrentFilter = new FilterCriteria();
            this.BaseResults = Array.Empty<GameDTO>();
            this.DisplayedResults = Array.Empty<GameDTO>();
            this.HasNoResults = false;

            this.SelectedMaximumPrice = DefaultMaxPrice;
            this.SelectedStartDate = null;
            this.SelectedEndDate = null;

            this.SearchCommand = new RelayCommand(_ => this.SearchGamesByFilter(this.CurrentFilter));
            this.NextPageCommand = new RelayCommand(_ => this.NextPage());
            this.PreviousPageCommand = new RelayCommand(_ => this.PreviousPage());
            this.GoBackCommand = new RelayCommand(_ => this.GoBack());

            this.SelectGameCommand = new RelayCommand(selectedGameObject =>
            {
                try
                {
                    if (selectedGameObject is GameDTO selectedGameDto)
                    {
                        if (this.GameImages.TryGetValue(selectedGameDto.GameId, out var gameImage))
                        {
                            this.SelectedGameImage = gameImage;
                        }
                        else
                        {
                            this.SelectedGameImage = null;
                        }

                        this.SelectGame(selectedGameDto.GameId);
                    }
                }
                catch (Exception ex)
                {
                    this.RaiseError($"Could not open game details. {ex.Message}");
                }
            });

            this.ApplySelectedUiFiltersCommand = new RelayCommand(_ => this.ApplySelectedUiFilters());
            this.ClearFiltersCommand = new RelayCommand(_ => this.ClearAllFilters());
        }

        /// <summary>
        /// Initializes the ViewModel with a pre-built filter (e.g. passed from the home/search page)
        /// and immediately executes a search with those criteria.
        /// </summary>
        /// <param name="initialFilter">The filter to apply on startup.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="initialFilter"/> is <c>null</c>.</exception>
        public void Initialize(FilterCriteria initialFilter)
        {
            try
            {
                if (initialFilter == null)
                {
                    throw new ArgumentNullException(nameof(initialFilter));
                }

                this.CurrentFilter = initialFilter;
                this.CitySearchText = this.CurrentFilter.City ?? string.Empty;

                if (this.CurrentFilter.AvailabilityRange != null)
                {
                    this.SelectedStartDate = new DateTimeOffset(this.CurrentFilter.AvailabilityRange.StartTime);
                    this.SelectedEndDate = new DateTimeOffset(this.CurrentFilter.AvailabilityRange.EndTime);
                }
                else
                {
                    this.SelectedStartDate = null;
                    this.SelectedEndDate = null;
                }

                this.SearchGamesByFilter(this.CurrentFilter);
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not initialize search results. {ex.Message}");
            }
        }

        /// <summary>
        /// Performs a full search using <paramref name="searchCriteria"/> and replaces both
        /// <see cref="BaseResults"/> and <see cref="DisplayedResults"/> with the new results.
        /// Resets the page to the first page.
        /// </summary>
        /// <param name="searchCriteria">Criteria sent to the search service.</param>
        public void LoadSearchResults(FilterCriteria searchCriteria)
        {
            try
            {
                this.BaseResults = this.searchService.SearchGamesByFilter(searchCriteria) ?? Array.Empty<GameDTO>();
                this.DisplayedResults = this.BaseResults;
                this.Games = this.DisplayedResults.ToList();
                this.CurrentPage = FirstPage;
                this.OnPropertyChanged(nameof(this.TotalPages));
                this.RefreshPage();
                this.UpdateNoResultsState();
            }
            catch (Exception ex)
            {
                this.BaseResults = Array.Empty<GameDTO>();
                this.DisplayedResults = Array.Empty<GameDTO>();
                this.Games = new List<GameDTO>();
                this.RefreshPage();
                this.UpdateNoResultsState();
                this.RaiseError($"Could not load search results. {ex.Message}");
            }
        }

        /// <summary>
        /// Loads a pre-computed array of discovery results (e.g. "Recommended for you") directly
        /// without calling the search service. Resets the page to the first page.
        /// </summary>
        /// <param name="discoveryResults">Array of games to display. Passing <c>null</c> is treated as an empty array.</param>
        public void LoadDiscoveryResults(GameDTO[] discoveryResults)
        {
            try
            {
                this.BaseResults = discoveryResults ?? Array.Empty<GameDTO>();
                this.DisplayedResults = this.BaseResults;
                this.Games = this.DisplayedResults.ToList();
                this.CurrentPage = FirstPage;
                this.OnPropertyChanged(nameof(this.TotalPages));
                this.RefreshPage();
                this.UpdateNoResultsState();
            }
            catch (Exception ex)
            {
                this.BaseResults = Array.Empty<GameDTO>();
                this.DisplayedResults = Array.Empty<GameDTO>();
                this.Games = new List<GameDTO>();
                this.RefreshPage();
                this.UpdateNoResultsState();
                this.RaiseError($"Could not load discovery results. {ex.Message}");
            }
        }

        /// <summary>
        /// Applies <see cref="CurrentFilter"/> on top of <see cref="BaseResults"/> without re-querying the service.
        /// Validates the date range before proceeding.
        /// </summary>
        public void ApplyFilters()
        {
            try
            {
                if (!this.searchService.IsValidDateRange(
                    this.CurrentFilter.AvailabilityRange?.StartTime,
                    this.CurrentFilter.AvailabilityRange?.EndTime))
                {
                    this.RaiseError("Please select a valid date range.");
                    return;
                }

                this.DisplayedResults = this.searchService.ApplyFilters(this.BaseResults, this.CurrentFilter) ?? Array.Empty<GameDTO>();
                this.Games = this.DisplayedResults.ToList();
                this.CurrentPage = FirstPage;
                this.OnPropertyChanged(nameof(this.TotalPages));
                this.RefreshPage();
                this.UpdateNoResultsState();
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not apply filters. {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the current values of the UI filter controls
        /// (<see cref="SelectedMaximumPrice"/>, <see cref="SelectedMinimumPlayers"/>,
        /// <see cref="SelectedStartDate"/>, <see cref="SelectedEndDate"/>),
        /// writes them into <see cref="CurrentFilter"/>, and calls <see cref="ApplyFilters"/>.
        /// </summary>
        public void ApplySelectedUiFilters()
        {
            try
            {
                if (!this.searchService.IsValidPlayersCount((int?)this.SelectedMinimumPlayers) ||
                    !this.searchService.IsValidDateRange(
                        this.SelectedStartDate?.DateTime,
                        this.SelectedEndDate?.DateTime))
                {
                    this.RaiseError("Please enter valid filter values.");
                    return;
                }

                this.searchService.UpdateFilterFromUI(
                    this.CurrentFilter,
                    this.SelectedMaximumPrice,
                    this.SelectedMinimumPlayers,
                    this.SelectedStartDate?.DateTime,
                    this.SelectedEndDate?.DateTime);

                this.ApplyFilters();
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not apply selected filters. {ex.Message}");
            }
        }

        /// <summary>Removes the name filter from <see cref="CurrentFilter"/> and re-applies the remaining filters.</summary>
        public void RemoveNameFilter()
        {
            try
            {
                this.CurrentFilter.Name = null;
                this.ApplyFilters();
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not remove name filter. {ex.Message}");
            }
        }

        /// <summary>Removes the city filter from <see cref="CurrentFilter"/> and re-applies the remaining filters.</summary>
        public void RemoveCityFilter()
        {
            try
            {
                this.CurrentFilter.City = null;
                this.ApplyFilters();
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not remove city filter. {ex.Message}");
            }
        }

        /// <summary>Removes the maximum-price filter from <see cref="CurrentFilter"/> and re-applies the remaining filters.</summary>
        public void RemovePriceFilter()
        {
            try
            {
                this.CurrentFilter.MaximumPrice = null;
                this.ApplyFilters();
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not remove price filter. {ex.Message}");
            }
        }

        /// <summary>Removes the player-count filter from <see cref="CurrentFilter"/> and re-applies the remaining filters.</summary>
        public void RemovePlayersFilter()
        {
            try
            {
                this.CurrentFilter.PlayerCount = null;
                this.ApplyFilters();
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not remove players filter. {ex.Message}");
            }
        }

        /// <summary>Removes the availability-date filter from <see cref="CurrentFilter"/> and re-applies the remaining filters.</summary>
        public void RemoveDateFilter()
        {
            try
            {
                this.CurrentFilter.AvailabilityRange = null;
                this.ApplyFilters();
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not remove date filter. {ex.Message}");
            }
        }

        /// <summary>Sets the sort order to <see cref="SortOption.PriceAscending"/> and re-applies filters.</summary>
        public void SetPriceAscendingSort()
        {
            try
            {
                this.CurrentFilter.SortOption = SortOption.PriceAscending;
                this.ApplyFilters();
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not sort by ascending price. {ex.Message}");
            }
        }

        /// <summary>Sets the sort order to <see cref="SortOption.PriceDescending"/> and re-applies filters.</summary>
        public void SetPriceDescendingSort()
        {
            try
            {
                this.CurrentFilter.SortOption = SortOption.PriceDescending;
                this.ApplyFilters();
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not sort by descending price. {ex.Message}");
            }
        }

        /// <summary>Clears the sort option (<see cref="SortOption.None"/>) and re-applies filters.</summary>
        public void ClearSorting()
        {
            try
            {
                this.CurrentFilter.SortOption = SortOption.None;
                this.ApplyFilters();
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not clear sorting. {ex.Message}");
            }
        }

        /// <summary>
        /// Resets all filter and sort state to defaults and restores <see cref="BaseResults"/>
        /// as the displayed result set.
        /// </summary>
        public void ClearAllFilters()
        {
            try
            {
                this.CurrentFilter.Reset();
                this.SelectedMaximumPrice = DefaultMaxPrice;
                this.SelectedMinimumPlayers = MinimumPlayers;
                this.SelectedSortOption = null;
                this.SelectedStartDate = null;
                this.SelectedEndDate = null;
                this.CitySearchText = string.Empty;
                this.LocationError = string.Empty;

                this.DisplayedResults = this.BaseResults;
                this.Games = this.DisplayedResults.ToList();
                this.CurrentPage = FirstPage;
                this.OnPropertyChanged(nameof(this.TotalPages));
                this.RefreshPage();
                this.UpdateNoResultsState();
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not clear filters. {ex.Message}");
            }
        }

        /// <summary>
        /// Applies only the sort option selected in the UI without touching any other filter.
        /// When "Closest to me" is selected, a full search is triggered so the service can
        /// calculate distances; otherwise <see cref="ApplyFilters"/> is called.
        /// Raises <see cref="LocationError"/> if "Closest to me" is chosen without a city.
        /// </summary>
        public void ApplySortOnly()
        {
            try
            {
                if (this.SelectedSortOption == "Closest to me" && string.IsNullOrWhiteSpace(this.CitySearchText))
                {
                    this.LocationError = "Please enter a city to measure from.";
                    this.selectedSortOption = null;
                    this.OnPropertyChanged(nameof(this.SelectedSortOption));
                    return;
                }

                this.LocationError = string.Empty;

                this.CurrentFilter.SortOption = this.SelectedSortOption switch
                {
                    "Price: lowest to highest" => SortOption.PriceAscending,
                    "Price: highest to lowest" => SortOption.PriceDescending,
                    "Closest to me" => SortOption.Location,
                    _ => SortOption.None
                };

                if (this.CurrentFilter.SortOption == SortOption.Location)
                {
                    this.SearchGamesByFilter(this.CurrentFilter);
                }
                else
                {
                    this.ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not apply sorting. {ex.Message}");
            }
        }

        /// <summary>
        /// Fires <see cref="OnGameSelectedRequest"/> with the specified game ID,
        /// signalling the view to navigate to the game-details page.
        /// </summary>
        /// <param name="gameId">The ID of the game to navigate to.</param>
        public void SelectGame(int gameId)
        {
            try
            {
                this.OnGameSelectedRequest?.Invoke(gameId);
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not navigate to game details. {ex.Message}");
            }
        }

        /// <summary>
        /// Executes a full search via the service using the supplied criteria.
        /// Both <see cref="BaseResults"/> and <see cref="DisplayedResults"/> are replaced with the new results
        /// and the page is reset to the first page.
        /// Validates the currently selected date range before calling the service.
        /// </summary>
        /// <param name="filterCriteria">The filter criteria to pass to the search service.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filterCriteria"/> is <c>null</c>.</exception>
        public void SearchGamesByFilter(FilterCriteria filterCriteria)
        {
            try
            {
                if (filterCriteria == null)
                {
                    throw new ArgumentNullException(nameof(filterCriteria));
                }

                if (!this.searchService.IsValidDateRange(
                    this.SelectedStartDate?.DateTime,
                    this.SelectedEndDate?.DateTime))
                {
                    this.RaiseError("Please select a valid date range.");
                    return;
                }

                this.Games = this.searchService.SearchGamesByFilter(filterCriteria)?.ToList() ?? new List<GameDTO>();
                this.DisplayedResults = this.Games.ToArray();
                this.BaseResults = this.DisplayedResults;
                this.CurrentPage = FirstPage;
                this.OnPropertyChanged(nameof(this.TotalPages));
                this.RefreshPage();
                this.UpdateNoResultsState();
            }
            catch (Exception ex)
            {
                this.Games = new List<GameDTO>();
                this.DisplayedResults = Array.Empty<GameDTO>();
                this.BaseResults = Array.Empty<GameDTO>();
                this.RefreshPage();
                this.UpdateNoResultsState();
                this.RaiseError($"Search failed. {ex.Message}");
            }
        }

        /// <summary>
        /// Gets or sets the text currently entered in the city search box.
        /// Writing to this property updates <see cref="CurrentFilter"/>.<see cref="FilterCriteria.City"/>
        /// and asynchronously refreshes <see cref="CitySuggestions"/>.
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
                    this.CurrentFilter.City = value;
                    this.UpdateCitySuggestions(value);
                }
            }
        }

        /// <summary>Moves to the next page of results if the current page is not the last one.</summary>
        public void NextPage()
        {
            try
            {
                if (this.CurrentPage * ItemsPerPage < this.Games.Count)
                {
                    this.CurrentPage++;
                    this.RefreshPage();
                }
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not go to next page. {ex.Message}");
            }
        }

        /// <summary>Moves to the previous page of results if the current page is not the first one.</summary>
        public void PreviousPage()
        {
            try
            {
                if (this.CurrentPage > MinimumPageNumber)
                {
                    this.CurrentPage--;
                    this.RefreshPage();
                }
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not go to previous page. {ex.Message}");
            }
        }

        /// <summary>Raises <see cref="PropertyChanged"/> for the named property.</summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected void OnPropertyChanged(string? propertyName = null) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Updates <see cref="HasNoResults"/> based on whether <see cref="DisplayedResults"/> is empty
        /// and notifies the UI.
        /// </summary>
        private void UpdateNoResultsState()
        {
            this.HasNoResults = this.DisplayedResults.Length == 0;
            this.OnPropertyChanged(nameof(this.HasNoResults));
            this.OnPropertyChanged(nameof(this.NoResultsMessage));
        }

        /// <summary>
        /// Slices <see cref="Games"/> to the current page window, populates <see cref="VisibleGames"/>,
        /// and asynchronously loads any missing game images.
        /// </summary>
        private async void RefreshPage()
        {
            try
            {
                this.VisibleGames.Clear();

                var pageListings = this.Games
                    .Skip((this.CurrentPage - 1) * ItemsPerPage)
                    .Take(ItemsPerPage)
                    .ToList();

                foreach (var gameItem in pageListings)
                {
                    this.VisibleGames.Add(gameItem);
                }

                foreach (var gameItem in pageListings)
                {
                    if (gameItem.Image != null && gameItem.GameImage == null)
                    {
                        await this.LoadGameImage(gameItem);
                    }
                }

                this.OnPropertyChanged(nameof(this.VisibleGames));
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not refresh the page. {ex.Message}");
            }
        }

        /// <summary>Fires <see cref="OnGoBackRequest"/> to tell the view to navigate back.</summary>
        private void GoBack()
        {
            try
            {
                this.OnGoBackRequest?.Invoke();
            }
            catch (Exception ex)
            {
                this.RaiseError($"Could not go back. {ex.Message}");
            }
        }

        /// <summary>
        /// Converts <paramref name="gameDTO"/>'s raw image bytes to a <see cref="BitmapImage"/>
        /// and stores the result in both <see cref="GameDTO.GameImage"/> and <see cref="gameImages"/>.
        /// On failure the image is set to <c>null</c> rather than propagating the exception.
        /// </summary>
        /// <param name="gameDTO">The game whose image should be loaded.</param>
        private async Task LoadGameImage(GameDTO gameDTO)
        {
            try
            {
                gameDTO.GameImage = await GameImage.ToBitmapImage(gameDTO.Image);
                this.gameImages[gameDTO.GameId] = gameDTO.GameImage;
            }
            catch
            {
                gameDTO.GameImage = null;
                this.gameImages[gameDTO.GameId] = null;
            }
        }

        /// <summary>
        /// Queries <see cref="geographicalService"/> for cities that match <paramref name="input"/>
        /// and populates <see cref="CitySuggestions"/>. No suggestions are shown for inputs shorter
        /// than <see cref="MinimumCharactersForCitySearch"/> characters.
        /// </summary>
        /// <param name="input">The partial city name typed by the user.</param>
        private void UpdateCitySuggestions(string input)
        {
            try
            {
                this.CitySuggestions.Clear();

                if (!string.IsNullOrWhiteSpace(input) && input.Length >= MinimumCharactersForCitySearch)
                {
                    var cityMatches = this.geographicalService.GetCitySuggestions(input);
                    foreach (var city in cityMatches)
                    {
                        this.CitySuggestions.Add(city);
                    }
                }
            }
            catch (Exception ex)
            {
                this.CitySuggestions.Clear();
                this.RaiseError($"Could not load city suggestions. {ex.Message}");
            }
        }

        /// <summary>Raises <see cref="OnErrorOccurred"/> with the given message.</summary>
        /// <param name="message">The error message to propagate to the view.</param>
        private void RaiseError(string message) => this.OnErrorOccurred?.Invoke(message);
    }
}