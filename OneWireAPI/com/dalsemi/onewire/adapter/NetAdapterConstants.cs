/*---------------------------------------------------------------------------
 * Copyright (C) 2002 Dallas Semiconductor Corporation, All Rights Reserved.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a
 * copy of this software and associated documentation files (the "Software"),
 * to deal in the Software without restriction, including without limitation
 * the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the
 * Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included
 * in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY,  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL DALLAS SEMICONDUCTOR BE LIABLE FOR ANY CLAIM, DAMAGES
 * OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
 * ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
 * OTHER DEALINGS IN THE SOFTWARE.
 *
 * Except as contained in this notice, the name of Dallas Semiconductor
 * shall not be used except as stated in the Dallas Semiconductor
 * Branding Policy.
 *---------------------------------------------------------------------------
 */
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;

namespace com.dalsemi.onewire.adapter
{

    using com.dalsemi.onewire.logging;
    using System.Threading;
    /// <summary>
    /// Static class for holding all constants related to Network Adapter communications.
    /// This interface is used by both NetAdapterHost and the NetAdapter.  In
    /// addition, the common utility class <code>Connection</code> is defined here.
    /// 
    /// @author SH
    /// @version 1.00
    /// </summary>
    public sealed class NetAdapterConstants
	{
	   public const int versionUID = 1;
	   public const string DEFAULT_PORT = "6161";
	   public const string DEFAULT_SECRET = "Adapter Secret Default";
	   public const string DEFAULT_MULTICAST_GROUP = "228.5.6.7";

	   public const int DEFAULT_MULTICAST_PORT = 6163;

	   public static readonly byte RET_SUCCESS = 0xFF;
	   public static readonly byte RET_FAILURE = 0xF0;

	   public const byte CMD_CLOSECONNECTION = 0x08;
	   public const byte CMD_PINGCONNECTION = 0x09;
	   public const byte CMD_RESET = 0x10;
	   public const byte CMD_PUTBIT = 0x11;
	   public const byte CMD_PUTBYTE = 0x12;
	   public const byte CMD_GETBIT = 0x13;
	   public const byte CMD_GETBYTE = 0x14;
	   public const byte CMD_GETBLOCK = 0x15;
	   public const byte CMD_DATABLOCK = 0x16;
	   public const byte CMD_SETPOWERDURATION = 0x17;
	   public const byte CMD_STARTPOWERDELIVERY = 0x18;
	   public const byte CMD_SETPROGRAMPULSEDURATION = 0x19;
	   public const byte CMD_STARTPROGRAMPULSE = 0x1A;
	   public const byte CMD_STARTBREAK = 0x1B;
	   public const byte CMD_SETPOWERNORMAL = 0x1C;
	   public const byte CMD_SETSPEED = 0x1D;
	   public const byte CMD_GETSPEED = 0x1E;
	   public const byte CMD_BEGINEXCLUSIVE = 0x1F;
	   public const byte CMD_ENDEXCLUSIVE = 0x20;
	   public const byte CMD_FINDFIRSTDEVICE = 0x21;
	   public const byte CMD_FINDNEXTDEVICE = 0x22;
	   public const byte CMD_GETADDRESS = 0x23;
	   public const byte CMD_SETSEARCHONLYALARMINGDEVICES = 0x24;
	   public const byte CMD_SETNORESETSEARCH = 0x25;
	   public const byte CMD_SETSEARCHALLDEVICES = 0x26;
	   public const byte CMD_TARGETALLFAMILIES = 0x27;
	   public const byte CMD_TARGETFAMILY = 0x28;
	   public const byte CMD_EXCLUDEFAMILY = 0x29;
	   public const byte CMD_CANBREAK = 0x2A;
	   public const byte CMD_CANDELIVERPOWER = 0x2B;
	   public const byte CMD_CANDELIVERSMARTPOWER = 0x2C;
	   public const byte CMD_CANFLEX = 0x2D;
	   public const byte CMD_CANHYPERDRIVE = 0x2E;
	   public const byte CMD_CANOVERDRIVE = 0x2F;
	   public const byte CMD_CANPROGRAM = 0x30;

	   public static readonly Connection EMPTY_CONNECTION = new Connection();

       public sealed class Connection
       {
            /// <summary>
            /// socket to host </summary>
            public Windows.Networking.Sockets.StreamSocket sock = null;

            /// <summary>
            /// input stream from socket </summary>
            public Windows.Storage.Streams.DataReader input = null;

            /// <summary>
            /// output stream from socket </summary>
            public Windows.Storage.Streams.DataWriter output = null;

            /// <summary>
            /// Cancellaton token to manage DataReader.LoadAsync()
            /// </summary>
            public System.Threading.CancellationTokenSource cts = null;

            /// <summary>
            /// Non-blocking read implementation
            /// </summary>
            /// <param name="socket"></param>
            /// <param name="size"></param>
            /// <returns></returns>
            public byte[] ReadNonBlocking(Connection conn, uint size)
            {
                try
                {
                    StreamSocket socket = conn.sock;

                    IBuffer buffer = new Windows.Storage.Streams.Buffer(size);
                    var t = Task<byte[]>.Run(async () =>
                    {
                        buffer = await socket.InputStream.ReadAsync(buffer, size, InputStreamOptions.Partial);
                        if (buffer.Length == 0)
                            return null;
                        else
                            return buffer.ToArray();
                    });
                    t.Wait();
                    return t.Result;
                }
                catch (Exception e)
                {
                    OneWireEventSource.Log.Debug("ReadNonBlocking(): " + e.ToString());
                }

                return null;
            }

            /// <summary>
            /// Blocking read implementation
            /// </summary>
            /// <param name="socket"></param>
            /// <param name="size"></param>
            /// <returns></returns>
            public byte[] ReadBlocking(Connection c, uint size)
            {
                try
                {
                    // determine number of bytes to load, if any
                    byte[] res = null;

                    if (size > c.input.UnconsumedBufferLength)
                    {
                        uint len = size - c.input.UnconsumedBufferLength;

                        var t = Task<uint>.Run(async () =>
                        {
                            DataReaderLoadOperation read = c.input.LoadAsync(len);
                            return await read.AsTask<uint>(this.cts.Token);
                        });
                        t.Wait();
                        if (t.Status == TaskStatus.RanToCompletion)
                        {
                            if (t.Result > 0)
                            {
                                res = new byte[size];
                                for (var i = 0; i < res.Length; i++)
                                {
                                    res[i] = c.input.ReadByte();
                                }
                            }
                        }
                        return res;
                    }

                    res = new byte[size];
                    for (var i = 0; i < res.Length; i++)
                    {
                        res[i] = c.input.ReadByte();
                    }

                    return res;
                }
                catch (Exception e)
                {
                    OneWireEventSource.Log.Debug("ReadBlocking(): " + e.ToString());
                }

                return null;
            }
        }

    }
}