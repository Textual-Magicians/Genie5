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
using System.Xml;

using GenieClient.Genie;
using GenieClient.UI.Services;

namespace GenieClient.Views;

/// <summary>
/// Enum representing the display mode of the AutoMapper.
/// </summary>
public enum AutoMapperMode
{
    Integrated,  // Embedded in the main window
    Popup        // Separate popup window
}

/// <summary>
/// Event args for mode toggle requests.
/// </summary>
public class ModeToggleRequestedEventArgs : EventArgs
{
    public AutoMapperMode RequestedMode { get; }
    
    public ModeToggleRequestedEventArgs(AutoMapperMode requestedMode)
    {
        RequestedMode = requestedMode;
    }
}

/// <summary>
/// UserControl containing the AutoMapper functionality.
/// Can be hosted in either the main window (integrated) or a popup window.
/// </summary>
public partial class AutoMapperPanel : UserControl
{
    private readonly List<MapInfo> _availableMaps = new();
    private MapData? _currentMap;
    private double _scale = 1.0;
    private int _currentLevel = 0;
    private Point? _lastPanPoint;
    private bool _isPanning;
    private string _mapDirectory = "";
    private int? _currentNodeId;
    private GameManager? _gameManager;
    private Globals? _globals;
    private AutoMapperMode _currentMode = AutoMapperMode.Popup;
    private HashSet<int> _highlightedPath = new();

    // Event fired when user requests to toggle between integrated/popup mode
    public event EventHandler<ModeToggleRequestedEventArgs>? ModeToggleRequested;

    // Colors matching the Windows Forms version
    private readonly IBrush _nodeColor = new SolidColorBrush(Color.FromRgb(255, 255, 192));
    private readonly IBrush _nodeBorderColor = new SolidColorBrush(Color.FromRgb(100, 100, 100));
    private readonly IBrush _nodeOtherLevelColor = new SolidColorBrush(Color.FromRgb(180, 180, 180));
    private readonly IBrush _lineColor = new SolidColorBrush(Color.FromRgb(100, 100, 100));
    private readonly IBrush _lineOtherLevelColor = new SolidColorBrush(Color.FromRgb(60, 60, 60));
    private readonly IBrush _currentNodeColor = new SolidColorBrush(Color.FromRgb(255, 0, 255)); // Magenta
    private readonly IBrush _labelColor = new SolidColorBrush(Color.FromRgb(200, 200, 200));
    private readonly IBrush _linkNodeBorderColor = new SolidColorBrush(Color.FromRgb(0, 0, 255));
    private readonly IBrush _pathNodeColor = new SolidColorBrush(Color.FromRgb(0, 200, 100)); // Green for path
    private readonly IBrush _destinationNodeColor = new SolidColorBrush(Color.FromRgb(255, 100, 100)); // Red for destination

    public AutoMapperPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Gets or sets the current display mode.
    /// </summary>
    public AutoMapperMode CurrentMode
    {
        get => _currentMode;
        set
        {
            _currentMode = value;
            UpdateModeUI();
        }
    }

    /// <summary>
    /// Initializes the panel with the required dependencies.
    /// </summary>
    public void Initialize(string mapDirectory, GameManager? gameManager)
    {
        _mapDirectory = mapDirectory;
        _gameManager = gameManager;
        LoadAvailableMaps();
        SubscribeToGameEvents();
        SyncWithCurrentLocation();
    }

    /// <summary>
    /// Gets the current Globals instance from the GameManager.
    /// </summary>
    private Genie.Globals? GetGlobals()
    {
        return _gameManager?.Globals ?? _globals;
    }

    private void UpdateModeUI()
    {
        if (_currentMode == AutoMapperMode.Integrated)
        {
            ModeIcon.Text = "⧉";
            ModeText.Text = "Popup";
            ToolTip.SetTip(ModeToggleButton, "Switch to popup window mode");
        }
        else
        {
            ModeIcon.Text = "⬚";
            ModeText.Text = "Dock";
            ToolTip.SetTip(ModeToggleButton, "Switch to integrated/docked mode");
        }
    }

    private void OnModeToggleClick(object? sender, RoutedEventArgs e)
    {
        var requestedMode = _currentMode == AutoMapperMode.Integrated 
            ? AutoMapperMode.Popup 
            : AutoMapperMode.Integrated;
        
        ModeToggleRequested?.Invoke(this, new ModeToggleRequestedEventArgs(requestedMode));
    }

    public void SubscribeToGameEvents()
    {
        if (_gameManager != null)
        {
            _gameManager.VariableChanged += OnVariableChanged;
        }
    }

    public void UnsubscribeFromGameEvents()
    {
        if (_gameManager != null)
        {
            _gameManager.VariableChanged -= OnVariableChanged;
        }
    }

    private void OnVariableChanged(string variable, string value)
    {
        // Handle zone/room changes from the game
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (variable == "zoneid" || variable == "zonename")
            {
                TryLoadMapForCurrentZone();
            }
            else if (variable == "roomid")
            {
                if (int.TryParse(value, out int roomId) && roomId > 0)
                {
                    SetCurrentNode(roomId);
                    CenterOnCurrentNode();
                }
                else
                {
                    SetCurrentNode(null);
                }
            }
            else if (variable == "prompt")
            {
                TryLocateCurrentRoom();
            }
        });
    }

    /// <summary>
    /// Tries to find the current room on the loaded map.
    /// </summary>
    private void TryLocateCurrentRoom()
    {
        if (_currentMap == null)
        {
            StatusText.Text = "No map loaded";
            return;
        }
        
        var globals = GetGlobals();
        if (globals?.VariableList == null)
        {
            StatusText.Text = "Not connected";
            return;
        }

        var roomName = GetVariable("roomname");
        var roomDesc = GetVariable("roomdesc");

        if (string.IsNullOrEmpty(roomName))
        {
            return;
        }

        // Get available exits for matching
        var exits = new HashSet<string>();
        if (GetVariable("north") == "1") exits.Add("north");
        if (GetVariable("northeast") == "1") exits.Add("northeast");
        if (GetVariable("east") == "1") exits.Add("east");
        if (GetVariable("southeast") == "1") exits.Add("southeast");
        if (GetVariable("south") == "1") exits.Add("south");
        if (GetVariable("southwest") == "1") exits.Add("southwest");
        if (GetVariable("west") == "1") exits.Add("west");
        if (GetVariable("northwest") == "1") exits.Add("northwest");
        if (GetVariable("up") == "1") exits.Add("up");
        if (GetVariable("down") == "1") exits.Add("down");
        if (GetVariable("out") == "1") exits.Add("out");

        // Find matching nodes
        var candidates = new List<MapNode>();
        foreach (var node in _currentMap.Nodes.Values)
        {
            if (node.IsMapLink) continue;

            if (!string.Equals(node.Name, roomName, StringComparison.OrdinalIgnoreCase))
                continue;

            bool descMatch = true;
            if (node.Descriptions.Count > 0 && !string.IsNullOrEmpty(roomDesc))
            {
                descMatch = node.Descriptions.Any(d => 
                    string.Equals(d.Trim(), roomDesc.Trim(), StringComparison.OrdinalIgnoreCase));
            }

            if (!descMatch) continue;

            bool exitsMatch = true;
            var nodeCardinalExits = node.Arcs
                .Where(a => IsCardinalDirection(a.Direction))
                .Select(a => a.Exit.ToLower())
                .ToHashSet();

            if (exits.Count > 0 && nodeCardinalExits.Count > 0)
            {
                var matchingExits = exits.Count(e => nodeCardinalExits.Contains(e.ToLower()));
                exitsMatch = matchingExits >= Math.Min(exits.Count, nodeCardinalExits.Count) / 2;
            }

            if (exitsMatch)
            {
                candidates.Add(node);
            }
        }

        if (candidates.Count == 1)
        {
            var foundNode = candidates[0];
            SetCurrentNode(foundNode.Id);
            CenterOnCurrentNode();
            
            if (globals?.VariableList != null)
            {
                globals.VariableList["roomid"] = foundNode.Id.ToString();
            }
            
            StatusText.Text = $"#{foundNode.Id} {foundNode.Name}";
        }
        else if (candidates.Count > 1)
        {
            if (_currentNodeId.HasValue)
            {
                var connectedCandidate = candidates.FirstOrDefault(c =>
                    _currentMap.Nodes.Values.Any(n => 
                        n.Id == _currentNodeId.Value && 
                        n.Arcs.Any(a => a.DestinationId == c.Id)));

                if (connectedCandidate != null)
                {
                    SetCurrentNode(connectedCandidate.Id);
                    CenterOnCurrentNode();
                    if (globals?.VariableList != null)
                    {
                        globals.VariableList["roomid"] = connectedCandidate.Id.ToString();
                    }
                    StatusText.Text = $"#{connectedCandidate.Id} {connectedCandidate.Name}";
                    return;
                }
            }

            var firstMatch = candidates[0];
            SetCurrentNode(firstMatch.Id);
            CenterOnCurrentNode();
            StatusText.Text = $"Multiple matches ({candidates.Count})";
        }
        else
        {
            if (!TryLoadMapForRoom(roomName, roomDesc))
            {
                StatusText.Text = $"Not found: {roomName}";
            }
        }
    }

    private bool TryLoadMapForRoom(string roomName, string roomDesc)
    {
        foreach (var mapInfo in _availableMaps)
        {
            if (_currentMap != null && mapInfo.FilePath == 
                _availableMaps.FirstOrDefault(m => m.ZoneId == _currentMap.ZoneId)?.FilePath)
                continue;

            try
            {
                var content = File.ReadAllText(mapInfo.FilePath);
                if (content.Contains($"name=\"{roomName}\"", StringComparison.OrdinalIgnoreCase))
                {
                    LoadMap(mapInfo.FilePath);
                    MapSelector.SelectedItem = mapInfo;
                    TryLocateCurrentRoom();
                    return true;
                }
            }
            catch
            {
                // Skip maps that can't be read
            }
        }

        return false;
    }

    private string GetVariable(string name)
    {
        var globals = GetGlobals();
        if (globals?.VariableList?.ContainsKey(name) == true)
        {
            return globals.VariableList[name]?.ToString() ?? "";
        }
        return "";
    }

    private static bool IsCardinalDirection(MapDirection dir)
    {
        return dir switch
        {
            MapDirection.North or MapDirection.NorthEast or MapDirection.East or
            MapDirection.SouthEast or MapDirection.South or MapDirection.SouthWest or
            MapDirection.West or MapDirection.NorthWest => true,
            _ => false
        };
    }

    private void SyncWithCurrentLocation()
    {
        var globals = GetGlobals();
        if (globals?.VariableList == null) return;

        TryLoadMapForCurrentZone();

        if (globals.VariableList.ContainsKey("roomid"))
        {
            var roomIdStr = globals.VariableList["roomid"]?.ToString() ?? "0";
            if (int.TryParse(roomIdStr, out int roomId) && roomId > 0)
            {
                SetCurrentNode(roomId);
                CenterOnCurrentNode();
                return;
            }
        }

        if (_currentMap != null)
        {
            TryLocateCurrentRoom();
        }
        else
        {
            var roomName = GetVariable("roomname");
            var roomDesc = GetVariable("roomdesc");
            if (!string.IsNullOrEmpty(roomName))
            {
                TryLoadMapForRoom(roomName, roomDesc);
            }
        }
    }

    private void TryLoadMapForCurrentZone()
    {
        var globals = GetGlobals();
        if (globals?.VariableList == null) return;

        var zoneId = globals.VariableList.ContainsKey("zoneid") 
            ? globals.VariableList["zoneid"]?.ToString() ?? "" 
            : "";
        var zoneName = globals.VariableList.ContainsKey("zonename") 
            ? globals.VariableList["zonename"]?.ToString() ?? "" 
            : "";

        if (string.IsNullOrEmpty(zoneId) && string.IsNullOrEmpty(zoneName))
            return;

        if (_currentMap != null)
        {
            if (_currentMap.ZoneId == zoneId || 
                _currentMap.ZoneName.Equals(zoneName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        var mapInfo = _availableMaps.FirstOrDefault(m =>
            m.ZoneId == zoneId ||
            (!string.IsNullOrEmpty(zoneName) && m.ZoneName.Equals(zoneName, StringComparison.OrdinalIgnoreCase)));

        if (mapInfo != null)
        {
            MapSelector.SelectedItem = mapInfo;
            LoadMap(mapInfo.FilePath);
            StatusText.Text = $"Loaded: {mapInfo.ZoneName}";
        }
    }

    private void CenterOnCurrentNode()
    {
        if (_currentMap == null || _currentNodeId == null) return;
        if (!_currentMap.Nodes.TryGetValue(_currentNodeId.Value, out var node)) return;

        if (node.Z != _currentLevel)
        {
            _currentLevel = node.Z;
            LevelText.Text = _currentLevel.ToString();
            RedrawMap();
        }

        var minX = _currentMap.Nodes.Values.Min(n => n.X);
        var minY = _currentMap.Nodes.Values.Min(n => n.Y);
        var padding = 40;
        var offsetX = -minX + padding;
        var offsetY = -minY + padding;

        var nodeScreenX = (node.X + offsetX) * _scale;
        var nodeScreenY = (node.Y + offsetY) * _scale;

        var viewportWidth = MapScrollViewer.Viewport.Width;
        var viewportHeight = MapScrollViewer.Viewport.Height;

        var scrollX = Math.Max(0, nodeScreenX - viewportWidth / 2);
        var scrollY = Math.Max(0, nodeScreenY - viewportHeight / 2);

        MapScrollViewer.Offset = new Vector(scrollX, scrollY);
    }

    private void LoadAvailableMaps()
    {
        _availableMaps.Clear();

        try
        {
            if (!Directory.Exists(_mapDirectory))
            {
                Directory.CreateDirectory(_mapDirectory);
                StatusText.Text = "No maps found";
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

            _availableMaps.Sort((a, b) => 
                string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));

            MapSelector.ItemsSource = _availableMaps;

            if (_availableMaps.Count > 0)
            {
                StatusText.Text = $"{_availableMaps.Count} maps";
            }
            else
            {
                StatusText.Text = "No maps found";
            }
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
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

                    node.IsMapLink = node.Note.Contains(".xml");

                    var colorStr = GetAttribute(xn, "color", "");
                    if (!string.IsNullOrEmpty(colorStr))
                    {
                        node.NodeColor = ParseColor(colorStr);
                    }

                    var xPos = xn.SelectSingleNode("position");
                    if (xPos != null)
                    {
                        node.X = int.Parse(GetAttribute(xPos, "x", "0"));
                        node.Y = int.Parse(GetAttribute(xPos, "y", "0"));
                        node.Z = int.Parse(GetAttribute(xPos, "z", "0"));
                    }

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

                            arc.Direction = ParseDirection(arc.Exit);
                            node.Arcs.Add(arc);
                        }
                    }

                    mapData.Nodes[node.Id] = node;
                }
            }

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

            StatusText.Text = $"{mapData.ZoneName} ({mapData.Nodes.Count} rooms)";
            LevelText.Text = _currentLevel.ToString();
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
    }

    private void RedrawMap()
    {
        MapCanvas.Children.Clear();

        if (_currentMap == null) return;

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
            var isOnPath = _highlightedPath.Contains(node.Id);
            var isDestination = isOnPath && _highlightedPath.Count > 0 && node.Id == _highlightedPath.Last();
            
            IBrush fillBrush;
            
            if (isDestination)
            {
                fillBrush = _destinationNodeColor;
            }
            else if (isOnPath)
            {
                fillBrush = _pathNodeColor;
            }
            else if (node.NodeColor.HasValue)
            {
                fillBrush = new SolidColorBrush(node.NodeColor.Value);
            }
            else
            {
                fillBrush = isCurrentLevel ? _nodeColor : _nodeOtherLevelColor;
            }

            var borderBrush = node.IsMapLink ? _linkNodeBorderColor : _nodeBorderColor;
            var borderThickness = isOnPath ? 2.0 : (node.IsMapLink ? 2.0 : 1.0);

            var rect = new Rectangle
            {
                Width = nodeSize,
                Height = nodeSize,
                Fill = fillBrush,
                Stroke = borderBrush,
                StrokeThickness = borderThickness,
                Tag = node,
                Focusable = false
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
                    IsHitTestVisible = false,
                    Focusable = false
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
            var props = e.GetCurrentPoint(rect).Properties;
            
            if (props.IsRightButtonPressed)
            {
                NavigateToNode(node);
                e.Handled = true;
                return;
            }
            
            if (props.IsLeftButtonPressed && node.IsMapLink && !string.IsNullOrEmpty(node.Note))
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

    /// <summary>
    /// Handles a goto command (e.g., "#goto 123" or "#goto bank").
    /// </summary>
    public bool HandleGotoCommand(string argument)
    {
        if (string.IsNullOrWhiteSpace(argument))
        {
            EchoText("[Mapper] Goto - please specify a room id to travel to.");
            SetDestination("0");
            return false;
        }

        if (_currentMap == null)
        {
            EchoText("[Mapper] No map loaded - select a map first.");
            SetDestination("0");
            return false;
        }

        int nodeId = 0;

        if (argument.Length <= 5 || !int.TryParse(argument, out nodeId))
        {
            int.TryParse(argument, out nodeId);
        }

        if (nodeId == 0)
        {
            foreach (var node in _currentMap.Nodes.Values)
            {
                if (string.IsNullOrEmpty(node.Note)) continue;

                foreach (var note in node.Note.Split('|'))
                {
                    if (note.StartsWith(argument, StringComparison.OrdinalIgnoreCase))
                    {
                        EchoText($"[Mapper] Goto: {note}");
                        nodeId = node.Id;
                        break;
                    }
                }
                if (nodeId > 0) break;
            }
        }

        if (nodeId > 0)
        {
            if (_currentMap.Nodes.TryGetValue(nodeId, out var destinationNode))
            {
                EchoText($"#goto {argument}");
                SetDestination(nodeId.ToString());
                ParseMapperText("DESTINATION FOUND");
                NavigateToNode(destinationNode);
                return true;
            }
            else
            {
                EchoText($"[Mapper] Destination ID #{nodeId} not found.");
                SetDestination("0");
                ParseMapperText("DESTINATION NOT FOUND");
                return false;
            }
        }
        else
        {
            EchoText($"[Mapper] Destination ID \"{argument}\" not found.");
            SetDestination("0");
            ParseMapperText("DESTINATION NOT FOUND");
            return false;
        }
    }

    private void EchoText(string message)
    {
        _gameManager?.EchoMapperText(message);
        StatusText.Text = message;
    }

    private void SetDestination(string nodeId)
    {
        var globals = GetGlobals();
        if (globals?.VariableList != null)
        {
            globals.VariableList["destination"] = nodeId;
        }
    }

    private void ParseMapperText(string text)
    {
        Console.WriteLine($"[Mapper] {text}");
    }

    private void NavigateToNode(MapNode destinationNode)
    {
        if (_currentMap == null || _gameManager == null)
        {
            StatusText.Text = "Cannot navigate";
            return;
        }

        if (_currentNodeId == null)
        {
            StatusText.Text = "Location unknown - click Find Me";
            return;
        }

        if (_currentNodeId == destinationNode.Id)
        {
            StatusText.Text = "Already at destination!";
            return;
        }

        if (!_currentMap.Nodes.TryGetValue(_currentNodeId.Value, out var startNode))
        {
            StatusText.Text = "Current node not found";
            return;
        }

        var path = FindShortestPath(startNode, destinationNode);
        
        if (path == null || path.Count == 0)
        {
            StatusText.Text = $"No path to #{destinationNode.Id}";
            return;
        }

        var pathCommands = new List<string>();
        for (int i = 0; i < path.Count - 1; i++)
        {
            var currentNode = path[i];
            var nextNode = path[i + 1];
            
            var arc = currentNode.Arcs.FirstOrDefault(a => a.DestinationId == nextNode.Id);
            if (arc != null)
            {
                var move = !string.IsNullOrEmpty(arc.Move) ? arc.Move : DirectionToCommand(arc.Direction);
                pathCommands.Add(move);
            }
        }

        if (pathCommands.Count == 0)
        {
            StatusText.Text = "Could not build path";
            return;
        }

        var pathString = string.Join(" ", pathCommands.Select(c => c.Contains(" ") ? $"\"{c}\"" : c));
        _gameManager.RunScript($"automapper {pathString}");
        
        StatusText.Text = $"→ #{destinationNode.Id} ({pathCommands.Count} moves)";
        HighlightPath(path);
    }

    private List<MapNode>? FindShortestPath(MapNode start, MapNode end)
    {
        if (_currentMap == null) return null;

        var visited = new HashSet<int>();
        var queue = new Queue<List<MapNode>>();
        
        queue.Enqueue(new List<MapNode> { start });
        visited.Add(start.Id);

        while (queue.Count > 0)
        {
            var path = queue.Dequeue();
            var current = path[^1];

            if (current.Id == end.Id)
            {
                return path;
            }

            foreach (var arc in current.Arcs)
            {
                if (arc.DestinationId <= 0) continue;
                if (visited.Contains(arc.DestinationId)) continue;
                if (!_currentMap.Nodes.TryGetValue(arc.DestinationId, out var nextNode)) continue;

                visited.Add(arc.DestinationId);
                var newPath = new List<MapNode>(path) { nextNode };
                queue.Enqueue(newPath);
            }
        }

        return null;
    }

    private void HighlightPath(List<MapNode> path)
    {
        _highlightedPath = path.Select(n => n.Id).ToHashSet();
        RedrawMap();
    }

    private static string DirectionToCommand(MapDirection direction)
    {
        return direction switch
        {
            MapDirection.North => "n",
            MapDirection.NorthEast => "ne",
            MapDirection.East => "e",
            MapDirection.SouthEast => "se",
            MapDirection.South => "s",
            MapDirection.SouthWest => "sw",
            MapDirection.West => "w",
            MapDirection.NorthWest => "nw",
            MapDirection.Up => "up",
            MapDirection.Down => "down",
            MapDirection.Out => "out",
            _ => ""
        };
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
            
            MapScrollViewer.Offset = new Vector(
                MapScrollViewer.Offset.X - delta.X,
                MapScrollViewer.Offset.Y - delta.Y);
            
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

    private void OnFindMeClick(object? sender, RoutedEventArgs e)
    {
        _highlightedPath.Clear();
        
        var globals = GetGlobals();
        if (globals?.VariableList == null)
        {
            StatusText.Text = "Not connected";
            return;
        }
        
        var roomName = GetVariable("roomname");
        var roomDesc = GetVariable("roomdesc");
        
        if (string.IsNullOrEmpty(roomName))
        {
            StatusText.Text = "No room name";
            return;
        }
        
        if (_currentMap == null)
        {
            StatusText.Text = $"Searching...";
            if (!TryLoadMapForRoom(roomName, roomDesc))
            {
                StatusText.Text = $"Not found: {roomName}";
            }
            return;
        }
        
        TryLocateCurrentRoom();
        
        if (_currentNodeId == null)
        {
            if (!TryLoadMapForRoom(roomName, roomDesc))
            {
                StatusText.Text = $"Not found: {roomName}";
            }
        }
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
            if (colorStr.StartsWith("#"))
            {
                return Color.Parse(colorStr);
            }
            
            if (Color.TryParse(colorStr, out var color))
            {
                return color;
            }

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
        
        if (_highlightedPath.Count > 0 && nodeId.HasValue)
        {
            if (nodeId.Value == _highlightedPath.Last())
            {
                _highlightedPath.Clear();
            }
            else if (!_highlightedPath.Contains(nodeId.Value))
            {
                _highlightedPath.Clear();
            }
        }
        
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

