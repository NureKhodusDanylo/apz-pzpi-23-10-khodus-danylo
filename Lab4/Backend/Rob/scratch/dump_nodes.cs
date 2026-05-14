
using System;
using Microsoft.Data.Sqlite;
using System.Collections.Generic;

class Program {
    static void Main() {
        string dbPath = @"e:\Dodikuser\GovnoNURE\Rob_Delivery\Backend\Rob\Infrastructure\DB_Storage\RobDelivery.db";
        using (var connection = new SqliteConnection($"Data Source={dbPath}")) {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT Id, Name, Type, BatteryLevel, Status, BatteryCapacityJoules, EnergyConsumptionPerMeterJoules, CurrentLatitude, CurrentLongitude, CurrentNodeId FROM Robots";
            using (var reader = command.ExecuteReader()) {
                Console.WriteLine("Id|Name|Type|Battery|Status|Capacity|Consumption|Lat|Lon|NodeId");
                while (reader.Read()) {
                    Console.WriteLine($"{reader["Id"]}|{reader["Name"]}|{reader["Type"]}|{reader["BatteryLevel"]}|{reader["Status"]}|{reader["BatteryCapacityJoules"]}|{reader["EnergyConsumptionPerMeterJoules"]}|{reader["CurrentLatitude"]}|{reader["CurrentLongitude"]}|{reader["CurrentNodeId"]}");
                }
            }
        }
    }
}
