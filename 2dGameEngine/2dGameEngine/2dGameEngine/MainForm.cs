using System;
using System.Drawing;
using System.Numerics;
using System.Windows.Forms;
using _2dGameEngine.Core;

namespace _2dGameEngine;

/// <summary>
/// Hosts the Phase 1 engine runtime demonstration.
/// </summary>
public sealed class MainForm : Form
{
    private readonly Engine _engine;
    private readonly Entity _demoEntity;
    private readonly Label _engineStatusLabel;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainForm"/> class.
    /// </summary>
    public MainForm()
    {
        Text = "2dGameEngine";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(480, 240);
        ClientSize = new Size(640, 320);

        TableLayoutPanel layout = new()
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(24),
            RowCount = 2,
            ColumnCount = 1,
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Label titleLabel = new()
        {
            AutoSize = true,
            Font = new Font(Font.FontFamily, 18.0f, FontStyle.Bold),
            Text = "2dGameEngine Phase 1 Runtime",
        };

        _engineStatusLabel = new Label
        {
            AutoSize = true,
            Font = new Font(FontFamily.GenericMonospace, 10.0f),
            Margin = new Padding(0, 12, 0, 0),
            Text = "Starting engine...",
        };

        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(_engineStatusLabel, 0, 1);
        Controls.Add(layout);

        _engine = new Engine();
        Scene scene = new("Phase 1 Demo Scene");
        _demoEntity = scene.CreateEntity("Updating Entity");
        _demoEntity.AddComponent(new EntityMotionComponent(new Vector2(32.0f, 0.0f)));

        _engine.SetActiveScene(scene);
        _engine.Updated += OnEngineUpdated;
        _engine.Start();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _engine.Stop();
        _engine.Updated -= OnEngineUpdated;
        base.OnFormClosed(e);
    }

    private void OnEngineUpdated(object? sender, EngineUpdatedEventArgs args)
    {
        Vector2 position = _demoEntity.Transform.Value.Position;
        _engineStatusLabel.Text = FormattableString.Invariant(
            $"Frame: {args.Time.FrameCount}\nDelta: {args.Time.DeltaTime.TotalMilliseconds:0.00} ms\nEntity: {_demoEntity.Name}\nPosition: ({position.X:0.00}, {position.Y:0.00})");
    }
}
