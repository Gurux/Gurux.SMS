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

#if ANDROID
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Hardware.Usb;
using Gurux.Serial;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MauiApplication = Microsoft.Maui.Controls.Application;

using System.Globalization;
using Gurux.SMS.Example.Properties;

namespace Gurux.SMS.Example.Platforms;

/// <summary>Selects USB CDC ACM or paired Bluetooth serial transport for this application.</summary>
public static class ExampleSerialConnection
{
    /// <summary>Override to select the page belonging to the opening window.</summary>
    public static Func<Page?>? PageProvider { get; set; }

    /// <summary>Creates the serial stream opener used by the modem session.</summary>
    /// <returns>A cancellable USB or Bluetooth stream opener.</returns>
    public static Func<SerialPortSettings, CancellationToken, ValueTask<Stream>> CreateFactory() => OpenAsync;

    /// <summary>Selects and opens the configured USB or paired Bluetooth serial device.</summary>
    /// <param name="settings">Serial parameters updated with the selected device identifier.</param>
    /// <param name="cancellationToken">Cancels selection, permission requests or opening.</param>
    /// <returns>The selected serial transport stream.</returns>
    /// <exception cref="IOException">The selected device or permission request is unavailable.</exception>
    /// <exception cref="UnauthorizedAccessException">Device permissions are denied.</exception>
    /// <exception cref="InvalidOperationException">Bluetooth is disabled or the window is unavailable.</exception>
    /// <exception cref="NotSupportedException">USB services are unavailable.</exception>
    public static async ValueTask<Stream> OpenAsync(SerialPortSettings settings, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var context = Platform.AppContext;
        string name = settings.PortName ?? "";
        if (name.StartsWith("ble:", StringComparison.OrdinalIgnoreCase))
        {
            await EnsureBluetoothPermissionAsync(cancellationToken);
            var adapter = ((BluetoothManager?)context.GetSystemService(Context.BluetoothService))?.Adapter;
            if (adapter?.IsEnabled != true) { throw new InvalidOperationException(ExampleResources.EnableBluetooth); }
            var device = adapter.GetRemoteDevice(name[4..]);
            var profile = settings.BluetoothProfile ?? BluetoothLeProfile.FromDeviceName(device?.Name);
            if (device is null || profile is null) { throw new NotSupportedException(ExampleResources.BluetoothProfileRequired); }
            settings.BluetoothProfile = profile;
            return await AndroidSerialTransports.BluetoothLe(context, device, profile)(settings, cancellationToken);
        }
        bool bluetooth = name.StartsWith("bluetooth:", StringComparison.OrdinalIgnoreCase);
        bool usb = name.StartsWith("usb:", StringComparison.OrdinalIgnoreCase);
        if (!bluetooth && !usb)
        {
            string choice = await ChooseAsync(ExampleResources.SerialTransport, new[] { ExampleResources.UsbTransport, ExampleResources.BluetoothTransport }, cancellationToken);
            bluetooth = choice == ExampleResources.BluetoothTransport;
        }
        if (bluetooth)
        {
            await EnsureBluetoothPermissionAsync(cancellationToken);
            var adapter = ((BluetoothManager?)context.GetSystemService(Context.BluetoothService))?.Adapter;
            if (adapter == null || !adapter.IsEnabled)
            {
                throw new InvalidOperationException(ExampleResources.EnableBluetooth);
            }
            var devices = adapter.BondedDevices?.OrderBy(it => it.Address).ToArray() ?? Array.Empty<BluetoothDevice>();
            var device = await SelectAsync(devices, name, "bluetooth:", it => it.Address ?? "", it => string.Format(CultureInfo.CurrentCulture, ExampleResources.BluetoothDeviceLabel, it.Name ?? ExampleResources.BluetoothDevice, it.Address), cancellationToken);
            settings.PortName = "bluetooth:" + device.Address;
            var profile = string.Equals(name, settings.PortName, StringComparison.OrdinalIgnoreCase)
                ? settings.BluetoothProfile ?? BluetoothLeProfile.FromDeviceName(device.Name)
                : BluetoothLeProfile.FromDeviceName(device.Name);
            settings.BluetoothProfile = profile;
            if (profile != null)
            {
                return await AndroidSerialTransports.BluetoothLe(context, device, profile)(settings, cancellationToken);
            }
            if (device.Type == BluetoothDeviceType.Le)
            {
                throw new NotSupportedException(ExampleResources.BluetoothProfileRequired);
            }
            return await AndroidSerialTransports.BluetoothRfcomm(device)(settings, cancellationToken);
        }
        else
        {
            var manager = (UsbManager?)context.GetSystemService(Context.UsbService)
                ?? throw new NotSupportedException(ExampleResources.UsbUnavailable);
            var devices = manager.DeviceList?.Values.OrderBy(it => it.DeviceName).ToArray() ?? Array.Empty<UsbDevice>();
            var device = await SelectAsync(devices, name, "usb:", it => it.DeviceName, it => string.Format(CultureInfo.CurrentCulture, ExampleResources.UsbDeviceLabel, it.VendorId, it.ProductId, it.DeviceName), cancellationToken);
            await EnsureUsbPermissionAsync(context, manager, device, cancellationToken);
            settings.PortName = "usb:" + device.DeviceName;
            return await AndroidSerialTransports.Usb(manager, device)(settings, cancellationToken);
        }
    }

    /// <summary>Resolves a stored device identifier or asks the user to choose a device.</summary>
    /// <typeparam name="T">The platform device type.</typeparam>
    /// <param name="devices">Devices currently available.</param>
    /// <param name="requested">The persisted device identifier.</param>
    /// <param name="prefix">The transport identifier prefix.</param>
    /// <param name="id">Reads a stable device identifier.</param>
    /// <param name="label">Formats a device for selection.</param>
    /// <param name="token">Cancels selection.</param>
    /// <returns>The configured or newly selected device.</returns>
    /// <exception cref="IOException">No matching device is available.</exception>
    private static async Task<T> SelectAsync<T>(T[] devices, string requested, string prefix, Func<T, string> id, Func<T, string> label, CancellationToken token)
    {
        if (requested.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            string wanted = requested[prefix.Length..];
            foreach (var device in devices)
            {
                if (string.Equals(id(device), wanted, StringComparison.OrdinalIgnoreCase))
                {
                    return device;
                }
            }
            throw new IOException(string.Format(CultureInfo.CurrentCulture, ExampleResources.DeviceUnavailable, requested));
        }
        if (devices.Length == 0)
        {
            throw new IOException(ExampleResources.NoDevices);
        }
        string[] labels = devices.Select(label).ToArray();
        string selected = await ChooseAsync(ExampleResources.SelectSerialDevice, labels, token);
        return devices[Array.IndexOf(labels, selected)];
    }

    /// <summary>Displays a native choice sheet on the owning page.</summary>
    /// <param name="title">The localized selection title.</param>
    /// <param name="choices">The available localized choices.</param>
    /// <param name="token">Cancels waiting for selection.</param>
    /// <returns>The selected choice.</returns>
    /// <exception cref="OperationCanceledException">Selection is canceled.</exception>
    /// <exception cref="InvalidOperationException">The application has no active page.</exception>
    private static async Task<string> ChooseAsync(string title, string[] choices, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        string? selected = await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            token.ThrowIfCancellationRequested();
            var page = PageProvider?.Invoke() ?? MauiApplication.Current?.Windows.FirstOrDefault()?.Page
                ?? throw new InvalidOperationException(ExampleResources.WindowNotReady);
            return await page.DisplayActionSheetAsync(title, ExampleResources.Cancel, null, choices);
        }).WaitAsync(token);
        token.ThrowIfCancellationRequested();
        if (selected == null || !choices.Contains(selected))
        {
            throw new OperationCanceledException(ExampleResources.DeviceSelectionCanceled, token);
        }
        return selected;
    }

    /// <summary>Requests the Android nearby-device permission when required.</summary>
    /// <param name="token">Cancels waiting for permission.</param>
    /// <returns>A task completing after permission is granted.</returns>
    /// <exception cref="UnauthorizedAccessException">Bluetooth permission is denied.</exception>
    private static async Task EnsureBluetoothPermissionAsync(CancellationToken token)
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var status = await MainThread.InvokeOnMainThreadAsync(() => Permissions.RequestAsync<BluetoothConnectPermission>()).WaitAsync(token);
            if (status != PermissionStatus.Granted)
            {
                throw new UnauthorizedAccessException(ExampleResources.BluetoothPermission);
            }
        }
        token.ThrowIfCancellationRequested();
    }

    /// <summary>Describes the Android permission required to connect to paired Bluetooth devices.</summary>
    public sealed class BluetoothConnectPermission : Permissions.BasePlatformPermission
    {
        /// <inheritdoc/>
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            new[] { ("android.permission.BLUETOOTH_CONNECT", true) };
    }

    /// <summary>Requests permission for one USB device using a package-scoped callback.</summary>
    /// <param name="context">The Android application context.</param>
    /// <param name="manager">The USB device service.</param>
    /// <param name="device">The selected USB device.</param>
    /// <param name="token">Cancels waiting for permission.</param>
    /// <returns>A task completing after USB permission is granted.</returns>
    /// <exception cref="IOException">A native permission request cannot be created.</exception>
    /// <exception cref="UnauthorizedAccessException">USB permission is denied.</exception>
    private static async Task EnsureUsbPermissionAsync(Context context, UsbManager manager, UsbDevice device, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if (manager.HasPermission(device))
        {
            return;
        }
        // Unique package-scoped action prevents another pending request from satisfying this one.
        string action = context.PackageName + ".USB_PERMISSION." + Guid.NewGuid().ToString("N");
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var receiver = new UsbPermissionReceiver(action, manager, device, completion);
        using var filter = new IntentFilter(action);
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            context.RegisterReceiver(receiver, filter, ReceiverFlags.NotExported);
        }
        else
        {
#pragma warning disable CA1422
            context.RegisterReceiver(receiver, filter);
#pragma warning restore CA1422
        }
        try
        {
            using var intent = new Intent(action).SetPackage(context.PackageName);
            var flags = OperatingSystem.IsAndroidVersionAtLeast(23) ? PendingIntentFlags.Immutable : (PendingIntentFlags)0;
            using var pending = PendingIntent.GetBroadcast(context, 0, intent, flags)
                ?? throw new IOException(ExampleResources.UsbPermissionRequest);
            using var registration = token.Register(() => completion.TrySetCanceled(token));
            try
            {
                token.ThrowIfCancellationRequested();
                manager.RequestPermission(device, pending);
                if (!await completion.Task.ConfigureAwait(false))
                {
                    throw new UnauthorizedAccessException(ExampleResources.UsbPermissionDenied);
                }
            }
            finally
            {
                pending.Cancel();
            }
        }
        finally
        {
            context.UnregisterReceiver(receiver);
        }
    }

    /// <summary>Completes one USB permission request after checking the actual system permission.</summary>
    /// <param name="action">The unique permission broadcast action.</param>
    /// <param name="manager">The USB service used to verify permission.</param>
    /// <param name="device">The device awaiting permission.</param>
    /// <param name="completion">Receives the verified permission result.</param>
    private sealed class UsbPermissionReceiver(string action, UsbManager manager, UsbDevice device, TaskCompletionSource<bool> completion) : BroadcastReceiver
    {
        /// <inheritdoc/>
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action == action)
            {
                // Check OS permission state directly; do not trust broadcast extras.
                completion.TrySetResult(manager.HasPermission(device));
            }
        }
    }
}
#endif
