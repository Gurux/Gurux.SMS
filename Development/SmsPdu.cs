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
using System.Text;

namespace Gurux.SMS;
/// <summary>Encodes SMS-SUBMIT and decodes SMS-DELIVER protocol data units.</summary>
public static class SmsPdu
{
    /// <summary>Maps GSM default alphabet septets to Unicode characters.</summary>
    const string Alphabet = "@£$¥èéùìòÇ\nØø\rÅåΔ_ΦΓΛΩΠΨΣΘΞ\u001bÆæßÉ !\"#¤%&'()*+,-./0123456789:;<=>?¡ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§¿abcdefghijklmnopqrstuvwxyzäöñüà";
    /// <summary>Maps characters in the GSM extension table to their escaped septet values.</summary>
    static readonly Dictionary<char, byte> _extension = new()
    {
        ['\f'] = 10,
        ['^'] = 20,
        ['{'] = 40,
        ['}'] = 41,
        ['\\'] = 47,
        ['['] = 60,
        ['~'] = 61,
        [']'] = 62,
        ['|'] = 64,
        ['€'] = 101
    };
    /// <summary>Encodes one SMS-SUBMIT PDU using the modem's default service centre.</summary>
    /// <param name="receiver">The destination telephone number, optionally prefixed with a plus sign.</param>
    /// <param name="message">The message text to encode using the selected alphabet.</param>
    /// <param name="type">The SMS alphabet used to encode the outgoing payload.</param>
    /// <returns>The hexadecimal SMS-SUBMIT PDU including the default service centre prefix.</returns>
    /// <exception cref="ArgumentException">The recipient is invalid or the message cannot be represented using the selected alphabet.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The alphabet is unsupported or the payload exceeds the single-message limit.</exception>
    /// <exception cref="ArgumentNullException">The recipient or message is null.</exception>
    /// <exception cref="ArgumentException">The recipient is invalid or the text cannot be represented in the requested alphabet.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The alphabet is unsupported or the message exceeds a single SMS payload.</exception>
    public static string EncodeSubmit(string receiver, string message, MessageCodeType type = MessageCodeType.Bits7)
    {
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(message);
        bool international = receiver.StartsWith('+');
        string number = international ? receiver[1..] : receiver;
        if (number.Length is < 1 or > 20 || number.Any(c => !char.IsAsciiDigit(c)))
        {
            throw new ArgumentException(global::Gurux.SMS.Properties.Resources.Get("AnSMSRecipientMustContain1To20Digits"), nameof(receiver));
        }

        var header = new List<byte>
        {
            0,
            0x11,
            0,
            (byte)number.Length,
            international ? (byte)0x91 : (byte)0x81
        };
        for (int i = 0; i < number.Length; i += 2)
        {
            header.Add((byte)((number[i] - '0') | ((i + 1 < number.Length ? number[i + 1] - '0' : 15) << 4)));
        }

        byte[] payload;
        int length;
        if (type == MessageCodeType.Bits7)
        {
            var septets = new List<byte>();
            foreach (char c in message)
            {
                if (_extension.TryGetValue(c, out byte extended))
                {
                    septets.Add(27);
                    septets.Add(extended);
                }
                else
                {
                    int index = Alphabet.IndexOf(c);
                    if (index < 0 || index == 27)
                    {
                        throw new ArgumentException(global::Gurux.SMS.Properties.Resources.Get("CharacterCannotBeRepresentedInGSM7BitSelectUnicode"), nameof(message));
                    }

                    septets.Add((byte)index);
                }
            }

            length = septets.Count;
            if (length > 160)
            {
                throw new ArgumentOutOfRangeException(nameof(message), global::Gurux.SMS.Properties.Resources.Get("ASingleSMSPermits160GSMSeptets"));
            }

            payload = new byte[(length * 7 + 7) / 8];
            for (int i = 0; i < length; i++)
            {
                int bit = i * 7, offset = bit % 8;
                payload[bit / 8] |= (byte)(septets[i] << offset);
                if (offset > 1)
                {
                    payload[bit / 8 + 1] |= (byte)(septets[i] >> (8 - offset));
                }
            }
        }
        else if (type == MessageCodeType.Unicode)
        {
            if (message.Any(char.IsSurrogate))
            {
                throw new ArgumentException(global::Gurux.SMS.Properties.Resources.Get("SMSUCS2CannotEncodeSurrogateCharacters"), nameof(message));
            }

            payload = Encoding.BigEndianUnicode.GetBytes(message);
            length = payload.Length;
        }
        else if (type == MessageCodeType.Bits8)
        {
            if (message.Any(c => c > 255))
            {
                throw new ArgumentException(global::Gurux.SMS.Properties.Resources.Get("8BitMessagesRequireCharactersInTheByteRange"), nameof(message));
            }

            payload = message.Select(c => (byte)c).ToArray();
            length = payload.Length;
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        if (payload.Length > 140)
        {
            throw new ArgumentOutOfRangeException(nameof(message), global::Gurux.SMS.Properties.Resources.Get("ASingleSMSPermits140OctetsSplitLongerMessagesExplicitly"));
        }

        header.AddRange([0, (byte)type, 0xAA, (byte)length]);
        header.AddRange(payload);
        return Convert.ToHexString(header.ToArray());
    }

    /// <summary>Decodes a validated SMS-DELIVER PDU, including GSM extensions and user-data headers.</summary>
    /// <param name="pdu">The hexadecimal SMS-DELIVER protocol data unit.</param>
    /// <returns>The decoded sender, timestamp, coding and message payload.</returns>
    /// <exception cref="FormatException">The hexadecimal input or encoded PDU fields are malformed or truncated.</exception>
    /// <exception cref="NotSupportedException">The PDU is not SMS-DELIVER or uses an unsupported coding scheme.</exception>
    /// <exception cref="ArgumentNullException">The PDU is null.</exception>
    /// <exception cref="FormatException">The hexadecimal PDU is malformed or truncated.</exception>
    /// <exception cref="NotSupportedException">The PDU type or coding scheme is unsupported.</exception>
    public static GXSMSMessage DecodeDeliver(string pdu)
    {
        ArgumentNullException.ThrowIfNull(pdu);
        byte[] data = Convert.FromHexString(pdu);
        int index = 0;
        // Reads the next PDU byte and advances the shared cursor.
        // Returns: The next byte at the cursor.
        // FormatException: The cursor has reached the end of the PDU.
        byte Read()
        {
            if (index >= data.Length)
            {
                throw new FormatException(global::Gurux.SMS.Properties.Resources.Get("TruncatedSMSPDU"));
            }

            return data[index++];
        }

        // Copies a contiguous PDU segment and advances the shared cursor.
        // count: The number of bytes to consume.
        // Returns: The copied segment.
        // FormatException: The requested segment exceeds the remaining PDU bytes.
        byte[] Take(int count)
        {
            if (count < 0 || count > data.Length - index)
            {
                throw new FormatException(global::Gurux.SMS.Properties.Resources.Get("TruncatedSMSPDU"));
            }

            var bytes = data.AsSpan(index, count).ToArray();
            index += count;
            return bytes;
        }

        // Decodes a telephone number stored as low-nibble-first semi-octets.
        // bytes: The packed address digits.
        // digits: The number of address digits, excluding a trailing filler nibble.
        // Returns: The address digits in dialing order.
        // FormatException: The declared address length exceeds the supplied semi-octets.
        static string Number(byte[] bytes, int digits)
        {
            var result = new StringBuilder();
            foreach (byte b in bytes)
            {
                result.Append((char)('0' + (b & 15)));
                result.Append((char)('0' + (b >> 4)));
            }

            if (digits > result.Length)
            {
                throw new FormatException(global::Gurux.SMS.Properties.Resources.Get("InvalidAddressLength"));
            }

            return result.ToString(0, digits);
        }

        // Unpacks GSM default alphabet septets, including escaped extension characters.
        // bytes: The packed septets and any preceding user data header.
        // count: The number of septets to decode.
        // bitOffset: The bit offset at which message septets begin.
        // Returns: The decoded message text.
        // FormatException: A septet is truncated or the final escape has no extension character.
        static string Unpack(byte[] bytes, int count, int bitOffset = 0)
        {
            var text = new StringBuilder();
            bool escaped = false;
            for (int i = 0; i < count; i++)
            {
                int bit = bitOffset + i * 7, offset = bit % 8;
                if (bit + 7 > bytes.Length * 8)
                {
                    throw new FormatException(global::Gurux.SMS.Properties.Resources.Get("TruncatedGSMSeptets"));
                }

                int value = bytes[bit / 8] >> offset;
                if (offset > 1 && bit / 8 + 1 < bytes.Length)
                {
                    value |= bytes[bit / 8 + 1] << (8 - offset);
                }

                value &= 127;
                if (escaped)
                {
                    char ch = _extension.FirstOrDefault(pair => pair.Value == value).Key;
                    text.Append(ch == 0 ? '�' : ch);
                    escaped = false;
                }
                else if (value == 27)
                {
                    escaped = true;
                }
                else
                {
                    text.Append(Alphabet[value]);
                }
            }

            if (escaped)
            {
                throw new FormatException(global::Gurux.SMS.Properties.Resources.Get("IncompleteGSMExtensionEscape"));
            }

            return text.ToString();
        }

        int centreLength = Read();
        string centre = "";
        if (centreLength != 0)
        {
            byte type = Read();
            var address = Take(centreLength - 1);
            int digits = address.Length * 2;
            if (address.Length > 0 && (address[^1] >> 4) == 15)
            {
                digits--;
            }

            centre = (type == 0x91 ? "+" : "") + Number(address, digits);
        }

        byte first = Read();
        if ((first & 3) != 0)
        {
            throw new NotSupportedException(global::Gurux.SMS.Properties.Resources.Get("OnlySMSDELIVERPDUsAreSupported"));
        }

        int senderLength = Read();
        byte senderType = Read();
        string sender;
        if ((senderType & 0x70) == 0x50)
        {
            var bytes = Take((senderLength + 1) / 2);
            sender = Unpack(bytes, senderLength * 4 / 7);
        }
        else
        {
            sender = (senderType == 0x91 ? "+" : "") + Number(Take((senderLength + 1) / 2), senderLength);
        }

        _ = Read();
        byte dcs = Read();
        if ((dcs & 0x20) != 0 || (dcs & 0xC0) != 0)
        {
            throw new NotSupportedException(global::Gurux.SMS.Properties.Resources.Get("CompressedOrMessageClassSMSCodingIsNotSupported"));
        }

        var coding = (MessageCodeType)(dcs & 12);
        if (coding is not (MessageCodeType.Bits7 or MessageCodeType.Bits8 or MessageCodeType.Unicode))
        {
            throw new NotSupportedException(global::Gurux.SMS.Properties.Resources.Get("UnsupportedSMSCoding"));
        }

        // Decodes a swapped BCD timestamp component and validates both decimal digits.
        // b: The timestamp byte with the tens digit in its low nibble.
        // Returns: The decimal timestamp component between zero and ninety-nine.
        // FormatException: Either nibble contains a nondecimal digit.
        static int Decimal(byte b)
        {
            int result = (b & 15) * 10 + (b >> 4);
            if (result > 99 || (b & 15) > 9 || (b >> 4) > 9)
            {
                throw new FormatException(global::Gurux.SMS.Properties.Resources.Get("InvalidSMSTimestamp"));
            }

            return result;
        }

        int year = Decimal(Read()), month = Decimal(Read()), day = Decimal(Read()), hour = Decimal(Read()), minute = Decimal(Read()), second = Decimal(Read());
        byte zone = Read();
        int quarters = Decimal((byte)(zone & 0xF7));
        var timestamp = new DateTimeOffset(2000 + year, month, day, hour, minute, second, TimeSpan.FromMinutes(quarters * 15 * ((zone & 8) != 0 ? -1 : 1)));
        int length = Read();
        var payload = Take(coding == MessageCodeType.Bits7 ? (length * 7 + 7) / 8 : length);
        int header = 0;
        if ((first & 64) != 0)
        {
            if (payload.Length == 0 || payload[0] + 1 > payload.Length)
            {
                throw new FormatException(global::Gurux.SMS.Properties.Resources.Get("InvalidSMSUserDataHeader"));
            }

            header = payload[0] + 1;
        }

        string text;
        if (coding == MessageCodeType.Bits7)
        {
            int headerSeptets = (header * 8 + 6) / 7;
            if (length < headerSeptets)
            {
                throw new FormatException(global::Gurux.SMS.Properties.Resources.Get("InvalidGSMUserDataLength"));
            }

            text = Unpack(payload, length - headerSeptets, headerSeptets * 7);
        }
        else if (coding == MessageCodeType.Unicode)
        {
            if ((payload.Length - header) % 2 != 0)
            {
                throw new FormatException(global::Gurux.SMS.Properties.Resources.Get("InvalidUCS2Length"));
            }

            text = Encoding.BigEndianUnicode.GetString(payload, header, payload.Length - header);
        }
        else
        {
            text = Encoding.Latin1.GetString(payload, header, payload.Length - header);
        }

        return new GXSMSMessage
        {
            Pdu = Convert.ToHexString(data),
            Data = text,
            PhoneNumber = sender,
            ServiceCenterNumber = centre,
            CodeType = coding,
            Time = timestamp.LocalDateTime
        };
    }
}
