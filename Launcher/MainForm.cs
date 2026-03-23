using System.Drawing;
using System.Windows.Forms;

namespace Il2CppDumperLauncher;

internal sealed class MainForm : Form
{
    private readonly TextBox txtInput = new() { Dock = DockStyle.Fill };
    private readonly TextBox txtMetadata = new() { Dock = DockStyle.Fill };
    private readonly TextBox txtOutput = new() { Dock = DockStyle.Fill };
    private readonly ComboBox cmbArch = new()
    {
        Dock = DockStyle.Fill,
        DropDownStyle = ComboBoxStyle.DropDownList,
    };
    private readonly TextBox txtLog = new()
    {
        Dock = DockStyle.Fill,
        Multiline = true,
        ReadOnly = true,
        ScrollBars = ScrollBars.Vertical,
    };

    private readonly Button btnBrowseInput = new() { Text = "..." };
    private readonly Button btnBrowseMetadata = new() { Text = "..." };
    private readonly Button btnBrowseOutput = new() { Text = "..." };
    private readonly Button btnDump = new() { Text = "Dump", Dock = DockStyle.Fill, Height = 36 };

    public MainForm()
    {
        Text = "Il2CppDumper Launcher";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(920, 600);
        Width = 1040;
        Height = 700;

        cmbArch.Items.AddRange(new object[]
        {
            new ArchComboItem("Auto", LaunchArchMode.Auto),
            new ArchComboItem("32-bit", LaunchArchMode.Force32),
            new ArchComboItem("64-bit", LaunchArchMode.Force64),
        });
        cmbArch.SelectedIndex = 0;

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 7,
            Padding = new Padding(10),
        };

        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 40));

        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        table.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        table.Controls.Add(new Label { Text = "Binary / libil2cpp.so / APK / XAPK", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 0);
        table.Controls.Add(txtInput, 1, 0);
        table.Controls.Add(btnBrowseInput, 2, 0);

        table.Controls.Add(new Label { Text = "global-metadata.dat", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 1);
        table.Controls.Add(txtMetadata, 1, 1);
        table.Controls.Add(btnBrowseMetadata, 2, 1);

        var metadataHint = new Label { Text = "Optional for APK/XAPK in auto-resolve mode", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill };
        table.Controls.Add(metadataHint, 0, 2);
        table.SetColumnSpan(metadataHint, 3);

        table.Controls.Add(new Label { Text = "Output folder", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 3);
        table.Controls.Add(txtOutput, 1, 3);
        table.Controls.Add(btnBrowseOutput, 2, 3);

        table.Controls.Add(new Label { Text = "Architecture", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 4);
        table.Controls.Add(cmbArch, 1, 4);
        table.Controls.Add(new Label { Text = string.Empty }, 2, 4);

        table.SetColumnSpan(btnDump, 3);
        table.Controls.Add(btnDump, 0, 5);

        table.SetColumnSpan(txtLog, 3);
        table.Controls.Add(txtLog, 0, 6);

        Controls.Add(table);

        btnBrowseInput.Click += (_, _) => BrowseInput();
        btnBrowseMetadata.Click += (_, _) => BrowseFile(txtMetadata, "Select global-metadata.dat", "Metadata file (global-metadata.dat)|global-metadata.dat|All files (*.*)|*.*");
        btnBrowseOutput.Click += (_, _) => BrowseFolder(txtOutput);
        btnDump.Click += async (_, _) => await RunDumpAsync();
    }

    private void BrowseInput()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select binary, libil2cpp.so, APK, or XAPK",
            Filter = "Supported input (*.xapk;*.apk;*.so;*.dll;*.exe)|*.xapk;*.apk;*.so;*.dll;*.exe|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtInput.Text = dialog.FileName;
        }
    }

    private void BrowseFile(TextBox target, string title, string filter)
    {
        using var dialog = new OpenFileDialog
        {
            Title = title,
            Filter = filter,
            CheckFileExists = true,
            Multiselect = false,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            target.Text = dialog.FileName;
        }
    }

    private void BrowseFolder(TextBox target)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select output folder",
            UseDescriptionForTitle = true,
            ShowNewFolderButton = true,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            target.Text = dialog.SelectedPath;
        }
    }

    private async Task RunDumpAsync()
    {
        var inputPath = txtInput.Text.Trim();
        var metadataPath = txtMetadata.Text.Trim();
        var outputPath = txtOutput.Text.Trim();
        var mode = ((ArchComboItem)cmbArch.SelectedItem!).Mode;
        var isPackage = Program.IsPackagePath(inputPath);

        if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
        {
            MessageBox.Show(this, "Specify a valid binary, APK, or XAPK path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!isPackage && (string.IsNullOrWhiteSpace(metadataPath) || !File.Exists(metadataPath)))
        {
            MessageBox.Show(this, "Specify a valid global-metadata.dat path.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (!string.IsNullOrWhiteSpace(metadataPath) && !File.Exists(metadataPath))
        {
            MessageBox.Show(this, "The selected global-metadata.dat file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            MessageBox.Show(this, "Specify an output folder.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        Directory.CreateDirectory(outputPath);

        SetUiEnabled(false);
        txtLog.Clear();
        AppendLog("Starting...");

        try
        {
            if (isPackage && string.IsNullOrWhiteSpace(metadataPath))
            {
                AppendLog("Metadata file was not selected. The launcher will try to resolve it automatically from the package.");
            }

            var exitCode = await Program.LaunchAsync(
                inputPath,
                string.IsNullOrWhiteSpace(metadataPath) ? null : metadataPath,
                outputPath,
                mode,
                AppendLog);

            AppendLog(exitCode == 0 ? "Finished." : $"Failed with code {exitCode}.");
        }
        catch (Exception ex)
        {
            AppendLog(ex.ToString());
            MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetUiEnabled(true);
        }
    }

    private void SetUiEnabled(bool enabled)
    {
        btnBrowseInput.Enabled = enabled;
        btnBrowseMetadata.Enabled = enabled;
        btnBrowseOutput.Enabled = enabled;
        btnDump.Enabled = enabled;
        txtInput.Enabled = enabled;
        txtMetadata.Enabled = enabled;
        txtOutput.Enabled = enabled;
        cmbArch.Enabled = enabled;
    }

    private void AppendLog(string message)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.BeginInvoke(new Action<string>(AppendLog), message);
            return;
        }

        txtLog.AppendText(message + Environment.NewLine);
    }

    private sealed class ArchComboItem
    {
        public ArchComboItem(string title, LaunchArchMode mode)
        {
            Title = title;
            Mode = mode;
        }

        public string Title { get; }
        public LaunchArchMode Mode { get; }

        public override string ToString() => Title;
    }
}
