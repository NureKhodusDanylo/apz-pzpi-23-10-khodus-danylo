
using System;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

class Program {
    static void Main() {
        string dbPath = @"e:\Dodikuser\GovnoNURE\Rob_Delivery\Backend\Rob\Infrastructure\DB_Storage\RobDelivery.db";
        using (var connection = new SqliteConnection($"Data Source={dbPath}")) {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Name, Latitude, Longitude FROM Nodes WHERE Type=1";
            
            var fromLat = 43.2185226; // Tehnikumi
            var fromLon = 27.893892;
            var toLat = 43.2311418; // Trakata
            var toLon = 27.9982368;
            
            using (var reader = command.ExecuteReader()) {
                while (reader.Read()) {
                    var name = reader.GetString(0);
                    var lat = reader.GetDouble(1);
                    var lon = reader.GetDouble(2);
                    
                    double d1 = CalculateDistance(fromLat, fromLon, lat, lon);
                    double d2 = CalculateDistance(lat, lon, toLat, toLon);
                    
                    if (d1 < 5450 && d2 < 5450) {
                        Console.WriteLine($"Found path through: {name}. D1={Math.Round(d1)}m, D2={Math.Round(d2)}m");
                    }
                }
            }
        }
    }

    static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        var R = 6371e3;
        var phi1 = lat1 * Math.PI / 180;
        var phi2 = lat2 * Math.PI / 180;
        var dphi = (lat2 - lat1) * Math.PI / 180;
        var dlambda = (lon2 - lon1) * Math.PI / 180;
        var a = Math.Sin(dphi / 2) * Math.Sin(dphi / 2) + Math.Cos(phi1) * Math.Cos(phi2) * Math.Sin(dlambda / 2) * Math.Sin(dlambda / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }
}
