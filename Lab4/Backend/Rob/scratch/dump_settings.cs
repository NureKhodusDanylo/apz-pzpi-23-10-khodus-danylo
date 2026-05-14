
using System;
using System.IO;
using Microsoft.Data.Sqlite;

class Program {
    static void Main() {
        string dbPath = @"e:\Dodikuser\GovnoNURE\Rob_Delivery\Backend\Rob\RobDeliveryAPI\RobDelivery.db";
        using (var connection = new SqliteConnection($"Data Source={dbPath}")) {
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT * FROM SystemSettings";
            using (var reader = command.ExecuteReader()) {
                while (reader.Read()) {
                    for (int i = 0; i < reader.FieldCount; i++) {
                        Console.Write($"{reader.GetName(i)}: {reader.GetValue(i)} | ");
                    }
                    Console.WriteLine();
                }
            }
        }
    }
}
