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
    private readonly TextBox received = new() { Name = "ReceivedData", IsReadOnly = true, AcceptsReturn = true };

    /// <summary>Displays connection state and selected endpoints in the fixed bottom status bar.</summary>
    private readonly TextBlock mediaStatus = new() { Name = "MediaStatus" };

    /// <summary>Persists this application's media and outgoing data preferences.</summary>
    private readonly ExampleSettingsStore preferences = new();
    /// <summary>Completes when startup preferences have been applied on the UI thread.</summary>
    private Task preferencesLoading = Task.CompletedTask;
    /// <summary>Tracks the latest queued write so shutdown waits for the final disk update.</summary>
    private Task preferencesSaving = Task.CompletedTask;
    /// <summary>Prevents persistence until startup restoration has completed.</summary>
    private bool preferencesReady;
    /// <summary>Tracks the most recently requested snapshot and avoids saving unchanged controls.</summary>
    private string? preferencesSnapshot;

    /// <summary>Stores the latest transport connection phase independently of command activity.</summary>
    private int connectionPhase;
    /// <summary>Prevents callbacks from refreshing disposed resources.</summary>
    private int shuttingDown;
    /// <summary>The modem session owned by this view.</summary>
    private readonly ExampleSession session;
    /// <summary>Cancels operations when the view shuts down.</summary>
    private readonly CancellationTokenSource lifetime = new();
    /// <summary>Tracks discovery of the selected serial port without blocking the UI thread.</summary>
    private readonly global::Gurux.Serial.SerialPortAvailability portAvailability = new();
    /// <summary>Refreshes UI state without transport callbacks touching controls.</summary>
    private readonly DispatcherTimer timer;
    /// <summary>Displays whether the modem is open.</summary>
    private readonly TextBlock state = new() { Name = "ConnectionState" };
    /// <summary>Displays operation results and failures.</summary>
    private readonly TextBlock error = new() { Name = "ActionError" };
    /// <summary>Displays the bounded modem activity history.</summary>
    private readonly TextBox log = new() { IsReadOnly = true, AcceptsReturn = true, MinHeight = 220, Name = "ActivityLog" };
    /// <summary>Accepts a message or terminal command.</summary>
    private readonly TextBox text = new() { PlaceholderText = ExampleResources.TextPlaceholder, Name = "SendText" };
    /// <summary>Allows closing an open connection, including canceling an active reply.</summary>
    private Button? closeButton;
    /// <summary>Contains actions disabled while the modem is busy.</summary>
    private readonly List<Button> unsafeButtons = [];
    /// <summary>Accepts the destination phone number.</summary>
    private readonly TextBox receiver = new() { PlaceholderText = ExampleResources.Recipient, Name = "Receiver" };
    /// <summary>Shares the asynchronous disposal operation between lifecycle callbacks.</summary>
    private Task? disposal;
    /// <summary>Cancels the active inbox watcher independently of the view.</summary>
    private CancellationTokenSource? watch;
    /// <summary>Tracks the inbox operation independently of other session work.</summary>
    private bool watching;
    /// <summary>Disables the inbox stop action when no watcher is active.</summary>
    private Button? stopWatchingButton;

    /// <summary>Contains the persistent messaging controls.</summary>
    private readonly StackPanel dataView = new() { Name = "DataView", Spacing = 12 };
    /// <summary>Contains communication options independently of activity history.</summary>
    private readonly StackPanel communicationView = new() { Name = "CommunicationView", Spacing = 12, IsVisible = false };
    /// <summary>Contains the existing activity history.</summary>
    private readonly StackPanel activityView = new() { Name = "ActivityView", Spacing = 12, IsVisible = false };
    /// <summary>Maps native menu actions to the existing toolbar controls.</summary>
    private readonly Dictionary<string, Button> actionButtons = [];
    /// <summary>Prevents navigation while the settings editor owns the view.</summary>
    private bool settingsShowing;
    /// <summary>Refreshes availability of native action menu items.</summary>
    private readonly Dictionary<MenuItem, string> actionMenus = [];
    /// <summary>Tracks navigation entries that are disabled during exclusive editing and shutdown.</summary>
    private readonly List<MenuItem> viewMenus = [];

    /// <summary>Creates Serial-style connection, messaging and reception cards with inbox tools in Communication settings.</summary>
    /// <remarks>Native close requests await shared cleanup and then close the window even when cleanup fails.</remarks>
    /// <param name="serialFactory">Optional USB, Bluetooth or test stream opener.</param>
    public MediaExampleWindow(Func<SerialPortSettings, CancellationToken, ValueTask<Stream>>? serialFactory = null)
    {
        Title = ExampleResources.Title;
        session = new ExampleSession(serialFactory);
        ExampleLayout.ConfigureWindow(this);
        var body = ExampleLayout.CreateBody(state);
        var connection = ExampleLayout.Toolbar(dataView);
        AddButton(connection, "Open", ExampleResources.Open, () => session.OpenAsync(lifetime.Token), true);
        AddButton(connection, "Close", ExampleResources.Close, async () => { watch?.Cancel(); await session.CloseAsync(); });
        AddButton(null, "Settings", ExampleResources.Settings, EditMediaSettingsAsync, true);
        text.FontFamily = new global::Avalonia.Media.FontFamily("Cascadia Mono, DejaVu Sans Mono, Consolas, Menlo, monospace");
        var payload = ExampleLayout.Field(ExampleSettingsResources.PayloadLabel, text);
        payload.MaxWidth = double.PositiveInfinity;
        payload.HorizontalAlignment = HorizontalAlignment.Stretch;
        text.MinHeight = 92;
        text.AcceptsReturn = true;
        var message = new StackPanel { Spacing = 10 };
        message.Children.Add(ExampleLayout.Field(ExampleResources.Recipient, receiver));
        message.Children.Add(payload);
        dataView.Children.Add(ExampleLayout.Group(ExampleResources.Data, message));
        var actions = ExampleLayout.Row();
        message.Children.Add(actions);
        var inboxActions = ExampleLayout.Row();
        communicationView.Children.Add(ExampleLayout.Group(ExampleSettingsResources.MessageActionsGroup, inboxActions));
        AddButton(actions, "Send", ExampleResources.Send, async () =>
        {
            string? result = await session.SendAsync(text.Text ?? "", receiver.Text ?? "", lifetime.Token);
            if (result is not null) { error.Text = result; }
        }, true);
        AddButton(actions, "Clear", ExampleResources.Clear, () => { session.ClearReceived(); return Task.CompletedTask; });
        AddButton(inboxActions, "ListMessages", ExampleResources.ListMessages, async () =>
        {
            var messages = await session.ListMessagesAsync(lifetime.Token);
            error.Text = string.Join(Environment.NewLine, messages.Select(message => string.Format(CultureInfo.CurrentCulture, ExampleResources.ListedMessage, message.Time, message.PhoneNumber, message.Data)));
        }, true);
        AddButton(inboxActions, "WatchMessages", ExampleResources.WatchMessages, async () =>
        {
            watch?.Cancel(); watch?.Dispose(); watch = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            watching = true;
            try { await session.WatchMessagesAsync(watch.Token); }
            finally { watching = false; }
        }, true);
        AddButton(inboxActions, "StopWatching", ExampleResources.StopWatching, () => { watch?.Cancel(); return Task.CompletedTask; });
        dataView.Children.Add(ExampleLayout.Group(ExampleResources.ReceivedData, received));
        ExampleLayout.ConfigureLog(received);
        ExampleLayout.ConfigureLog(log);
        var activityActions = ExampleLayout.Row();
        AddButton(activityActions, "ClearActivity", ExampleResources.Clear, () => { session.ClearActivity(); return Task.CompletedTask; });
        activityView.Children.Add(activityActions);
        activityView.Children.Add(ExampleLayout.Group(ExampleResources.ActivityLog, log));
        communicationView.Children.Add(new TextBlock { Text = ExampleResources.CommunicationInformation, TextWrapping = global::Avalonia.Media.TextWrapping.Wrap });
        body.Children.Add(dataView);
        body.Children.Add(communicationView);
        body.Children.Add(activityView);
        body.Children.Add(error);
        Content = ExampleLayout.WithStatus(body, mediaStatus);
        CreateMenu();
        timer = new DispatcherTimer(TimeSpan.FromMilliseconds(200), DispatcherPriority.Background, (_, _) => Refresh());
        Volatile.Write(ref connectionPhase, (int)(session.Media.IsOpen ? MediaState.Open : MediaState.Closed));
        session.Media.OnMediaStateChange += MediaStateChanged;
        timer.Start(); Refresh();

        preferencesLoading = RestorePreferencesAsync();
        Closing += async (_, args) =>
        {
            if (disposal is null)
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
            else if (!disposal.IsCompleted) { args.Cancel = true; }
        };
    }

    /// <summary>Records transport lifecycle events and refreshes controls on the UI thread.</summary>
    /// <param name="sender">The transport publishing the event.</param>
    /// <param name="args">The new media state.</param>
    private void MediaStateChanged(object? sender, MediaStateEventArgs args)
    {
        if (Volatile.Read(ref shuttingDown) != 0 || args.State == MediaState.Changed)
        {
            return;
        }
        Volatile.Write(ref connectionPhase, (int)args.State);
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
        if (Volatile.Read(ref shuttingDown) == 0)
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
        if (name == "Close") { closeButton = button; }
        if (name == "StopWatching") { stopWatchingButton = button; }
        button.Click += async (_, _) => await RunAsync(action);
        parent?.Children.Add(button);
        actionButtons[name] = button;
        if (unsafeAction) { unsafeButtons.Add(button); }
    }

    /// <summary>Displays asynchronous action errors and refreshes operation availability.</summary>
    /// <param name="action">The action initiated by the user.</param>
    /// <returns>A task completing after execution and UI refresh.</returns>
    private async Task RunAsync(Func<Task> action)
    {
        try
        {
            error.Text = "";
            Task operation = action();
            Refresh();
            await operation;
        }
        catch (OperationCanceledException) { error.Text = ExampleResources.Canceled; }
        catch (Exception ex) { error.Text = ex.Message; }
        finally { Refresh(); }
    }

    /// <summary>Copies modem state, selected serial port availability and activity to controls on the UI thread.</summary>
    private void Refresh()
    {
        if (Volatile.Read(ref shuttingDown) != 0)
        {
            if (closeButton is not null) { closeButton.IsEnabled = false; }
            foreach (var button in unsafeButtons) { button.IsEnabled = false; }
            if (stopWatchingButton is not null) { stopWatchingButton.IsEnabled = false; }
            RefreshMenus();
            return;
        }
        MediaState phase = (MediaState)Volatile.Read(ref connectionPhase);
        bool closed = phase == MediaState.Closed;
        bool busy = session.IsBusy;
        bool open = phase == MediaState.Open;
        if (closed && !busy && !lifetime.IsCancellationRequested)
        {
            _ = portAvailability.RefreshAsync(session.Media.PortName, lifetime.Token);
        }
        if (stopWatchingButton is not null) { stopWatchingButton.IsEnabled = open && watching && watch is { IsCancellationRequested: false }; }
        ExampleLayout.UpdateStatus(state, open, busy);
        ExampleLayout.UpdateError(error);
        if (closeButton is not null) { closeButton.IsEnabled = CanNavigate && open; }
        foreach (var button in unsafeButtons)
        {
            button.IsEnabled = CanNavigate && !busy && (button.Name is "Settings" or "Open" ? closed : open)
                && (button.Name != "Open" || (!lifetime.IsCancellationRequested && portAvailability.IsAvailable));
        }
        RefreshMenus();
        string receivedSnapshot = session.ReceivedData(false);
        if (received.Text != receivedSnapshot) { received.Text = receivedSnapshot; }
        string snapshot = string.Join(Environment.NewLine, session.Log);
        if (log.Text != snapshot) { log.Text = snapshot; }
        _ = SavePreferencesAsync();
        mediaStatus.Text = string.Format(System.Globalization.CultureInfo.CurrentCulture, StatusResources.StatusSerial,
            session.Media.IsOpen ? StatusResources.StatusOpen : StatusResources.StatusClosed,
            string.IsNullOrWhiteSpace(session.Media.PortName) ? StatusResources.StatusNotConfigured : session.Media.PortName);

    }

    /// <summary>Stops UI polling and disposes the modem once.</summary>
    /// <returns>The shared asynchronous disposal operation.</returns>
    public ValueTask DisposeAsync() => new(disposal ??= DisposeCoreAsync());

    /// <summary>Cancels view operations and releases the owned modem resources.</summary>
    /// <returns>A task completing after all owned resources are released.</returns>
    private async Task DisposeCoreAsync()
    {
        Volatile.Write(ref shuttingDown, 1);
        session.Media.OnMediaStateChange -= MediaStateChanged;
        if (Dispatcher.UIThread.CheckAccess()) { Refresh(); }
        else { Dispatcher.UIThread.Post(Refresh); }
        timer.Stop(); lifetime.Cancel(); watch?.Cancel();
        await preferencesLoading;
        await SavePreferencesAsync();
        await session.DisposeAsync();
        watch?.Dispose(); lifetime.Dispose();
    }
    /// <summary>Captures media configuration and outgoing controls without opening a connection.</summary>
    /// <returns>A detached JSON object containing this view's persistent preferences.</returns>
    private JsonObject CapturePreferences()
    {
        var values = new JsonObject
        {
            ["Media"] = JsonNode.Parse(session.Media.JsonSettings ?? "{}"),
            ["SendText"] = text.Text ?? "",
            ["Recipient"] = receiver.Text ?? ""
        };
        (values["Media"] as JsonObject)?.Remove("PIN");
        return values;
    }

    /// <summary>Applies stored media configuration and interface values while the transport remains closed.</summary>
    /// <param name="values">The saved preferences; missing entries retain their current defaults.</param>
    private void ApplyPreferences(JsonObject values)
    {
        if (values["Media"] is JsonObject media) { session.Media.JsonSettings = media.ToJsonString(); }
        text.Text = values["SendText"]?.GetValue<string>() ?? text.Text;
        receiver.Text = values["Recipient"]?.GetValue<string>() ?? receiver.Text;
    }

    /// <summary>Restores preferences asynchronously before enabling interaction and keeps defaults if loading fails.</summary>
    /// <returns>A task completing after the saved values or original defaults have been applied.</returns>
    private async Task RestorePreferencesAsync()
    {
        IsEnabled = false;
        JsonObject defaults = CapturePreferences();
        try
        {
            JsonObject? values = await preferences.LoadAsync();
            if (System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
            if (values is not null) { ApplyPreferences(values); }
        }
        catch (Exception ex)
        {
            if (System.Threading.Volatile.Read(ref shuttingDown) != 0) { return; }
            ApplyPreferences(defaults);
            error.Text = ex.Message;
        }
        preferencesSnapshot = CapturePreferences().ToJsonString();
        preferencesReady = true;
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
        if (!preferencesReady) { return; }
        string? snapshot = null;
        try
        {
            JsonObject values = CapturePreferences();
            snapshot = values.ToJsonString();
            if (snapshot == preferencesSnapshot) { await preferencesSaving; return; }
            preferencesSnapshot = snapshot;
            preferencesSaving = preferences.SaveAsync(values);
            await preferencesSaving;
        }
        catch (Exception ex)
        {
            if (preferencesSnapshot == snapshot) { preferencesSnapshot = null; }
            error.Text = ex.Message;
        }
    }


    /// <summary>Gets whether navigation can change this page's persistent sections.</summary>
    private bool CanNavigate => preferencesReady && !settingsShowing && System.Threading.Volatile.Read(ref shuttingDown) == 0;
    /// <summary>Shows exactly one existing section without recreating its controls.</summary>
    /// <param name="section">The existing section to display.</param>
    private void ShowSection(StackPanel section)
    {
        if (!CanNavigate) { return; }
        dataView.IsVisible = ReferenceEquals(section, dataView);
        communicationView.IsVisible = ReferenceEquals(section, communicationView);
        activityView.IsVisible = ReferenceEquals(section, activityView);
    }
    /// <summary>Edits media configuration with exclusive ownership while the connection is closed.</summary>
    /// <returns>A task completing when the editor releases the view.</returns>
    private async Task EditMediaSettingsAsync()
    {
        if (!CanNavigate || session.IsBusy || session.Media.IsOpen || (Gurux.Common.MediaState)System.Threading.Volatile.Read(ref connectionPhase) != Gurux.Common.MediaState.Closed) { return; }
        settingsShowing = true;
        Refresh();
        try { await new AvaloniaMediaSettingsEditor(this).ShowAsync(session.Media, cancellationToken: lifetime.Token); }
        finally { settingsShowing = false; Refresh(); }
    }

    /// <summary>Creates the native desktop menu outside the scrolling content.</summary>
    private void CreateMenu()
    {
        var connection = new MenuItem { Name = "ConnectionMenu", Header = ExampleResources.ConnectionMenu };
        connection.Items.Add(ActionMenu("OpenMenu", "Open", ExampleResources.Open));
        connection.Items.Add(ActionMenu("CloseMenu", "Close", ExampleResources.Close));
        var view = new MenuItem { Name = "ViewMenu", Header = ExampleResources.ViewMenu };
        view.Items.Add(ViewMenu("DataNavigation", ExampleResources.Data, dataView));
        view.Items.Add(ViewMenu("ActivityNavigation", ExampleResources.ActivityLog, activityView));
        var settings = new MenuItem { Name = "SettingsMenu", Header = ExampleResources.Settings };
        settings.Items.Add(ActionMenu("Settings", "Settings", ExampleResources.MediaSettings));
        settings.Items.Add(ViewMenu("CommunicationNavigation", ExampleResources.CommunicationSettings, communicationView));
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
        actionMenus[item] = action;
        item.Click += (_, _) => { if (CanNavigate && actionButtons[action].IsEnabled) { actionButtons[action].RaiseEvent(new global::Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); } };
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
        viewMenus.Add(item);
        item.Click += (_, _) => ShowSection(section);
        return item;
    }
    /// <summary>Synchronizes native actions with their existing toolbar controls.</summary>
    private void RefreshMenus()
    {
        foreach (var item in viewMenus) { item.IsEnabled = CanNavigate; }
        foreach (var item in actionMenus) { item.Key.IsEnabled = CanNavigate && actionButtons[item.Value].IsEnabled; }
    }

}
