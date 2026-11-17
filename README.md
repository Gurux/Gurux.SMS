# Gurux.SMS

Gurux.SMS provides asynchronous SMS messaging for .NET 10 and .NET 11. `GXSMS` uses the Android phone's native SMS service on Android, or an external AT-command modem through Gurux.Terminal and Gurux.Serial.

The transport library is independent of MAUI, Avalonia, Blazor and WinForms. Optional settings editors and the example application provide UI separately.

## Packages and requirements

| Package | Purpose |
| --- | --- |
| Gurux.SMS | SMS operations, PDU codecs and native/modem transport integration. |
| Gurux.SMS.Avalonia | Optional Avalonia settings editor. |
| Gurux.SMS.Blazor | Optional Blazor settings components. |
| Gurux.SMS.Maui | Optional native MAUI settings editor. |

This README describes the **9.0 API**. Earlier 8.x packages use different APIs; use a matching 9.0 package release or build the current source.

The library targets `net10.0` and `net11.0`. `BuildMobile=true` enables matching Android targets; `BuildApple=true` also enables iOS targets. Building from source uses the stable .NET 11 SDK selected by [global.json](global.json), including for .NET 10 outputs. Native mobile builds require the corresponding workloads and platform tooling.

## Choose a transport

| Behavior | Native Android SMS | External AT modem |
| --- | --- | --- |
| Selection | `new GXSMS()` on an Android target. | Default constructor on desktop, or an explicitly supplied serial stream factory on any supported target. |
| Connection | Android telephony service. | Serial port, Bluetooth/USB modem adapter or another owned serial stream. |
| Required configuration | SMS permissions and a telephony-capable device/service. | Port/accessory identity, line settings and optionally the modem's SIM PIN. |
| Outgoing encoding | Android selects encoding and splits multipart messages. | `CodeType` selects the PDU alphabet; automatic multipart submission is not implemented. |
| Incoming storage | Bounded messages received while this component is open. | Received messages stored by the modem. |
| Deletion | Device inbox deletion is unsupported. | Explicit `DeleteMessageAsync(index)`. |

`UsesNativeSms` identifies the selected path. On Android, supplying a serial stream factory explicitly selects the modem path instead of the phone's SMS service. Native SMS does not need a serial port and ignores modem PIN, PDU alphabet and initialization settings.

Windows, Linux and macOS use an external modem through a supported serial transport. Android also supports external modem streams supplied by the application's selected adapter. iOS has no native phone-SMS backend in this library; an iOS build needs a supported external accessory/BLE modem stream, and arbitrary physical COM/USB serial ports are not exposed.

See [Gurux.Serial transport and platform requirements](../Gurux.Serial/README.md) for device selection, Bluetooth, Android USB and custom stream factories.

## Send through an external modem

This console example takes the recipient number and message text as its first two command-line arguments. It requires a PDU-capable modem on the selected port. Replace `COM3` with the modem's actual device name.

~~~csharp
using Gurux.SMS;

if (args.Length != 2)
{
    throw new ArgumentException(nameof(args));
}

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
await using var sms = new GXSMS
{
    PortName = "COM3",
    BaudRate = 115200,
    Mode = SmsMode.SendOnly,
    CodeType = MessageCodeType.Unicode
};

await sms.OpenAsync(timeout.Token);
string response = await sms.SendMessageAsync(args[0], args[1], timeout.Token);
Console.WriteLine(response);
await sms.CloseAsync();
~~~

On the modem path, opening runs the configured initialization commands (default `AT` and `ATE0`), supplies an optional `PIN` and selects PDU mode with `AT+CMGF=0`. It does not dial `PhoneNumber`.

`SendMessageAsync(recipient, text)` submits an SMS-SUBMIT PDU through the modem's prompt exchange. Success means the modem accepted the operation; it does not confirm recipient delivery.

The inherited `SendAsync(data, receiver)` interprets `data` as UTF-8 message text. It uses `receiver` when supplied, otherwise `PhoneNumber`. It is not a raw PDU submission API.

## Send through Android telephony

Reference an Android library target and obtain the required SMS permission in the host application before opening. This console-style fragment uses its first two arguments as recipient and text; in a UI application, supply the values from your own view model.

~~~csharp
using Gurux.SMS;

if (args.Length != 2)
{
    throw new ArgumentException(nameof(args));
}

using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
await using var sms = new GXSMS
{
    Mode = SmsMode.SendOnly,
    ConnectTimeout = TimeSpan.FromSeconds(30)
};

await sms.OpenAsync(timeout.Token);
await sms.SendMessageAsync(args[0], args[1], timeout.Token);
await sms.CloseAsync();
~~~

On an Android target, the default constructor selects `SmsManager`. The same default constructor on desktop selects an external modem, so the transport distinction matters.

### Android permissions

Declare and obtain permissions appropriate to the selected `Mode` before calling `OpenAsync()`:

| Mode | Required permissions |
| --- | --- |
| `SmsMode.SendOnly` | `android.permission.SEND_SMS` |
| `SmsMode.ReceiveOnly` | `android.permission.RECEIVE_SMS` |
| `SmsMode.SendAndReceive` | Both sending and reception permissions. |

The native implementation requires no `READ_SMS` permission or default-SMS-application role. It does not read or modify the Android device inbox. Applications own runtime permission requests; the MAUI example requests only the permissions required by the selected mode.

Android selects message encoding and multipart boundaries. `SendMessageAsync` waits for successful submission acknowledgements for every part, bounded by `ConnectTimeout`. These acknowledgements are not delivery reports. Cancellation or closing stops waiting but cannot recall parts already submitted.

## Receive and watch messages

Set `Mode` before opening:

- `SendAndReceive` is the default, including settings that omit the mode.
- `SendOnly` rejects reception operations.
- `ReceiveOnly` rejects outgoing submissions.

The following example watches an external modem for one minute and prints each decoded message. It never deletes messages.

~~~csharp
using Gurux.SMS;

using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
await using var sms = new GXSMS
{
    PortName = "COM3",
    BaudRate = 115200,
    Mode = SmsMode.ReceiveOnly
};

try
{
    await sms.OpenAsync(timeout.Token);
    await foreach (GXSMSMessage message in sms.WatchMessagesAsync(
        TimeSpan.FromSeconds(5), timeout.Token))
    {
        Console.WriteLine(message.PhoneNumber);
        Console.WriteLine(message.Data);
    }
}
catch (OperationCanceledException) when (timeout.IsCancellationRequested)
{
}
finally
{
    await sms.CloseAsync();
}
~~~

For native Android reception, use `new GXSMS { Mode = SmsMode.ReceiveOnly }` after obtaining `RECEIVE_SMS` permission. The watcher then waits for incoming broadcasts instead of polling a modem.

### Message operations

| Operation | External modem | Native Android |
| --- | --- | --- |
| `ListMessagesAsync()` | Lists received SMS-DELIVER records in modem storage. | Returns the current session's retained messages. |
| `ReadMessageAsync(index)` | Reads a modem storage index using `AT+CMGR`. | Looks up a retained session-local index. |
| `WatchMessagesAsync(interval, token)` | Polls stored incoming messages while enumerated. | Replays retained messages, then waits for incoming broadcasts. |
| `DeleteMessageAsync(index)` | Explicitly removes the selected modem-stored message. | Throws `NotSupportedException`. |

For decoded content, use `GXSMSMessage.Data`, `PhoneNumber`, `Time` and `Index`. Modem PDU decoding also exposes the service-centre number, alphabet and canonical `Pdu`. `Time` is converted to local time. Modem storage indices and native session indices are not interchangeable.

### Modem reception

`ListMessagesAsync` uses `AT+CMGL=4,1`. The modem must support listing mode 1, which preserves read/unread status. Stored outgoing SMS-SUBMIT records are excluded from the received-message results.

Each watcher tracks index/PDU pairs and suppresses unchanged records between polls. Removed or replaced entries can be delivered again. Tracking stays bounded by the current inbox snapshot. Polling runs only while the sequence is enumerated; no messages are deleted automatically.

For decoded modem messages, use the message APIs rather than treating serial transport events as a decoded SMS inbox.

### Native Android reception

The incoming `SMS_RECEIVED` receiver exists only while the component is open. Each session retains at most the newest **128 messages**. Closing clears retained messages, unregisters reception and cancels pending operations.

Each `WatchMessagesAsync` enumeration has an independent bounded queue. A watcher that falls behind its 128-message capacity fails explicitly rather than silently dropping data.

`OnReceived` publishes UTF-8 message bytes with the sender number in `SenderInfo`; shared buffered receives are also available. Use the typed message APIs when you need SMS metadata.

## Modem encoding and PDU limits

`CodeType` applies to the external modem path. Android telephony chooses its own encoding.

| `MessageCodeType` | Single-message limit | Notes |
| --- | --- | --- |
| `Bits7` | 160 GSM septets | GSM extension characters consume two septets. This is the default. |
| `Bits8` | 140 octets | Characters must fit the supported byte range. |
| `Unicode` | 70 UCS-2 characters / 140 octets | Surrogate characters, including surrogate-pair emoji, are unsupported. |

The submit codec validates recipient digits, supports a leading `+`, encodes odd-length addresses and rejects unsupported characters or oversized messages.

Automatic multipart submission is not implemented on the modem path. Splitting text into separate submissions produces independent messages; automatic concatenation headers are not supplied.

`SmsPdu.EncodeSubmit` and `SmsPdu.DecodeDeliver` can be used without opening a modem. Delivery decoding handles service-centre/sender addresses, GSM extensions, 8-bit/UCS-2 data, timestamps and user-data headers. Multipart deliveries are decoded per segment and are not automatically reassembled. Compressed or unsupported coding schemes are rejected.

## Settings, lifecycle and errors

| Setting | Purpose |
| --- | --- |
| `Mode` | Permitted send/receive operations; set while closed. |
| `PhoneNumber` | Default recipient for the media `SendAsync` API; opening does not dial it. |
| `PortName`, `BaudRate` and `PortSettings` | External modem transport and serial framing. |
| `PIN` | Optional 4–8 digit SIM PIN for external modem initialization. |
| `CodeType` | Outgoing modem PDU alphabet. |
| `InitializeCommands` | Modem initialization commands; defaults to `AT` and `ATE0`. |
| `ConnectTimeout` | Modem command timeout or native submission-acknowledgement timeout; defaults to 30 seconds. |

`Settings` stores XML; `JsonSettings` stores JSON. Both retain the mode, alphabet, modem settings and configured PIN. The PIN is serialized in **plain text**: protect persisted settings and avoid logging them. Replacing serialized settings requires closed media.

Prefer asynchronous operations and pass cancellation tokens. Modem command timeout/cancellation closes the transport to prevent stale responses from satisfying a later operation. On native Android, closing releases the receive session and cancels pending work.

Sending can fail because of invalid input, unsupported modem encoding, missing/revoked Android permissions, modem rejection, native submission failure, timeout or cancellation. Submission success is not recipient-delivery confirmation.

Events are not UI-dispatched. Keep handlers short, marshal UI updates to the application dispatcher and never hold a Monitor lock across `await`.

The media implements `IGXMediaIconProvider` with its own SMS PNG icon. Each `OpenIcon()` stream is owned by the caller and must be disposed.

Delivery-status reports and phonebook management are not currently exposed as dedicated APIs. On the modem path, `ExecuteCommandAsync` is available for supported modem-specific commands.

## Example application

[Gurux.SMS.Example](Gurux.SMS.Example/Gurux.SMS.Example.csproj) provides its own Core session, Avalonia desktop view and native MAUI view. It includes mode selection, settings, sending, listing, message watching and activity history.

Run the Avalonia desktop example from this repository directory:

~~~powershell
dotnet run --project Gurux.SMS.Example/Gurux.SMS.Example.csproj -f net11.0 -p:BuildDesktop=true -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
~~~

Build the Android example:

~~~powershell
dotnet build Gurux.SMS.Example/Gurux.SMS.Example.csproj -f net11.0-android -p:BuildMobile=true -p:GuruxExamplesBuild=true -p:GeneratePackageOnBuild=false
~~~

Use the matching MAUI workloads for native Windows/Android hosts and Apple tooling on a Mac for iOS. Native Android SMS and an external modem remain distinct transport choices.

## Build and test

Run from this repository directory:

~~~powershell
dotnet build Development/Gurux.SMS.csproj -p:GeneratePackageOnBuild=false
dotnet run --project Tests/Sms.Tests.csproj -p:GeneratePackageOnBuild=false
dotnet run --project ../Gurux.Serial/Tests/Serial.Tests.csproj -p:GeneratePackageOnBuild=false
~~~

[SMS tests](Tests/Sms.Tests.csproj) cover native session, mode and submission-acknowledgement contracts using a substituted operating-system service. The shared [Serial/AT/PDU suite](../Gurux.Serial/Tests/Serial.Tests.csproj) covers modem stream behavior and PDU codecs.

These headless tests do not validate a physical modem, SIM, carrier, recipient delivery or Android hardware. Validate those scenarios on the intended platform and devices.

## License

Gurux.SMS is available under either [GPL-2.0-only](LICENSE) or a commercial license from Gurux Ltd. See [commercial licensing information](LICENSES/LicenseRef-Gurux-Commercial) or contact [Gurux Ltd](https://www.gurux.fi/).
