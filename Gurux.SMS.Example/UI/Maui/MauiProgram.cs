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

using Gurux.SMS.Example.Maui;
using Gurux.Serial;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace Gurux.SMS.Example.Host;

/// <summary>Configures the native application, including Android's built-in SMS service.</summary>
public static class MauiProgram
{
    /// <summary>Builds the SMS application using native Android SMS or the default platform modem transport.</summary>
    /// <returns>The configured native application.</returns>
    public static MauiApp CreateMauiApp()
    {
        Func<SerialPortSettings, CancellationToken, ValueTask<Stream>>? serialFactory = null;
        return MauiApp.CreateBuilder()
            .UseMauiApp<MediaExampleApplication>(_ => new MediaExampleApplication(serialFactory))
            .Build();
    }
}
