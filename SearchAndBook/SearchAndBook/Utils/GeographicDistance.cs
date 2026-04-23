namespace SearchAndBook.Utils
{
    using System;

    /// <summary>
    /// Provides methods for calculating the distance between two geographic coordinates using the Haversine formula.
    /// </summary>
    /// <remarks>This class is intended for use in scenarios where an approximate distance between two points
    /// on the Earth's surface is required. The calculation assumes a spherical Earth and does not account for elevation
    /// or local variations in terrain. All distances are calculated in kilometers.</remarks>
    public class GeographicDistance
    {
        private const double EarthRadiusKm = 6371;
        private const double DegreesToRadiansFactor = Math.PI / 180;

        /// <summary>
        /// Calculates the great-circle distance, in kilometers, between two geographic coordinates using the Haversine
        /// formula.
        /// </summary>
        /// <remarks>This method assumes a spherical Earth and does not account for elevation differences
        /// or ellipsoidal effects. The result is an approximation suitable for most general-purpose distance
        /// calculations.</remarks>
        /// <param name="latitudeFirstCity">The latitude of the first location, in decimal degrees. Must be between -90 and 90.</param>
        /// <param name="longitudeFirstCity">The longitude of the first location, in decimal degrees. Must be between -180 and 180.</param>
        /// <param name="latitudeSecondCity">The latitude of the second location, in decimal degrees. Must be between -90 and 90.</param>
        /// <param name="longitudeSecondCity">The longitude of the second location, in decimal degrees. Must be between -180 and 180.</param>
        /// <returns>The distance between the two locations, in kilometers, measured along the surface of the Earth.</returns>
        public static double CalculateDistance(
            double latitudeFirstCity,
            double longitudeFirstCity,
            double latitudeSecondCity,
            double longitudeSecondCity)
        {
            var deltaLatitude = DegreesToRadians(latitudeSecondCity - latitudeFirstCity);
            var deltaLongitude = DegreesToRadians(longitudeSecondCity - longitudeFirstCity);

            var lat1Rad = DegreesToRadians(latitudeFirstCity);
            var lat2Rad = DegreesToRadians(latitudeSecondCity);

            var haversineValue =
                (Math.Sin(deltaLatitude / 2) * Math.Sin(deltaLatitude / 2)) +
                (Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLongitude / 2) * Math.Sin(deltaLongitude / 2));

            var centralAngle =
                2 * Math.Atan2(Math.Sqrt(haversineValue), Math.Sqrt(1 - haversineValue));

            var distance = EarthRadiusKm * centralAngle;

            return distance;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * DegreesToRadiansFactor;
        }
    }
}