# Gurux.SMS.Avalonia

## Why use this component?

Use this optional package to add a ready-made settings editor for `GXSMS` to an existing Avalonia desktop application. It supplies localized fields, input validation and a staged Save/Cancel workflow so the host does not have to implement the media settings form itself.

This adapter edits SMS operating mode and the transport-specific modem settings, including recipient, encoding and SIM PIN where applicable.

The media library can be used without this package in services, command-line tools, automated tests or applications that provide their own settings UI. Reference this adapter when users need to edit those settings interactively.

## Choose the UI for your application

| Host application | Settings adapter |
| --- | --- |
| Avalonia desktop | [Gurux.SMS.Avalonia](../Gurux.SMS.Avalonia/README.md) |
| Interactive Blazor / Razor UI | [Gurux.SMS.Blazor](../Gurux.SMS.Blazor/README.md) |
| Native .NET MAUI | [Gurux.SMS.Maui](../Gurux.SMS.Maui/README.md) |

Choose the adapter matching the host's UI framework; installing all three is unnecessary. A native MAUI host can use the MAUI adapter directly without a BlazorWebView. The Blazor adapter provides web components rather than native MAUI or Avalonia controls.

## How the pieces fit together

| Layer | Responsibility |
| --- | --- |
| [Gurux.SMS transport](../../README.md) | Connection lifecycle, protocol operations and media settings. It remains independent of UI frameworks. |
| `Gurux.SMS.Avalonia` | Settings controls and editing transactions for this framework and media type. |
| Host application | Owns the media, opens/closes connections, sends/receives data, persists accepted settings and supplies platform permissions. |
| [Gurux.SMS.Example](../../Gurux.SMS.Example/Gurux.SMS.Example.csproj) | A complete example application with connection/data controls and activity views. Its application screens are separate from this reusable settings adapter. |

This package is for configuration forms. Adding it does not supply a complete communication application, automatically connect a device or persist settings.

## Editing and ownership

Keep the supplied media closed while editing. The editor stages changes in a private, unopened draft; Save validates before applying settings to the caller-owned media. Cancel discards pending edits. Open media and conflicting external changes are rejected.

The host retains ownership of the media and disposes it when appropriate. Dispose host-owned embedded views or drafts when their containing form ends. A successful Save updates the media configuration; the application decides whether to persist it and when to open a connection.

Use AvaloniaMediaSettingsEditor to show this media-specific editor from the host window.

Serial/Bluetooth discovery and explicit GATT inspection can access native device services; the host remains responsible for permission requests and supported platform transports. Displaying settings does not grant hardware access. Terminal and SMS adapters reuse Serial settings controls where applicable.

Native Android SMS and external AT modems have different requirements. The settings UI does not grant SMS permissions or send messages. Protect persisted settings containing a modem SIM PIN; password fields do not encrypt the media's serialized configuration.

## Integration details

Optional Avalonia 12 settings editor for `GXSMS` on .NET 10. Register `new Gurux.SMS.Avalonia.AvaloniaMediaSettingsEditor(owner)` as the media settings editor and show the owner before calling `ShowAsync`. An optional parent uses `Gurux.SMS.Avalonia.AvaloniaMediaWindow`.

Edits are staged on an unopened private media instance. Save validates all fields before applying settings, rejects an open transport and detects settings changed externally. Cancel, owner close and cancellation discard the draft. Cancellation completes with `OperationCanceledException`. Calls are dispatched to the UI thread and overlapping dialogs on an editor are rejected.

All labels and validation messages are in `Properties/Resources.resx`. No transport connection is opened by this library. Terminal and SMS reuse the Serial Avalonia port controls.
