using System.Data;
using System.CommandLine;

var filepath = new FileInfo("../../../../cedump/bin/Debug/cedump.exe");
var fileArg = new Argument<FileInfo>("data folder", getDefaultValue: () => filepath);
var rootCommand = new RootCommand("dump sqlce files as csv") { fileArg };

rootCommand.SetHandler((file) =>
{
    if (file.Exists)
    {
        // use file argument
        filepath = file;
    }
    else
    {
        // search parent directory for cedump.exe
        var found = Directory.GetFiles("../../../../cedump", "cedump.exe", SearchOption.AllDirectories)
            .FirstOrDefault();
        if (found is not null)
        {
            filepath = new FileInfo(found);
        }
        else
        {
            // report error and exit
            Console.WriteLine("cedump.exe not found");
            Environment.Exit(1);
        }

    }
    filepath = file;
}, fileArg);

System.Diagnostics.Process process = new System.Diagnostics.Process();
process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
process.StartInfo.FileName = filepath.FullName;
process.StartInfo.UseShellExecute = false;
process.StartInfo.CreateNoWindow = true;
process.StartInfo.RedirectStandardOutput = true;
process.Start();

DataSet dataset = new();
DataTable table = new();

// read output and store in stringbuilder
while (!process.StandardOutput.EndOfStream)
{
    var line = process.StandardOutput.ReadLine();
    if (string.IsNullOrEmpty(line)) continue;

    // parse line and check for #tablename
    if (line.StartsWith("#"))
    {
        table = dataset.Tables.Add(line.Substring(1));
        line = process.StandardOutput.ReadLine();
        if (string.IsNullOrEmpty(line)) continue;

        // parse header and add columns to table
        foreach (var name in line.Split(","))
        {
            table.Columns.Add(name, typeof(string));
        }
        continue;
    }
    // parse data and add row to table
    var row = table.NewRow();
    var values = line.Split(",");
    for (var i = 0; i < values.Length; i++)
    {
        row[i] = values[i];
    }
    table.Rows.Add(row);
}

Console.WriteLine(dataset.GetXml());
