// --------------------------------------------------------------------------
// Gurux Ltd
//
// Copyright (c) Gurux Ltd.
//
// This file is part of Gurux Device Framework.
//
// Commit: $Format:%H$
// Date:   $Format:%cI$
//
// This software is available under either of the following licenses:
//
// 1. GNU General Public License, version 2 only (GPL-2.0-only).
//    See the LICENSE file included with this distribution.
//
// 2. A commercial license granted by Gurux Ltd under a separate written
//    agreement. Use under this option requires a valid commercial license
//    and is governed by the terms of that agreement.
//
// If you have not obtained a commercial license from Gurux Ltd,
// use of this software is governed by GPL-2.0-only.
//
// Commercial licensing: https://www.gurux.fi/
//
// SPDX-License-Identifier: GPL-2.0-only OR LicenseRef-Gurux-Commercial
// --------------------------------------------------------------------------

using Microsoft.Maui.Controls;
using System.Globalization;
using Gurux.Serial;
using Gurux.SMS.Example.Properties;
using LayoutResources = Gurux.SMS.Example.Maui.Properties.ExampleSettingsResources;

using System.Text.Json.Nodes;

using StatusResources = Gurux.SMS.Example.Maui.Properties.ExampleSettingsResources;

namespace Gurux.SMS.Example.Maui;

/// <summary>Provides modem settings, connection lifecycle, messaging and activity display.</summary>
public sealed class MediaExamplePage : ContentPage, IAsyncDisposable
{
    /// <summary>Displays received payloads independently of the diagnostic activity log.</summary>
    private readonly Editor _received = new() { AutomationId = "ReceivedData", IsReadOnly = true };

    /// <summary>Displays connection state and selected endpoints in the fixed bottom status bar.</summary>
    private readonly Label _mediaStatus = new() { AutomationId = "MediaStatus" };

    /// <summary>Persists this application's media and outgoing data preferences.</summary>
    private readonly ExampleSettingsStore _preferences = new();
    /// <summary>Completes when startup preferences have been applied on the UI thread.</summary>
    private Task _preferencesLoading = Task.CompletedTask;
    /// <summary>Tracks the latest queued write so shutdown waits for the final disk update.</summary>
    private Task _preferencesSaving = Task.CompletedTask;
    /// <summary>Prevents persistence until startup restoration has completed.</summary>
    private bool _preferencesReady;
    /// <summary>Tracks the most recently requested snapshot and avoids saving unchanged controls.</summary>
    private string? _preferencesSnapshot;

    /// <summary>The modem session owned by this view.</summary>
    private readonly ExampleSession _session;
    /// <summary>Cancels operations when the view shuts down.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    /// <summary>Tracks whether the selected serial port is currently available without blocking UI refresh.</summary>
    private readonly global::Gurux.Serial.SerialPortAvailability _portAvailability = new();
    /// <summary>Refreshes UI state without transport callbacks touching controls.</summary>
    private readonly Microsoft.Maui.Dispatching.IDispatcherTimer _timer;
    /// <summary>Displays whether the modem is open.</summary>
    private readonly Label _state = new();
    /// <summary>Displays operation results and failures.</summary>
    private readonly Label _error = new();
    /// <summary>Displays the bounded modem activity history.</summary>
    private readonly Editor _log = new() { IsReadOnly = true, HeightRequest = 240, AutomationId = "ActivityLog" };
    /// <summary>Accepts a message or terminal command.</summary>
    private readonly Editor _text = new() { HeightRequest = 88, AutoSize = EditorAutoSizeOption.TextChanges, Placeholder = ExampleResources.TextPlaceholder, AutomationId = "SendText" };
    /// <summary>Contains actions disabled while the modem is busy.</summary>
    private readonly List<Button> _unsafeButtons = [];
    /// <summary>Accepts the destination phone number.</summary>
    private readonly Entry _receiver = new() { Placeholder = ExampleResources.Recipient, AutomationId = "Receiver" };
    /// <summary>Allows Close to cancel pending sends or close an open connection.</summary>
    private Button? _closeButton;
    /// <summary>Shares the asynchronous disposal operation between lifecycle callbacks.</summary>
    private Task? _disposal;
    /// <summary>Cancels the active inbox watcher independently of the view.</summary>
    private CancellationTokenSource? _watch;
    /// <summary>Tracks whether the inbox watcher is still running rather than merely retaining its token source.</summary>
    private bool _watching;
    /// <summary>Allows cancellation only while an uncanceled inbox watcher is active.</summary>
    private Button? _stopWatchingButton;

    /// <summary>Stores the latest connection phase independently of periodic UI refreshes.</summary>
    private int _mediaPhase;
    /// <summary>Prevents callbacks and queued refreshes from touching a disposed view.</summary>
    private int _shuttingDown;
    /// <summary>Marshals transport state notifications onto the native UI thread.</summary>
    private readonly Microsoft.Maui.Dispatching.IDispatcher _uiDispatcher;

    /// <summary>Contains the persistent messaging controls.</summary>
    private readonly VerticalStackLayout _dataView = new() { AutomationId = "DataView", Spacing = 12 };
    /// <summary>Contains communication options independently of activity history.</summary>
    private readonly VerticalStackLayout _communicationView = new() { AutomationId = "CommunicationView", Spacing = 12, IsVisible = false };
    /// <summary>Contains the existing activity history.</summary>
    private readonly VerticalStackLayout _activityView = new() { AutomationId = "ActivityView", Spacing = 12, IsVisible = false };
    /// <summary>Maps native menu actions to the existing toolbar controls.</summary>
    private readonly Dictionary<string, Button> _actionButtons = [];
    /// <summary>Prevents navigation while the settings editor owns the view.</summary>
    private bool _settingsShowing;
    /// <summary>Prevents overlapping native navigation sheets.</summary>
    private bool _navigationShowing;
    /// <summary>Refreshes availability of native commands.</summary>
    private readonly List<Command> _menuCommands = [];

    /// <summary>Creates Serial-style connection, messaging and reception cards with inbox tools in Communication settings.</summary>
    /// <param name="serialFactory">Optional USB, Bluetooth or test stream opener.</param>
    public MediaExamplePage(Func<SerialPortSettings, CancellationToken, ValueTask<Stream>>? serialFactory = null)
    {
        Title = ExampleResources.Title;
        _session = new ExampleSession(serialFactory);
        ExampleLayout.Apply(this);
        ExampleLayout.Activity(_received);
        ExampleLayout.Activity(_log);
        if (OperatingSystem.IsWindows()) { _text.FontFamily = "Consolas"; }
        ExampleLayout.Notice(_error);
        var body = ExampleLayout.Body();
        var toolbar = ExampleLayout.Row();
        var sendActions = ExampleLayout.Row();
        var inboxActions = ExampleLayout.Row();
        body.Children.Add(ExampleLayout.Header(_state));
        if (OperatingSystem.IsIOS()) { body.Children.Add(new Label { Text = ExampleResources.SerialUnavailable }); }
        AddButton(toolbar, "Open", ExampleResources.Open, OpenAsync, true);
        AddButton(toolbar, "Close", ExampleResources.Close, async () => { _watch?.Cancel(); await _session.CloseAsync(); });
        AddButton(null, "Settings", ExampleResources.Settings, EditMediaSettingsAsync, true);
        AddButton(sendActions, "Send", ExampleResources.Send, async () =>
        {
            string? result = await _session.SendAsync(_text.Text ?? "", _receiver.Text ?? "", _lifetime.Token);
            if (result is not null) { _error.Text = result; }
        }, true);
        AddButton(sendActions, "Clear", ExampleResources.Clear, () => { _session.ClearReceived(); return Task.CompletedTask; });
        AddButton(inboxActions, "ListMessages", ExampleResources.ListMessages, async () =>
        {
            var messages = await _session.ListMessagesAsync(_lifetime.Token);
            _error.Text = string.Join(Environment.NewLine, messages.Select(message => string.Format(CultureInfo.CurrentCulture, ExampleResources.ListedMessage, message.Time, message.PhoneNumber, message.Data)));
        }, true);
        AddButton(inboxActions, "WatchMessages", ExampleResources.WatchMessages, async () =>
        {
            _watch?.Cancel(); _watch?.Dispose(); _watch = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            _watching = true;
            try
            {
                await _session.WatchMessagesAsync(_watch.Token);
            }
            finally
            {
                _watching = false;
            }
        }, true);
        AddButton(inboxActions, "StopWatching", ExampleResources.StopWatching, () => { _watch?.Cancel(); return Task.CompletedTask; });
        _dataView.Children.Add(ExampleLayout.Group(LayoutResources.ConnectionGroup, toolbar));
        _dataView.Children.Add(ExampleLayout.Group(ExampleResources.Data, ExampleLayout.Stack(
            ExampleLayout.Field(ExampleResources.Recipient, _receiver),
            ExampleLayout.Field(LayoutResources.PayloadLabel, _text), sendActions)));
        _communicationView.Children.Add(ExampleLayout.Group(LayoutResources.InboxGroup, inboxActions));
        _dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedData, _received));
        var activityActions = ExampleLayout.Row();
        AddButton(activityActions, "ClearActivity", ExampleResources.Clear, () => { _session.ClearActivity(); return Task.CompletedTask; });
        _activityView.Children.Add(activityActions);
        _activityView.Children.Add(ExampleLayout.Group(ExampleResources.ActivityLog, _log));
        _communicationView.Children.Add(new Label { Text = ExampleResources.CommunicationInformation });
        body.Children.Add(_dataView);
        body.Children.Add(_communicationView);
        body.Children.Add(_activityView);
        body.Children.Add(_error);
        Content = ExampleLayout.WithStatus(body, _mediaStatus);
        CreateMenu();
        _uiDispatcher = Dispatcher;
        _mediaPhase = (int)(_session.Media.IsOpen ? Gurux.Common.MediaState.Open : Gurux.Common.MediaState.Closed);
        _session.Media.OnMediaStateChange += MediaStateChanged;
        _timer = Dispatcher.CreateTimer(); _timer.Interval = TimeSpan.FromMilliseconds(200);
        _timer.Tick += (_, _) => Refresh(); _timer.Start(); Refresh();
        _preferencesLoading = RestorePreferencesAsync();

    }

    /// <summary>Obtains native SMS permissions on Android and then opens the configured media.</summary>
    /// <returns>A task completing after permission checks and media startup.</returns>
    private async Task OpenAsync()
    {
#if ANDROID
        if (_session.Media.UsesNativeSms) { await SmsPermissions.EnsureAsync(_session.Media.Mode); }
#endif
        await _session.OpenAsync(_lifetime.Token);
    }

    /// <summary>Adds an action whose errors and cancellation are shown in the view.</summary>
    /// <param name="parent">The optional action row; null retains a command for the Settings menu.</param>
    /// <param name="name">The stable automation identifier.</param>
    /// <param name="caption">The localized button caption.</param>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="unsafeAction">Whether connection state and busy status restrict the action.</param>
    private void AddButton(Layout? parent, string name, string caption, Func<Task> action, bool unsafeAction = false)
    {
        var button = new Button { AutomationId = name, Text = caption, HorizontalOptions = LayoutOptions.Start };
        ExampleLayout.StyleButton(button, name is "Open" or "Send");
        button.Margin = new Thickness(0, 0, 8, 8);
        button.Clicked += async (_, _) => await RunAsync(action);
        parent?.Children.Add(button);
        _actionButtons[name] = button;
        if (name == "Close") { _closeButton = button; }
        if (name == "StopWatching") { _stopWatchingButton = button; }
        if (unsafeAction) { _unsafeButtons.Add(button); }
    }

    /// <summary>Displays asynchronous action errors and refreshes operation availability.</summary>
    /// <param name="action">The action initiated by the user.</param>
    /// <returns>A task completing after execution and UI refresh.</returns>
    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            _error.Text = "";
            Task operation = action();
            Refresh();
            await operation;
        }
        catch (OperationCanceledException) { _error.Text = ExampleResources.Canceled; }
        catch (Exception ex) { _error.Text = ex.Message; }
        finally { Refresh(); }
    }

    /// <summary>Caches connection phases and schedules immediate native control updates.</summary>
    /// <param name="sender">The media reporting its lifecycle state.</param>
    /// <param name="args">The reported lifecycle phase or settings-change notification.</param>
    private void MediaStateChanged(object? sender, Gurux.Common.MediaStateEventArgs args)
    {
        if (System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
        if (args.State != Gurux.Common.MediaState.Changed)
        {
            System.Threading.Volatile.Write(ref _mediaPhase, (int)args.State);
        }
        if (_uiDispatcher.IsDispatchRequired) { _uiDispatcher.Dispatch(Refresh); }
        else { Refresh(); }
    }
    /// <summary>Polls selected port availability and copies modem state and activity to controls on the UI thread.</summary>
    private void Refresh()
    {
        if (System.Threading.Volatile.Read(ref _shuttingDown) != 0)
        {
            foreach (var button in _unsafeButtons) { button.IsEnabled = false; }
            if (_closeButton is not null) { _closeButton.IsEnabled = false; }
            if (_stopWatchingButton is not null) { _stopWatchingButton.IsEnabled = false; }
            RefreshMenus();
            return;
        }
        var phase = (Gurux.Common.MediaState)System.Threading.Volatile.Read(ref _mediaPhase);
        bool closed = phase == Gurux.Common.MediaState.Closed;
        bool busy = _session.IsBusy;
        bool open = phase == Gurux.Common.MediaState.Open;
        if (!_session.Media.UsesNativeSms && !_lifetime.IsCancellationRequested && !open && !busy)
        {
            _ = _portAvailability.RefreshAsync(_session.Media.PortName, _lifetime.Token);
        }
        ExampleLayout.Status(_state, busy, open);
        if (_closeButton is not null) { _closeButton.IsEnabled = CanNavigate && open; }
        if (_stopWatchingButton is not null)
        {
            _stopWatchingButton.IsEnabled = _watching && _watch is { IsCancellationRequested: false };
        }
        foreach (var button in _unsafeButtons)
        {
            button.IsEnabled = CanNavigate && (!OperatingSystem.IsIOS() || _session.Media.PortName?.StartsWith("ble:", StringComparison.OrdinalIgnoreCase) == true || button.AutomationId is "Settings" or "RightSettings") && !busy && (button.AutomationId is "Settings" or "Open" ? closed : open)
                && (button.AutomationId != "Open" || (!_lifetime.IsCancellationRequested && (_session.Media.UsesNativeSms || _portAvailability.IsAvailable)))
                && (button.AutomationId != "Send" || _session.Media.Mode != SmsMode.ReceiveOnly)
                && (button.AutomationId is not ("ListMessages" or "WatchMessages") || _session.Media.Mode != SmsMode.SendOnly);
        }
        RefreshMenus();
        string receivedSnapshot = _session.ReceivedData(false);
        if (_received.Text != receivedSnapshot) { _received.Text = receivedSnapshot; }
        string snapshot = string.Join(Environment.NewLine, _session.Log);
        if (_log.Text != snapshot) { _log.Text = snapshot; }
        _ = SavePreferencesAsync();
        _mediaStatus.Text = _session.Media.UsesNativeSms
            ? string.Format(CultureInfo.CurrentCulture, StatusResources.StatusNativeSms,
                _session.Media.IsOpen ? StatusResources.StatusOpen : StatusResources.StatusClosed)
            : string.Format(CultureInfo.CurrentCulture, StatusResources.StatusSerial,
                _session.Media.IsOpen ? StatusResources.StatusOpen : StatusResources.StatusClosed,
                string.IsNullOrWhiteSpace(_session.Media.PortName) ? StatusResources.StatusNotConfigured : _session.Media.PortName);

    }

    /// <summary>Shuts down the view and reports any disposal failure.</summary>
    /// <returns>A task completing when shutdown finishes.</returns>
    public async Task ShutdownAsync()
    {
        try { await DisposeAsync(); }
        catch (Exception ex) { _error.Text = ex.Message; }
    }

    /// <summary>Stops UI polling and disposes the modem once.</summary>
    /// <returns>The shared asynchronous disposal operation.</returns>
    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());

    /// <summary>Cancels view operations and releases the owned modem resources.</summary>
    /// <returns>A task completing after all owned resources are released.</returns>
    private async Task DisposeCoreAsync()
    {
        System.Threading.Interlocked.Exchange(ref _shuttingDown, 1);
        if (_uiDispatcher.IsDispatchRequired) { _uiDispatcher.Dispatch(Refresh); }
        else { Refresh(); }
        _session.Media.OnMediaStateChange -= MediaStateChanged;
        _timer.Stop(); _lifetime.Cancel(); _watch?.Cancel();
        await _preferencesLoading;
        await SavePreferencesAsync();
        await _session.DisposeAsync();
        _watch?.Dispose(); _lifetime.Dispose();
    }
    /// <summary>Captures media configuration and outgoing controls without opening a connection.</summary>
    /// <returns>A detached JSON object containing this view's persistent preferences.</returns>
    private JsonObject CapturePreferences()
    {
        var values = new JsonObject
        {
            ["Media"] = JsonNode.Parse(_session.Media.JsonSettings ?? "{}"),
            ["SendText"] = _text.Text ?? "",
            ["Recipient"] = _receiver.Text ?? ""
        };
        (values["Media"] as JsonObject)?.Remove("PIN");
        return values;
    }

    /// <summary>Applies stored media configuration and interface values while the transport remains closed.</summary>
    /// <param name="values">The saved preferences; missing entries retain their current defaults.</param>
    private void ApplyPreferences(JsonObject values)
    {
        if (values["Media"] is JsonObject media) { _session.Media.JsonSettings = media.ToJsonString(); }
        _text.Text = values["SendText"]?.GetValue<string>() ?? _text.Text;
        _receiver.Text = values["Recipient"]?.GetValue<string>() ?? _receiver.Text;
    }

    /// <summary>Restores preferences asynchronously before enabling interaction and keeps defaults if loading fails.</summary>
    /// <returns>A task completing after the saved values or original defaults have been applied.</returns>
    private async Task RestorePreferencesAsync()
    {
        IsEnabled = false;
        JsonObject defaults = CapturePreferences();
        try
        {
            JsonObject? values = await _preferences.LoadAsync();
            if (System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
            if (values is not null) { ApplyPreferences(values); }
        }
        catch (Exception ex)
        {
            if (System.Threading.Volatile.Read(ref _shuttingDown) != 0) { return; }
            ApplyPreferences(defaults);
            _error.Text = ex.Message;
        }
        _preferencesSnapshot = CapturePreferences().ToJsonString();
        _preferencesReady = true;
        IsEnabled = true;
        Refresh();
    }

    /// <summary>Persists changed controls and reports disk failures without interrupting transport cleanup.</summary>
    /// <returns>A task completing after the current snapshot has been saved or skipped.</returns>
    private async Task SavePreferencesAsync()
    {
        if (_uiDispatcher.IsDispatchRequired)
        {
            await Microsoft.Maui.ApplicationModel.MainThread.InvokeOnMainThreadAsync(SavePreferencesAsync);
            return;
        }
        if (!_preferencesReady) { return; }
        string? snapshot = null;
        try
        {
            JsonObject values = CapturePreferences();
            snapshot = values.ToJsonString();
            if (snapshot == _preferencesSnapshot) { await _preferencesSaving; return; }
            _preferencesSnapshot = snapshot;
            _preferencesSaving = _preferences.SaveAsync(values);
            await _preferencesSaving;
        }
        catch (Exception ex)
        {
            if (_preferencesSnapshot == snapshot) { _preferencesSnapshot = null; }
            _error.Text = ex.Message;
        }
    }


    /// <summary>Gets whether navigation can change this page's persistent sections.</summary>
    private bool CanNavigate => _preferencesReady && !_settingsShowing && !_navigationShowing && System.Threading.Volatile.Read(ref _shuttingDown) == 0;
    /// <summary>Shows one retained section and updates the mobile navigation title without recreating controls.</summary>
    /// <param name="section">The existing section to display.</param>
    private void ShowSection(VerticalStackLayout section)
    {
        if (!CanNavigate) { return; }
        _dataView.IsVisible = ReferenceEquals(section, _dataView);
        _communicationView.IsVisible = ReferenceEquals(section, _communicationView);
        _activityView.IsVisible = ReferenceEquals(section, _activityView);
        if (!OperatingSystem.IsWindows())
        {
            Title = ExampleResources.Title;
        }
    }
    /// <summary>Edits media configuration with exclusive ownership while the connection is closed.</summary>
    /// <returns>A task completing when the editor releases the view.</returns>
    private async Task EditMediaSettingsAsync()
    {
        if (!CanNavigate || _session.IsBusy || _session.Media.IsOpen || (Gurux.Common.MediaState)System.Threading.Volatile.Read(ref _mediaPhase) != Gurux.Common.MediaState.Closed) { return; }
        _settingsShowing = true;
        Refresh();
        try { await new MauiMediaSettingsEditor(this).ShowAsync(_session.Media, cancellationToken: _lifetime.Token); }
        finally { _settingsShowing = false; Refresh(); }
    }

    /// <summary>Creates Windows menu groups or a native mobile navigation toolbar action.</summary>
    private void CreateMenu()
    {
        if (OperatingSystem.IsWindows())
        {
            var connection = new MenuBarItem { Text = ExampleResources.ConnectionMenu };
            connection.Add(ActionMenu("OpenMenu", "Open", ExampleResources.Open));
            connection.Add(ActionMenu("CloseMenu", "Close", ExampleResources.Close));
            var view = new MenuBarItem { Text = ExampleResources.ViewMenu };
            view.Add(ViewMenu("DataNavigation", ExampleResources.Data, _dataView));
            view.Add(ViewMenu("ActivityNavigation", ExampleResources.ActivityLog, _activityView));
            var settings = new MenuBarItem { Text = ExampleResources.Settings };
            settings.Add(ActionMenu("Settings", "Settings", ExampleResources.MediaSettings));
            settings.Add(ViewMenu("CommunicationNavigation", ExampleResources.CommunicationSettings, _communicationView));
            MenuBarItems.Add(connection); MenuBarItems.Add(view); MenuBarItems.Add(settings);
        }
        else
        {
            Title = ExampleResources.Title;
            var command = new Command(async () => await RunAsync(ShowMobileMenuAsync), () => CanNavigate);
            _menuCommands.Add(command);
            var navigation = new ToolbarItem { AutomationId = "NavigationMenu", Text = ExampleResources.Menu, Command = command };
            if (OperatingSystem.IsAndroid())
            {
                var icon = new FontImageSource { Glyph = "\u2630", FontFamily = "sans-serif", Size = 24 };
                icon.SetAppThemeColor(FontImageSource.ColorProperty, Microsoft.Maui.Graphics.Colors.Black, Microsoft.Maui.Graphics.Colors.White);
                navigation.IconImageSource = icon;
                navigation.Order = ToolbarItemOrder.Primary;
                navigation.Text = string.Empty;
                SemanticProperties.SetDescription(navigation, ExampleResources.Menu);
            }
            ToolbarItems.Add(navigation);
        }
    }
    /// <summary>Creates a native action sharing the existing toolbar command and availability.</summary>
    /// <param name="name">The automation identifier.</param>
    /// <param name="action">The toolbar identifier.</param>
    /// <param name="caption">The localized caption.</param>
    /// <returns>The native menu item.</returns>
    private MenuFlyoutItem ActionMenu(string name, string action, string caption)
    {
        var command = new Command(() => { if (CanNavigate && _actionButtons[action].IsEnabled) { _actionButtons[action].SendClicked(); } }, () => CanNavigate && _actionButtons[action].IsEnabled);
        _menuCommands.Add(command);
        return new MenuFlyoutItem { AutomationId = name, Text = caption, Command = command };
    }
    /// <summary>Creates navigation to a retained content section.</summary>
    /// <param name="name">The automation identifier.</param>
    /// <param name="caption">The localized caption.</param>
    /// <param name="section">The target section.</param>
    /// <returns>The native navigation item.</returns>
    private MenuFlyoutItem ViewMenu(string name, string caption, VerticalStackLayout section)
    {
        var command = new Command(() => ShowSection(section), () => CanNavigate);
        _menuCommands.Add(command);
        return new MenuFlyoutItem { AutomationId = name, Text = caption, Command = command };
    }
    /// <summary>Shows a platform action sheet and ignores results after shutdown starts.</summary>
    /// <returns>A task completing after the native navigation sheet closes.</returns>
    private async Task ShowMobileMenuAsync()
    {
        if (!CanNavigate) { return; }
        _navigationShowing = true;
        RefreshMenus();
        string? selected;
        try { selected = await DisplayActionSheetAsync(ExampleResources.Menu, ExampleResources.Cancel, null, ExampleResources.Data, ExampleResources.MediaSettings, ExampleResources.CommunicationSettings, ExampleResources.ActivityLog); }
        finally { _navigationShowing = false; RefreshMenus(); }
        if (!CanNavigate) { return; }
        if (selected == ExampleResources.Data) { ShowSection(_dataView); }
        else if (selected == ExampleResources.CommunicationSettings) { ShowSection(_communicationView); }
        else if (selected == ExampleResources.ActivityLog) { ShowSection(_activityView); }
        else if (selected == ExampleResources.MediaSettings) { await EditMediaSettingsAsync(); }
    }
    /// <summary>Refreshes availability of every native menu command.</summary>
    private void RefreshMenus()
    {
        foreach (var command in _menuCommands) { command.ChangeCanExecute(); }
    }

}
