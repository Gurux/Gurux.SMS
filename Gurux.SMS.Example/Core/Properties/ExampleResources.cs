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

using System.Globalization;
using System.Resources;

namespace Gurux.SMS.Example.Properties;

/// <summary>Provides localized captions, operation messages and activity formats for the application.</summary>
public static class ExampleResources
{
    /// <summary>Gets the caption for clearing displayed history.</summary>
    public static string Clear => Manager.GetString(nameof(Clear), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(Clear);
    /// <summary>Gets the heading for received payloads, separate from diagnostic activity.</summary>
    public static string ReceivedData => Manager.GetString(nameof(ReceivedData), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(ReceivedData);
    /// <summary>Reads the application resources embedded in the core assembly.</summary>
    private static readonly ResourceManager Manager = new("Gurux.SMS.Example.Properties.ExampleResources", typeof(ExampleResources).Assembly);
    /// <summary>Gets the localized text: Gurux SMS example.</summary>
    public static string Title => Manager.GetString(nameof(Title), CultureInfo.CurrentUICulture) ?? nameof(Title);
    /// <summary>Gets the localized text: Opened..</summary>
    public static string Opened => Manager.GetString(nameof(Opened), CultureInfo.CurrentUICulture) ?? nameof(Opened);
    /// <summary>Gets the localized text: Closed..</summary>
    public static string Closed => Manager.GetString(nameof(Closed), CultureInfo.CurrentUICulture) ?? nameof(Closed);
    /// <summary>Gets the localized text: Open.</summary>
    public static string Open => Manager.GetString(nameof(Open), CultureInfo.CurrentUICulture) ?? nameof(Open);
    /// <summary>Gets the localized text: Settings.</summary>
    public static string Settings => Manager.GetString(nameof(Settings), CultureInfo.CurrentUICulture) ?? nameof(Settings);
    /// <summary>Gets the localized text: Close / cancel.</summary>
    public static string Close => Manager.GetString(nameof(Close), CultureInfo.CurrentUICulture) ?? nameof(Close);
    /// <summary>Gets the localized text: Send.</summary>
    public static string Send => Manager.GetString(nameof(Send), CultureInfo.CurrentUICulture) ?? nameof(Send);
    /// <summary>Gets the localized text: Canceled..</summary>
    public static string Canceled => Manager.GetString(nameof(Canceled), CultureInfo.CurrentUICulture) ?? nameof(Canceled);
    /// <summary>Gets the localized text: SMS message text.</summary>
    public static string TextPlaceholder => Manager.GetString(nameof(TextPlaceholder), CultureInfo.CurrentUICulture) ?? nameof(TextPlaceholder);
    /// <summary>Gets the localized text: SMS recipient.</summary>
    public static string Recipient => Manager.GetString(nameof(Recipient), CultureInfo.CurrentUICulture) ?? nameof(Recipient);
    /// <summary>Gets the localized text: List messages.</summary>
    public static string ListMessages => Manager.GetString(nameof(ListMessages), CultureInfo.CurrentUICulture) ?? nameof(ListMessages);
    /// <summary>Gets the localized text: Watch messages.</summary>
    public static string WatchMessages => Manager.GetString(nameof(WatchMessages), CultureInfo.CurrentUICulture) ?? nameof(WatchMessages);
    /// <summary>Gets the localized text: Stop watching.</summary>
    public static string StopWatching => Manager.GetString(nameof(StopWatching), CultureInfo.CurrentUICulture) ?? nameof(StopWatching);
    /// <summary>Gets the localized guidance for selecting an iOS Bluetooth Low Energy device.</summary>
    public static string SerialUnavailable => Manager.GetString(nameof(SerialUnavailable), CultureInfo.CurrentUICulture) ?? nameof(SerialUnavailable);
    /// <summary>Gets the localized text: RX {0}: {1}.</summary>
    public static string Received => Manager.GetString(nameof(Received), CultureInfo.CurrentUICulture) ?? nameof(Received);
    /// <summary>Gets the localized text: RX {0}: {1} | {2}.</summary>
    public static string ReceivedBytes => Manager.GetString(nameof(ReceivedBytes), CultureInfo.CurrentUICulture) ?? nameof(ReceivedBytes);
    /// <summary>Gets the localized text: Error: {0}.</summary>
    public static string Error => Manager.GetString(nameof(Error), CultureInfo.CurrentUICulture) ?? nameof(Error);
    /// <summary>Gets the localized text: Enter an SMS recipient..</summary>
    public static string RecipientRequired => Manager.GetString(nameof(RecipientRequired), CultureInfo.CurrentUICulture) ?? nameof(RecipientRequired);
    /// <summary>Gets the localized text: SMS sent..</summary>
    public static string SmsSent => Manager.GetString(nameof(SmsSent), CultureInfo.CurrentUICulture) ?? nameof(SmsSent);
    /// <summary>Gets the localized text: SMS: {0}.</summary>
    public static string SmsMessage => Manager.GetString(nameof(SmsMessage), CultureInfo.CurrentUICulture) ?? nameof(SmsMessage);
    /// <summary>Gets the localized text: {0:g} {1}: {2}.</summary>
    public static string ListedMessage => Manager.GetString(nameof(ListedMessage), CultureInfo.CurrentUICulture) ?? nameof(ListedMessage);
    /// <summary>Gets the localized text: {0:HH:mm:ss} {1}.</summary>
    public static string LogEntry => Manager.GetString(nameof(LogEntry), CultureInfo.CurrentUICulture) ?? nameof(LogEntry);
    /// <summary>Gets the localized text: USB services are unavailable..</summary>
    public static string UsbUnavailable => Manager.GetString(nameof(UsbUnavailable), CultureInfo.CurrentUICulture) ?? nameof(UsbUnavailable);
    /// <summary>Gets the localized text: Serial transport.</summary>
    public static string SerialTransport => Manager.GetString(nameof(SerialTransport), CultureInfo.CurrentUICulture) ?? nameof(SerialTransport);
    /// <summary>Gets the localized text: USB CDC ACM.</summary>
    public static string UsbTransport => Manager.GetString(nameof(UsbTransport), CultureInfo.CurrentUICulture) ?? nameof(UsbTransport);
    /// <summary>Gets the localized text: Paired Bluetooth Classic.</summary>
    public static string BluetoothTransport => Manager.GetString(nameof(BluetoothTransport), CultureInfo.CurrentUICulture) ?? nameof(BluetoothTransport);
    /// <summary>Gets the localized text: Enable Bluetooth and pair the serial device in Android Settings first..</summary>
    public static string EnableBluetooth => Manager.GetString(nameof(EnableBluetooth), CultureInfo.CurrentUICulture) ?? nameof(EnableBluetooth);
    /// <summary>Gets the localized text: Bluetooth device.</summary>
    public static string BluetoothDevice => Manager.GetString(nameof(BluetoothDevice), CultureInfo.CurrentUICulture) ?? nameof(BluetoothDevice);
    /// <summary>Gets the localized text: {0} ({1}).</summary>
    public static string BluetoothDeviceLabel => Manager.GetString(nameof(BluetoothDeviceLabel), CultureInfo.CurrentUICulture) ?? nameof(BluetoothDeviceLabel);
    /// <summary>Gets the localized text: USB {0:X4}:{1:X4} ({2}).</summary>
    public static string UsbDeviceLabel => Manager.GetString(nameof(UsbDeviceLabel), CultureInfo.CurrentUICulture) ?? nameof(UsbDeviceLabel);
    /// <summary>Gets the localized text: The selected device '{0}' is unavailable. Clear the port field to select another device..</summary>
    public static string DeviceUnavailable => Manager.GetString(nameof(DeviceUnavailable), CultureInfo.CurrentUICulture) ?? nameof(DeviceUnavailable);
    /// <summary>Gets the localized text: No devices are available. Attach a USB CDC ACM device or pair a Bluetooth Classic serial device in Android Settings..</summary>
    public static string NoDevices => Manager.GetString(nameof(NoDevices), CultureInfo.CurrentUICulture) ?? nameof(NoDevices);
    /// <summary>Gets the localized text: Select serial device.</summary>
    public static string SelectSerialDevice => Manager.GetString(nameof(SelectSerialDevice), CultureInfo.CurrentUICulture) ?? nameof(SelectSerialDevice);
    /// <summary>Gets the localized text: The application window is not ready..</summary>
    public static string WindowNotReady => Manager.GetString(nameof(WindowNotReady), CultureInfo.CurrentUICulture) ?? nameof(WindowNotReady);
    /// <summary>Gets the localized text: Cancel.</summary>
    public static string Cancel => Manager.GetString(nameof(Cancel), CultureInfo.CurrentUICulture) ?? nameof(Cancel);
    /// <summary>Gets the localized text: Device selection was canceled..</summary>
    public static string DeviceSelectionCanceled => Manager.GetString(nameof(DeviceSelectionCanceled), CultureInfo.CurrentUICulture) ?? nameof(DeviceSelectionCanceled);
    /// <summary>Gets the localized text: Nearby devices permission is required to use paired Bluetooth serial devices..</summary>
    public static string BluetoothPermission => Manager.GetString(nameof(BluetoothPermission), CultureInfo.CurrentUICulture) ?? nameof(BluetoothPermission);
    /// <summary>Gets the localized text: Cannot create USB permission request..</summary>
    public static string UsbPermissionRequest => Manager.GetString(nameof(UsbPermissionRequest), CultureInfo.CurrentUICulture) ?? nameof(UsbPermissionRequest);
    /// <summary>Gets the localized text: USB device permission was denied..</summary>
    public static string UsbPermissionDenied => Manager.GetString(nameof(UsbPermissionDenied), CultureInfo.CurrentUICulture) ?? nameof(UsbPermissionDenied);
    /// <summary>Gets the message shown when a BLE device has no configured serial characteristic profile.</summary>
    public static string BluetoothProfileRequired => Manager.GetString(nameof(BluetoothProfileRequired), System.Globalization.CultureInfo.CurrentUICulture) ?? nameof(BluetoothProfileRequired);
    /// <summary>Gets the localized caption or guidance: Connection</summary>
    public static string ConnectionMenu => Manager.GetString(nameof(ConnectionMenu), CultureInfo.CurrentUICulture) ?? nameof(ConnectionMenu);

    /// <summary>Gets the localized caption or guidance: View</summary>
    public static string ViewMenu => Manager.GetString(nameof(ViewMenu), CultureInfo.CurrentUICulture) ?? nameof(ViewMenu);

    /// <summary>Gets the localized caption or guidance: Data</summary>
    public static string Data => Manager.GetString(nameof(Data), CultureInfo.CurrentUICulture) ?? nameof(Data);

    /// <summary>Gets the localized caption or guidance: Communication settings</summary>
    public static string CommunicationSettings => Manager.GetString(nameof(CommunicationSettings), CultureInfo.CurrentUICulture) ?? nameof(CommunicationSettings);

    /// <summary>Gets the localized caption or guidance: Activity Log</summary>
    public static string ActivityLog => Manager.GetString(nameof(ActivityLog), CultureInfo.CurrentUICulture) ?? nameof(ActivityLog);

    /// <summary>Gets the localized caption or guidance: Menu</summary>
    public static string Menu => Manager.GetString(nameof(Menu), CultureInfo.CurrentUICulture) ?? nameof(Menu);

    /// <summary>Gets the guidance for modem configuration and the inbox actions in Communication settings.</summary>
    public static string CommunicationInformation => Manager.GetString(nameof(CommunicationInformation), CultureInfo.CurrentUICulture) ?? nameof(CommunicationInformation);

    /// <summary>Gets the localized caption for the native media settings editor.</summary>
    public static string MediaSettings => Manager.GetString(nameof(MediaSettings), CultureInfo.CurrentUICulture) ?? nameof(MediaSettings);

}
