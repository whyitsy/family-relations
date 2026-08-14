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
/// 自绘关系图谱：分层树布局 + 平移/缩放 + 节点点击选择/拖拽 + 选中高亮直接连线。
/// 全部控件用代码创建，无反射、无动态 XAML，AOT 安全。
/// </summary>
public partial class GraphCanvas : UserControl
{
    private const double NodeWidth = 150;
    private const double NodeHeight = 54;
    private const double DragThreshold = 3;

    private readonly TranslateTransform _translate = new();
    private readonly ScaleTransform _scale = new();
    private readonly TransformGroup _transform = new();
    private readonly Dictionary<string, Border> _nodes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Point> _nodePositions = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Point> _manualPositions = new(StringComparer.Ordinal);
    private readonly List<EdgeVisual> _edges = new();

    private Action<Person>? _onSelect;
    private Action? _onDeselect;
    private string? _selfId;
    private string? _selectedId;
    private FamilyData? _lastData;
    private IReadOnlyList<KinshipResult> _lastResults = Array.Empty<KinshipResult>();

    // 平移状态
    private Point _lastPan;
    private Point _pressPos;
    private bool _panning;
    private bool _moved;

    // 节点拖拽状态
    private string? _dragNodeId;
    private Point _dragStartPointer;
    private Point _dragStartPos;
    private bool _dragMoved;

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
        Action<Person> onSelect,
        Action? onDeselect = null)
    {
        _selfId = selfId;
        _selectedId = selectedId;
        _onSelect = onSelect;
        _onDeselect = onDeselect;
        _lastData = data;
        _lastResults = results;

        Rebuild();
    }

    /// <summary>清空手动拖拽的位置，恢复自动布局并重置缩放与平移。</summary>
    public void ResetLayout()
    {
        if (_lastData is null)
            return;

        _manualPositions.Clear();
        _scale.ScaleX = 1;
        _scale.ScaleY = 1;
        _translate.X = 0;
        _translate.Y = 0;
        _centered = false;

        Rebuild();
    }

    private void Rebuild()
    {
        if (_lastData is null)
            return;

        Surface.Children.Clear();
        _nodes.Clear();
        _edges.Clear();
        _nodePositions.Clear();

        var (layouts, edges) = GraphLayoutEngine.Compute(_lastData, _selfId, _lastResults);

        // 清理已不存在节点的拖拽位置
        var validIds = new HashSet<string>(layouts.Select(n => n.Person.Id), StringComparer.Ordinal);
        foreach (var stale in _manualPositions.Keys.Where(k => !validIds.Contains(k)).ToList())
            _manualPositions.Remove(stale);

        foreach (var n in layouts)
        {
            var pos = _manualPositions.TryGetValue(n.Person.Id, out var mp) ? mp : new Point(n.X, n.Y);
            _nodePositions[n.Person.Id] = pos;
        }

        // 先画边（置于底层）
        foreach (var e in edges)
        {
            if (!_nodePositions.TryGetValue(e.FromId, out var a) || !_nodePositions.TryGetValue(e.ToId, out var b))
                continue;

            var line = new Line
            {
                Stroke = new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF)),
                StrokeThickness = 1.5,
                IsHitTestVisible = false,
            };
            SetEdgeEndpoints(line, a, b);
            Surface.Children.Add(line);
            _edges.Add(new EdgeVisual(e.FromId, e.ToId, line));
        }

        // 再画节点（置于上层）
        foreach (var n in layouts)
        {
            var pos = _nodePositions[n.Person.Id];
            var border = BuildNode(n);
            Canvas.SetLeft(border, pos.X);
            Canvas.SetTop(border, pos.Y);
            Surface.Children.Add(border);
            _nodes[n.Person.Id] = border;
        }

        ApplySelectionVisuals();
        EnsureCentered();
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
            _dragNodeId = n.Person.Id;
            _dragStartPointer = e.GetPosition(Viewport);
            _dragStartPos = _nodePositions[n.Person.Id];
            _dragMoved = false;
            e.Pointer.Capture(Viewport);
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

        // 高亮/加粗与选中成员直接相连的边，其余边淡出。
        foreach (var edge in _edges)
        {
            var connected = _selectedId is not null && (edge.FromId == _selectedId || edge.ToId == _selectedId);
            edge.Line.Stroke = connected
                ? new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81))
                : new SolidColorBrush(Color.FromRgb(0x9C, 0xA3, 0xAF));
            edge.Line.StrokeThickness = connected ? 3.0 : 1.5;
            edge.Line.Opacity = _selectedId is null || connected ? 1.0 : 0.35;
        }
    }

    private void MoveNode(string id, Point pos)
    {
        _nodePositions[id] = pos;
        _manualPositions[id] = pos;

        if (_nodes.TryGetValue(id, out var border))
        {
            Canvas.SetLeft(border, pos.X);
            Canvas.SetTop(border, pos.Y);
        }

        foreach (var edge in _edges)
        {
            if (edge.FromId != id && edge.ToId != id)
                continue;
            if (_nodePositions.TryGetValue(edge.FromId, out var a) && _nodePositions.TryGetValue(edge.ToId, out var b))
                SetEdgeEndpoints(edge.Line, a, b);
        }
    }

    private static void SetEdgeEndpoints(Line line, Point a, Point b)
    {
        line.StartPoint = new Point(a.X + NodeWidth / 2, a.Y + NodeHeight / 2);
        line.EndPoint = new Point(b.X + NodeWidth / 2, b.Y + NodeHeight / 2);
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
        e.Pointer.Capture(Viewport);
        _panning = true;
        _lastPan = e.GetPosition(Viewport);
        _pressPos = _lastPan;
        _moved = false;
    }

    private void OnViewportPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        e.Pointer.Capture(null);

        if (_dragNodeId is not null)
        {
            _dragNodeId = null;
            return;
        }

        if (_panning && !_moved)
            _onDeselect?.Invoke();

        _panning = false;
    }

    private void OnViewportPointerMoved(object? sender, PointerEventArgs e)
    {
        var p = e.GetPosition(Viewport);

        if (_dragNodeId is not null)
        {
            var dx = p.X - _dragStartPointer.X;
            var dy = p.Y - _dragStartPointer.Y;

            if (!_dragMoved && (Math.Abs(dx) > DragThreshold || Math.Abs(dy) > DragThreshold))
                _dragMoved = true;

            if (_dragMoved)
            {
                var scale = _scale.ScaleX;
                var newPos = new Point(_dragStartPos.X + dx / scale, _dragStartPos.Y + dy / scale);
                MoveNode(_dragNodeId, newPos);
            }
            return;
        }

        if (!_panning)
            return;

        _translate.X += p.X - _lastPan.X;
        _translate.Y += p.Y - _lastPan.Y;
        _lastPan = p;

        if (!_moved && (Math.Abs(p.X - _pressPos.X) > DragThreshold || Math.Abs(p.Y - _pressPos.Y) > DragThreshold))
            _moved = true;
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

    private sealed class EdgeVisual
    {
        public string FromId { get; }
        public string ToId { get; }
        public Line Line { get; }

        public EdgeVisual(string fromId, string toId, Line line)
        {
            FromId = fromId;
            ToId = toId;
            Line = line;
        }
    }
}
