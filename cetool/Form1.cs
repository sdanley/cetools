using System.Data;
using System.Text;

namespace cetool
{
    public partial class Form1 : Form
    {
        FileInfo exeFileInfo;
        public Form1()
        {
            InitializeComponent();
            openToolStripMenuItem.PerformClick();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var found = Directory.GetFiles("../../../../cedump", "cedump.exe", SearchOption.AllDirectories)
                    .FirstOrDefault();
            if (found is not null)
            {
                exeFileInfo = new FileInfo(found);
            }
            else
            {
                // configure open file dialog
                OpenFileDialog openFileDialog = new();
                openFileDialog.InitialDirectory = "../../../../cedump";
                openFileDialog.Filter = "exe files (*.exe)|*.exe|All files (*.*)|*.*";

                // show dialog and check for result
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    exeFileInfo = new FileInfo(openFileDialog.FileName);
                }
                else
                {
                    return;
                }
            }
            LoadData(sender, e);
        }

        // background task to load data
        private async void LoadData(object sender, EventArgs e)
        {
            if (exeFileInfo is null)
            {
                MessageBox.Show("Please select a cedump.exe file");
                return;
            }

            // create process
            System.Diagnostics.Process process = new();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            process.StartInfo.FileName = exeFileInfo.FullName;
            process.StartInfo.Arguments =  @"C:\System\Control\Data\ --skip Products";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.Start();

            // read output and store in stringbuilder
            StringBuilder sb = new();
            while (!process.StandardOutput.EndOfStream)
            {
                var line = await process.StandardOutput.ReadLineAsync();
                if (string.IsNullOrEmpty(line)) continue;
                sb.AppendLine(line);
            }

            // parse output and store in dataset
            DataSet dataset = new();
            DataTable table = new();
            using (var reader = new StringReader(sb.ToString()))
            {
                tabControl1.TabPages.Clear();
                while (reader.Peek() > -1)
                {
                    var line = reader.ReadLine();
                    if (string.IsNullOrEmpty(line)) continue;

                    // parse line and check for #tablename
                    if (line.StartsWith("#"))
                    {
                        table = dataset.Tables.Add(line.Substring(1));
                        tabControl1.TabPages.Add(table.TableName);
                        tabControl1.TabPages[tabControl1.TabPages.Count - 1].Controls.Add(new DataGridView() { Dock = DockStyle.Fill, DataSource = table });
                        line = reader.ReadLine();
                        if (string.IsNullOrEmpty(line)) continue;

                        // parse header and add columns to table
                        foreach (var name in line.Split(","))
                        {
                            table.Columns.Add(name, typeof(string));
                        }
                        continue;
                    }

                    // parse line and add to table
                    table.Rows.Add(line.Split(","));
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}