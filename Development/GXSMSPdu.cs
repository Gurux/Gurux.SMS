//
// --------------------------------------------------------------------------
//  Gurux Ltd
// 
//
//
// Filename:        $HeadURL$
//
// Version:         $Revision$,
//                  $Date$
//                  $Author$
//
// Copyright (c) Gurux Ltd
//
//---------------------------------------------------------------------------
//
//  DESCRIPTION
//
// This file is a part of Gurux Device Framework.
//
// Gurux Device Framework is Open Source software; you can redistribute it
// and/or modify it under the terms of the GNU General Public License 
// as published by the Free Software Foundation; version 2 of the License.
// Gurux Device Framework is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of 
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. 
// See the GNU General Public License for more details.
//
// This code is licensed under the GNU General Public License v2. 
// Full text may be retrieved at http://www.gnu.org/licenses/gpl-2.0.txt
//---------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gurux.SMS
{
    class GXSMSPdu
    {
        static string CodeInteger(int value)
	    {
            char[] tmp = new char[2];
            int b = ((byte)(value >> 4));
            tmp[0] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            b = ((byte)(value & 0x0F));
            tmp[1] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            return new string(tmp);
	    }

        static string CodeString(string data)
	    {
            StringBuilder sb = new StringBuilder(data.Length);
            for (int pos = 0; pos < data.Length; pos += 2)
		    {
                sb.Append(data[pos + 1]);
                sb.Append(data[pos]);
		    }
            return sb.ToString();
	    }

        /// <summary>
        /// Convert SMS char to Unicode char.
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        static int ASCIItoSMS(int ch)
        {            
            switch (ch)
            {
                case 0x40: //@
                    ch = 0;
                    break;
                case 0xA3:
                    ch = 1;
                    break;
                case 0x24: //'$'
                    ch = 2;
                    break;
                case 165:
                    ch = 3;
                    break;
                case 0xE8:
                    ch = 4;
                    break;
                case 0xE9: //
                    ch = 5;
                    break;
                case 0xF9: //
                    ch = 6;
                    break;
                case 0xEC: //
                    ch = 7;
                    break;
                case 0xF2: //
                    ch = 8;
                    break;
                case 0xC7:
                    ch = 9;
                    break;
                case 0xD8:
                    ch = 11;
                    break;
                case 0xF8:
                    ch = 12;
                    break;
                case 0xC5: //
                    ch = 14;
                    break;
                case 0xE5: //
                    ch = 15;
                    break;
                case 0x0394:
                    ch = 16;
                    break;
                case 0x5F: //'_'
                    ch = 17;
                    break;
                case 0x03A6:
                    ch = 18;
                    break;
                case 0x0393:
                    ch = 19;
                    break;
                case 0x039B:
                    ch = 20;
                    break;
                case 0x03A9:
                    ch = 21;
                    break;
                case 0x03A0:
                    ch = 22;
                    break;
                case 0x03A8:
                    ch = 23;
                    break;
                case 0x03A3:
                    ch = 24;
                    break;
                case 0x0398:
                    ch = 25;
                    break;
                case 0x039E:
                    ch = 26;
                    break;
                case 0xC6: //
                    ch = 28;
                    break;
                case 0xE6: //
                    ch = 29;
                    break;
                case 0xDF: //
                    ch = 30;
                    break;
                case 0xC9: //
                    ch = 31;
                    break;
                case 0xA4: //
                    ch = 36;
                    break;
                case 0xA1: //
                    ch = 64;
                    break;
                case 0xC4: //
                    ch = 91;
                    break;
                case 0xD6: //
                    ch = 92;
                    break;
                case 0xD1: //
                    ch = 93;
                    break;
                case 0xDC: //
                    ch = 94;
                    break;
                case 167: //Section sign
                    ch = 95;
                    break;
                case 0x00BF: //Inverted question mark.
                    ch = 96;
                    break;
                case 0xE4: //
                    ch = 123;
                    break;
                case 0xF6: //
                    ch = 124;
                    break;
                case 0xF1:
                    ch = 125;
                    break;
                case 0xFC: //
                    ch = 126;
                    break;
                case 0xE0: //
                    ch = 127;
                    break;
                case 12: //FORM FEED
                    ch = 0x1B0A;
                    break;
                case 94: //CIRCUMFLEX ACCENT ^
                    ch = 0x1B14;
                    break;
                case 123: //LEFT CURLY BRACKET {
                    ch = 0x1B28;
                    break;
                case 125: //RIGHT CURLY BRACKET }
                    ch = 0x1B29;
                    break;
                case 92: //REVERSE SOLIDUS (BACKSLASH)
                    ch = 0x1B2F;
                    break;
                case 91: //LEFT SQUARE BRACKET [
                    ch = 0x1B3C;
                    break;
                case 126: //TILDE
                    ch = 0x1B3D;
                    break;
                case 93: //RIGHT SQUARE BRACKET ]
                    ch = 0x1B3E;
                    break;
                case 124: //VERTICAL BAR | 
                    ch = 0x1B40;
                    break;
                case 0x20AC: //EURO SIGN
                    ch = 0x1B65;
                    break;
            }
            return ch;
        }
        
        
        /// <summary>
        /// Convert 8 bits data to the 7 bits data.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        static string Code7Bit(string data)
	    {
            if (string.IsNullOrEmpty(data))
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
		    //Convert ASCII chars to SMS chars.
            int pos;
		    for (pos = 0; pos < data.Length; ++pos)
		    {
			    int val = ASCIItoSMS(data[pos]);
			    //If 16 bits.
			    if ((val & 0xFF00) != 0)
			    {
                    sb.Append((char)((val >> 8) & 0xFF));
                    sb.Append((char)(val & 0xFF));
			    }
			    else //If 8 bits.
			    {
                    sb.Append((char) val);
			    }
		    }
		    string str = sb.ToString();
            string output = "";        
		    for(pos = 0; pos < str.Length; ++pos)
		    {
			    int mask = (1 << ((pos % 8) + 1)) - 1;
			    if (mask == 0xFF)
			    {
				    mask = 1;
				    continue;
			    }
			    int ch1 = str[pos] >> (pos % 8);
                int ch2 = 0;
                if (pos < str.Length - 1)
                {
                    ch2 = str[pos + 1] & mask;
                }
			    ch2 = ch2 << (7 - (pos % 8));
                output += CodeInteger(ch1 | ch2);
		    }            
            return output;
	    }

        static string Code8Bit(string data)
	    {
            if (string.IsNullOrEmpty(data))
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
		    foreach(int it in data)
		    {	
                sb.Append(CodeInteger(it));
		    }
            return sb.ToString();
	    }

        static string CodeUnicode(string data)
	    {
            if (string.IsNullOrEmpty(data))
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
		    foreach(int ch in data)
		    {
                //Add high part.
                sb.Append(CodeInteger((ch & 0xFF00) >> 8));
                //Add low part.
                sb.Append(CodeInteger(ch & 0xFF));
            }
            return sb.ToString();           
	    }

        static public string Code(string receiver, string message, MessageCodeType type)
        {
            if (string.IsNullOrEmpty(receiver))
            {
                throw new ArgumentNullException("Receiver is invalid.");
            }
            receiver = receiver.Trim();
            if (receiver.Length == 0)
            {
                throw new ArgumentNullException("Receiver is invalid.");
            }
            //Length of SMSC information. Here the length is 0, which means that the SMSC stored in the phone should be used. 
            //Note: This octet is optional. On some phones this octet should be omitted! 
            //(Using the SMSC stored in phone is thus implicit) 
            string data = CodeInteger(0x00);
            //First octet of the SMS-SUBMIT message. 
            data += CodeInteger(0x011);
            //TP-Message-Reference. The "00" value here lets the phone set the message reference number itself.  
            data += CodeInteger(0x00);
            //Is phone number give as internal format.
            bool bInternational = (receiver[0] == '+');
            if (bInternational)
            {
                receiver = receiver.Substring(1);                
            }
            //Address-Length. Length of phone number.            
            //If the length of the phone number is odd (11), a trailing F has been added.
            if ((receiver.Length % 2) != 0) 
            {
                receiver += "F";
            }
            data += CodeInteger(receiver.Length);
            //Type-of-Address. (91 indicates international format of the phone number).
            data += CodeInteger(bInternational ? 0x91 : 0x81);
            //The phone number in semi octets.
            data += CodeString(receiver);
            //TP-PID. Protocol identifier 
		    data += CodeInteger(0x00);

		    //TP-DCS. Data coding scheme. This message is coded according to the 7bit default alphabet. 
		    //Having "04" instead of "00" here, would indicate that the TP-User-Data field of this 
		    //message should be interpreted as 8bit rather than 7bit (used in e.g. smart messaging, OTA provisioning etc). 
            string msg;
            if (type == MessageCodeType.Bits7)
		    {
			    data += CodeInteger(0x00);
                msg = Code7Bit(message);
		    }
		    else if (type == MessageCodeType.Bits8)
		    {
			    data += CodeInteger(0x04);
                msg = Code8Bit(message);
		    }
		    else if(type == MessageCodeType.Unicode)
		    {
			    data += CodeInteger(0x08);
                msg = CodeUnicode(message);
		    }
		    else
		    {
                throw new ArgumentOutOfRangeException("Unknown message code type.");
		    }
		    //TP-Validity-Period. "AA" means 4 days. Note: This octet is optional, see bits 4 and 3 of the first octet 
            data += CodeInteger(0xAA);		
		    //TP-User-Data-Length. Length of message. The TP-DCS field indicated 7-bit data, so the length here is the number of septets (10). 
            data += CodeInteger(msg.Length / 2);		
		    //If the TP-DCS field were set to 8-bit data or Unicode, the length would be the number of octets. 
            data += msg;
            return data;
        }

        ///Get integer as Word. bSwap is used to swap lo byte and hibyte when time is get.
	    static int GetInteger(string data, int index)
	    {
            if (data.Length < 2)
		    {
                throw new ArgumentOutOfRangeException("Invalid data");
		    }
            int c = data[index];
            int value = (byte)((c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0')) << 4);
            c = data[index + 1];
            value |= (byte)(c > '9' ? (c > 'Z' ? (c - 'a' + 10) : (c - 'A' + 10)) : (c - '0'));
            return value;
	    }

        static string GetString(string data, int index, int length, bool swap)
	    {
            if (data.Length < 0)
		    {
                throw new ArgumentOutOfRangeException("Invalid data");
		    }
            if (swap)
            {
                char[] bytes = new char[length];
                for (int pos = 0; pos < length; pos += 2)
                {
                    bytes[pos] = data[index + pos + 1];
                    bytes[pos + 1] = data[index + pos];
                }
                return new string(bytes);
            }
            return data.Substring(index, length);
	    }

        //Convert SMS char to Unicode char.
        static int SMStoASCII(int ch, ref bool escch)
        {
            if (escch)
            {
                ch = 0x1B <<8 | ch;
            }
            switch (ch)
            {
                case 0:
                    ch = '@';
                    break;
                case 1:
                    ch = 0xA3;
                    break;
                case 2:
                    ch = '$';
                    break;
                case 3:
                    ch = 165;
                    break;
                case 4:
                    ch = 0xE8;
                    break;
                case 5:
                    ch = 0xE9;
                    break;
                case 6:
                    ch = 0xF9;
                    break;
                case 7:
                    ch = 0xEC;
                    break;
                case 8:
                    ch = 0xF2;
                    break;
                case 9:
                    ch = 0xC7;
                    break;
                case 11:
                    ch = 0xD8;
                    break;
                case 12:
                    ch = 0xF8;
                    break;
                case 14:
                    ch = 0xC5;
                    break;
                case 15:
                    ch = 0xE5;
                    break;
                case 16:
                    ch = 0x0394;
                    break;
                case 17:
                    ch = '_';
                    break;
                case 18:
                    ch = 0x03A6;
                    break;
                case 19:
                    ch = 0x0393;
                    break;
                case 20:
                    ch = 0x039B;
                    break;
                case 21:
                    ch = 0x03A9;
                    break;
                case 22:
                    ch = 0x03A0;
                    break;
                case 23:
                    ch = 0x03A8;
                    break;
                case 24:
                    ch = 0x03A3;
                    break;
                case 25:
                    ch = 0x0398;
                    break;
                case 26:
                    ch = 0x039E;
                    break;
                case 28:
                    ch = 0xC6;
                    break;
                case 29:
                    ch = 0xE6;
                    break;
                case 30:
                    ch = 0xDF;
                    break;
                case 31:
                    ch = 0xC9;
                    break;
                case 36:
                    ch = 0xA4;
                    break;
                case 64:
                    ch = 0xA1;
                    break;
                case 91:
                    ch = 0xC4;
                    break;
                case 92:
                    ch = 0xD6;
                    break;
                case 93:
                    ch = 0xD1;
                    break;
                case 94:
                    ch = 0xDC;
                    break;
                case 95: //Section sign.
                    ch = 167;
                    break;
                case 96: //Inverted question mark.
                    ch = 0x40;
                    break;
                case 123:
                    ch = 0xE4;
                    break;
                case 124:
                    ch = 0xF6;
                    break;
                case 125:
                    ch = 0xF1;
                    break;
                case 126:
                    ch = 0xFC;
                    break;
                case 127:
                    ch = 0xE0;
                    break;
                case 0x1B0A: //FORM FEED
                    ch = 12;
                    break;
                case 0x1B14: //CIRCUMFLEX ACCENT ^
                    ch = 94;
                    break;
                case 0x1B28: //LEFT CURLY BRACKET {
                    ch = 123;
                    break;
                case 0x1B29: //RIGHT CURLY BRACKET }
                    ch = 125;
                    break;
                case 0x1B2F: //REVERSE SOLIDUS (BACKSLASH)
                    ch = 92;
                    break;
                case 0x1B3C: //LEFT SQUARE BRACKET [
                    ch = 91;
                    break;
                case 0x1B3D: //TILDE
                    ch = 126;
                    break;
                case 0x1B3E: //RIGHT SQUARE BRACKET ]
                    ch = 93;
                    break;
                case 0x1B40: //VERTICAL BAR | 
                    ch = 124;
                    break;
                case 0x1B65: //EURO SIGN
                    ch = 0x20AC;
                    break;
            }
            escch = ch == 27;
            if (escch)
            {
                return 0;
            }
            if ((ch & 0xFF00) == 0xFF00)
            {
                ch = ch & 0xFF;
            }
            return ch;
        }

        ///Convert 7 bits data to the 8 bits data.
	    static string Decode7Bit(string data)
	    {
            int[] bytes = new int[data.Length / 2];
		    //Convert data to the byte array.
            for (int pos = 0; pos < bytes.Length; ++pos)
            {
                bytes[pos] = GetInteger(data, 2 * pos);
            }
		    //Get data at 7 bits at the time
            int newData = 0;
            int off = 0;
            int ch = 0;
            int mask = 1;
            long chCnt = 0;
            bool escch = false;
            StringBuilder sb = new StringBuilder();
            for (int pos = 0; pos < bytes.Length; ++pos)
            {
                newData = bytes[pos] << off;
                for (long bitpos = 0; bitpos < 8; ++bitpos)
                {
                    if (mask == 0x80)
                    {
                        int newCh = SMStoASCII(ch, ref escch);
                        if (newCh != 0)
                        {
                            sb.Append((char)newCh);
                        }
                        off = ch = 0;
                        mask = 1;
                        newData = newData >> 7;
                        if (++chCnt == data.Length)
                        {
                            break;
                        }
                    }
                    ch |= (newData & mask);
                    mask = mask << 1;
                    ++off;
                }
            }		   
		    if (mask == 0x80)
		    {
                int newCh = SMStoASCII(ch, ref escch);
			    if (newCh != 0)
			    {
                    sb.Append((char)newCh);
			    }
		    }
		    return sb.ToString();
	    }

        /// <summary>
        /// Convert hex string to string data.
        /// </summary>
        static string Decode8Bit(string data)
	    {
            StringBuilder sb = new StringBuilder();
            for(int pos = 0; pos != data.Length; pos += 2)
            {
                sb.Append((char) GetInteger(data, pos));
            }		    
		    return sb.ToString();
	    }

        
	    /// <summary>
        /// Decode UNICODE data.
	    /// </summary>
        static string DecodeUnicode(string data)
	    {
            StringBuilder sb = new StringBuilder();
            for (int pos = 0; pos != data.Length; pos += 4)
            {
                int value = GetInteger(data, pos) << 8;
                value |= GetInteger(data, pos + 2);
                sb.Append((char) value);
            }
            return sb.ToString();
	    }

        public static void Encode(string data, GXSMSMessage msg)
        {
            if (data.Length < 1)
            {
                throw new ArgumentOutOfRangeException("Invalid data.");
            }            
            int index = 0;
            // Service Center Number length
            int ServCenterlen = GetInteger(data, index);
            index += 2;
            //Type-of-address of the SMSC.
            int SMSCType = GetInteger(data, index);
            index += 2;
            //Get Service Center number            
            if (ServCenterlen > 0)
            {
                int sz = 2 * ServCenterlen - 1;
                if (data.Length < index + sz)
                {
                    throw new ArgumentOutOfRangeException("Invalid data.");
                }                
                string serviceCenterNumber = data.Substring(index, sz);            
                if (SMSCType == 0x91) //If international format.
                {
                    serviceCenterNumber = "+" + serviceCenterNumber;
                }
                msg.ServiceCenterNumber = serviceCenterNumber;
                index += sz;
            }
            //First octet of the SMS-DELIVER PDU
            int SMSDeliver = GetInteger(data, index);
            index += 2;
            //Length of the sender number
            int SenderLen = GetInteger(data, index);
            index += 2;
            SenderLen += SenderLen % 2;
            //Type-of-address of the sender number.
            int SenderType = GetInteger(data, index);
            index += 2;
            //Get Sender number            
            string phoneNumber = GetString(data, index, SenderLen, true); 
            if (SenderType == 0x91) //If international format.
            {
                phoneNumber = "+" + phoneNumber;
            }
            msg.PhoneNumber = phoneNumber;
            index += SenderLen;
            //TP-PID. Protocol identifier.
            int ProtocolID = GetInteger(data, index);
            index += 2;
            //TP-DCS Data coding scheme		
            msg.CodeType = (MessageCodeType)GetInteger(data, index);
            index += 2;
            int year = GetInteger(data, index);
            index += 2;
            if (year != 0xAA)
            {
                year += 2000;                
                int month = GetInteger(data, index);
                index += 2;
                int day = GetInteger(data, index);
                index += 2;
                int hour = GetInteger(data, index);
                index += 2;
                int minute = GetInteger(data, index);
                index += 2;
                int second = GetInteger(data, index);
                index += 2;
                int timezone = GetInteger(data, index);
                index += 2;
                //TODO: lisää time zone.
                //DateTime dt = new DateTime(year, month, day, hour, minute, second);
            }
            //TP-UDL. User data length, length of message. 
            // The TP-DCS field indicated 7-bit data, so the length here is the number of septets (10). 
            // If the TP-DCS field were set to indicate 8-bit data or Unicode, the length would be the number of octets (9). 
            int DataLen = GetInteger(data, index);
            index += 2;
            string buff = GetString(data, index, 2 * DataLen, false); //data.Substring(index, DataLen);
            index += DataLen;
            //TP-UD. Message 8-bit octets representing 7-bit data.
            //When DataCodingScheme = 0, PDU code is coded from 7bit charactor (see GSM 03.38).
            if (msg.CodeType == MessageCodeType.Bits7)
            {
                msg.Data = Decode7Bit(buff);
            }
            // When DataCodingScheme = 4, PDU code is coded using 8 bits codec...
            else if (msg.CodeType == MessageCodeType.Bits8)
            {
                msg.Data = Decode8Bit(buff);
            }
            // When DataCodingScheme = 8, PDU code is coded from Unicode charactor (see GSM 03.38).
            else if (msg.CodeType == MessageCodeType.Unicode)
            {
                msg.Data = DecodeUnicode(buff);
            }
            else
            {
                throw new ArgumentOutOfRangeException("Invalid data coding scheme");
            }            
        }
    }
}
