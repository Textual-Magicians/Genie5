using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using GenieClient.Genie;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GenieClient.Views;

public partial class PreferencesDialog : Window
{
    private readonly Config? _config;

    // Control references
    private TextBox _scriptCharTextBox = null!;
    private TextBox _commandCharTextBox = null!;
    private TextBox _separatorCharTextBox = null!;
    private NumericUpDown _scriptTimeoutNumeric = null!;
    
    private CheckBox _triggerOnInputCheckBox = null!;
    private CheckBox _autoLogCheckBox = null!;
    private CheckBox _reconnectCheckBox = null!;
    private CheckBox _keepInputCheckBox = null!;
    private CheckBox _abortDupeScriptCheckBox = null!;
    private CheckBox _playSoundsCheckBox = null!;
    private CheckBox _showSpellTimerCheckBox = null!;
    private CheckBox _showLinksCheckBox = null!;
    private CheckBox _showImagesCheckBox = null!;
    
    private TextBox _promptTextBox = null!;
    private CheckBox _promptBreakCheckBox = null!;
    private CheckBox _condensedCheckBox = null!;
    
    private TextBox _scriptDirTextBox = null!;
    private TextBox _mapDirTextBox = null!;
    private TextBox _logDirTextBox = null!;
    private TextBox _soundDirTextBox = null!;
    private TextBox _pluginDirTextBox = null!;
    private TextBox _configDirTextBox = null!;
    private TextBox _artDirTextBox = null!;
    
    private NumericUpDown _serverTimeoutNumeric = null!;
    private TextBox _serverTimeoutCommandTextBox = null!;
    private NumericUpDown _userTimeoutNumeric = null!;
    private TextBox _userTimeoutCommandTextBox = null!;
    private NumericUpDown _roundtimeOffsetNumeric = null!;
    
    private TextBox _editorTextBox = null!;
    private TextBox _rubyPathTextBox = null!;
    private TextBox _lichPathTextBox = null!;
    private TextBox _lichArgumentsTextBox = null!;
    private TextBox _lichServerTextBox = null!;
    private NumericUpDown _lichPortNumeric = null!;
    private NumericUpDown _lichStartPauseNumeric = null!;
    
    private CheckBox _ignoreScriptWarningsCheckBox = null!;
    private CheckBox _ignoreCloseAlertCheckBox = null!;
    private CheckBox _parseGameOnlyCheckBox = null!;
    private CheckBox _webLinkSafetyCheckBox = null!;
    private CheckBox _autoMapperCheckBox = null!;
    private CheckBox _checkForUpdatesCheckBox = null!;
    private NumericUpDown _maxGoSubDepthNumeric = null!;
    private NumericUpDown _bufferLineSizeNumeric = null!;

    public PreferencesDialog()
    {
        InitializeComponent();
        FindControls();
    }

    public PreferencesDialog(Config? config) : this()
    {
        _config = config;
        LoadSettings();
    }

    private void FindControls()
    {
        // General tab - Script settings
        _scriptCharTextBox = this.FindControl<TextBox>("ScriptCharTextBox")!;
        _commandCharTextBox = this.FindControl<TextBox>("CommandCharTextBox")!;
        _separatorCharTextBox = this.FindControl<TextBox>("SeparatorCharTextBox")!;
        _scriptTimeoutNumeric = this.FindControl<NumericUpDown>("ScriptTimeoutNumeric")!;
        
        // General tab - Behavior
        _triggerOnInputCheckBox = this.FindControl<CheckBox>("TriggerOnInputCheckBox")!;
        _autoLogCheckBox = this.FindControl<CheckBox>("AutoLogCheckBox")!;
        _reconnectCheckBox = this.FindControl<CheckBox>("ReconnectCheckBox")!;
        _keepInputCheckBox = this.FindControl<CheckBox>("KeepInputCheckBox")!;
        _abortDupeScriptCheckBox = this.FindControl<CheckBox>("AbortDupeScriptCheckBox")!;
        _playSoundsCheckBox = this.FindControl<CheckBox>("PlaySoundsCheckBox")!;
        _showSpellTimerCheckBox = this.FindControl<CheckBox>("ShowSpellTimerCheckBox")!;
        _showLinksCheckBox = this.FindControl<CheckBox>("ShowLinksCheckBox")!;
        _showImagesCheckBox = this.FindControl<CheckBox>("ShowImagesCheckBox")!;
        
        // General tab - Prompt
        _promptTextBox = this.FindControl<TextBox>("PromptTextBox")!;
        _promptBreakCheckBox = this.FindControl<CheckBox>("PromptBreakCheckBox")!;
        _condensedCheckBox = this.FindControl<CheckBox>("CondensedCheckBox")!;
        
        // Directories tab
        _scriptDirTextBox = this.FindControl<TextBox>("ScriptDirTextBox")!;
        _mapDirTextBox = this.FindControl<TextBox>("MapDirTextBox")!;
        _logDirTextBox = this.FindControl<TextBox>("LogDirTextBox")!;
        _soundDirTextBox = this.FindControl<TextBox>("SoundDirTextBox")!;
        _pluginDirTextBox = this.FindControl<TextBox>("PluginDirTextBox")!;
        _configDirTextBox = this.FindControl<TextBox>("ConfigDirTextBox")!;
        _artDirTextBox = this.FindControl<TextBox>("ArtDirTextBox")!;
        
        // Timeouts tab
        _serverTimeoutNumeric = this.FindControl<NumericUpDown>("ServerTimeoutNumeric")!;
        _serverTimeoutCommandTextBox = this.FindControl<TextBox>("ServerTimeoutCommandTextBox")!;
        _userTimeoutNumeric = this.FindControl<NumericUpDown>("UserTimeoutNumeric")!;
        _userTimeoutCommandTextBox = this.FindControl<TextBox>("UserTimeoutCommandTextBox")!;
        _roundtimeOffsetNumeric = this.FindControl<NumericUpDown>("RoundtimeOffsetNumeric")!;
        
        // Advanced tab
        _editorTextBox = this.FindControl<TextBox>("EditorTextBox")!;
        _rubyPathTextBox = this.FindControl<TextBox>("RubyPathTextBox")!;
        _lichPathTextBox = this.FindControl<TextBox>("LichPathTextBox")!;
        _lichArgumentsTextBox = this.FindControl<TextBox>("LichArgumentsTextBox")!;
        _lichServerTextBox = this.FindControl<TextBox>("LichServerTextBox")!;
        _lichPortNumeric = this.FindControl<NumericUpDown>("LichPortNumeric")!;
        _lichStartPauseNumeric = this.FindControl<NumericUpDown>("LichStartPauseNumeric")!;
        
        _ignoreScriptWarningsCheckBox = this.FindControl<CheckBox>("IgnoreScriptWarningsCheckBox")!;
        _ignoreCloseAlertCheckBox = this.FindControl<CheckBox>("IgnoreCloseAlertCheckBox")!;
        _parseGameOnlyCheckBox = this.FindControl<CheckBox>("ParseGameOnlyCheckBox")!;
        _webLinkSafetyCheckBox = this.FindControl<CheckBox>("WebLinkSafetyCheckBox")!;
        _autoMapperCheckBox = this.FindControl<CheckBox>("AutoMapperCheckBox")!;
        _checkForUpdatesCheckBox = this.FindControl<CheckBox>("CheckForUpdatesCheckBox")!;
        _maxGoSubDepthNumeric = this.FindControl<NumericUpDown>("MaxGoSubDepthNumeric")!;
        _bufferLineSizeNumeric = this.FindControl<NumericUpDown>("BufferLineSizeNumeric")!;
    }

    // Default values (matching Config.cs defaults)
    private const char DefaultScriptChar = '.';
    private const char DefaultCommandChar = '#';
    private const char DefaultSeparatorChar = ';';
    private const int DefaultScriptTimeout = 5000;
    private const string DefaultPrompt = "> ";
    private const string DefaultScriptDir = "Scripts";
    private const string DefaultMapDir = "Maps";
    private const string DefaultLogDir = "Logs";
    private const string DefaultSoundDir = "Sounds";
    private const string DefaultPluginDir = "Plugins";
    private const string DefaultConfigDir = "Config";
    private const string DefaultArtDir = "Art";
    private const int DefaultServerTimeout = 180;
    private const string DefaultServerTimeoutCommand = "fatigue";
    private const int DefaultUserTimeout = 300;
    private const string DefaultUserTimeoutCommand = "quit";
    private const string DefaultEditor = "notepad.exe";
    private const string DefaultLichServer = "localhost";
    private const int DefaultLichPort = 11024;
    private const int DefaultLichStartPause = 5;
    private const int DefaultMaxGoSubDepth = 50;
    private const int DefaultBufferLineSize = 5;

    private void LoadSettings()
    {
        // General tab - Script settings
        _scriptCharTextBox.Text = (_config?.ScriptChar ?? DefaultScriptChar).ToString();
        _commandCharTextBox.Text = (_config?.cCommandChar ?? DefaultCommandChar).ToString();
        _separatorCharTextBox.Text = (_config?.cSeparatorChar ?? DefaultSeparatorChar).ToString();
        _scriptTimeoutNumeric.Value = _config?.iScriptTimeout ?? DefaultScriptTimeout;
        
        // General tab - Behavior
        _triggerOnInputCheckBox.IsChecked = _config?.bTriggerOnInput ?? true;
        _autoLogCheckBox.IsChecked = _config?.bAutoLog ?? true;
        _reconnectCheckBox.IsChecked = _config?.bReconnect ?? true;
        _keepInputCheckBox.IsChecked = _config?.bKeepInput ?? false;
        _abortDupeScriptCheckBox.IsChecked = _config?.bAbortDupeScript ?? true;
        _playSoundsCheckBox.IsChecked = _config?.bPlaySounds ?? true;
        _showSpellTimerCheckBox.IsChecked = _config?.bShowSpellTimer ?? true;
        _showLinksCheckBox.IsChecked = _config?.bShowLinks ?? false;
        _showImagesCheckBox.IsChecked = _config?.bShowImages ?? true;
        
        // General tab - Prompt
        _promptTextBox.Text = _config?.sPrompt ?? DefaultPrompt;
        _promptBreakCheckBox.IsChecked = _config?.PromptBreak ?? true;
        _condensedCheckBox.IsChecked = _config?.Condensed ?? false;
        
        // Directories tab - use defaults if empty or null
        _scriptDirTextBox.Text = GetDirectoryOrDefault(_config?.sScriptDir, DefaultScriptDir);
        _mapDirTextBox.Text = GetDirectoryOrDefault(_config?.sMapDir, DefaultMapDir);
        _logDirTextBox.Text = GetDirectoryOrDefault(_config?.sLogDir, DefaultLogDir);
        _soundDirTextBox.Text = GetDirectoryOrDefault(_config?.sSoundDir, DefaultSoundDir);
        _pluginDirTextBox.Text = GetDirectoryOrDefault(_config?.sPluginDir, DefaultPluginDir);
        _configDirTextBox.Text = GetDirectoryOrDefault(_config?.sConfigDir, DefaultConfigDir);
        _artDirTextBox.Text = GetDirectoryOrDefault(_config?.sArtDir, DefaultArtDir);
        
        // Timeouts tab
        _serverTimeoutNumeric.Value = _config?.iServerActivityTimeout ?? DefaultServerTimeout;
        _serverTimeoutCommandTextBox.Text = _config?.sServerActivityCommand ?? DefaultServerTimeoutCommand;
        _userTimeoutNumeric.Value = _config?.iUserActivityTimeout ?? DefaultUserTimeout;
        _userTimeoutCommandTextBox.Text = _config?.sUserActivityCommand ?? DefaultUserTimeoutCommand;
        _roundtimeOffsetNumeric.Value = (decimal)(_config?.dRTOffset ?? 0);
        
        // Advanced tab
        _editorTextBox.Text = _config?.sEditor ?? DefaultEditor;
        _rubyPathTextBox.Text = _config?.RubyPath ?? "";
        _lichPathTextBox.Text = _config?.LichPath ?? "";
        _lichArgumentsTextBox.Text = _config?.LichArguments ?? "--genie --dragonrealms";
        _lichServerTextBox.Text = _config?.LichServer ?? DefaultLichServer;
        _lichPortNumeric.Value = _config?.LichPort ?? DefaultLichPort;
        _lichStartPauseNumeric.Value = _config?.LichStartPause ?? DefaultLichStartPause;
        
        _ignoreScriptWarningsCheckBox.IsChecked = _config?.bIgnoreScriptWarnings ?? false;
        _ignoreCloseAlertCheckBox.IsChecked = _config?.bIgnoreCloseAlert ?? false;
        _parseGameOnlyCheckBox.IsChecked = _config?.bParseGameOnly ?? false;
        _webLinkSafetyCheckBox.IsChecked = _config?.bWebLinkSafety ?? true;
        _autoMapperCheckBox.IsChecked = _config?.bAutoMapper ?? true;
        _checkForUpdatesCheckBox.IsChecked = _config?.CheckForUpdates ?? true;
        _maxGoSubDepthNumeric.Value = _config?.iMaxGoSubDepth ?? DefaultMaxGoSubDepth;
        _bufferLineSizeNumeric.Value = _config?.iBufferLineSize ?? DefaultBufferLineSize;
    }

    private static string GetDirectoryOrDefault(string? value, string defaultValue)
    {
        return string.IsNullOrWhiteSpace(value) ? defaultValue : value;
    }

    private void ApplySettings()
    {
        if (_config == null)
            return;

        // General tab - Script settings
        if (!string.IsNullOrEmpty(_scriptCharTextBox.Text))
            _config.ScriptChar = _scriptCharTextBox.Text[0];
        if (!string.IsNullOrEmpty(_commandCharTextBox.Text))
            _config.cCommandChar = _commandCharTextBox.Text[0];
        if (!string.IsNullOrEmpty(_separatorCharTextBox.Text))
            _config.cSeparatorChar = _separatorCharTextBox.Text[0];
        _config.iScriptTimeout = (int)(_scriptTimeoutNumeric.Value ?? 5000);
        
        // General tab - Behavior
        _config.bTriggerOnInput = _triggerOnInputCheckBox.IsChecked ?? false;
        _config.bAutoLog = _autoLogCheckBox.IsChecked ?? false;
        _config.bReconnect = _reconnectCheckBox.IsChecked ?? false;
        _config.bKeepInput = _keepInputCheckBox.IsChecked ?? false;
        _config.bAbortDupeScript = _abortDupeScriptCheckBox.IsChecked ?? false;
        _config.bPlaySounds = _playSoundsCheckBox.IsChecked ?? true;
        _config.bShowSpellTimer = _showSpellTimerCheckBox.IsChecked ?? true;
        _config.bShowLinks = _showLinksCheckBox.IsChecked ?? false;
        _config.bShowImages = _showImagesCheckBox.IsChecked ?? true;
        
        // General tab - Prompt
        _config.sPrompt = _promptTextBox.Text ?? "> ";
        _config.PromptBreak = _promptBreakCheckBox.IsChecked ?? true;
        _config.Condensed = _condensedCheckBox.IsChecked ?? false;
        
        // Directories tab
        if (!string.IsNullOrEmpty(_scriptDirTextBox.Text))
            _config.sScriptDir = _scriptDirTextBox.Text;
        if (!string.IsNullOrEmpty(_mapDirTextBox.Text))
            _config.sMapDir = _mapDirTextBox.Text;
        if (!string.IsNullOrEmpty(_logDirTextBox.Text))
            _config.sLogDir = _logDirTextBox.Text;
        if (!string.IsNullOrEmpty(_soundDirTextBox.Text))
            _config.sSoundDir = _soundDirTextBox.Text;
        if (!string.IsNullOrEmpty(_pluginDirTextBox.Text))
            _config.sPluginDir = _pluginDirTextBox.Text;
        if (!string.IsNullOrEmpty(_configDirTextBox.Text))
            _config.sConfigDir = _configDirTextBox.Text;
        if (!string.IsNullOrEmpty(_artDirTextBox.Text))
            _config.sArtDir = _artDirTextBox.Text;
        
        // Timeouts tab
        _config.iServerActivityTimeout = (int)(_serverTimeoutNumeric.Value ?? 180);
        _config.sServerActivityCommand = _serverTimeoutCommandTextBox.Text ?? "fatigue";
        _config.iUserActivityTimeout = (int)(_userTimeoutNumeric.Value ?? 300);
        _config.sUserActivityCommand = _userTimeoutCommandTextBox.Text ?? "quit";
        _config.dRTOffset = (double)(_roundtimeOffsetNumeric.Value ?? 0);
        
        // Advanced tab
        _config.sEditor = _editorTextBox.Text ?? "notepad.exe";
        _config.RubyPath = _rubyPathTextBox.Text ?? "";
        _config.LichPath = _lichPathTextBox.Text ?? "";
        _config.LichArguments = _lichArgumentsTextBox.Text ?? "";
        _config.LichServer = _lichServerTextBox.Text ?? "localhost";
        _config.LichPort = (int)(_lichPortNumeric.Value ?? 11024);
        _config.LichStartPause = (int)(_lichStartPauseNumeric.Value ?? 5);
        
        _config.bIgnoreScriptWarnings = _ignoreScriptWarningsCheckBox.IsChecked ?? false;
        _config.bIgnoreCloseAlert = _ignoreCloseAlertCheckBox.IsChecked ?? false;
        _config.bParseGameOnly = _parseGameOnlyCheckBox.IsChecked ?? false;
        _config.bWebLinkSafety = _webLinkSafetyCheckBox.IsChecked ?? true;
        _config.bAutoMapper = _autoMapperCheckBox.IsChecked ?? true;
        _config.CheckForUpdates = _checkForUpdatesCheckBox.IsChecked ?? true;
        _config.iMaxGoSubDepth = (int)(_maxGoSubDepthNumeric.Value ?? 50);
        _config.iBufferLineSize = (int)(_bufferLineSizeNumeric.Value ?? 5);
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        ApplySettings();
        _config?.Save();
        Close();
    }

    private void OnApply(object? sender, RoutedEventArgs e)
    {
        ApplySettings();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    #region Directory Browse Handlers

    private async void OnBrowseScripts(object? sender, RoutedEventArgs e)
    {
        var result = await BrowseForFolder("Select Scripts Directory");
        if (result != null)
            _scriptDirTextBox.Text = result;
    }

    private async void OnBrowseMaps(object? sender, RoutedEventArgs e)
    {
        var result = await BrowseForFolder("Select Maps Directory");
        if (result != null)
            _mapDirTextBox.Text = result;
    }

    private async void OnBrowseLogs(object? sender, RoutedEventArgs e)
    {
        var result = await BrowseForFolder("Select Logs Directory");
        if (result != null)
            _logDirTextBox.Text = result;
    }

    private async void OnBrowseSounds(object? sender, RoutedEventArgs e)
    {
        var result = await BrowseForFolder("Select Sounds Directory");
        if (result != null)
            _soundDirTextBox.Text = result;
    }

    private async void OnBrowsePlugins(object? sender, RoutedEventArgs e)
    {
        var result = await BrowseForFolder("Select Plugins Directory");
        if (result != null)
            _pluginDirTextBox.Text = result;
    }

    private async void OnBrowseConfig(object? sender, RoutedEventArgs e)
    {
        var result = await BrowseForFolder("Select Config Directory");
        if (result != null)
            _configDirTextBox.Text = result;
    }

    private async void OnBrowseArt(object? sender, RoutedEventArgs e)
    {
        var result = await BrowseForFolder("Select Art Directory");
        if (result != null)
            _artDirTextBox.Text = result;
    }

    private async Task<string?> BrowseForFolder(string title)
    {
        var storage = StorageProvider;
        var result = await storage.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = title,
            AllowMultiple = false
        });

        if (result.Count > 0)
        {
            return result[0].Path.LocalPath;
        }

        return null;
    }

    #endregion

    /// <summary>
    /// Shows the preferences dialog and returns when closed.
    /// </summary>
    public static async Task ShowDialog(Window parent, Config? config)
    {
        var dialog = new PreferencesDialog(config);
        await dialog.ShowDialog(parent);
    }
}

