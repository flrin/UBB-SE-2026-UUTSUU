namespace SearchAndBook.Utils
{
    using System;

    public class GeographicDistance
    {
        private const double EarthRadiusKm = 6371;
        private const double DegreesToRadiansFactor = Math.PI / 180;

        public static double CalculateDistance(double longitudeFirstCity, double longitudeSecondCity, double latitudeFirstCity, double latitudeSecondCity)
        {
            var differenceLatitude = DegreesToRadians(latitudeFirstCity - longitudeFirstCity);
            var differenceLongitude = DegreesToRadians(latitudeSecondCity - longitudeSecondCity);
            var a =
                Math.Sin(differenceLatitude / 2) * Math.Sin(differenceLatitude / 2) +
                Math.Cos(DegreesToRadians(longitudeFirstCity)) * Math.Cos(DegreesToRadians(latitudeFirstCity)) *
                Math.Sin(differenceLongitude / 2) * Math.Sin(differenceLongitude / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKm * c;
        }

        private static double DegreesToRadians(double degrees)
        {
            return degrees * DegreesToRadiansFactor;
        }
    }
}