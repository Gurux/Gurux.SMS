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
using Microsoft.Maui.Controls;
using Gurux.SMS.Maui.Properties;

namespace Gurux.SMS.Maui;

/// <summary>Connects native controls to an unopened private media copy.</summary>
internal sealed class MediaSettingsDraft : IDisposable
{
    /// <summary>Owns the staged copy and conflict checks.</summary>
    private readonly MediaSettingsSession _session;
    /// <summary>Reads this media's native fields.</summary>
    private readonly SettingsFields _fields;
    /// <summary>Owns the shared serial picker and deferred settings readers.</summary>
    private readonly Gurux.Serial.Maui.SerialSettingsForm? _serial;
    /// <summary>Gets the native form layout.</summary>
    internal View Form => _fields.Layout;
    /// <summary>Copies settings and creates fields without opening a transport.</summary>
    /// <param name="source">The closed media whose settings will be staged.</param>
    internal MediaSettingsDraft(GXSMS source)
        : this(source, Gurux.Serial.GXSerial.GetPortNames)
    {
    }
    /// <summary>Creates staged controls from a port enumeration captured once for this dialog.</summary>
    /// <param name="source">The closed media whose settings will be staged.</param>
    /// <param name="getPortNames">Provides the available port names without changing transport behavior.</param>
    internal MediaSettingsDraft(GXSMS source, Func<string[]> getPortNames)
    {
        var copy = source.CreateSettingsDraft();
        _session = new MediaSettingsSession(source, copy);
        _fields = new SettingsFields();
        try
        {
            SettingsFields fields = _fields;
            if (!source.UsesNativeSms)
            {
                _serial = new Gurux.Serial.Maui.SerialSettingsForm(copy.PortSettings, getPortNames());
                fields.Layout.Children.Add(_serial);
            }
            fields.Choice(Resources.Mode, copy.Mode, value => copy.Mode = value,
                value => value switch
                {
                    SmsMode.SendOnly => Resources.SendOnly,
                    SmsMode.ReceiveOnly => Resources.ReceiveOnly,
                    _ => Resources.SendAndReceive
                });
            fields.Text(Resources.Phone, copy.PhoneNumber, value => copy.PhoneNumber = value);
            if (source.UsesNativeSms) { return; }
            fields.Text(Resources.Commands, string.Join('\n', copy.InitializeCommands), value => copy.InitializeCommands = value.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), multiline: true);
            fields.Number(Resources.Timeout, checked((int)copy.ConnectTimeout.TotalSeconds), value => copy.ConnectTimeout = TimeSpan.FromSeconds(value));
            fields.Text(Resources.Pin, copy.PIN, value => copy.PIN = value, password: true);
            fields.Choice(Resources.Encoding, copy.CodeType, value => copy.CodeType = value);
        }
        catch { _session.Dispose(); throw; }
    }
    /// <summary>Gets whether native settings can be saved or the modem form has a valid port selected.</summary>
    internal bool CanSave => _serial?.CanSave ?? true;
    /// <summary>Notifies the view immediately when serial selection changes save availability.</summary>
    internal event EventHandler? CanSaveChanged
    {
        add { if (_serial != null) { _serial.CanSaveChanged += value; } }
        remove { if (_serial != null) { _serial.CanSaveChanged -= value; } }
    }
    /// <summary>Reads native controls and saves only after validation and conflict checks.</summary>
    /// <exception cref="FormatException">No enumerated port is selected or another input is invalid.</exception>
    internal void Commit() => _session.Commit(() => { _serial?.Read(); _fields.Read(); });
    /// <summary>Releases the unopened private media instance.</summary>
    public void Dispose() => _session.Dispose();
}
