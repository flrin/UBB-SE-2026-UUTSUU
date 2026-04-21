namespace SearchAndBook.Utils
{
    using System;

    public class GeographicDistance
    {
        private const double EarthRadiusKm = 6371;
        private const double DegreesToRadiansFactor = Math.PI / 180;

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
                Math.Sin(deltaLatitude / 2) * Math.Sin(deltaLatitude / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLongitude / 2) * Math.Sin(deltaLongitude / 2);

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