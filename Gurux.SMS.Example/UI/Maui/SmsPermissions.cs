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
using Microsoft.Maui.ApplicationModel;
using Gurux.SMS.Example.Maui.Properties;

namespace Gurux.SMS.Example.Maui;

/// <summary>Requests only the runtime permissions needed by the selected native SMS mode.</summary>
internal static class SmsPermissions
{
    /// <summary>Requests sending and reception independently before opening the component.</summary>
    /// <param name="mode">The operations the user selected.</param>
    /// <returns>A task completing when all required permissions have been granted.</returns>
    /// <exception cref="UnauthorizedAccessException">A required permission was denied.</exception>
    internal static async Task EnsureAsync(SmsMode mode)
    {
        if (mode != SmsMode.ReceiveOnly && await Permissions.RequestAsync<SendPermission>() != PermissionStatus.Granted)
        {
            throw new UnauthorizedAccessException(ExampleSettingsResources.SmsPermissionDenied);
        }
        if (mode != SmsMode.SendOnly && await Permissions.RequestAsync<ReceivePermission>() != PermissionStatus.Granted)
        {
            throw new UnauthorizedAccessException(ExampleSettingsResources.SmsPermissionDenied);
        }
    }

    /// <summary>Requests permission to submit SMS without permission to read or receive messages.</summary>
    private sealed class SendPermission : Permissions.BasePlatformPermission
    {
        /// <summary>Creates the sending permission requested by the MAUI permission service.</summary>
        public SendPermission() { }
        /// <inheritdoc/>
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => [(Android.Manifest.Permission.SendSms, true)];
    }

    /// <summary>Requests incoming broadcasts without permission to read the device inbox.</summary>
    private sealed class ReceivePermission : Permissions.BasePlatformPermission
    {
        /// <summary>Creates the receiving permission requested by the MAUI permission service.</summary>
        public ReceivePermission() { }
        /// <inheritdoc/>
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => [(Android.Manifest.Permission.ReceiveSms, true)];
    }
}
#endif
