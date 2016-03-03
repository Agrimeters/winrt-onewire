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
namespace com.dalsemi.onewire.adapter
{
	/// <summary>
	/// Interface for holding all constants related to Network Adapter communications.
	/// This interface is used by both NetAdapterHost and the NetAdapter.  In
	/// addition, the common utility class <code>Connection</code> is defined here.
	/// 
	/// @author SH
	/// @version 1.00
	/// </summary>
	public interface NetAdapterConstants
	{
	   /// <summary>
	   /// Debug message flag </summary>

	   /// <summary>
	   /// version UID, used to detect incompatible host </summary>

	   /// <summary>
	   /// Indicates whether or not to buffer the output (probably always true!) </summary>

	   /// <summary>
	   /// Default port for NetAdapter TCP/IP connection </summary>

	   /// <summary>
	   /// default secret for authentication with the server </summary>

	   /// <summary>
	   /// Address for Multicast group for NetAdapter Datagram packets </summary>

	   /// <summary>
	   /// Default port for NetAdapter Datagram packets </summary>

	   /*------------------------------------------------------------*/
	   /*----- Method Return codes ----------------------------------*/
	   /*------------------------------------------------------------*/
	   /*------------------------------------------------------------*/

	   /*------------------------------------------------------------*/
	   /*----- Method command bytes ---------------------------------*/
	   /*------------------------------------------------------------*/
	   /*------------------------------------------------------------*/
	   /* Raw Data methods ------------------------------------------*/
	   /*------------------------------------------------------------*/
	   /* Power methods ---------------------------------------------*/
	   /*------------------------------------------------------------*/
	   /* Speed methods ---------------------------------------------*/
	   /*------------------------------------------------------------*/
	   /* Network Semaphore methods ---------------------------------*/
	   /*------------------------------------------------------------*/
	   /* Searching methods -----------------------------------------*/
	   /*------------------------------------------------------------*/
	   /* feature methods -------------------------------------------*/
	   /*------------------------------------------------------------*/

	   /// <summary>
	   /// An inner utility class for coupling Socket with I/O streams
	   /// </summary>

	   /// <summary>
	   /// instance for an empty connection, basically it's a NULL object
	   ///  that's safe to synchronize on. 
	   /// </summary>
	}

	public static class NetAdapterConstants_Fields
	{
	   public const bool DEBUG = false;
	   public const int versionUID = 1;
	   public const bool BUFFERED_OUTPUT = true;
	   public const int DEFAULT_PORT = 6161;
	   public const string DEFAULT_SECRET = "Adapter Secret Default";
	   public const string DEFAULT_MULTICAST_GROUP = "228.5.6.7";
	   public const int DEFAULT_MULTICAST_PORT = 6163;
	   public static readonly byte RET_SUCCESS = 0x0FF;
	   public static readonly byte RET_FAILURE = 0x0F0;
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
	   public static readonly NetAdapterConstants_Connection EMPTY_CONNECTION = new NetAdapterConstants_Connection();
	}

	public sealed class NetAdapterConstants_Connection
	{
	  /// <summary>
	  /// socket to host </summary>
	  public System.Net.Sockets.Socket sock = null;
        
	  /// <summary>
	  /// input stream from socket </summary>
	  public Windows.Storage.Streams.DataReader input = null;
        
	  /// <summary>
	  /// output stream from socket </summary>
	  public Windows.Storage.Streams.DataWriter output = null;
	}
}