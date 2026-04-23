namespace SearchAndBook.Views.Controls
{
    using Microsoft.UI.Xaml;
    using Microsoft.UI.Xaml.Controls;
    using Microsoft.UI.Xaml.Media;

    /// <summary>
    /// Represents a visual card control that displays summarized information about a game.
    /// </summary>
    public sealed partial class GameCard : UserControl
    {
        /// <summary>
        /// Identifies the <see cref="GameId"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GameIdProperty =
            DependencyProperty.Register(
                nameof(GameId),
                typeof(int),
                typeof(GameCard),
                new PropertyMetadata(0));

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(
                nameof(Title),
                typeof(string),
                typeof(GameCard),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="ImageSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ImageSourceProperty =
            DependencyProperty.Register(
                nameof(ImageSource),
                typeof(ImageSource),
                typeof(GameCard),
                new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="Location"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LocationProperty =
            DependencyProperty.Register(
                nameof(Location),
                typeof(string),
                typeof(GameCard),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="PlayersText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PlayersTextProperty =
            DependencyProperty.Register(
                nameof(PlayersText),
                typeof(string),
                typeof(GameCard),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="PriceText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PriceTextProperty =
            DependencyProperty.Register(
                nameof(PriceText),
                typeof(string),
                typeof(GameCard),
                new PropertyMetadata(string.Empty));

        /// <summary>
        /// Initializes a new instance of the <see cref="GameCard"/> class.
        /// </summary>
        public GameCard()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Gets or sets the unique identifier for the game.
        /// </summary>
        public int GameId
        {
            get => (int)this.GetValue(GameIdProperty);
            set => this.SetValue(GameIdProperty, value);
        }

        /// <summary>
        /// Gets or sets the title or name of the game.
        /// </summary>
        public string Title
        {
            get => (string)this.GetValue(TitleProperty);
            set => this.SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the image source for the game's thumbnail.
        /// </summary>
        public ImageSource ImageSource
        {
            get => (ImageSource)this.GetValue(ImageSourceProperty);
            set => this.SetValue(ImageSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the geographical location where the game is available.
        /// </summary>
        public string Location
        {
            get => (string)this.GetValue(LocationProperty);
            set => this.SetValue(LocationProperty, value);
        }

        /// <summary>
        /// Gets or sets the text representing the number of players allowed.
        /// </summary>
        public string PlayersText
        {
            get => (string)this.GetValue(PlayersTextProperty);
            set => this.SetValue(PlayersTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the text representing the price of the game rental.
        /// </summary>
        public string PriceText
        {
            get => (string)this.GetValue(PriceTextProperty);
            set => this.SetValue(PriceTextProperty, value);
        }

        /// <summary>
        /// Handles the click event on the game card to navigate to the details view.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void OnCardClicked(object sender, RoutedEventArgs e)
        {
            if (this.Parent is FrameworkElement parent)
            {
                var frame = parent.XamlRoot.Content as Frame;
                frame?.Navigate(typeof(SearchAndBook.Views.GameDetailsView), this.GameId);
            }
        }
    }
}