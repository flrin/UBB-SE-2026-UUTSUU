using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.Generic;

namespace SearchAndBook.Views.Test
{
    public sealed partial class FeedTestPage : Page
    {
        public FeedTestPage()
        {
            this.InitializeComponent();

            GamesFeed.ItemsSource = GetTestGames();
        }

        private List<GameFeedItem> GetTestGames()
        {
            return new List<GameFeedItem>
            {
                new GameFeedItem
                {
                    GameId=1,
                    Title = "Catan",
                    Location = "Cluj-Napoca",
                    PlayersText = "3 - 4 players",
                    PriceText = "10 RON / day",
                    ImageSource = new BitmapImage(new System.Uri("ms-appx:///Assets/SeedImages/catan.png"))
                },

                new GameFeedItem
                {
                    GameId=2,
                    Title = "Monopoly",
                    Location = "Turda",
                    PlayersText = "2 - 6 players",
                    PriceText = "8 RON / day",
                    ImageSource = new BitmapImage(new System.Uri("ms-appx:///Assets/SeedImages/monopoly.jpg"))
                },

                new GameFeedItem
                {
                    GameId=3,
                    Title = "Carcassonne",
                    Location = "Cluj-Napoca",
                    PlayersText = "2 - 5 players",
                    PriceText = "9 RON / day",
                    ImageSource = new BitmapImage(new System.Uri("ms-appx:///Assets/SeedImages/carcassonne.jpg"))
                },
            };
        }
    }

    public class GameFeedItem
    {
        public required int GameId { get; set; }

        public required string Title { get; set; }

        public required string Location { get; set; }

        public required string PlayersText { get; set; }

        public required string PriceText { get; set; }

        public required BitmapImage ImageSource { get; set; }
    }
}