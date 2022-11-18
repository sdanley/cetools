using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Data.SqlServerCe;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace cedump
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // tables to skip
            string[] skip = new string[] { "sysdiagrams", "Products" };

            var directory = new DirectoryInfo(@"C:\System\Control\Data\");
            var directoryArg = new Argument<DirectoryInfo>("folder containing .sdf files", getDefaultValue: () => directory);
            var skipOption = new Option<string[]>("--skip", description: "table names to skip", getDefaultValue: () => skip) { 
                
                AllowMultipleArgumentsPerToken = true 
            };
            var rootCommand = new RootCommand("dump sqlce files as csv") { directoryArg, skipOption };

            rootCommand.SetHandler((_dir, _skip) => { 
                directory = _dir; 
                skip = _skip;
            }, directoryArg, skipOption);
            rootCommand.InvokeAsync(args).Wait();

            // process all sdf files in directory
            foreach (var fileinfo in directory.GetFiles("*.sdf"))
            {
                var connection = new SqlCeConnection($"Data Source={fileinfo.FullName}");
                connection.Open();

                // read each table
                var command = new SqlCeCommand("select * from information_schema.tables", connection);
                var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    var table = reader[2].ToString();

                    // don't dump products table
                    if (skip.Contains(table))
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
