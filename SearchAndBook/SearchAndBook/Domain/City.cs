namespace SearchAndBook.Domain
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Represents a city with its primary name, alternative names, and geographic coordinates.
    /// </summary>
    public class City
    {
        /// <summary>
        /// Gets or sets the primary name associated with the object.
        /// </summary>
        required public string MainName { get; set; }

        /// <summary>
        /// Gets or sets the collection of names associated with the current instance.
        /// </summary>
        required public List<string> Names { get; set; }

        /// <summary>
        /// Gets or sets the latitude component of the geographic coordinate.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the geographic longitude coordinate.
        /// </summary>
        public double Longitude { get; set; }
    }
}
