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
using Gurux.SMS;
using Gurux.Serial.Blazor;

namespace Gurux.SMS.Blazor;
/// <summary>Owns a private unopened media copy until validated settings are committed.</summary>
public sealed class MediaSettingsDraft : IDisposable
{
    /// <summary>Tracks ownership and concurrent source changes.</summary>
    private readonly MediaSettingsSession _session;
    /// <summary>Builds and applies staged controls.</summary>
    private readonly SettingsFields _controls = new();
    /// <summary>Gets the staged controls in display order.</summary>
    public List<SettingsField> Fields => _controls.Fields;
    /// <summary>Gets whether an available serial port is explicitly selected.</summary>
    public bool CanSave => _controls.CanSave;
    /// <summary>Creates an owned transaction over an unopened media copy.</summary>
    /// <param name="source">The caller-owned media.</param>
    /// <param name="copy">The editor-owned media copy.</param>
    private MediaSettingsDraft(IGXMedia source, IGXMedia copy) { _session = new(source, copy); }
    /// <summary>Copies settings without opening or taking ownership of the source.</summary>
    /// <param name="source">The closed media to edit.</param>
    /// <returns>An independently owned settings draft.</returns>
    /// <exception cref="ArgumentException">The media is not the exact supported type.</exception>
    public static MediaSettingsDraft Create(IGXMedia source)
    {
        return Create(source, Gurux.Serial.GXSerial.GetPortNames());
    }
    /// <summary>Copies media settings and offers only ports in the supplied discovery snapshot.</summary>
    /// <param name="source">The closed caller-owned media to edit.</param>
    /// <param name="portNames">Port names captured for this editing session; this array is copied.</param>
    /// <returns>An independently owned settings draft requiring an explicit available port selection.</returns>
    /// <exception cref="ArgumentNullException">The source or port snapshot is null.</exception>
    /// <exception cref="ArgumentException">The media is not the exact supported type.</exception>
    public static MediaSettingsDraft Create(IGXMedia source, string[] portNames)
    {
        ArgumentNullException.ThrowIfNull(portNames);
        ArgumentNullException.ThrowIfNull(source);
        if (source.GetType() != typeof(GXSMS)) { throw new ArgumentException(Properties.Resources.UnsupportedMedia, nameof(source)); }
        var copy = new GXSMS();
        var result = new MediaSettingsDraft(source, copy);
        try
        {
            Gurux.Serial.Blazor.SerialPortFields.Add(result._controls, copy.PortSettings, portNames);
            result._controls.Choice(Properties.Resources.SmsMode, copy.Mode, value => copy.Mode = value);
                result._controls.Text(Properties.Resources.Phonenumber, copy.PhoneNumber, value => copy.PhoneNumber = value);
                result._controls.Text(Properties.Resources.Initializationcommandsoneperline, string.Join('\n', copy.InitializeCommands),
                    value => copy.InitializeCommands = value.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries), multiline: true);
                result._controls.Number(Properties.Resources.Connectiontimeoutseconds, checked((int)copy.ConnectTimeout.TotalSeconds),
                    value => copy.ConnectTimeout = TimeSpan.FromSeconds(value));
                result._controls.Text(Properties.Resources.SIMPIN, copy.PIN, value => copy.PIN = value, password: true);
                result._controls.Choice(Properties.Resources.Messageencoding, copy.CodeType, value => copy.CodeType = value);

            return result;
        }
        catch { result.Dispose(); throw; }
    }
    /// <summary>Validates staged settings and applies them if the closed source has not changed.</summary>
    /// <exception cref="InvalidOperationException">The source opened or changed during editing.</exception>
    /// <exception cref="ObjectDisposedException">The draft was already discarded.</exception>
    /// <exception cref="FormatException">No available port is selected or a staged number is invalid.</exception>
    public void Commit() { _session.Commit(_controls.Apply); }
    /// <summary>Discards and disposes the private copy without disposing the caller's media.</summary>
    public void Dispose()
    {
        foreach (var field in Fields) { if (field.Kind == "password") { field.Value = string.Empty; } }
        _session.Dispose();
    }
}
