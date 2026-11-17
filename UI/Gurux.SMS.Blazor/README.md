# Gurux.SMS.Blazor

## Why use this component?

Use this optional package to add a ready-made settings editor for `GXSMS` to an interactive Blazor application or a Razor-based settings surface. It supplies localized fields, input validation and a staged Save/Cancel workflow so the host does not have to implement the media settings form itself.

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
| `Gurux.SMS.Blazor` | Settings controls and editing transactions for this framework and media type. |
| Host application | Owns the media, opens/closes connections, sends/receives data, persists accepted settings and supplies platform permissions. |
| [Gurux.SMS.Example](../../Gurux.SMS.Example/Gurux.SMS.Example.csproj) | A complete example application with connection/data controls and activity views. Its application screens are separate from this reusable settings adapter. |

This package is for configuration forms. Adding it does not supply a complete communication application, automatically connect a device or persist settings.

## Editing and ownership

Keep the supplied media closed while editing. The editor stages changes in a private, unopened draft; Save validates before applying settings to the caller-owned media. Cancel discards pending edits. Open media and conflicting external changes are rejected.

The host retains ownership of the media and disposes it when appropriate. Dispose host-owned embedded views or drafts when their containing form ends. A successful Save updates the media configuration; the application decides whether to persist it and when to open a connection.

Render MediaSettingsEditor for a standalone editing session. For custom composition, create and dispose a MediaSettingsDraft and render MediaSettingsForm inside the host container; this form provides its own Save/Cancel actions.

Blazor Server edits media on the server: device discovery and transport access refer to that server, not the browser's computer. Browser WebAssembly cannot use these adapters to gain arbitrary serial-port or TCP-socket access. The host must select an interactive render mode and serve the required static web assets.

Serial/Bluetooth discovery and explicit GATT inspection can access native device services; the host remains responsible for permission requests and supported platform transports. Displaying settings does not grant hardware access. Terminal and SMS adapters reuse Serial settings controls where applicable.

Native Android SMS and external AT modems have different requirements. The settings UI does not grant SMS permissions or send messages. Protect persisted settings containing a modem SIM PIN; password fields do not encrypt the media's serialized configuration.

## Host setup

Reference `Gurux.SMS.Blazor` from the interactive Blazor host. In `Program.cs`, import `Gurux.SMS.Blazor` and call `builder.Services.AddGuruxMediaComponents()`. Add `@using Gurux.SMS.Blazor` to `_Imports.razor`.

Load the local appearance assets in the host HTML/head:

~~~html
<link data-gx-bootstrap rel="stylesheet" href="_content/Gurux.UI.Components/Appearance/themes/default.min.css" />
<link rel="stylesheet" href="_content/Gurux.UI.Components/Appearance/layout.css" />
~~~

Keep the standard Blazor host script and serve Razor static web assets. The appearance provider loads its module from the package; no CDN or extra appearance script tag is required.

Render `<MediaSettingsEditor Media="media" Completed="SettingsClosed" />` for a closed `GXSMS`. The `Completed` callback receives whether Save succeeded. Remove/recreate the editor to start a fresh editing session. The parent owns and disposes the media.

For custom composition, create `MediaSettingsDraft.Create(media)` and render `<MediaSettingsForm Draft="draft" Completed="SettingsClosed" />`. The form provides Save/Cancel and invokes `Completed` when the editing session ends. The containing host owns and disposes the draft and media; use the callback to close or remove the form.

This adapter exposes SMS mode, serial/modem configuration and a password input for the SIM PIN. It reuses Serial Blazor controls. It does not request native Android SMS permissions or turn a server-side modem form into a browser-device SMS transport.

## Build

From this directory:

~~~powershell
dotnet build Gurux.SMS.Blazor.csproj -p:GeneratePackageOnBuild=false
~~~

The project targets .NET 10. Labels and validation resources live in `Properties/Resources.resx`. Use browser/rendered checks separately from compilation and headless staging tests.
