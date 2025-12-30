using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using IOPath = System.IO.Path;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;

namespace GenieClient.Views;

public partial class AutoMapperWindow : Window
{
    private readonly List<MapInfo> _availableMaps = new();
    private MapData? _currentMap;
    private double _scale = 1.0;
    private int _currentLevel = 0;
    private Point _panOffset = new(0, 0);
    private Point? _lastPanPoint;
    private bool _isPanning;
    private string _mapDirectory = "";
    private int? _currentNodeId;

    // Colors matching the Windows Forms version
    private readonly IBrush _nodeColor = new SolidColorBrush(Color.FromRgb(255, 255, 192));
    private readonly IBrush _nodeBorderColor = new SolidColorBrush(Color.FromRgb(100, 100, 100));
    private readonly IBrush _nodeOtherLevelColor = new SolidColorBrush(Color.FromRgb(180, 180, 180));
    private readonly IBrush _lineColor = new SolidColorBrush(Color.FromRgb(100, 100, 100));
    private readonly IBrush _lineOtherLevelColor = new SolidColorBrush(Color.FromRgb(60, 60, 60));
    private readonly IBrush _currentNodeColor = new SolidColorBrush(Color.FromRgb(255, 0, 255)); // Magenta
    private readonly IBrush _labelColor = new SolidColorBrush(Color.FromRgb(200, 200, 200));
    private readonly IBrush _linkNodeBorderColor = new SolidColorBrush(Color.FromRgb(0, 0, 255));

    public AutoMapperWindow()
    {
        InitializeComponent();
    }

    public static async Task ShowWindow(Window owner, string mapDirectory)
    {
        var window = new AutoMapperWindow();
        window._mapDirectory = mapDirectory;
        window.LoadAvailableMaps();
        window.Show(owner);
    }

    private void LoadAvailableMaps()
    {
        _availableMaps.Clear();

        try
        {
            if (!Directory.Exists(_mapDirectory))
            {
                Directory.CreateDirectory(_mapDirectory);
                StatusText.Text = $"Created maps directory: {_mapDirectory}";
                return;
            }

            var files = Directory.GetFiles(_mapDirectory, "*.xml");
            foreach (var file in files)
            {
                try
                {
                    var mapInfo = LoadMapHeader(file);
                    if (mapInfo != null)
                    {
                        _availableMaps.Add(mapInfo);
                    }
                }
                catch
                {
                    // Skip invalid map files
                }
            }

            // Sort by display name (which includes zone ID)
            _availableMaps.Sort((a, b) => 
                string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));

            MapSelector.ItemsSource = _availableMaps;

            if (_availableMaps.Count > 0)
            {
                StatusText.Text = $"Found {_availableMaps.Count} maps";
            }
            else
            {
                StatusText.Text = "No maps found. Use Automapper → Update Maps to download.";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error loading maps: {ex.Message}";
        }
    }

    private MapInfo? LoadMapHeader(string filePath)
    {
        var xdoc = new XmlDocument();
        xdoc.Load(filePath);

        var xZone = xdoc.SelectSingleNode("zone");
        if (xZone == null) return null;

        var id = GetAttribute(xZone, "id", "0");
        var name = GetAttribute(xZone, "name", "Unknown");

        if (string.IsNullOrEmpty(name))
                    name = IOPath.GetFileNameWithoutExtension(filePath);

        return new MapInfo
        {
            FilePath = filePath,
            ZoneId = id,
            ZoneName = name,
            DisplayName = $"{id}. {name}"
        };
    }

    private void OnMapSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (MapSelector.SelectedItem is MapInfo mapInfo)
        {
            LoadMap(mapInfo.FilePath);
        }
    }

    private void LoadMap(string filePath)
    {
        try
        {
            var xdoc = new XmlDocument();
            xdoc.Load(filePath);

            var mapData = new MapData();

            var xZone = xdoc.SelectSingleNode("zone");
            if (xZone != null)
            {
                mapData.ZoneId = GetAttribute(xZone, "id", "0");
                mapData.ZoneName = GetAttribute(xZone, "name", "Unknown");
            }

            // Load nodes
            var xNodes = xdoc.SelectNodes("zone/node");
            if (xNodes != null)
            {
                foreach (XmlNode xn in xNodes)
                {
                    var node = new MapNode
                    {
                        Id = int.Parse(GetAttribute(xn, "id", "0")),
                        Name = GetAttribute(xn, "name", ""),
                        Note = GetAttribute(xn, "note", "")
                    };

                    // Check if this is a link to another map
                    node.IsMapLink = node.Note.Contains(".xml");

                    // Parse color
                    var colorStr = GetAttribute(xn, "color", "");
                    if (!string.IsNullOrEmpty(colorStr))
                    {
                        node.NodeColor = ParseColor(colorStr);
                    }

                    // Parse position
                    var xPos = xn.SelectSingleNode("position");
                    if (xPos != null)
                    {
                        node.X = int.Parse(GetAttribute(xPos, "x", "0"));
                        node.Y = int.Parse(GetAttribute(xPos, "y", "0"));
                        node.Z = int.Parse(GetAttribute(xPos, "z", "0"));
                    }

                    // Parse descriptions
                    var xDescs = xn.SelectNodes("description");
                    if (xDescs != null)
                    {
                        foreach (XmlNode xDesc in xDescs)
                        {
                            if (!string.IsNullOrEmpty(xDesc.InnerText))
                            {
                                node.Descriptions.Add(xDesc.InnerText);
                            }
                        }
                    }

                    // Parse arcs (exits)
                    var xArcs = xn.SelectNodes("arc");
                    if (xArcs != null)
                    {
                        foreach (XmlNode xArc in xArcs)
                        {
                            var arc = new MapArc
                            {
                                DestinationId = int.Parse(GetAttribute(xArc, "destination", "0")),
                                Exit = GetAttribute(xArc, "exit", ""),
                                Move = GetAttribute(xArc, "move", ""),
                                Hidden = GetAttribute(xArc, "hidden", "").ToLower() == "true"
                            };

                            // Parse direction from exit name
                            arc.Direction = ParseDirection(arc.Exit);

                            node.Arcs.Add(arc);
                        }
                    }

                    mapData.Nodes[node.Id] = node;
                }
            }

            // Load labels
            var xLabels = xdoc.SelectNodes("zone/label");
            if (xLabels != null)
            {
                foreach (XmlNode xl in xLabels)
                {
                    var label = new MapLabel
                    {
                        Text = GetAttribute(xl, "text", "")
                    };

                    var xPos = xl.SelectSingleNode("position");
                    if (xPos != null)
                    {
                        label.X = int.Parse(GetAttribute(xPos, "x", "0"));
                        label.Y = int.Parse(GetAttribute(xPos, "y", "0"));
                        label.Z = int.Parse(GetAttribute(xPos, "z", "0"));
                    }

                    mapData.Labels.Add(label);
                }
            }

            _currentMap = mapData;
            _currentLevel = 0;
            ResetView();
            RedrawMap();

            // Update UI
            Title = $"AutoMapper - [{mapData.ZoneName}]";
            ZoneIdText.Text = $"Zone: {mapData.ZoneId}";
            RoomCountText.Text = $"Rooms: {mapData.Nodes.Count}";
            StatusText.Text = $"Loaded: {mapData.ZoneName}";
            LevelText.Text = _currentLevel.ToString();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error loading map: {ex.Message}";
        }
    }

    private void RedrawMap()
    {
        MapCanvas.Children.Clear();

        if (_currentMap == null) return;

        // Calculate bounds
        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        foreach (var node in _currentMap.Nodes.Values)
        {
            if (node.X < minX) minX = node.X;
            if (node.Y < minY) minY = node.Y;
            if (node.X > maxX) maxX = node.X;
            if (node.Y > maxY) maxY = node.Y;
        }

        // Add padding
        var padding = 40;
        var offsetX = -minX + padding;
        var offsetY = -minY + padding;
        var canvasWidth = (maxX - minX + padding * 2) * _scale;
        var canvasHeight = (maxY - minY + padding * 2) * _scale;

        MapCanvas.Width = Math.Max(canvasWidth, 400);
        MapCanvas.Height = Math.Max(canvasHeight, 400);

        // Draw arcs (lines) first
        foreach (var node in _currentMap.Nodes.Values)
        {
            if (node.Z != _currentLevel && node.Z > _currentLevel) continue;

            foreach (var arc in node.Arcs)
            {
                if (arc.Hidden) continue;
                if (arc.DestinationId <= 0) continue;
                if (!_currentMap.Nodes.TryGetValue(arc.DestinationId, out var destNode)) continue;
                if (destNode.Z > _currentLevel) continue;

                var startX = (node.X + offsetX) * _scale;
                var startY = (node.Y + offsetY) * _scale;
                var endX = (destNode.X + offsetX) * _scale;
                var endY = (destNode.Y + offsetY) * _scale;

                var isCurrentLevel = node.Z == _currentLevel && destNode.Z == _currentLevel;
                var lineBrush = isCurrentLevel ? _lineColor : _lineOtherLevelColor;

                var line = new Line
                {
                    StartPoint = new Point(startX, startY),
                    EndPoint = new Point(endX, endY),
                    Stroke = lineBrush,
                    StrokeThickness = 1
                };
                MapCanvas.Children.Add(line);
            }
        }

        // Draw labels
        foreach (var label in _currentMap.Labels)
        {
            if (label.Z != _currentLevel) continue;

            var x = (label.X + offsetX) * _scale;
            var y = (label.Y + offsetY) * _scale;

            var textBlock = new TextBlock
            {
                Text = label.Text,
                Foreground = _labelColor,
                FontSize = 10 * _scale
            };
            Canvas.SetLeft(textBlock, x);
            Canvas.SetTop(textBlock, y);
            MapCanvas.Children.Add(textBlock);
        }

        // Draw nodes
        var nodeSize = 8 * _scale;
        foreach (var node in _currentMap.Nodes.Values)
        {
            if (node.Z != _currentLevel && node.Z > _currentLevel) continue;

            var x = (node.X + offsetX) * _scale - nodeSize / 2;
            var y = (node.Y + offsetY) * _scale - nodeSize / 2;

            var isCurrentLevel = node.Z == _currentLevel;
            IBrush fillBrush;
            
            if (node.NodeColor.HasValue)
            {
                fillBrush = new SolidColorBrush(node.NodeColor.Value);
            }
            else
            {
                fillBrush = isCurrentLevel ? _nodeColor : _nodeOtherLevelColor;
            }

            var borderBrush = node.IsMapLink ? _linkNodeBorderColor : _nodeBorderColor;
            var borderThickness = node.IsMapLink ? 2.0 : 1.0;

            var rect = new Rectangle
            {
                Width = nodeSize,
                Height = nodeSize,
                Fill = fillBrush,
                Stroke = borderBrush,
                StrokeThickness = borderThickness,
                Tag = node
            };
            rect.PointerEntered += OnNodePointerEntered;
            rect.PointerExited += OnNodePointerExited;
            rect.PointerPressed += OnNodePointerPressed;

            Canvas.SetLeft(rect, x);
            Canvas.SetTop(rect, y);
            MapCanvas.Children.Add(rect);

            // Draw current node marker
            if (node.Id == _currentNodeId && isCurrentLevel)
            {
                var innerSize = nodeSize - 4 * _scale;
                var innerRect = new Rectangle
                {
                    Width = innerSize,
                    Height = innerSize,
                    Fill = _currentNodeColor,
                    IsHitTestVisible = false
                };
                Canvas.SetLeft(innerRect, x + 2 * _scale);
                Canvas.SetTop(innerRect, y + 2 * _scale);
                MapCanvas.Children.Add(innerRect);
            }
        }
    }

    private void OnNodePointerEntered(object? sender, PointerEventArgs e)
    {
        if (sender is Rectangle rect && rect.Tag is MapNode node)
        {
            var noteInfo = string.IsNullOrEmpty(node.Note) ? "" : $" [{node.Note}]";
            HoverNodeText.Text = $"#{node.Id} {node.Name}{noteInfo}";
        }
    }

    private void OnNodePointerExited(object? sender, PointerEventArgs e)
    {
        HoverNodeText.Text = "";
    }

    private void OnNodePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Rectangle rect && rect.Tag is MapNode node)
        {
            // If it's a map link, load that map
            if (node.IsMapLink && !string.IsNullOrEmpty(node.Note))
            {
                var mapFile = node.Note.Split('|')
                    .FirstOrDefault(s => s.ToLower().EndsWith(".xml"));
                if (!string.IsNullOrEmpty(mapFile))
                {
                    var fullPath = mapFile.Contains(IOPath.DirectorySeparatorChar) 
                        ? mapFile 
                        : IOPath.Combine(_mapDirectory, mapFile);
                    
                    if (File.Exists(fullPath))
                    {
                        LoadMap(fullPath);
                        // Update selector to match
                        var mapInfo = _availableMaps.FirstOrDefault(m => 
                            m.FilePath.Equals(fullPath, StringComparison.OrdinalIgnoreCase));
                        if (mapInfo != null)
                        {
                            MapSelector.SelectedItem = mapInfo;
                        }
                    }
                }
            }
        }
    }

    private void OnCanvasPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var props = e.GetCurrentPoint(MapCanvas).Properties;
        if (props.IsRightButtonPressed || props.IsMiddleButtonPressed)
        {
            _isPanning = true;
            _lastPanPoint = e.GetPosition(MapCanvas);
            e.Handled = true;
        }
    }

    private void OnCanvasPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_isPanning && _lastPanPoint.HasValue)
        {
            var currentPoint = e.GetPosition(MapCanvas);
            var delta = currentPoint - _lastPanPoint.Value;
            
            // Scroll the view
            var scrollViewer = MapScrollViewer;
            scrollViewer.Offset = new Vector(
                scrollViewer.Offset.X - delta.X,
                scrollViewer.Offset.Y - delta.Y);
            
            _lastPanPoint = currentPoint;
        }
    }

    private void OnCanvasPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _isPanning = false;
        _lastPanPoint = null;
    }

    private void OnCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Zoom with mouse wheel
        if (e.Delta.Y > 0)
            ZoomIn();
        else if (e.Delta.Y < 0)
            ZoomOut();
        e.Handled = true;
    }

    private void OnZoomInClick(object? sender, RoutedEventArgs e) => ZoomIn();
    private void OnZoomOutClick(object? sender, RoutedEventArgs e) => ZoomOut();

    private void ZoomIn()
    {
        if (_scale < 3.0)
        {
            _scale += 0.25;
            UpdateZoom();
        }
    }

    private void ZoomOut()
    {
        if (_scale > 0.25)
        {
            _scale -= 0.25;
            UpdateZoom();
        }
    }

    private void UpdateZoom()
    {
        ZoomText.Text = $"{(int)(_scale * 100)}%";
        RedrawMap();
    }

    private void OnResetViewClick(object? sender, RoutedEventArgs e) => ResetView();

    private void ResetView()
    {
        _scale = 1.0;
        ZoomText.Text = "100%";
        MapScrollViewer.Offset = new Vector(0, 0);
        RedrawMap();
    }

    private void OnLevelUpClick(object? sender, RoutedEventArgs e)
    {
        _currentLevel++;
        LevelText.Text = _currentLevel.ToString();
        RedrawMap();
    }

    private void OnLevelDownClick(object? sender, RoutedEventArgs e)
    {
        _currentLevel--;
        LevelText.Text = _currentLevel.ToString();
        RedrawMap();
    }

    private void OnReloadMapsClick(object? sender, RoutedEventArgs e)
    {
        LoadAvailableMaps();
    }

    private static string GetAttribute(XmlNode node, string name, string defaultValue = "")
    {
        return node.Attributes?.GetNamedItem(name)?.Value ?? defaultValue;
    }

    private static MapDirection ParseDirection(string exit)
    {
        return exit.ToLower() switch
        {
            "n" or "north" => MapDirection.North,
            "ne" or "northeast" => MapDirection.NorthEast,
            "e" or "east" => MapDirection.East,
            "se" or "southeast" => MapDirection.SouthEast,
            "s" or "south" => MapDirection.South,
            "sw" or "southwest" => MapDirection.SouthWest,
            "w" or "west" => MapDirection.West,
            "nw" or "northwest" => MapDirection.NorthWest,
            "u" or "up" => MapDirection.Up,
            "d" or "down" => MapDirection.Down,
            "out" or "o" => MapDirection.Out,
            _ when exit.StartsWith("go ") => MapDirection.Go,
            _ when exit.StartsWith("climb ") => MapDirection.Climb,
            _ => MapDirection.None
        };
    }

    private static Color? ParseColor(string colorStr)
    {
        try
        {
            // Handle named colors and hex colors
            if (colorStr.StartsWith("#"))
            {
                return Color.Parse(colorStr);
            }
            
            // Try to parse as a named color
            if (Color.TryParse(colorStr, out var color))
            {
                return color;
            }

            // Handle "Color [name]" format from .NET
            if (colorStr.StartsWith("Color ["))
            {
                var name = colorStr.Replace("Color [", "").Replace("]", "");
                if (Color.TryParse(name, out var namedColor))
                {
                    return namedColor;
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Sets the current node (player's location) to highlight on the map.
    /// </summary>
    public void SetCurrentNode(int? nodeId)
    {
        _currentNodeId = nodeId;
        RedrawMap();
    }

    /// <summary>
    /// Loads a map by zone ID or file name.
    /// </summary>
    public void LoadMapByZone(string zoneIdOrName)
    {
        var mapInfo = _availableMaps.FirstOrDefault(m =>
            m.ZoneId == zoneIdOrName ||
            m.ZoneName.Equals(zoneIdOrName, StringComparison.OrdinalIgnoreCase) ||
            IOPath.GetFileNameWithoutExtension(m.FilePath).Equals(zoneIdOrName, StringComparison.OrdinalIgnoreCase));

        if (mapInfo != null)
        {
            MapSelector.SelectedItem = mapInfo;
            LoadMap(mapInfo.FilePath);
        }
    }
}

// Data classes for map structure
public class MapInfo
{
    public string FilePath { get; set; } = "";
    public string ZoneId { get; set; } = "";
    public string ZoneName { get; set; } = "";
    public string DisplayName { get; set; } = "";
}

public class MapData
{
    public string ZoneId { get; set; } = "";
    public string ZoneName { get; set; } = "";
    public Dictionary<int, MapNode> Nodes { get; } = new();
    public List<MapLabel> Labels { get; } = new();
}

public class MapNode
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Note { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
    public bool IsMapLink { get; set; }
    public Color? NodeColor { get; set; }
    public List<string> Descriptions { get; } = new();
    public List<MapArc> Arcs { get; } = new();
}

public class MapArc
{
    public int DestinationId { get; set; }
    public string Exit { get; set; } = "";
    public string Move { get; set; } = "";
    public MapDirection Direction { get; set; }
    public bool Hidden { get; set; }
}

public class MapLabel
{
    public string Text { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Z { get; set; }
}

public enum MapDirection
{
    None,
    North,
    NorthEast,
    East,
    SouthEast,
    South,
    SouthWest,
    West,
    NorthWest,
    Up,
    Down,
    Out,
    Go,
    Climb
}

