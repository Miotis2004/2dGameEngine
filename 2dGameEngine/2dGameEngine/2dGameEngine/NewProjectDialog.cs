using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace _2dGameEngine;

/// <summary>
/// Collects the name and destination folder for a new editor project.
/// </summary>
public sealed class NewProjectDialog : Form
{
    private readonly TextBox _projectNameTextBox;
    private readonly TextBox _projectRootTextBox;
    private readonly Button _createButton;

    /// <summary>
    /// Initializes a new instance of the <see cref="NewProjectDialog"/> class.
    /// </summary>
    public NewProjectDialog()
    {
        Text = "Create New Project";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(520, 170);

        Label nameLabel = new()
        {
            AutoSize = true,
            Location = new Point(16, 20),
            Text = "Project name",
        };
        _projectNameTextBox = new TextBox
        {
            Location = new Point(130, 16),
            Size = new Size(360, 23),
            Text = "NewPlatformerGame",
        };

        Label rootLabel = new()
        {
            AutoSize = true,
            Location = new Point(16, 62),
            Text = "Create under",
        };
        _projectRootTextBox = new TextBox
        {
            Location = new Point(130, 58),
            Size = new Size(280, 23),
            Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        };
        Button browseButton = new()
        {
            Location = new Point(420, 57),
            Size = new Size(70, 25),
            Text = "Browse...",
        };
        browseButton.Click += OnBrowseClicked;

        _createButton = new Button
        {
            DialogResult = DialogResult.OK,
            Location = new Point(320, 118),
            Size = new Size(80, 28),
            Text = "Create",
        };
        Button cancelButton = new()
        {
            DialogResult = DialogResult.Cancel,
            Location = new Point(410, 118),
            Size = new Size(80, 28),
            Text = "Cancel",
        };

        AcceptButton = _createButton;
        CancelButton = cancelButton;

        Controls.Add(nameLabel);
        Controls.Add(_projectNameTextBox);
        Controls.Add(rootLabel);
        Controls.Add(_projectRootTextBox);
        Controls.Add(browseButton);
        Controls.Add(_createButton);
        Controls.Add(cancelButton);
    }

    /// <summary>
    /// Gets the requested project name.
    /// </summary>
    public string ProjectName => _projectNameTextBox.Text.Trim();

    /// <summary>
    /// Gets the parent directory where the project should be created.
    /// </summary>
    public string ProjectRootDirectory => _projectRootTextBox.Text.Trim();

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (DialogResult == DialogResult.OK)
        {
            if (string.IsNullOrWhiteSpace(ProjectName))
            {
                MessageBox.Show(this, "Enter a project name.", "Project Name Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(ProjectRootDirectory) || !Directory.Exists(ProjectRootDirectory))
            {
                MessageBox.Show(this, "Choose an existing parent folder.", "Project Folder Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                e.Cancel = true;
            }
        }

        base.OnFormClosing(e);
    }

    private void OnBrowseClicked(object? sender, EventArgs e)
    {
        using FolderBrowserDialog dialog = new()
        {
            Description = "Choose where the new game project folder should be created.",
            SelectedPath = Directory.Exists(ProjectRootDirectory) ? ProjectRootDirectory : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            UseDescriptionForTitle = true,
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _projectRootTextBox.Text = dialog.SelectedPath;
        }
    }
}
