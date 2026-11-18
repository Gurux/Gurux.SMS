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

namespace Gurux.SMS;

/// <summary>Requires successful Android submission acknowledgements for every part of one outgoing SMS.</summary>
internal sealed class SmsSubmission
{
    /// <summary>Serializes acknowledgement updates from platform callbacks.</summary>
    private readonly object _sync = new();
    /// <summary>Tracks acknowledged part indexes without counting duplicate callbacks.</summary>
    private readonly bool[] _acknowledged;
    /// <summary>Completes when every part succeeds or the first part fails.</summary>
    private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    /// <summary>Counts distinct successful acknowledgements.</summary>
    private int _completed;

    /// <summary>Starts acknowledgement tracking for a native split message.</summary>
    /// <param name="parts">The positive number of outgoing parts.</param>
    /// <exception cref="ArgumentOutOfRangeException">The part count is not positive.</exception>
    internal SmsSubmission(int parts)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(parts);
        _acknowledged = new bool[parts];
    }
    /// <summary>Gets the task representing submission, without implying recipient delivery.</summary>
    internal Task Completion => _completion.Task;

    /// <summary>Records a native sent-intent result, ignoring duplicate or invalid indexes.</summary>
    /// <param name="part">The zero-based part index.</param>
    /// <param name="result">Android's result code; -1 is Activity.RESULT_OK.</param>
    internal void CompletePart(int part, int result)
    {
        lock (_sync)
        {
            if (part < 0 || part >= _acknowledged.Length || _acknowledged[part]) { return; }
            _acknowledged[part] = true;
            if (result != -1)
            {
                _completion.TrySetException(new IOException(string.Format(CultureInfo.CurrentCulture,
                    Properties.Resources.Get("SmsSubmissionFailed"), part + 1, result)));
            }
            else if (++_completed == _acknowledged.Length)
            {
                _completion.TrySetResult();
            }
        }
    }
}
