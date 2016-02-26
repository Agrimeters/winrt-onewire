using System;
using System.Collections;
using System.Threading;

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


	using com.dalsemi.onewire;
	using com.dalsemi.onewire.utils;

	/// <summary>
	/// <P>NetAdapterHost is the host (or server) component for a network-based
	/// DSPortAdapter.  It actually wraps the hardware DSPortAdapter and handles
	/// connections from outside sources (NetAdapter) who want to access it.</P>
	/// 
	/// <P>NetAdapterHost is designed to be run in a thread, waiting for incoming
	/// connections.  You can run this in the same thread as your main program or
	/// you can establish the connections yourself (presumably using some higher
	/// level of security) and then call the <code>handleConnection(Socket)</code> </summary>
	/// {<seealso cref= #handleConnection(Socket)}.</P>
	/// 
	/// <P>Once a NetAdapter is connected with the host, a version check is performed
	/// followed by a simple authentication step.  The authentication is dependent
	/// upon a secret shared between the NetAdapter and the host.  Both will use
	/// a default value, that each will agree with if you don't provide a secret
	/// of your own.  To set the secret, add the following line to your
	/// onewire.properties file:
	/// <ul>
	///    <li>NetAdapter.secret="This is my custom secret"</li>
	/// </ul>
	/// Optionally, the secret can be set by calling the <code>setSecret(String)</code> </seealso>
	/// {<seealso cref= #setSecret(String)}</P>
	/// 
	/// <P>The NetAdapter and NetAdapterHost support multicast broadcasts for
	/// automatic discovery of compatible servers on your LAN.  To start the
	/// multicast listener for this NetAdapterHost, call the
	/// <code>createMulticastListener()</code> method </seealso>
	/// {<seealso cref= #createMulticastListener()}.</P>
	/// 
	/// <P>For information on creating the client component, see the JavaDocs
	/// for the  <seealso cref="com.dalsemi.onewire.adapter.NetAdapter NetAdapter"/>.
	/// </seealso>
	/// <seealso cref= NetAdapter
	/// 
	/// @author SH
	/// @version    1.00, 9 Jan 2002 </seealso>
	public class NetAdapterHost : Runnable, NetAdapterConstants
	{
	   /// <summary>
	   /// random number generator, used to issue challenges to client </summary>
	   protected internal static readonly Random rand = new Random();

	   /// <summary>
	   /// The adapter this NetAdapter will proxy too </summary>
	   protected internal DSPortAdapter adapter = null;

	   /// <summary>
	   /// The server socket for listening for connections </summary>
	   protected internal ServerSocket serverSocket = null;

	   /// <summary>
	   /// secret for authentication with the server </summary>
	   protected internal sbyte[] netAdapterSecret = null;

	   /// <summary>
	   /// boolean flags for stopping the host </summary>
	   protected internal volatile bool hostStopped = false, hostRunning = false;

	   /// <summary>
	   /// boolean flag to indicate whether or not the host is single or multi-threaded </summary>
	   protected internal bool singleThreaded = true;

	   /// <summary>
	   /// Map of all Service threads created, only for multi-threaded </summary>
	   protected internal Hashtable hashHandlers = null;

	   /// <summary>
	   /// Optional, listens for datagram packets from potential clients </summary>
	   protected internal MulticastListener multicastListener = null;

	   /// <summary>
	   /// timeout for socket receive, in seconds </summary>
	   protected internal int timeoutInSeconds = 30;

	   /// <summary>
	   /// <P>Creates an instance of a NetAdapterHost which wraps the provided
	   /// adapter.  The host listens on the default port as specified by
	   /// NetAdapterConstants.</P>
	   /// 
	   /// <P>Note that the secret used for authentication is the value specified
	   /// in the onewire.properties file as "NetAdapter.secret=mySecret".
	   /// To set the secret to another value, use the
	   /// <code>setSecret(String)</code> method.</P>
	   /// </summary>
	   /// <param name="adapter"> DSPortAdapter that this NetAdapterHost will proxy
	   /// commands to.
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterHost(DSPortAdapter adapter) : this(adapter, NetAdapterConstants_Fields.DEFAULT_PORT, false)
	   {
	   }

	   /// <summary>
	   /// <P>Creates a single-threaded instance of a NetAdapterHost which wraps the
	   /// provided adapter.  The host listens on the specified port.</P>
	   /// 
	   /// <P>Note that the secret used for authentication is the value specified
	   /// in the onewire.properties file as "NetAdapter.secret=mySecret".
	   /// To set the secret to another value, use the
	   /// <code>setSecret(String)</code> method.</P>
	   /// </summary>
	   /// <param name="adapter"> DSPortAdapter that this NetAdapterHost will proxy
	   /// commands to. </param>
	   /// <param name="listenPort"> the TCP/IP port to listen on for incoming connections
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterHost(DSPortAdapter adapter, int listenPort) : this(adapter, listenPort, false)
	   {
	   }

	   /// <summary>
	   /// <P>Creates an (optionally multithreaded) instance of a NetAdapterHost
	   /// which wraps the provided adapter.  The listen port is set to the
	   /// default port as defined in NetAdapterConstants.</P>
	   /// 
	   /// <P>Note that the secret used for authentication is the value specified
	   /// in the onewire.properties file as "NetAdapter.secret=mySecret".
	   /// To set the secret to another value, use the
	   /// <code>setSecret(String)</code> method.</P>
	   /// </summary>
	   /// <param name="adapter"> DSPortAdapter that this NetAdapterHost will proxy
	   /// commands to. </param>
	   /// <param name="multiThread"> if true, multiple TCP/IP connections are allowed
	   /// to interact simulataneously with this adapter.
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterHost(DSPortAdapter adapter, bool multiThread) : this(adapter, NetAdapterConstants_Fields.DEFAULT_PORT, multiThread)
	   {
	   }

	   /// <summary>
	   /// <P>Creates an (optionally multi-threaded) instance of a NetAdapterHost which
	   /// wraps the provided adapter.  The host listens on the specified port.</P>
	   /// 
	   /// <P>Note that the secret used for authentication is the value specified
	   /// in the onewire.properties file as "NetAdapter.secret=mySecret".
	   /// To set the secret to another value, use the
	   /// <code>setSecret(String)</code> method.</P>
	   /// </summary>
	   /// <param name="adapter"> DSPortAdapter that this NetAdapterHost will proxy
	   /// commands to. </param>
	   /// <param name="listenPort"> the TCP/IP port to listen on for incoming connections </param>
	   /// <param name="multiThread"> if true, multiple TCP/IP connections are allowed
	   /// to interact simulataneously with this adapter.
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterHost(DSPortAdapter adapter, int listenPort, bool multiThread)
	   {
		  //save reference to adapter
		  this.adapter = adapter;

		  // create the server socket
		  this.serverSocket = new ServerSocket(listenPort);

		  // set multithreaded flag
		  this.singleThreaded = !multiThread;
		  if (multiThread)
		  {
			 this.hashHandlers = new Hashtable();
			 this.timeoutInSeconds = 0;
		  }

		  // get the shared secret
		  string secret = OneWireAccessProvider.getProperty("NetAdapter.secret");
		  if (!string.ReferenceEquals(secret, null))
		  {
			 netAdapterSecret = secret.GetBytes();
		  }
		  else
		  {
			 netAdapterSecret = NetAdapterConstants_Fields.DEFAULT_SECRET.GetBytes();
		  }
	   }

	   /// <summary>
	   /// <P>Creates an instance of a NetAdapterHost which wraps the provided
	   /// adapter.  The host listens on the default port as specified by
	   /// NetAdapterConstants.</P>
	   /// 
	   /// <P>Note that the secret used for authentication is the value specified
	   /// in the onewire.properties file as "NetAdapter.secret=mySecret".
	   /// To set the secret to another value, use the
	   /// <code>setSecret(String)</code> method.</P>
	   /// </summary>
	   /// <param name="adapter"> DSPortAdapter that this NetAdapterHost will proxy
	   /// commands to. </param>
	   /// <param name="serverSock"> the ServerSocket for incoming connections
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterHost(DSPortAdapter adapter, ServerSocket serverSock) : this(adapter, serverSock, false)
	   {
	   }

	   /// <summary>
	   /// <P>Creates an (optionally multi-threaded) instance of a NetAdapterHost which
	   /// wraps the provided adapter.  The host listens on the specified port.</P>
	   /// 
	   /// <P>Note that the secret used for authentication is the value specified
	   /// in the onewire.properties file as "NetAdapter.secret=mySecret".
	   /// To set the secret to another value, use the
	   /// <code>setSecret(String)</code> method.</P>
	   /// </summary>
	   /// <param name="adapter"> DSPortAdapter that this NetAdapterHost will proxy
	   /// commands to. </param>
	   /// <param name="serverSock"> the ServerSocket for incoming connections </param>
	   /// <param name="multiThread"> if true, multiple TCP/IP connections are allowed
	   /// to interact simulataneously with this adapter.
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterHost(DSPortAdapter adapter, ServerSocket serverSock, bool multiThread)
	   {
		  //save reference to adapter
		  this.adapter = adapter;

		  // create the server socket
		  this.serverSocket = serverSock;

		  // set multithreaded flag
		  this.singleThreaded = !multiThread;
		  if (multiThread)
		  {
			 this.hashHandlers = new Hashtable();
			 this.timeoutInSeconds = 0;
		  }

		  // get the shared secret
		  string secret = OneWireAccessProvider.getProperty("NetAdapter.secret");
		  if (!string.ReferenceEquals(secret, null))
		  {
			 netAdapterSecret = secret.GetBytes();
		  }
		  else
		  {
			 netAdapterSecret = NetAdapterConstants_Fields.DEFAULT_SECRET.GetBytes();
		  }
	   }

	   /// <summary>
	   /// Sets the secret used for authenticating incoming client connections.
	   /// </summary>
	   /// <param name="secret"> The shared secret information used for authenticating
	   ///               incoming client connections. </param>
	   public virtual string Secret
	   {
		   set
		   {
			  netAdapterSecret = value.GetBytes();
		   }
	   }

	   /// <summary>
	   /// Creates a Multicast Listener to allow NetAdapter clients to discover
	   /// this NetAdapterHost automatically.  Uses defaults for Multicast group
	   /// and port.
	   /// </summary>
	   public virtual void createMulticastListener()
	   {
		  createMulticastListener(NetAdapterConstants_Fields.DEFAULT_MULTICAST_PORT);
	   }

	   /// <summary>
	   /// Creates a Multicast Listener to allow NetAdapter clients to discover
	   /// this NetAdapterHost automatically.  Uses default for Multicast group.
	   /// </summary>
	   /// <param name="port"> The port the Multicast socket will receive packets on </param>
	   public virtual void createMulticastListener(int port)
	   {
		  string group = OneWireAccessProvider.getProperty("NetAdapter.MulticastGroup");
		  if (string.ReferenceEquals(group, null))
		  {
			 group = NetAdapterConstants_Fields.DEFAULT_MULTICAST_GROUP;
		  }
		  createMulticastListener(port, group);
	   }

	   /// <summary>
	   /// Creates a Multicast Listener to allow NetAdapter clients to discover
	   /// this NetAdapterHost automatically.
	   /// </summary>
	   /// <param name="port"> The port the Multicast socket will receive packets on </param>
	   /// <param name="group"> The group the Multicast socket will join </param>
	   public virtual void createMulticastListener(int port, string group)
	   {
		  if (multicastListener == null)
		  {
			 // 4 bytes for integer versionUID
			 sbyte[] versionBytes = Convert.toByteArray(NetAdapterConstants_Fields.versionUID);

			 // this byte array is 5 because length is used to determine different
			 // packet types by client
			 sbyte[] listenPortBytes = new sbyte[5];
			 Convert.toByteArray(serverSocket.LocalPort, listenPortBytes, 0, 4);
			 listenPortBytes[4] = unchecked((sbyte)0x0FF);

			 multicastListener = new MulticastListener(port, group, versionBytes, listenPortBytes);
			 (new Thread(multicastListener)).Start();
		  }
	   }


	   /// <summary>
	   /// Run method for threaded NetAdapterHost.  Maintains server socket which
	   /// waits for incoming connections.  Whenever a connection is received
	   /// launches it services the socket or (optionally) launches a new thread
	   /// for servicing the socket.
	   /// </summary>
	   public virtual void run()
	   {
		  hostRunning = true;
		  while (!hostStopped)
		  {
			 Socket sock = null;
			 try
			 {
				sock = serverSocket.accept();
				handleConnection(sock);
			 }
			 catch (IOException)
			 {
				try
				{
				   if (sock != null)
				   {
					  sock.close();
				   }
				}
				catch (IOException)
				{
					;
				}
			 }
		  }
		  hostRunning = false;
	   }

	   /// <summary>
	   /// Handles a socket connection.  If single-threaded, the connection is
	   /// serviced in the current thread.  If multi-threaded, a new thread is
	   /// created for servicing this connection.
	   /// </summary>
	   public virtual void handleConnection(Socket sock)
	   {
		  SocketHandler sh = new SocketHandler(this, sock);
		  if (singleThreaded)
		  {
			 // single-threaded
			 sh.run();
		  }
		  else
		  {
			 // multi-threaded
			 Thread t = new Thread(sh);
			 t.Start();
			 lock (hashHandlers)
			 {
				hashHandlers[t] = sh;
			 }
		  }
	   }
	   /// <summary>
	   /// Stops all threads and kills the server socket.
	   /// </summary>
	   public virtual void stopHost()
	   {
		  this.hostStopped = true;
		  try
		  {
			 this.serverSocket.close();
		  }
		  catch (IOException)
		  {
			  ;
		  }

		  // wait for run method to quit, with a timeout of 1 second
		  int i = 0;
		  while (hostRunning && i++<100)
		  {
			 try
			 {
			 Thread.Sleep(10);
			 }
		 catch (Exception)
		 {
			 ;
		 }
		  }

		  if (!singleThreaded)
		  {
			 lock (hashHandlers)
			 {
				System.Collections.IEnumerator e = hashHandlers.Values.GetEnumerator();
				while (e.MoveNext())
				{
				   ((SocketHandler)e.Current).stopHandler();
				}
			 }
		  }

		  if (multicastListener != null)
		  {
			 multicastListener.stopListener();
		  }

		  // ensure that there is no exclusive use of the adapter
		  adapter.endExclusive();
	   }

	   /// <summary>
	   /// Transmits the versionUID of the current NetAdapter protocol to
	   /// the client connection.  If it matches the clients versionUID,
	   /// the client returns RET_SUCCESS.
	   /// </summary>
	   /// <param name="conn"> The connection to send/receive data. </param>
	   /// <returns> <code>true</code> if the versionUID matched. </returns>
	   private bool sendVersionUID(NetAdapterConstants_Connection conn)
	   {
		  // write server version
		  conn.output.writeInt(NetAdapterConstants_Fields.versionUID);
		  conn.output.flush();

		  sbyte retVal = conn.input.readByte();

		  return (retVal == NetAdapterConstants_Fields.RET_SUCCESS);
	   }

	   /// <summary>
	   /// Reads in command from client and calls the appropriate handler function.
	   /// </summary>
	   /// <param name="conn"> The connection to send/receive data.
	   ///  </param>
	   private void processRequests(NetAdapterConstants_Connection conn)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("\n------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

		  // get the next command
		  sbyte cmd = 0x00;

		  cmd = conn.input.readByte();

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("CMD received: " + cmd.ToString("x"));
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

		  try
		  {
			 // ... and fire the appropriate method
			 switch (cmd)
			 {
				/* Connection keep-alive and close commands */
				case NetAdapterConstants_Fields.CMD_PINGCONNECTION:
				   // no-op, might update timer of some sort later
				   conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
				   conn.output.flush();
				   break;
				case NetAdapterConstants_Fields.CMD_CLOSECONNECTION:
				   close(conn);
				   break;
				/* Raw Data commands */
				case NetAdapterConstants_Fields.CMD_RESET:
				   adapterReset(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_PUTBIT:
				   adapterPutBit(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_PUTBYTE:
				   adapterPutByte(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_GETBIT:
				   adapterGetBit(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_GETBYTE:
				   adapterGetByte(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_GETBLOCK:
				   adapterGetBlock(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_DATABLOCK:
				   adapterDataBlock(conn);
				   break;
				/* Power methods */
				case NetAdapterConstants_Fields.CMD_SETPOWERDURATION:
				   adapterSetPowerDuration(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_STARTPOWERDELIVERY:
				   adapterStartPowerDelivery(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_SETPROGRAMPULSEDURATION:
				   adapterSetProgramPulseDuration(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_STARTPROGRAMPULSE:
				   adapterStartProgramPulse(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_STARTBREAK:
				   adapterStartBreak(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_SETPOWERNORMAL:
				   adapterSetPowerNormal(conn);
				   break;
				/* Speed methods */
				case NetAdapterConstants_Fields.CMD_SETSPEED:
				   adapterSetSpeed(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_GETSPEED:
				   adapterGetSpeed(conn);
				   break;
				/* Network Semaphore methods */
				case NetAdapterConstants_Fields.CMD_BEGINEXCLUSIVE:
				   adapterBeginExclusive(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_ENDEXCLUSIVE:
				   adapterEndExclusive(conn);
				   break;
				/* Searching methods */
				case NetAdapterConstants_Fields.CMD_FINDFIRSTDEVICE:
				   adapterFindFirstDevice(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_FINDNEXTDEVICE:
				   adapterFindNextDevice(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_GETADDRESS:
				   adapterGetAddress(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_SETSEARCHONLYALARMINGDEVICES:
				   adapterSetSearchOnlyAlarmingDevices(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_SETNORESETSEARCH:
				   adapterSetNoResetSearch(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_SETSEARCHALLDEVICES:
				   adapterSetSearchAllDevices(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_TARGETALLFAMILIES:
				   adapterTargetAllFamilies(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_TARGETFAMILY:
				   adapterTargetFamily(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_EXCLUDEFAMILY:
				   adapterExcludeFamily(conn);
				   break;
				/* feature methods */
				case NetAdapterConstants_Fields.CMD_CANBREAK:
				   adapterCanBreak(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_CANDELIVERPOWER:
				   adapterCanDeliverPower(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_CANDELIVERSMARTPOWER:
				   adapterCanDeliverSmartPower(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_CANFLEX:
				   adapterCanFlex(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_CANHYPERDRIVE:
				   adapterCanHyperdrive(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_CANOVERDRIVE:
				   adapterCanOverdrive(conn);
				   break;
				case NetAdapterConstants_Fields.CMD_CANPROGRAM:
				   adapterCanProgram(conn);
				   break;
				default:
				   //System.out.println("Unkown command: " + cmd);
				   break;
			 }
		  }
		  catch (OneWireException owe)
		  {
			 conn.output.writeByte(NetAdapterConstants_Fields.RET_FAILURE);
			 conn.output.writeUTF(owe.ToString());
			 conn.output.flush();
		  }
	   }

	   /// <summary>
	   /// Closes the provided connection.
	   /// </summary>
	   /// <param name="conn"> The connection to send/receive data. </param>
	   private void close(NetAdapterConstants_Connection conn)
	   {
		  try
		  {
			 if (conn.sock != null)
			 {
				conn.sock.close();
			 }
		  }
		  catch (IOException)
		  { //drain
	;
		  }

		  conn.sock = null;
		  conn.input = null;
		  conn.output = null;

		  // ensure that there is no exclusive use of the adapter
		  adapter.endExclusive();
	   }

	   //--------
	   //-------- Finding iButton/1-Wire device options
	   //--------

	   private void adapterFindFirstDevice(NetAdapterConstants_Connection conn)
	   {
		  bool b = adapter.findFirstDevice();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   findFirstDevice returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   private void adapterFindNextDevice(NetAdapterConstants_Connection conn)
	   {
		  bool b = adapter.findNextDevice();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   findNextDevice returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   private void adapterGetAddress(NetAdapterConstants_Connection conn)
	   {
		  // read in the address
		  sbyte[] address = new sbyte[8];
		  // call getAddress
		  adapter.getAddress(address);

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   adapter.getAddress(byte[]) called, speed=" + adapter.Speed);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.write(address, 0, 8);
		  conn.output.flush();
	   }

	   private void adapterSetSearchOnlyAlarmingDevices(NetAdapterConstants_Connection conn)
	   {
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   setSearchOnlyAlarmingDevices called, speed=" + adapter.Speed);
		  }

		  adapter.setSearchOnlyAlarmingDevices();

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterSetNoResetSearch(NetAdapterConstants_Connection conn)
	   {
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   setNoResetSearch called, speed=" + adapter.Speed);
		  }

		  adapter.setNoResetSearch();

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterSetSearchAllDevices(NetAdapterConstants_Connection conn)
	   {
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   setSearchAllDevices called, speed=" + adapter.Speed);
		  }

		  adapter.setSearchAllDevices();

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterTargetAllFamilies(NetAdapterConstants_Connection conn)
	   {
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   targetAllFamilies called, speed=" + adapter.Speed);
		  }

		  adapter.targetAllFamilies();

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterTargetFamily(NetAdapterConstants_Connection conn)
	   {
		  // get the number of family codes to expect
		  int len = conn.input.readInt();
		  // get the family codes
		  sbyte[] family = new sbyte[len];
		  conn.input.readFully(family, 0, len);

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   targetFamily called, speed=" + adapter.Speed);
			 Debug.WriteLine("      families: " + Convert.toHexString(family));
		  }

		  // call targetFamily
		  adapter.targetFamily(family);

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterExcludeFamily(NetAdapterConstants_Connection conn)
	   {
		  // get the number of family codes to expect
		  int len = conn.input.readInt();
		  // get the family codes
		  sbyte[] family = new sbyte[len];
		  conn.input.readFully(family, 0, len);

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   excludeFamily called, speed=" + adapter.Speed);
			 Debug.WriteLine("      families: " + Convert.toHexString(family));
		  }

		  // call excludeFamily
		  adapter.excludeFamily(family);

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   //--------
	   //-------- 1-Wire Network Semaphore methods
	   //--------

	   private void adapterBeginExclusive(NetAdapterConstants_Connection conn)
	   {
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   adapter.beginExclusive called, speed=" + adapter.Speed);
		  }

		  // get blocking boolean
		  bool blocking = conn.input.readBoolean();
		  // call beginExclusive
		  bool b = adapter.beginExclusive(blocking);

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("      adapter.beginExclusive returned " + b);
		  }
	   }

	   private void adapterEndExclusive(NetAdapterConstants_Connection conn)
	   {
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   adapter.endExclusive called, speed=" + adapter.Speed);
		  }

		  // call endExclusive
		  adapter.endExclusive();

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   //--------
	   //-------- Primitive 1-Wire Network data methods
	   //--------

	   private void adapterReset(NetAdapterConstants_Connection conn)
	   {
		  int i = adapter.reset();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   reset, speed=" + adapter.Speed + ", returned " + i);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeInt(i);
		  conn.output.flush();
	   }

	   private void adapterPutBit(NetAdapterConstants_Connection conn)
	   {
		  // get the value of the bit
		  bool bit = conn.input.readBoolean();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   putBit called, speed=" + adapter.Speed);
			 Debug.WriteLine("      bit=" + bit);
		  }

		  adapter.putBit(bit);

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterPutByte(NetAdapterConstants_Connection conn)
	   {
		  // get the value of the byte
		  sbyte b = conn.input.readByte();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   putByte called, speed=" + adapter.Speed);
			 Debug.WriteLine("      byte=" + Convert.toHexString(b));
		  }

		  adapter.putByte(b);

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterGetBit(NetAdapterConstants_Connection conn)
	   {
		  bool bit = adapter.Bit;

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   getBit called, speed=" + adapter.Speed);
			 Debug.WriteLine("      bit=" + bit);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(bit);
		  conn.output.flush();
	   }

	   private void adapterGetByte(NetAdapterConstants_Connection conn)
	   {
		  int b = adapter.Byte;

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   getByte called, speed=" + adapter.Speed);
			 Debug.WriteLine("      byte=" + Convert.toHexString((sbyte)b));
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeByte(b);
		  conn.output.flush();
	   }
	   private void adapterGetBlock(NetAdapterConstants_Connection conn)
	   {
		  // get the number requested
		  int len = conn.input.readInt();
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   getBlock called, speed=" + adapter.Speed);
			 Debug.WriteLine("      len=" + len);
		  }

		  // get the bytes
		  sbyte[] b = adapter.getBlock(len);

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("      returned: " + Convert.toHexString(b));
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.write(b, 0, len);
		  conn.output.flush();
	   }

	   private void adapterDataBlock(NetAdapterConstants_Connection conn)
	   {
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   DataBlock called, speed=" + adapter.Speed);
		  }
		  // get the number to block
		  int len = conn.input.readInt();
		  // get the bytes to block
		  sbyte[] b = new sbyte[len];
		  conn.input.readFully(b, 0, len);

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("      " + len + " bytes");
			 Debug.WriteLine("      Send: " + Convert.toHexString(b));
		  }

		  // do the block
		  adapter.dataBlock(b, 0, len);

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("      Recv: " + Convert.toHexString(b));
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.write(b, 0, len);
		  conn.output.flush();
	   }

	   //--------
	   //-------- 1-Wire Network power methods
	   //--------

	   private void adapterSetPowerDuration(NetAdapterConstants_Connection conn)
	   {
		  // get the time factor value
		  int timeFactor = conn.input.readInt();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   setPowerDuration called, speed=" + adapter.Speed);
			 Debug.WriteLine("      timeFactor=" + timeFactor);
		  }

		  // call setPowerDuration
		  adapter.PowerDuration = timeFactor;

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterStartPowerDelivery(NetAdapterConstants_Connection conn)
	   {
		  // get the change condition value
		  int changeCondition = conn.input.readInt();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   startPowerDelivery called, speed=" + adapter.Speed);
			 Debug.WriteLine("      changeCondition=" + changeCondition);
		  }

		  // call startPowerDelivery
		  bool success = adapter.startPowerDelivery(changeCondition);

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(success);
		  conn.output.flush();
	   }

	   private void adapterSetProgramPulseDuration(NetAdapterConstants_Connection conn)
	   {
		  // get the time factor value
		  int timeFactor = conn.input.readInt();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   setProgramPulseDuration called, speed=" + adapter.Speed);
			 Debug.WriteLine("      timeFactor=" + timeFactor);
		  }

		  // call setProgramPulseDuration
		  adapter.ProgramPulseDuration = timeFactor;

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterStartProgramPulse(NetAdapterConstants_Connection conn)
	   {
		  // get the change condition value
		  int changeCondition = conn.input.readInt();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   startProgramPulse called, speed=" + adapter.Speed);
			 Debug.WriteLine("      changeCondition=" + changeCondition);
		  }

		  // call startProgramPulse();
		  bool success = adapter.startProgramPulse(changeCondition);

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(success);
		  conn.output.flush();
	   }

	   private void adapterStartBreak(NetAdapterConstants_Connection conn)
	   {
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   startBreak called, speed=" + adapter.Speed);
		  }

		  // call startBreak();
		  adapter.startBreak();

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterSetPowerNormal(NetAdapterConstants_Connection conn)
	   {
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   setPowerNormal called, speed=" + adapter.Speed);
		  }

		  // call setPowerNormal
		  adapter.setPowerNormal();

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   //--------
	   //-------- 1-Wire Network speed methods
	   //--------

	   private void adapterSetSpeed(NetAdapterConstants_Connection conn)
	   {
		  // get the value of the new speed
		  int speed = conn.input.readInt();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   setSpeed called, speed=" + adapter.Speed);
			 Debug.WriteLine("      speed=" + speed);
		  }

		  // do the setSpeed
		  adapter.Speed = speed;

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterGetSpeed(NetAdapterConstants_Connection conn)
	   {
		  // get the adapter speed
		  int speed = adapter.Speed;

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   getSpeed called, speed=" + adapter.Speed);
			 Debug.WriteLine("      speed=" + speed);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeInt(speed);
		  conn.output.flush();
	   }


	   //--------
	   //-------- Adapter feature methods
	   //--------

	   private void adapterCanOverdrive(NetAdapterConstants_Connection conn)
	   {
		  bool b = adapter.canOverdrive();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   canOverdrive returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   private void adapterCanHyperdrive(NetAdapterConstants_Connection conn)
	   {
		  bool b = adapter.canHyperdrive();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   canHyperDrive returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   private void adapterCanFlex(NetAdapterConstants_Connection conn)
	   {
		  bool b = adapter.canFlex();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   canFlex returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   private void adapterCanProgram(NetAdapterConstants_Connection conn)
	   {
		  bool b = adapter.canProgram();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   canProgram returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   private void adapterCanDeliverPower(NetAdapterConstants_Connection conn)
	   {
		  bool b = adapter.canDeliverPower();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   canDeliverPower returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   private void adapterCanDeliverSmartPower(NetAdapterConstants_Connection conn)
	   {
		  bool b = adapter.canDeliverSmartPower();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   canDeliverSmartPower returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   private void adapterCanBreak(NetAdapterConstants_Connection conn)
	   {
		  bool b = adapter.canBreak();

		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   canBreak returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   //--------
	   //-------- Inner classes
	   //--------

	   /// <summary>
	   /// Private inner class for servicing new connections.
	   /// Can be run in it's own thread or in the same thread.
	   /// </summary>
	   private class SocketHandler : Runnable
	   {
		   private readonly NetAdapterHost outerInstance;

		  /// <summary>
		  /// The connection that is being serviced.
		  /// </summary>
		  internal NetAdapterConstants_Connection conn;

		  /// <summary>
		  /// indicates whether or not the handler is currently running
		  /// </summary>
		  internal volatile bool handlerRunning = false;

		  /// <summary>
		  /// Constructor for socket servicer.  Creates the input and output
		  /// streams and send's the version of this host to the client
		  /// connection.
		  /// </summary>
		  public SocketHandler(NetAdapterHost outerInstance, Socket sock)
		  {
			  this.outerInstance = outerInstance;
			 // set socket timeout to 10 seconds
			 sock.SoTimeout = outerInstance.timeoutInSeconds * 1000;

			 // create the connection object
			 conn = new NetAdapterConstants_Connection();
			 conn.sock = sock;
			 conn.input = new DataInputStream(conn.sock.InputStream);
			 if (NetAdapterConstants_Fields.BUFFERED_OUTPUT)
			 {
				conn.output = new DataOutputStream(new BufferedOutputStream(conn.sock.OutputStream));
			 }
			 else
			 {
				conn.output = new DataOutputStream(conn.sock.OutputStream);
			 }

			 // first thing transmitted should be version info
			 if (!outerInstance.sendVersionUID(conn))
			 {
				throw new IOException("send version failed");
			 }

			 // authenticate the client
			 sbyte[] chlg = new sbyte[8];
			 rand.NextBytes(chlg);
			 conn.output.write(chlg);
			 conn.output.flush();

			 // compute the crc of the secret and the challenge
			 int crc = CRC16.compute(outerInstance.netAdapterSecret, 0);
			 crc = CRC16.compute(chlg, crc);
			 int answer = conn.input.readInt();
			 if (answer != crc)
			 {
				conn.output.writeByte(NetAdapterConstants_Fields.RET_FAILURE);
				conn.output.writeUTF("Client Authentication Failed");
				conn.output.flush();
				throw new IOException("authentication failed");
			 }
			 else
			 {
				conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
				conn.output.flush();
			 }
		  }

		  /// <summary>
		  /// Run method for socket Servicer.
		  /// </summary>
		  public virtual void run()
		  {
			 handlerRunning = true;
			 try
			 {
				while (!outerInstance.hostStopped && conn.sock != null)
				{
				   outerInstance.processRequests(conn);
				}
			 }
			 catch (Exception t)
			 {
				if (NetAdapterConstants_Fields.DEBUG)
				{
				   Debug.WriteLine(t.ToString());
				   Debug.Write(t.StackTrace);
				}
				outerInstance.close(conn);
			 }
			 handlerRunning = false;

			 if (!outerInstance.hostStopped && !outerInstance.singleThreaded)
			 {
				lock (outerInstance.hashHandlers)
				{
				   // thread finished running without being stopped.
				   // politely remove it from the hashtable.
				   outerInstance.hashHandlers.Remove(Thread.CurrentThread);
				}
			 }
		  }

		  /// <summary>
		  /// Waits for handler to finish, with a timeout.
		  /// </summary>
		  public virtual void stopHandler()
		  {
			 int i = 0;
			 int timeout = 3000;
			 while (handlerRunning && i++<timeout)
			 {
				try
				{
				Thread.Sleep(10);
				}
			catch (Exception)
			{
				;
			}
			 }
		  }
	   }

	   //--------
	   //-------- Default Main Method, for launching server with defaults
	   //--------
	   /// <summary>
	   /// A Default Main Method, for launching NetAdapterHost getting the
	   /// default adapter with the OneWireAccessProvider and listening on
	   /// the default port specified by DEFAULT_PORT.
	   /// </summary>
	   public static void Main(string[] args)
	   {
		  DSPortAdapter adapter = com.dalsemi.onewire.OneWireAccessProvider.DefaultAdapter;

		  NetAdapterHost host = new NetAdapterHost(adapter, true);

		  Debug.WriteLine("Starting Multicast Listener");
		  host.createMulticastListener();

		  Debug.WriteLine("Starting NetAdapter Host");
		  (new Thread(host)).Start();

		  //if(System.in!=null)
		  //{
		  //   System.out.println("\nPress Enter to Shutdown");
		  //   (new BufferedReader(new InputStreamReader(System.in))).readLine();
		  //   host.stopHost();
		  //   System.exit(1);
		  //}
	   }
	}

}