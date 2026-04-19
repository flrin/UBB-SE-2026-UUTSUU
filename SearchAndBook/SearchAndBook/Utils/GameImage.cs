namespace SearchAndBook.Utils
{
    using System;
    using System.Runtime.InteropServices.WindowsRuntime;
    using System.Threading.Tasks;
    using Microsoft.UI.Xaml.Media.Imaging;
    using Windows.Storage.Streams;

    /// <summary>
    /// Helper class for converting image data from a byte array (e.g. from database)
    /// into a BitmapImage that can be displayed in the WinUI interface.
    /// </summary>
    internal class GameImage
    {
        /// <summary>
        /// Converts a byte array into a BitmapImage for UI display.
        /// </summary>
        /// <param name="imageBytes">Raw image data (e.g. from database).</param>
        /// <returns>A BitmapImage usable in XAML, or null if input is empty.</returns>
        public static async Task<BitmapImage?> ToBitmapImage(byte[]? imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0)
            {
                return null;
            }

            using var stream = new InMemoryRandomAccessStream();
            await stream.WriteAsync(imageBytes.AsBuffer());
            stream.Seek(0);

            var bitmap = new BitmapImage();
            await bitmap.SetSourceAsync(stream);

            return bitmap;
        }
    }
}
