using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using KinshipCalculator.App.Services;
using KinshipCalculator.Core.Calculator;
using KinshipCalculator.Core.Models;

namespace KinshipCalculator.App.Views;

/// <summary>
/// 自绘关系图谱：分层树布局 + 平移/缩放 + 节点点击选择。
/// 全部控件用代码创建，无反射、无动态 XAML，AOT 安全。
/// </summary>
public partial class GraphCanvas : UserControl
{
    private const double NodeWidth = 150;
    private const double NodeHeight = 54;

    private readonly TranslateTransform _translate = new();
    private readonly ScaleTransform _scale = new();
    private readonly TransformGroup _transform = new();
    private readonly Dictionary<string, Border> _nodes = new(StringComparer.Ordinal);

    private Action<Person>? _onSelect;
    private string? _selfId;
    private string? _selectedId;
    private Point _lastPan;
    private bool _panning;
    private bool _centered;

    public GraphCanvas()
    {
        InitializeComponent();
        _transform.Children.Add(_scale);
        _transform.Children.Add(_translate);
        Surface.RenderTransform = _transform;
    }

    public void SetData(
        FamilyData data,
        string? selfId,
        string? selectedId,
        IReadOnlyList<KinshipResult> results,
        Action<Person> onSelect)
    {
        _selfId = selfId;
        _selectedId = selectedId;
        _onSelect = onSelect;

        Surface.Children.Clear();
        _nodes.Clear();

        var (layouts, edges) = GraphLayoutEngine.Compute(data, selfId, results);
        var positions = layouts.ToDictionary(n => n.Person.Id, n => n, StringComparer.Ordinal);

        // 先画边（置于底层）
        foreach (var e in edges)
        {
            if (!positions.TryGetValue(e.FromId, out var a) || !positions.TryGetValue(e.ToId, out var b))
                continue;

            var line = new Line
            {
                StartPoint = new Point(a.X + NodeWidth / 2, a.Y + NodeHeight / 2),
                EndPoint = new Point(b.X + NodeWidth / 2, b.Y + NodeHeight / 2),
                Stroke = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF)),
                StrokeThickness = 1.5,
                IsHitTestVisible = false,
            };
            Surface.Children.Add(line);
        }

        // 再画节点（置于上层）
        foreach (var n in layouts)
        {
            var border = BuildNode(n);
            Canvas.SetLeft(border, n.X);
            Canvas.SetTop(border, n.Y);
            Surface.Children.Add(border);
            _nodes[n.Person.Id] = border;
        }

        ApplySelectionVisuals();
        EnsureCentered();
    }

    public void SetSelection(string? selectedId)
    {
        _selectedId = selectedId;
        ApplySelectionVisuals();
    }

    private Border BuildNode(GraphNodeLayout n)
    {
        var name = new TextBlock
        {
            Text = n.Person.Name,
            FontSize = 13,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
        };

        var stack = new StackPanel { Margin = new Thickness(10, 5) };
        stack.Children.Add(name);

        if (!string.IsNullOrEmpty(n.Term))
        {
            stack.Children.Add(new TextBlock
            {
                Text = n.Term,
                FontSize = 10,
                Foreground = new SolidColorBrush(Color.FromRgb(0xE8, 0xEA, 0xED)),
                TextWrapping = TextWrapping.NoWrap,
            });
        }

        var border = new Border
        {
            Width = NodeWidth,
            MinHeight = NodeHeight,
            Background = GenderBrush(n.Person.Gender),
            CornerRadius = new CornerRadius(8),
            Child = stack,
            Cursor = new Cursor(StandardCursorType.Hand),
        };

        border.PointerPressed += (_, e) =>
        {
            _onSelect?.Invoke(n.Person);
            e.Handled = true;
        };

        return border;
    }

    private static IBrush GenderBrush(Gender g) => g switch
    {
        Gender.Male => new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
        Gender.Female => new SolidColorBrush(Color.FromRgb(0xEC, 0x48, 0x99)),
        _ => new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)),
    };

    private void ApplySelectionVisuals()
    {
        foreach (var (id, border) in _nodes)
        {
            var isSelf = id == _selfId;
            var isSelected = id == _selectedId;

            border.BorderBrush = isSelf
                ? new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B))
                : isSelected
                    ? new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81))
                    : Brushes.Transparent;
            border.BorderThickness = new Thickness(isSelf || isSelected ? 3 : 0);
        }
    }

    private void EnsureCentered()
    {
        if (_centered || Viewport.Bounds.Width <= 0 || Viewport.Bounds.Height <= 0)
            return;

        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;

        foreach (var (_, border) in _nodes)
        {
            var x = Canvas.GetLeft(border);
            var y = Canvas.GetTop(border);
            minX = Math.Min(minX, x);
            maxX = Math.Max(maxX, x + NodeWidth);
            minY = Math.Min(minY, y);
            maxY = Math.Max(maxY, y + NodeHeight);
        }

        if (minX == double.MaxValue)
            return;

        _translate.X = Viewport.Bounds.Width / 2 - (minX + maxX) / 2;
        _translate.Y = Viewport.Bounds.Height / 2 - (minY + maxY) / 2;
        _centered = true;
    }

    private void OnViewportPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _panning = true;
        _lastPan = e.GetPosition(Viewport);
    }

    private void OnViewportPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _panning = false;
    }

    private void OnViewportPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_panning)
            return;

        var p = e.GetPosition(Viewport);
        _translate.X += p.X - _lastPan.X;
        _translate.Y += p.Y - _lastPan.Y;
        _lastPan = p;
    }

    private void OnViewportPointerWheel(object? sender, PointerWheelEventArgs e)
    {
        var p = e.GetPosition(Viewport);
        var factor = e.Delta.Y > 0 ? 1.1 : 1.0 / 1.1;
        var newScale = Math.Clamp(_scale.ScaleX * factor, 0.2, 4.0);
        factor = newScale / _scale.ScaleX;

        _scale.ScaleX = newScale;
        _scale.ScaleY = newScale;
        _translate.X = p.X - (p.X - _translate.X) * factor;
        _translate.Y = p.Y - (p.Y - _translate.Y) * factor;
    }
}
