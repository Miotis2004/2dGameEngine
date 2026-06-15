using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace _2dGameEngine;

public sealed class BuildSettingsDialog : Form
{
    private readonly TextBox _productNameTextBox = new();
    private readonly TextBox _versionTextBox = new();
    private readonly ComboBox _configurationComboBox = new();
    private readonly ComboBox _startupSceneComboBox = new();
    private readonly TextBox _outputDirectoryTextBox = new();
    private readonly TextBox _iconPathTextBox = new();
    private readonly CheckBox _runnableFolderCheckBox = new();
    private readonly CreatedProject _project;

    public BuildSettingsDialog(CreatedProject project)
    {
        _project = project;
        BuildSettings defaults = BuildSettings.CreateDefault(project);
        Text = "Build Settings";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(560, 360);
        ClientSize = new Size(620, 380);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 8,
            Padding = new Padding(12),
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));

        _productNameTextBox.Text = defaults.ProductName;
        _versionTextBox.Text = defaults.Version;
        _configurationComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _configurationComboBox.Items.AddRange(Enum.GetNames<BuildConfiguration>());
        _configurationComboBox.SelectedItem = defaults.Configuration.ToString();
        _startupSceneComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        foreach (string scene in Directory.GetFiles(project.ScenesDirectory, "*.scene.json", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.OrdinalIgnoreCase))
        {
            _startupSceneComboBox.Items.Add(scene);
        }
        if (_startupSceneComboBox.Items.Count == 0)
        {
            _startupSceneComboBox.Items.Add(defaults.StartupScene);
        }
        _startupSceneComboBox.SelectedItem = defaults.StartupScene;
        _outputDirectoryTextBox.Text = defaults.OutputDirectory;
        _iconPathTextBox.Text = defaults.IconPath;
        _runnableFolderCheckBox.Text = "Export runnable folder";
        _runnableFolderCheckBox.Checked = defaults.RunnableFolderExport;

        AddRow(layout, 0, "Product", _productNameTextBox);
        AddRow(layout, 1, "Version", _versionTextBox);
        AddRow(layout, 2, "Configuration", _configurationComboBox);
        AddRow(layout, 3, "Startup scene", _startupSceneComboBox);
        AddPathRow(layout, 4, "Output", _outputDirectoryTextBox, BrowseOutput);
        AddPathRow(layout, 5, "Icon", _iconPathTextBox, BrowseIcon);
        layout.Controls.Add(new Label { Text = "Target", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 6);
        layout.Controls.Add(new Label { Text = "Windows Desktop", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 1, 6);
        layout.Controls.Add(_runnableFolderCheckBox, 1, 7);

        FlowLayoutPanel buttons = new() { Dock = DockStyle.Bottom, FlowDirection = FlowDirection.RightToLeft, Height = 42, Padding = new Padding(8) };
        Button buildButton = new() { Text = "Build", DialogResult = DialogResult.OK };
        Button cancelButton = new() { Text = "Cancel", DialogResult = DialogResult.Cancel };
        buttons.Controls.Add(buildButton);
        buttons.Controls.Add(cancelButton);
        AcceptButton = buildButton;
        CancelButton = cancelButton;
        Controls.Add(layout);
        Controls.Add(buttons);
    }

    public BuildSettings Settings => new(
        BuildTargetPlatform.WindowsDesktop,
        Enum.Parse<BuildConfiguration>((string)_configurationComboBox.SelectedItem!),
        (string)_startupSceneComboBox.SelectedItem!,
        _outputDirectoryTextBox.Text,
        _productNameTextBox.Text,
        _versionTextBox.Text,
        _iconPathTextBox.Text,
        _runnableFolderCheckBox.Checked);

    private static void AddRow(TableLayoutPanel layout, int row, string label, Control control)
    {
        control.Dock = DockStyle.Fill;
        layout.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        layout.Controls.Add(control, 1, row);
        layout.SetColumnSpan(control, 2);
    }

    private static void AddPathRow(TableLayoutPanel layout, int row, string label, TextBox textBox, EventHandler browse)
    {
        textBox.Dock = DockStyle.Fill;
        Button button = new() { Text = "Browse", Dock = DockStyle.Fill };
        button.Click += browse;
        layout.Controls.Add(new Label { Text = label, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, row);
        layout.Controls.Add(textBox, 1, row);
        layout.Controls.Add(button, 2, row);
    }

    private void BrowseOutput(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dialog = new() { InitialDirectory = Directory.Exists(_outputDirectoryTextBox.Text) ? _outputDirectoryTextBox.Text : _project.ProjectDirectory };
        if (dialog.ShowDialog(this) == DialogResult.OK) _outputDirectoryTextBox.Text = dialog.SelectedPath;
    }

    private void BrowseIcon(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new() { Filter = "Icons and images|*.ico;*.png|All files|*.*", InitialDirectory = _project.ProjectDirectory };
        if (dialog.ShowDialog(this) == DialogResult.OK) _iconPathTextBox.Text = dialog.FileName;
    }
}
