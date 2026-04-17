using System;
using System.Collections.Generic;

namespace SearchAndBook.Domain
{
    public class City
    {
        public required string MainName { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public required List<string> Names { get; set; }
    }
}
