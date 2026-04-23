namespace SearchAndBook.Views.Test
{
    using System.Collections.Generic;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.UI.Xaml.Media.Imaging;

    /// <summary>
    /// Represents a test page for the games feed, used to verify the visual layout with mock data.
    /// </summary>
    public sealed partial class FeedTestPage : Page
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FeedTestPage"/> class.
        /// </summary>
        public FeedTestPage()
        {
            this.InitializeComponent();

            this.GamesFeed.ItemsSource = this.GetTestGames();
        }

        /// <summary>
        /// Generates a list of mock games for testing purposes.
        /// </summary>
        /// <returns>A list of <see cref="GameFeedItem"/> objects.</returns>
        private List<GameFeedItem> GetTestGames()
        {
            return new List<GameFeedItem>
            {
                new GameFeedItem
                {
                    GameId = 1,
                    Title = "Catan",
                    Location = "Cluj-Napoca",
                    PlayersText = "3 - 4 players",
                    PriceText = "10 RON / day",
                    ImageSource = new BitmapImage(new System.Uri("ms-appx:///Assets/SeedImages/catan.png")),
                },

                new GameFeedItem
                {
                    GameId = 2,
                    Title = "Monopoly",
                    Location = "Turda",
                    PlayersText = "2 - 6 players",
                    PriceText = "8 RON / day",
                    ImageSource = new BitmapImage(new System.Uri("ms-appx:///Assets/SeedImages/monopoly.jpg")),
                },

                new GameFeedItem
                {
                    GameId = 3,
                    Title = "Carcassonne",
                    Location = "Cluj-Napoca",
                    PlayersText = "2 - 5 players",
                    PriceText = "9 RON / day",
                    ImageSource = new BitmapImage(new System.Uri("ms-appx:///Assets/SeedImages/carcassonne.jpg")),
                },
            };
        }
    }

    /// <summary>
    /// Represents a data item for the games feed in a test scenario.
    /// </summary>
    public class GameFeedItem
    {
        /// <summary>
        /// Gets or sets the unique identifier for the game.
        /// </summary>
        required public int GameId { get; set; }

        /// <summary>
        /// Gets or sets the title of the game.
        /// </summary>
        required public string Title { get; set; }

        /// <summary>
        /// Gets or sets the location where the game is available.
        /// </summary>
        required public string Location { get; set; }

        /// <summary>
        /// Gets or sets the text describing the number of players.
        /// </summary>
        required public string PlayersText { get; set; }

        /// <summary>
        /// Gets or sets the text describing the rental price.
        /// </summary>
        required public string PriceText { get; set; }

        /// <summary>
        /// Gets or sets the source image for the game thumbnail.
        /// </summary>
        required public BitmapImage ImageSource { get; set; }
    }
}