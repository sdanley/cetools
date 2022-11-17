using System;
using System.CommandLine;
using System.Data.SqlServerCe;
using System.IO;

namespace cedump
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var directory = new DirectoryInfo(@"C:\System\Control\Data\");
            var directoryArg = new Argument<DirectoryInfo>("data folder", getDefaultValue: () => directory);
            var rootCommand = new RootCommand("dump sqlce files as csv") { directoryArg };

            rootCommand.SetHandler((dir) =>
            {
                directory = dir;
            }, directoryArg);

            rootCommand.InvokeAsync(args).Wait();

            foreach (var filename in new[] { "MachineModelsDB.sdf", "CustomerDB.sdf", "QuilterDB.sdf" })
            {
                var path = Path.Combine(@"C:\System\Control\Data\", filename);

                var connection = new SqlCeConnection($"Data Source={path}");
                connection.Open();

                // read each table
                var command = new SqlCeCommand("select * from information_schema.tables", connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var table = reader[2].ToString();

                    // don't dump products table
                    if (table == "Products")
                        continue;

                    Console.WriteLine($"#{table}");
                    command = new SqlCeCommand($"select * from {table}", connection);
                    var tableReader = command.ExecuteReader();

                    // write header row
                    int fieldCount = tableReader.FieldCount;
                    for (var i = 0; i < fieldCount; i++)
                    {
                        Console.Write(tableReader.GetName(i));
                        if (i < fieldCount - 1)
                            Console.Write(",");
                    }
                    Console.WriteLine();

                    // write data as csv
                    while (tableReader.Read())
                    {
                        for (var i = 0; i < fieldCount; i++)
                        {
                            Console.Write(tableReader[i]);
                            if (i < fieldCount - 1)
                                Console.Write(",");
                        }
                        Console.WriteLine();
                    }
                    tableReader.Close();
                }
                // cleanup
                reader.Close();
                connection.Close();
            }
        }
    }
}
