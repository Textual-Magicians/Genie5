using Avalonia.Controls;
using System;
using System.IO;
using System.Threading.Tasks;

using GenieClient.UI.Services;

namespace GenieClient.Views;

/// <summary>
/// Popup window that hosts the AutoMapperPanel.
/// This window is used when the mapper is in popup mode.
/// </summary>
public partial class AutoMapperWindow : Window
{
    private string _mapDirectory = "";
    private GameManager? _gameManager;
    private MainWindow? _mainWindow;

    // Static reference to the active AutoMapper window for command routing
    private static AutoMapperWindow? _activeInstance;

    public AutoMapperWindow()
    {
        InitializeComponent();
        Closed += OnWindowClosed;
        
        // Subscribe to mode toggle requests from the panel
        MapperPanel.ModeToggleRequested += OnModeToggleRequested;
        MapperPanel.CurrentMode = AutoMapperMode.Popup;
    }

    /// <summary>
    /// Gets the currently active AutoMapper window, or null if not open.
    /// </summary>
    public static AutoMapperWindow? ActiveInstance => _activeInstance;

    /// <summary>
    /// Gets the AutoMapperPanel hosted by this window.
    /// </summary>
    public AutoMapperPanel Panel => MapperPanel;

    public static async Task<AutoMapperWindow> ShowWindow(Window owner, string mapDirectory, GameManager? gameManager = null)
    {
        // If we already have an active instance, just bring it to focus
        if (_activeInstance != null)
        {
            _activeInstance.Activate();
            return _activeInstance;
        }

        var window = new AutoMapperWindow();
        window._mapDirectory = mapDirectory;
        window._gameManager = gameManager;
        window._mainWindow = owner as MainWindow;
        
        // Initialize the panel
        window.MapperPanel.Initialize(mapDirectory, gameManager);
        
        _activeInstance = window;
        window.Show(owner);
        return window;
    }

    /// <summary>
    /// Creates a window but doesn't show it - used for mode switching.
    /// </summary>
    public static AutoMapperWindow CreateWindow(MainWindow mainWindow, string mapDirectory, GameManager? gameManager)
    {
        var window = new AutoMapperWindow();
        window._mapDirectory = mapDirectory;
        window._gameManager = gameManager;
        window._mainWindow = mainWindow;
        
        _activeInstance = window;
        return window;
    }

    /// <summary>
    /// Transfers the panel from the main window to this popup window.
    /// </summary>
    public void AdoptPanel(AutoMapperPanel panel)
    {
        // The panel will be added by the caller after removing from MainWindow
        // We need to update our reference
        // Note: In Avalonia, we can't easily move controls between parents,
        // so we'll recreate the state instead
        panel.CurrentMode = AutoMapperMode.Popup;
    }

    private void OnModeToggleRequested(object? sender, ModeToggleRequestedEventArgs e)
    {
        if (e.RequestedMode == AutoMapperMode.Integrated && _mainWindow != null)
        {
            // User wants to switch to integrated mode
            // Close this window and tell MainWindow to show its integrated mapper
            _activeInstance = null;
            
            // Unsubscribe panel events before closing
            MapperPanel.ModeToggleRequested -= OnModeToggleRequested;
            MapperPanel.UnsubscribeFromGameEvents();
            
            // Tell MainWindow to show integrated mapper
            _mainWindow.ShowIntegratedMapper(_mapDirectory, _gameManager);
            
            Close();
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        MapperPanel.UnsubscribeFromGameEvents();
        MapperPanel.ModeToggleRequested -= OnModeToggleRequested;
        
        // Clear the static instance reference
        if (_activeInstance == this)
        {
            _activeInstance = null;
        }
    }

    /// <summary>
    /// Handles a goto command by delegating to the panel.
    /// </summary>
    public bool HandleGotoCommand(string argument)
    {
        return MapperPanel.HandleGotoCommand(argument);
    }
}
