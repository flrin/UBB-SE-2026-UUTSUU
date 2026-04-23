namespace SearchAndBook.Shared
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;
    using Microsoft.UI.Xaml.Media.Imaging;

    /// <summary>
    /// IMPORTANT - IMAGE HANDLING:
    /// This class stores the image in TWO different formats for different purposes:
    ///
    /// 1. Image (byte[])
    /// - Raw binary data (usually loaded from the database)
    /// - Used for storage, transport, and persistence
    /// - NOT directly usable in the UI
    ///
    /// 2. GameImage (BitmapImage)
    /// - UI-friendly image format used by WinUI
    /// - Can be directly bound to XAML controls (e.g. <Image Source="{Binding GameImage}" />)
    /// - Must be created by converting the byte[] using ImageHelper
    ///
    /// WHY BOTH EXIST:
    /// - The database works with byte[]
    /// - The UI works with BitmapImage
    /// - Keeping both avoids repeated conversions and improves performance
    ///
    /// TYPICAL FLOW:
    /// - Load Image (byte[]) from database
    /// - Convert it using ImageHelper.ToBitmapImage(...)
    /// - Assign result to GameImage for UI display.
    /// </summary>
    public class GameDTO : INotifyPropertyChanged
    {
        /// <summary>
        /// Bits of the image data are stored in the Image property, but this GameImage property is used for UI binding.
        /// </summary>
        private BitmapImage? gameImage;

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <remarks>This event is typically raised by classes that implement the INotifyPropertyChanged
        /// interface to notify subscribers that a property value has changed. Handlers receive the name of the property
        /// that changed in the PropertyChangedEventArgs parameter.</remarks>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Gets or sets the unique identifier for the game.
        /// </summary>
        public int GameId { get; set; }

        /// <summary>
        /// Gets or sets the name associated with the object.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the binary image data associated with this instance.
        /// </summary>
        public byte[]? Image { get; set; }

        /// <summary>
        /// Gets or sets the price associated with the item.
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Gets or sets the name of the city associated with the entity.
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the maximum number of players allowed.
        /// </summary>
        public int MaximumPlayerNumber { get; set; }

        /// <summary>
        /// Gets or sets the minimum number of players required to start the game.
        /// </summary>
        public int MinimumPlayerNumber { get; set; }

        /// <summary>
        /// Gets or sets the image associated with the game.
        /// </summary>
        public BitmapImage? GameImage
        {
            get => this.gameImage;
            set
            {
                this.gameImage = value;
                this.OnPropertyChanged(nameof(this.GameImage));
            }
        }

        /// <summary>
        /// Raises the PropertyChanged event to notify listeners that a property value has changed.
        /// </summary>
        /// <remarks>Use this method to implement the INotifyPropertyChanged interface in data-binding
        /// scenarios. Calling this method with the correct property name ensures that UI elements bound to the property
        /// are updated appropriately.</remarks>
        /// <param name="name">The name of the property that changed. This value is optional and is automatically provided when called from
        /// a property setter.</param>
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
