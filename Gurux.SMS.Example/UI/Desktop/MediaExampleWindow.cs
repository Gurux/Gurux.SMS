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

using Gurux.Common;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Threading;
using Gurux.SMS.Example.Desktop.Properties;
using System.Globalization;
using Gurux.Serial;
using Gurux.SMS.Example.Properties;

using System.Text.Json.Nodes;

using StatusResources = Gurux.SMS.Example.Desktop.Properties.ExampleSettingsResources;

namespace Gurux.SMS.Example.Desktop;

/// <summary>Provides modem settings, connection lifecycle, messaging and activity display.</summary>
public sealed class MediaExampleWindow : Window, IAsyncDisposable
{
    /// <summary>Displays received payloads independently of the diagnostic activity log.</summary>
    private readonly TextBox _received = new() { Name = "ReceivedData", IsReadOnly = true, AcceptsReturn = true };

    /// <summary>Displays connection state and selected endpoints in the fixed bottom status bar.</summary>
    private readonly TextBlock _mediaStatus = new() { Name = "MediaStatus" };

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

    /// <summary>Stores the latest transport connection phase independently of command activity.</summary>
    private int _connectionPhase;
    /// <summary>Prevents callbacks from refreshing disposed resources.</summary>
    private int _shuttingDown;
    /// <summary>The modem session owned by this view.</summary>
    private readonly ExampleSession _session;
    /// <summary>Cancels operations when the view shuts down.</summary>
    private readonly CancellationTokenSource _lifetime = new();
    /// <summary>Tracks discovery of the selected serial port without blocking the UI thread.</summary>
    private readonly global::Gurux.Serial.SerialPortAvailability _portAvailability = new();
    /// <summary>Refreshes UI state without transport callbacks touching controls.</summary>
    private readonly DispatcherTimer _timer;
    /// <summary>Displays whether the modem is open.</summary>
    private readonly TextBlock _state = new() { Name = "ConnectionState" };
    /// <summary>Displays operation results and failures.</summary>
    private readonly TextBlock _error = new() { Name = "ActionError" };
    /// <summary>Displays the bounded modem activity history.</summary>
    private readonly TextBox _log = new() { IsReadOnly = true, AcceptsReturn = true, MinHeight = 220, Name = "ActivityLog" };
    /// <summary>Accepts a message or terminal command.</summary>
    private readonly TextBox _text = new() { PlaceholderText = ExampleResources.TextPlaceholder, Name = "SendText" };
    /// <summary>Allows closing an open connection, including canceling an active reply.</summary>
    private Button? _closeButton;
    /// <summary>Contains actions disabled while the modem is busy.</summary>
    private readonly List<Button> _unsafeButtons = [];
    /// <summary>Accepts the destination phone number.</summary>
    private readonly TextBox _receiver = new() { PlaceholderText = ExampleResources.Recipient, Name = "Receiver" };
    /// <summary>Shares the asynchronous disposal operation between lifecycle callbacks.</summary>
    private Task? _disposal;
    /// <summary>Cancels the active inbox watcher independently of the view.</summary>
    private CancellationTokenSource? _watch;
    /// <summary>Tracks the inbox operation independently of other session work.</summary>
    private bool _watching;
    /// <summary>Disables the inbox stop action when no watcher is active.</summary>
    private Button? _stopWatchingButton;

    /// <summary>Contains the persistent messaging controls.</summary>
    private readonly StackPanel _dataView = new() { Name = "DataView", Spacing = 12 };
    /// <summary>Contains communication options independently of activity history.</summary>
    private readonly StackPanel _communicationView = new() { Name = "CommunicationView", Spacing = 12, IsVisible = false };
    /// <summary>Contains the existing activity history.</summary>
    private readonly StackPanel _activityView = new() { Name = "ActivityView", Spacing = 12, IsVisible = false };
    /// <summary>Maps native menu actions to the existing toolbar controls.</summary>
    private readonly Dictionary<string, Button> _actionButtons = [];
    /// <summary>Prevents navigation while the settings editor owns the view.</summary>
    private bool _settingsShowing;
    /// <summary>Refreshes availability of native action menu items.</summary>
    private readonly Dictionary<MenuItem, string> _actionMenus = [];
    /// <summary>Tracks navigation entries that are disabled during exclusive editing and shutdown.</summary>
    private readonly List<MenuItem> _viewMenus = [];

    /// <summary>Creates Serial-style connection, messaging and reception cards with inbox tools in Communication settings.</summary>
    /// <remarks>Native close requests await shared cleanup and then close the window even when cleanup fails.</remarks>
    /// <param name="serialFactory">Optional USB, Bluetooth or test stream opener.</param>
    public MediaExampleWindow(Func<SerialPortSettings, CancellationToken, ValueTask<Stream>>? serialFactory = null)
    {
        Title = ExampleResources.Title;
        _session = new ExampleSession(serialFactory);
        ExampleLayout.ConfigureWindow(this);
        var body = ExampleLayout.CreateBody(_state);
        var connection = ExampleLayout.Toolbar(_dataView);
        AddButton(connection, "Open", ExampleResources.Open, () => _session.OpenAsync(_lifetime.Token), true);
        AddButton(connection, "Close", ExampleResources.Close, async () => { _watch?.Cancel(); await _session.CloseAsync(); });
        AddButton(null, "Settings", ExampleResources.Settings, EditMediaSettingsAsync, true);
        _text.FontFamily = new global::Avalonia.Media.FontFamily("Cascadia Mono, DejaVu Sans Mono, Consolas, Menlo, monospace");
        var payload = ExampleLayout.Field(ExampleSettingsResources.PayloadLabel, _text);
        payload.MaxWidth = double.PositiveInfinity;
        payload.HorizontalAlignment = HorizontalAlignment.Stretch;
        _text.MinHeight = 92;
        _text.AcceptsReturn = true;
        var message = new StackPanel { Spacing = 10 };
        message.Children.Add(ExampleLayout.Field(ExampleResources.Recipient, _receiver));
        message.Children.Add(payload);
        _dataView.Children.Add(ExampleLayout.Group(ExampleResources.Data, message));
        var actions = ExampleLayout.Row();
        message.Children.Add(actions);
        var inboxActions = ExampleLayout.Row();
        _communicationView.Children.Add(ExampleLayout.Group(ExampleSettingsResources.MessageActionsGroup, inboxActions));
        AddButton(actions, "Send", ExampleResources.Send, async () =>
        {
            string? result = await _session.SendAsync(_text.Text ?? "", _receiver.Text ?? "", _lifetime.Token);
            if (result is not null) { _error.Text = result; }
        }, true);
        AddButton(actions, "Clear", ExampleResources.Clear, () => { _session.ClearReceived(); return Task.CompletedTask; });
        AddButton(inboxActions, "ListMessages", ExampleResources.ListMessages, async () =>
        {
            var messages = await _session.ListMessagesAsync(_lifetime.Token);
            _error.Text = string.Join(Environment.NewLine, messages.Select(message => string.Format(CultureInfo.CurrentCulture, ExampleResources.ListedMessage, message.Time, message.PhoneNumber, message.Data)));
        }, true);
        AddButton(inboxActions, "WatchMessages", ExampleResources.WatchMessages, async () =>
        {
            _watch?.Cancel(); _watch?.Dispose(); _watch = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            _watching = true;
            try { await _session.WatchMessagesAsync(_watch.Token); }
            finally { _watching = false; }
        }, true);
        AddButton(inboxActions, "StopWatching", ExampleResources.StopWatching, () => { _watch?.Cancel(); return Task.CompletedTask; });
        _dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedData, _received));
        ExampleLayout.ConfigureLog(_received);
        ExampleLayout.ConfigureLog(_log);
        var activityActions = ExampleLayout.Row();
        AddButton(activityActions, "ClearActivity", ExampleResources.Clear, () => { _session.ClearActivity(); return Task.CompletedTask; });
        _activityView.Children.Add(activityActions);
        _activityView.Children.Add(ExampleLayout.Group(ExampleResources.ActivityLog, _log));
        _communicationView.Children.Add(new TextBlock { Text = ExampleResources.CommunicationInformation, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });
        body.Children.Add(_dataView);
        body.Children.Add(_communicationView);
        body.Children.Add(_activityView);
        body.Children.Add(_error);
        Content = ExampleLayout.WithStatus(body, _mediaStatus);
        CreateMenu();
        _timer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, (_, _) => Refresh());
        Volatile.Write(ref _connectionPhase, (int)(_session.Media.IsOpen ? MediaState.Open : MediaState.Closed));
        _session.Media.OnMediaStateChange += MediaStateChanged;
        _timer.Start(); Refresh();

        _preferencesLoading = RestorePreferencesAsync();
        Closing += async (_, args) =>
        {
            if (_disposal is null)
            {
                args.Cancel = true;
                await RunAsync(async () =>
                {
                    try
                    {
                        await DisposeAsync();
                    }
                    finally
                    {
                        // Finish the current native closing event before issuing the final close request.
                        Dispatcher.UIThread.Post(() => Close());
                    }
                });
            }
            else if (!_disposal.IsCompleted) { args.Cancel = true; }
        };
    }

    /// <summary>Records transport lifecycle events and refreshes controls on the UI thread.</summary>
    /// <param name="sender">The transport publishing the event.</param>
    /// <param name="args">The new media state.</param>
    private void MediaStateChanged(object? sender, MediaStateEventArgs args)
    {
        if (Volatile.Read(ref _shuttingDown) != 0 || args.State == MediaState.Changed)
        {
            return;
        }
        Volatile.Write(ref _connectionPhase, (int)args.State);
        if (Dispatcher.UIThread.CheckAccess())
        {
            RefreshMediaState();
        }
        else
        {
            Dispatcher.UIThread.Post(RefreshMediaState);
        }
    }
    /// <summary>Ignores queued transport notifications after shutdown begins.</summary>
    private void RefreshMediaState()
    {
        if (Volatile.Read(ref _shuttingDown) == 0)
        {
            Refresh();
        }
    }
    /// <summary>Adds an action whose errors and cancellation are shown in the view.</summary>
    /// <param name="parent">The optional action row; null retains a command for the Settings menu.</param>
    /// <param name="name">The stable automation identifier.</param>
    /// <param name="caption">The localized button caption.</param>
    /// <param name="action">The asynchronous action to execute.</param>
    /// <param name="unsafeAction">Whether connection state and busy status restrict the action.</param>
    private void AddButton(Panel? parent, string name, string caption, Func<Task> action, bool unsafeAction = false)
    {
        var button = new Button { Name = name, Content = caption, HorizontalAlignment = HorizontalAlignment.Left };
        ExampleLayout.ConfigureAction(button, name is "Open" or "Send");
        if (name == "Close") { _closeButton = button; }
        if (name == "StopWatching") { _stopWatchingButton = button; }
        button.Click += async (_, _) => await RunAsync(action);
        parent?.Children.Add(button);
        _actionButtons[name] = button;
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

    /// <summary>Copies modem state, selected serial port availability and activity to controls on the UI thread.</summary>
    private void Refresh()
    {
        if (Volatile.Read(ref _shuttingDown) != 0)
        {
            if (_closeButton is not null) { _closeButton.IsEnabled = false; }
            foreach (var button in _unsafeButtons) { button.IsEnabled = false; }
            if (_stopWatchingButton is not null) { _stopWatchingButton.IsEnabled = false; }
            RefreshMenus();
            return;
        }
        MediaState phase = (MediaState)Volatile.Read(ref _connectionPhase);
        bool closed = phase == MediaState.Closed;
        bool busy = _session.IsBusy;
        bool open = phase == MediaState.Open;
        if (closed && !busy && !_lifetime.IsCancellationRequested)
        {
            _ = _portAvailability.RefreshAsync(_session.Media.PortName, _lifetime.Token);
        }
        if (_stopWatchingButton is not null) { _stopWatchingButton.IsEnabled = open && _watching && _watch is { IsCancellationRequested: false }; }
        ExampleLayout.UpdateStatus(_state, open, busy);
        ExampleLayout.UpdateError(_error);
        if (_closeButton is not null) { _closeButton.IsEnabled = CanNavigate && open; }
        foreach (var button in _unsafeButtons)
        {
            button.IsEnabled = CanNavigate && !busy && (button.Name is "Settings" or "Open" ? closed : open)
                && (button.Name != "Open" || (!_lifetime.IsCancellationRequested && _portAvailability.IsAvailable));
        }
        RefreshMenus();
        string receivedSnapshot = _session.ReceivedData(false);
        if (_received.Text != receivedSnapshot) { _received.Text = receivedSnapshot; }
        string snapshot = string.Join(Environment.NewLine, _session.Log);
        if (_log.Text != snapshot) { _log.Text = snapshot; }
        _ = SavePreferencesAsync();
        _mediaStatus.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, StatusResources.StatusSerial,
            _session.Media.IsOpen ? StatusResources.StatusOpen : StatusResources.StatusClosed,
            string.IsNullOrWhiteSpace(_session.Media.PortName) ? StatusResources.StatusNotConfigured : _session.Media.PortName);

    }

    /// <summary>Stops UI polling and disposes the modem once.</summary>
    /// <returns>The shared asynchronous disposal operation.</returns>
    public ValueTask DisposeAsync() => new(_disposal ??= DisposeCoreAsync());

    /// <summary>Cancels view operations and releases the owned modem resources.</summary>
    /// <returns>A task completing after all owned resources are released.</returns>
    private async Task DisposeCoreAsync()
    {
        Volatile.Write(ref _shuttingDown, 1);
        _session.Media.OnMediaStateChange -= MediaStateChanged;
        if (Dispatcher.UIThread.CheckAccess()) { Refresh(); }
        else { Dispatcher.UIThread.Post(Refresh); }
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
        if (!Dispatcher.UIThread.CheckAccess())
        {
            await Dispatcher.UIThread.InvokeAsync(SavePreferencesAsync);
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
    private bool CanNavigate => _preferencesReady && !_settingsShowing && System.Threading.Volatile.Read(ref _shuttingDown) == 0;
    /// <summary>Shows exactly one existing section without recreating its controls.</summary>
    /// <param name="section">The existing section to display.</param>
    private void ShowSection(StackPanel section)
    {
        if (!CanNavigate) { return; }
        _dataView.IsVisible = ReferenceEquals(section, _dataView);
        _communicationView.IsVisible = ReferenceEquals(section, _communicationView);
        _activityView.IsVisible = ReferenceEquals(section, _activityView);
    }
    /// <summary>Edits media configuration with exclusive ownership while the connection is closed.</summary>
    /// <returns>A task completing when the editor releases the view.</returns>
    private async Task EditMediaSettingsAsync()
    {
        if (!CanNavigate || _session.IsBusy || _session.Media.IsOpen || (Gurux.Common.MediaState)System.Threading.Volatile.Read(ref _connectionPhase) != Gurux.Common.MediaState.Closed) { return; }
        _settingsShowing = true;
        Refresh();
        try { await new AvaloniaMediaSettingsEditor(this).ShowAsync(_session.Media, cancellationToken: _lifetime.Token); }
        finally { _settingsShowing = false; Refresh(); }
    }

    /// <summary>Creates the native desktop menu outside the scrolling content.</summary>
    private void CreateMenu()
    {
        var connection = new MenuItem { Name = "ConnectionMenu", Header = ExampleResources.ConnectionMenu };
        connection.Items.Add(ActionMenu("OpenMenu", "Open", ExampleResources.Open));
        connection.Items.Add(ActionMenu("CloseMenu", "Close", ExampleResources.Close));
        var view = new MenuItem { Name = "ViewMenu", Header = ExampleResources.ViewMenu };
        view.Items.Add(ViewMenu("DataNavigation", ExampleResources.Data, _dataView));
        view.Items.Add(ViewMenu("ActivityNavigation", ExampleResources.ActivityLog, _activityView));
        var settings = new MenuItem { Name = "SettingsMenu", Header = ExampleResources.Settings };
        settings.Items.Add(ActionMenu("Settings", "Settings", ExampleResources.MediaSettings));
        settings.Items.Add(ViewMenu("CommunicationNavigation", ExampleResources.CommunicationSettings, _communicationView));
        var menu = new Menu { Name = "ApplicationMenu" };
        menu.Items.Add(connection); menu.Items.Add(view); menu.Items.Add(settings);
        var content = (Control)Content!;
        Content = null;
        var root = new DockPanel(); DockPanel.SetDock(menu, Dock.Top);
        root.Children.Add(menu); root.Children.Add(content); Content = root;
    }
    /// <summary>Creates a native menu action sharing an existing toolbar button.</summary>
    /// <param name="name">The automation identifier.</param>
    /// <param name="action">The toolbar identifier.</param>
    /// <param name="caption">The localized caption.</param>
    /// <returns>The native menu item.</returns>
    private MenuItem ActionMenu(string name, string action, string caption)
    {
        var item = new MenuItem { Name = name, Header = caption };
        _actionMenus[item] = action;
        item.Click += (_, _) => { if (CanNavigate && _actionButtons[action].IsEnabled) { _actionButtons[action].RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); } };
        return item;
    }
    /// <summary>Creates navigation to an existing content section.</summary>
    /// <param name="name">The automation identifier.</param>
    /// <param name="caption">The localized caption.</param>
    /// <param name="section">The persistent target section.</param>
    /// <returns>The native navigation item.</returns>
    private MenuItem ViewMenu(string name, string caption, StackPanel section)
    {
        var item = new MenuItem { Name = name, Header = caption };
        _viewMenus.Add(item);
        item.Click += (_, _) => ShowSection(section);
        return item;
    }
    /// <summary>Synchronizes native actions with their existing toolbar controls.</summary>
    private void RefreshMenus()
    {
        foreach (var item in _viewMenus) { item.IsEnabled = CanNavigate; }
        foreach (var item in _actionMenus) { item.Key.IsEnabled = CanNavigate && _actionButtons[item.Value].IsEnabled; }
    }

}
