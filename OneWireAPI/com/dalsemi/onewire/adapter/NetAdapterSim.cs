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
	/// <P>NetAdapterSim is the host (or server) component for a network-based
	/// DSPortAdapter.  It actually wraps the hardware DSPortAdapter and handles
	/// connections from outside sources (NetAdapter) who want to access it.</P>
	/// 
	/// <P>NetAdapterSim is designed to be run in a thread, waiting for incoming
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
	/// <P>The NetAdapter and NetAdapterSim support multicast broadcasts for
	/// automatic discovery of compatible servers on your LAN.  To start the
	/// multicast listener for this NetAdapterSim, call the
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
	public class NetAdapterSim : Runnable, NetAdapterConstants
	{
	   protected internal static bool SIM_DEBUG = false;

	   /// <summary>
	   /// random number generator, used to issue challenges to client </summary>
	   protected internal static readonly Random rand = new Random();

	   /// <summary>
	   /// Log file </summary>
	   protected internal PrintWriter logFile;

	   /// <summary>
	   /// exec command, command string to start the simulator </summary>
	   protected internal string execCommand;

	   protected internal Process process;
	   protected internal System.IO.StreamReader processOutput;
	   protected internal System.IO.StreamReader processError;
	   protected internal System.IO.StreamWriter processInput;

	   /// <summary>
	   /// fake address, returned from all search or getAddress commands </summary>
	   protected internal sbyte[] fakeAddress = null;

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
	   /// <P>Creates an instance of a NetAdapterSim which wraps the provided
	   /// adapter.  The host listens on the default port as specified by
	   /// NetAdapterConstants.</P>
	   /// 
	   /// <P>Note that the secret used for authentication is the value specified
	   /// in the onewire.properties file as "NetAdapter.secret=mySecret".
	   /// To set the secret to another value, use the
	   /// <code>setSecret(String)</code> method.</P>
	   /// </summary>
	   /// <param name="adapter"> DSPortAdapter that this NetAdapterSim will proxy
	   /// commands to.
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterSim(string execCmd, string logFilename) : this(execCmd, logFilename, NetAdapterConstants_Fields.DEFAULT_PORT, false)
	   {
	   }

	   /// <summary>
	   /// <P>Creates a single-threaded instance of a NetAdapterSim which wraps the
	   /// provided adapter.  The host listens on the specified port.</P>
	   /// 
	   /// <P>Note that the secret used for authentication is the value specified
	   /// in the onewire.properties file as "NetAdapter.secret=mySecret".
	   /// To set the secret to another value, use the
	   /// <code>setSecret(String)</code> method.</P>
	   /// </summary>
	   /// <param name="adapter"> DSPortAdapter that this NetAdapterSim will proxy
	   /// commands to. </param>
	   /// <param name="listenPort"> the TCP/IP port to listen on for incoming connections
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterSim(string execCmd, sbyte[] fakeAddress, string logFile, int listenPort) : this(execCmd, logFile, listenPort, false)
	   {
	   }

	   /// <summary>
	   /// <P>Creates an (optionally multithreaded) instance of a NetAdapterSim
	   /// which wraps the provided adapter.  The listen port is set to the
	   /// default port as defined in NetAdapterConstants.</P>
	   /// 
	   /// <P>Note that the secret used for authentication is the value specified
	   /// in the onewire.properties file as "NetAdapter.secret=mySecret".
	   /// To set the secret to another value, use the
	   /// <code>setSecret(String)</code> method.</P>
	   /// </summary>
	   /// <param name="adapter"> DSPortAdapter that this NetAdapterSim will proxy
	   /// commands to. </param>
	   /// <param name="multiThread"> if true, multiple TCP/IP connections are allowed
	   /// to interact simulataneously with this adapter.
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterSim(string execCmd, string logFilename, bool multiThread) : this(execCmd, logFilename, NetAdapterConstants_Fields.DEFAULT_PORT, multiThread)
	   {
	   }

	   /// <summary>
	   /// <P>Creates an (optionally multi-threaded) instance of a NetAdapterSim which
	   /// wraps the provided adapter.  The host listens on the specified port.</P>
	   /// 
	   /// <P>Note that the secret used for authentication is the value specified
	   /// in the onewire.properties file as "NetAdapter.secret=mySecret".
	   /// To set the secret to another value, use the
	   /// <code>setSecret(String)</code> method.</P>
	   /// </summary>
	   /// <param name="adapter"> DSPortAdapter that this NetAdapterSim will proxy
	   /// commands to. </param>
	   /// <param name="listenPort"> the TCP/IP port to listen on for incoming connections </param>
	   /// <param name="multiThread"> if true, multiple TCP/IP connections are allowed
	   /// to interact simulataneously with this adapter.
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterSim(string execCmd, string logFilename, int listenPort, bool multiThread)
	   {
		  // save references to file and command
		  this.execCommand = execCmd;
		  this.process = Runtime.Runtime.exec(execCmd);
		  this.processOutput = new System.IO.StreamReader(this.process.InputStream);
		  this.processError = new System.IO.StreamReader(this.process.ErrorStream);
		  this.processInput = new System.IO.StreamWriter(this.process.OutputStream);

		  // wait until process is ready
		  int complete = 0;
		  while (complete < 2)
		  {
			 string line = processOutput.ReadLine();
			 if (complete == 0 && line.IndexOf("read ok (data=17)", StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
			 if (complete == 1 && line.IndexOf(PROMPT, StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
		  }

		  if (!string.ReferenceEquals(logFilename, null))
		  {
			 this.logFile = new PrintWriter(new System.IO.StreamWriter(logFilename), true);
		  }

		   // Make sure we loaded the address of the device
		  simulationGetAddress();

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
	   /// <P>Creates an instance of a NetAdapterSim which wraps the provided
	   /// adapter.  The host listens on the default port as specified by
	   /// NetAdapterConstants.</P>
	   /// 
	   /// <P>Note that the secret used for authentication is the value specified
	   /// in the onewire.properties file as "NetAdapter.secret=mySecret".
	   /// To set the secret to another value, use the
	   /// <code>setSecret(String)</code> method.</P>
	   /// </summary>
	   /// <param name="adapter"> DSPortAdapter that this NetAdapterSim will proxy
	   /// commands to. </param>
	   /// <param name="serverSock"> the ServerSocket for incoming connections
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterSim(string execCmd, string logFilename, ServerSocket serverSock) : this(execCmd, logFilename, serverSock, false)
	   {
	   }

	   /// <summary>
	   /// <P>Creates an (optionally multi-threaded) instance of a NetAdapterSim which
	   /// wraps the provided adapter.  The host listens on the specified port.</P>
	   /// 
	   /// <P>Note that the secret used for authentication is the value specified
	   /// in the onewire.properties file as "NetAdapter.secret=mySecret".
	   /// To set the secret to another value, use the
	   /// <code>setSecret(String)</code> method.</P>
	   /// </summary>
	   /// <param name="adapter"> DSPortAdapter that this NetAdapterSim will proxy
	   /// commands to. </param>
	   /// <param name="serverSock"> the ServerSocket for incoming connections </param>
	   /// <param name="multiThread"> if true, multiple TCP/IP connections are allowed
	   /// to interact simulataneously with this adapter.
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterSim(string execCmd, string logFilename, ServerSocket serverSock, bool multiThread)
	   {
		  // save references to file and command
		  this.execCommand = execCmd;
		  this.process = Runtime.Runtime.exec(execCmd);
		  this.processOutput = new System.IO.StreamReader(this.process.InputStream);
		  this.processError = new System.IO.StreamReader(this.process.ErrorStream);
		  this.processInput = new System.IO.StreamWriter(this.process.OutputStream);

		  // wait  until process is ready
		  int complete = 0;
		  while (complete < 2)
		  {
			 string line = processOutput.ReadLine();
			 if (complete == 0 && line.IndexOf("read ok (data=17)", StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
			 if (complete == 1 && line.IndexOf(PROMPT, StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
		  }

		  if (!string.ReferenceEquals(logFilename, null))
		  {
			 this.logFile = new PrintWriter(new System.IO.StreamWriter(logFilename), true);
		  }

		   // Make sure we loaded the address of the device
		  simulationGetAddress();

		  // save reference to the server socket
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
	   /// this NetAdapterSim automatically.  Uses defaults for Multicast group
	   /// and port.
	   /// </summary>
	   public virtual void createMulticastListener()
	   {
		  createMulticastListener(NetAdapterConstants_Fields.DEFAULT_MULTICAST_PORT);
	   }

	   /// <summary>
	   /// Creates a Multicast Listener to allow NetAdapter clients to discover
	   /// this NetAdapterSim automatically.  Uses default for Multicast group.
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
	   /// this NetAdapterSim automatically.
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
	   /// Run method for threaded NetAdapterSim.  Maintains server socket which
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
				// reset time of last command, so we don't simulate a bunch of
				// unneccessary time
				timeOfLastCommand = DateTimeHelperClass.CurrentUnixTimeMillis();
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
		  //adapter.endExclusive();
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

	   protected internal long timeOfLastCommand = 0;
	   protected internal const long IGNORE_TIME_MIN = 2;
	   protected internal const long IGNORE_TIME_MAX = 1000;
	   /// <summary>
	   /// Reads in command from client and calls the appropriate handler function.
	   /// </summary>
	   /// <param name="conn"> The connection to send/receive data.
	   ///  </param>
	   private void processRequests(NetAdapterConstants_Connection conn)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
		  if (logFile != null)
		  {
			 logFile.println("\n------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

		  // get the next command
		  sbyte cmd = 0x00;

		  cmd = conn.input.readByte();

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
		  if (logFile != null)
		  {
			 logFile.println("CMD received: " + cmd.ToString("x"));
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

		  if (cmd == NetAdapterConstants_Fields.CMD_PINGCONNECTION)
		  {
			 // no-op, might update timer of some sort later
			 simulationPing(1000);
			 conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
			 conn.output.flush();
		  }
		  else
		  {
			 long timeDelta = DateTimeHelperClass.CurrentUnixTimeMillis() - timeOfLastCommand;
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.println("general: timeDelta=" + timeDelta);
			 }
			 if (timeDelta >= IGNORE_TIME_MIN && timeDelta <= IGNORE_TIME_MAX)
			 {
				// do something with timeDelta
				simulationPing(timeDelta);
			 }

			 try
			 {
				// ... and fire the appropriate method
				switch (cmd)
				{
				   /* Connection keep-alive and close commands */
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
					  if (SIM_DEBUG && logFile != null)
					  {
						 logFile.println("Unkown command received: " + cmd);
					  }
					  break;
				}
			 }
			 catch (OneWireException owe)
			 {
				if (SIM_DEBUG && logFile != null)
				{
				   logFile.println("Exception: " + owe.ToString());
				}
				conn.output.writeByte(NetAdapterConstants_Fields.RET_FAILURE);
				conn.output.writeUTF(owe.ToString());
				conn.output.flush();
			 }
			 timeOfLastCommand = DateTimeHelperClass.CurrentUnixTimeMillis();
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
		  //adapter.endExclusive();
	   }

	   //--------
	   //-------- Finding iButton/1-Wire device options
	   //--------

	   private void adapterFindFirstDevice(NetAdapterConstants_Connection conn)
	   {
		  bool b = true; //adapter.findFirstDevice();

		  if (logFile != null)
		  {
			 logFile.println("   findFirstDevice returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   private void adapterFindNextDevice(NetAdapterConstants_Connection conn)
	   {
		  bool b = false; //adapter.findNextDevice();

		  if (logFile != null)
		  {
			 logFile.println("   findNextDevice returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   private void adapterGetAddress(NetAdapterConstants_Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.println("   adapter.getAddress(byte[]) called");
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.write(fakeAddress, 0, 8);
		  conn.output.flush();
	   }

	   private void adapterSetSearchOnlyAlarmingDevices(NetAdapterConstants_Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.println("   setSearchOnlyAlarmingDevices called");
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterSetNoResetSearch(NetAdapterConstants_Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.println("   setNoResetSearch called");
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterSetSearchAllDevices(NetAdapterConstants_Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.println("   setSearchAllDevices called");
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterTargetAllFamilies(NetAdapterConstants_Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.println("   targetAllFamilies called");
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterTargetFamily(NetAdapterConstants_Connection conn)
	   {
		  // get the number of family codes to expect
		  int len = conn.input.readInt();
		  // get the family codes
		  sbyte[] family = new sbyte[len];
		  conn.input.readFully(family, 0, len);

		  if (logFile != null)
		  {
			 logFile.println("   targetFamily called");
			 logFile.println("      families: " + Convert.toHexString(family));
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterExcludeFamily(NetAdapterConstants_Connection conn)
	   {
		  // get the number of family codes to expect
		  int len = conn.input.readInt();
		  // get the family codes
		  sbyte[] family = new sbyte[len];
		  conn.input.readFully(family, 0, len);

		  if (logFile != null)
		  {
			 logFile.println("   excludeFamily called");
			 logFile.println("      families: " + Convert.toHexString(family));
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   //--------
	   //-------- 1-Wire Network Semaphore methods
	   //--------

	   // TODO
	   private void adapterBeginExclusive(NetAdapterConstants_Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.println("   adapter.beginExclusive called");
		  }

		  // get blocking boolean
		  //boolean blocking = 
			 conn.input.readBoolean();
		  // call beginExclusive
		  bool b = true;

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();

		  if (logFile != null)
		  {
			 logFile.println("      adapter.beginExclusive returned " + b);
		  }
	   }

	   // TODO
	   private void adapterEndExclusive(NetAdapterConstants_Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.println("   adapter.endExclusive called");
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   //--------
	   //-------- Primitive 1-Wire Network data methods
	   //--------

	   private void adapterReset(NetAdapterConstants_Connection conn)
	   {
		  int i = 1; // return 1 for presence pulse

		  if (logFile != null)
		  {
			 logFile.println("   reset returned " + i);
		  }

		  simulationReset();

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeInt(i);
		  conn.output.flush();
	   }

	   //TODO
	   private void adapterPutBit(NetAdapterConstants_Connection conn)
	   {
		  // get the value of the bit
		  bool bit = conn.input.readBoolean();

		  if (logFile != null)
		  {
			 logFile.println("   putBit called");
			 logFile.println("      bit=" + bit);
		  }

		  simulationPutBit(bit);
		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterPutByte(NetAdapterConstants_Connection conn)
	   {
		  // get the value of the byte
		  sbyte b = conn.input.readByte();

		  if (logFile != null)
		  {
			 logFile.println("   putByte called");
			 logFile.println("      byte=" + Convert.toHexString(b));
		  }

		  simulationPutByte(b);

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   private void adapterGetBit(NetAdapterConstants_Connection conn)
	   {
		  bool bit = simulationGetBit();

		  if (logFile != null)
		  {
			 logFile.println("   getBit called");
			 logFile.println("      bit=" + bit);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(bit);
		  conn.output.flush();
	   }

	   private void adapterGetByte(NetAdapterConstants_Connection conn)
	   {
		  int b = simulationGetByte();

		  if (logFile != null)
		  {
			 logFile.println("   getByte called");
			 logFile.println("      byte=" + Convert.toHexString((sbyte)b));
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeByte(b);
		  conn.output.flush();
	   }

	   private void adapterGetBlock(NetAdapterConstants_Connection conn)
	   {
		  // get the number requested
		  int len = conn.input.readInt();
		  if (logFile != null)
		  {
			 logFile.println("   getBlock called");
			 logFile.println("      len=" + len);
		  }

		  // get the bytes
		  sbyte[] b = new sbyte[len];
		  for (int i = 0; i < len; i++)
		  {
			 b[i] = simulationGetByte();
		  }

		  if (logFile != null)
		  {
			 logFile.println("      returned: " + Convert.toHexString(b));
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.write(b, 0, len);
		  conn.output.flush();
	   }

	   private void adapterDataBlock(NetAdapterConstants_Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.println("   DataBlock called");
		  }
		  // get the number to block
		  int len = conn.input.readInt();
		  // get the bytes to block
		  sbyte[] b = new sbyte[len];
		  conn.input.readFully(b, 0, len);

		  if (logFile != null)
		  {
			 logFile.println("      " + len + " bytes");
			 logFile.println("      Send: " + Convert.toHexString(b));
		  }

		  // do the block
		  for (int i = 0; i < len; i++)
		  {
			 if (b[i] == unchecked((sbyte)0x0FF))
			 {
				b[i] = simulationGetByte();
			 }
			 else
			 {
				simulationPutByte(b[i]);
			 }
		  }

		  if (logFile != null)
		  {
			 logFile.println("      Recv: " + Convert.toHexString(b));
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.write(b, 0, len);
		  conn.output.flush();
	   }

	   //--------
	   //-------- 1-Wire Network power methods
	   //--------

	   // TODO
	   private void adapterSetPowerDuration(NetAdapterConstants_Connection conn)
	   {
		  // get the time factor value
		  int timeFactor = conn.input.readInt();

		  if (logFile != null)
		  {
			 logFile.println("   setPowerDuration called");
			 logFile.println("      timeFactor=" + timeFactor);
		  }

		  // call setPowerDuration
		  //adapter.setPowerDuration(timeFactor);

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterStartPowerDelivery(NetAdapterConstants_Connection conn)
	   {
		  // get the change condition value
		  int changeCondition = conn.input.readInt();

		  if (logFile != null)
		  {
			 logFile.println("   startPowerDelivery called");
			 logFile.println("      changeCondition=" + changeCondition);
		  }

		  // call startPowerDelivery
		  bool success = true; //adapter.startPowerDelivery(changeCondition);

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(success);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterSetProgramPulseDuration(NetAdapterConstants_Connection conn)
	   {
		  // get the time factor value
		  int timeFactor = conn.input.readInt();

		  if (logFile != null)
		  {
			 logFile.println("   setProgramPulseDuration called");
			 logFile.println("      timeFactor=" + timeFactor);
		  }

		  // call setProgramPulseDuration
		  //adapter.setProgramPulseDuration(timeFactor);

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterStartProgramPulse(NetAdapterConstants_Connection conn)
	   {
		  // get the change condition value
		  int changeCondition = conn.input.readInt();

		  if (logFile != null)
		  {
			 logFile.println("   startProgramPulse called");
			 logFile.println("      changeCondition=" + changeCondition);
		  }

		  // call startProgramPulse();
		  bool success = true; //adapter.startProgramPulse(changeCondition);

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(success);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterStartBreak(NetAdapterConstants_Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.println("   startBreak called");
		  }

		  // call startBreak();
		  //adapter.startBreak();

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterSetPowerNormal(NetAdapterConstants_Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.println("   setPowerNormal called");
		  }

		  // call setPowerNormal
		  //adapter.setPowerNormal();

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   //--------
	   //-------- 1-Wire Network speed methods
	   //--------

	   // TODO
	   private void adapterSetSpeed(NetAdapterConstants_Connection conn)
	   {
		  // get the value of the new speed
		  int speed = conn.input.readInt();

		  if (logFile != null)
		  {
			 logFile.println("   setSpeed called");
			 logFile.println("      speed=" + speed);
		  }

		  // do the setSpeed
		  //adapter.setSpeed(speed);

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterGetSpeed(NetAdapterConstants_Connection conn)
	   {
		  // get the adapter speed
		  int speed = 0; //adapter.getSpeed();

		  if (logFile != null)
		  {
			 logFile.println("   getSpeed called");
			 logFile.println("      speed=" + speed);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeInt(speed);
		  conn.output.flush();
	   }


	   //--------
	   //-------- Adapter feature methods
	   //--------

	   // TODO
	   private void adapterCanOverdrive(NetAdapterConstants_Connection conn)
	   {
		  bool b = false; //adapter.canOverdrive();

		  if (logFile != null)
		  {
			 logFile.println("   canOverdrive returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterCanHyperdrive(NetAdapterConstants_Connection conn)
	   {
		  bool b = false; //adapter.canHyperdrive();

		  if (logFile != null)
		  {
			 logFile.println("   canHyperDrive returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterCanFlex(NetAdapterConstants_Connection conn)
	   {
		  bool b = false; //adapter.canFlex();

		  if (logFile != null)
		  {
			 logFile.println("   canFlex returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterCanProgram(NetAdapterConstants_Connection conn)
	   {
		  bool b = true; //adapter.canProgram();

		  if (logFile != null)
		  {
			 logFile.println("   canProgram returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterCanDeliverPower(NetAdapterConstants_Connection conn)
	   {
		  bool b = true; //adapter.canDeliverPower();

		  if (logFile != null)
		  {
			 logFile.println("   canDeliverPower returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterCanDeliverSmartPower(NetAdapterConstants_Connection conn)
	   {
		  bool b = true; //adapter.canDeliverSmartPower();

		  if (logFile != null)
		  {
			 logFile.println("   canDeliverSmartPower returned " + b);
		  }

		  conn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
		  conn.output.writeBoolean(b);
		  conn.output.flush();
	   }

	   // TODO
	   private void adapterCanBreak(NetAdapterConstants_Connection conn)
	   {
		  bool b = true; //adapter.canBreak();

		  if (logFile != null)
		  {
			 logFile.println("   canBreak returned " + b);
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
		   private readonly NetAdapterSim outerInstance;

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
		  public SocketHandler(NetAdapterSim outerInstance, Socket sock)
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
				if (outerInstance.logFile != null)
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

	   // -----------------------------------------------------------------------
	   // Simulation Methods
	   //
	   // -----------------------------------------------------------------------

	   private const string OW_RESET_RESULT = "onewire reset at time";
	   private const string OW_RESET_CMD = "task tb.xow_master.ow_reset";
	   private const int OW_RESET_RUN_LENGTH = 1000000;

	   private const string OW_WRITE_BYTE_ARG = "deposit tb.xow_master.ow_write_byte.data = 8'h"; // ie 8'hFF
	   private const string OW_WRITE_BYTE_CMD = "task tb.xow_master.ow_write_byte";
	   private const int OW_WRITE_BYTE_RUN_LENGTH = 520000;

	   private const string OW_READ_RESULT = "(data=";
	   private const string OW_READ_BYTE_CMD = "task tb.xow_master.ow_read_byte";
	   private const int OW_READ_BYTE_RUN_LENGTH = 632009;

	   private const string OW_READ_SLOT_CMD = "task tb.xow_master.ow_read_slot";
	   private const int OW_READ_SLOT_RUN_LENGTH = 80000;

	   private const string OW_WRITE_ZERO_CMD = "task tb.xow_master.ow_write0";
	   private const int OW_WRITE_ZERO_RUN_LENGTH = 80000;

	   private const string OW_WRITE_ONE_CMD = "task tb.xow_master.ow_write1";
	   private const int OW_WRITE_ONE_RUN_LENGTH = 80000;

	   private const string GENERIC_CMD_END = "Ran until";


	   private const long PING_MS_RUN_LENGTH = 1000000L;

	   private const string RUN = "run ";
	   private const string LINE_DELIM = "\r\n";
	   private const string PROMPT = "ncsim> ";

	   private void simulationReset()
	   {
		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.println("reset: Writing=" + OW_RESET_CMD);
			 logFile.println("reset: Writing=" + RUN + OW_RESET_RUN_LENGTH);
		  }
		  processInput.BaseStream.WriteByte(OW_RESET_CMD + LINE_DELIM);
		  processInput.BaseStream.WriteByte(RUN + OW_RESET_RUN_LENGTH + LINE_DELIM);
		  processInput.Flush();

		  // wait for it to complete
		  int complete = 0;
		  while (complete < 2)
		  {
			 string line = processOutput.ReadLine();
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.println("reset: complete=" + complete + ", read=" + line);
			 }
			 if (complete == 0 && line.IndexOf(OW_RESET_RESULT, StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
			 if (complete == 1 && line.IndexOf(GENERIC_CMD_END, StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
		  }
		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.println("reset: Complete");
		  }
	   }

	   private bool simulationGetBit()
	   {
		  bool bit = true;

		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.println("getBit: Writing=" + OW_READ_SLOT_CMD);
			 logFile.println("getBit: Writing=" + RUN + OW_READ_SLOT_RUN_LENGTH);
		  }
		  processInput.BaseStream.WriteByte(OW_READ_SLOT_CMD + LINE_DELIM);
		  processInput.BaseStream.WriteByte(RUN + OW_READ_SLOT_RUN_LENGTH + LINE_DELIM);
		  processInput.Flush();

		  // wait for it to complete
		  int complete = 0;
		  while (complete < 3)
		  {
			 string line = processOutput.ReadLine();
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.println("getBit: complete=" + complete + ", read=" + line);
			 }
			 if (complete == 0 && line.IndexOf("OW = 1'b0", StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
			 if (complete == 1 && line.IndexOf("OW = 1'b0", StringComparison.Ordinal) >= 0)
			 {
				bit = false;
				complete++;
				continue;
			 }
			 if (complete == 1 && line.IndexOf("OW = 1'b1", StringComparison.Ordinal) >= 0)
			 {
				bit = true;
				complete++;
				continue;
			 }
			 if (complete == 2 && line.IndexOf(GENERIC_CMD_END, StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
		  }
		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.println("getBit: Complete");
		  }
		  return bit;
	   }

	   private sbyte simulationGetByte()
	   {
		  sbyte bits = 0;

		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.println("getByte: Writing=" + OW_READ_BYTE_CMD);
			 logFile.println("getByte: Writing=" + RUN + OW_READ_BYTE_RUN_LENGTH);
		  }
		  processInput.BaseStream.WriteByte(OW_READ_BYTE_CMD + LINE_DELIM);
		  processInput.BaseStream.WriteByte(RUN + OW_READ_BYTE_RUN_LENGTH + LINE_DELIM);
		  processInput.Flush();

		  // wait for it to complete
		  try
		  {
			 int complete = 0;
			 while (complete < 2)
			 {
				string line = processOutput.ReadLine();
				if (SIM_DEBUG && logFile != null)
				{
				   logFile.println("getByte: complete=" + complete + ", read=" + line);
				}
				if (complete == 0 && line.IndexOf(OW_READ_RESULT, StringComparison.Ordinal) >= 0)
				{
				   int i = line.IndexOf(OW_READ_RESULT, StringComparison.Ordinal) + OW_READ_RESULT.Length;
				   string bitstr = line.Substring(i, 2);
				   if (SIM_DEBUG && logFile != null)
				   {
					  logFile.println("getByte: bitstr=" + bitstr);
				   }
				   bits = unchecked((sbyte)(Convert.toInt(bitstr) & 0x0FF));
				   complete++;
				   continue;
				}
				if (complete == 1 && line.IndexOf(GENERIC_CMD_END, StringComparison.Ordinal) >= 0)
				{
				   complete++;
				   continue;
				}
			 }
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.println("getByte: complete");
			 }
		  }
		  catch (Convert.ConvertException ce)
		  {
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.println("Error during hex string conversion: " + ce);
			 }
		  }
		  return bits;
	   }

	   private void simulationPutBit(bool bit)
	   {
		  if (bit)
		  {
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.println("putBit: Writing=" + OW_WRITE_ONE_CMD);
				logFile.println("putBit: Writing=" + RUN + OW_WRITE_ONE_RUN_LENGTH);
			 }
			 processInput.BaseStream.WriteByte(OW_WRITE_ONE_CMD + LINE_DELIM);
			 processInput.BaseStream.WriteByte(RUN + OW_WRITE_ONE_RUN_LENGTH + LINE_DELIM);
		  }
		  else
		  {
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.println("putBit: Writing=" + OW_WRITE_ZERO_CMD);
				logFile.println("putBit: Writing=" + RUN + OW_WRITE_ZERO_RUN_LENGTH);
			 }
			 processInput.BaseStream.WriteByte(OW_WRITE_ZERO_CMD + LINE_DELIM);
			 processInput.BaseStream.WriteByte(RUN + OW_WRITE_ZERO_RUN_LENGTH + LINE_DELIM);
		  }
		  processInput.Flush();

		  // wait for it to complete
		  int complete = 0;
		  while (complete < 1)
		  {
			 string line = processOutput.ReadLine();
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.println("putBit: complete=" + complete + ", read=" + line);
			 }
			 if (complete == 0 && line.IndexOf(GENERIC_CMD_END, StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
		  }
		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.println("putBit: complete");
		  }
	   }

	   private void simulationPutByte(sbyte b)
	   {
		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.println("putByte: Writing=" + OW_WRITE_BYTE_ARG + Convert.toHexString(b));
			 logFile.println("putByte: Writing=" + OW_WRITE_BYTE_CMD);
			 logFile.println("putByte: Writing=" + RUN + OW_WRITE_BYTE_RUN_LENGTH);
		  }
		  processInput.BaseStream.WriteByte(OW_WRITE_BYTE_ARG + Convert.toHexString(b) + LINE_DELIM);
		  processInput.BaseStream.WriteByte(OW_WRITE_BYTE_CMD + LINE_DELIM);
		  processInput.BaseStream.WriteByte(RUN + OW_WRITE_BYTE_RUN_LENGTH + LINE_DELIM);
		  processInput.Flush();

		  // wait for it to complete
		  int complete = 0;
		  while (complete < 1)
		  {
			 string line = processOutput.ReadLine();
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.println("putByte: complete=" + complete + ", read=" + line);
			 }
			 if (complete == 0 && line.IndexOf(GENERIC_CMD_END, StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
		  }
		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.println("putByte: complete");
		  }
	   }

	   private void simulationPing(long timeDelta)
	   {
		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.println("ping: timeDelta=" + timeDelta);
			 logFile.println("ping: Writing=" + RUN + (PING_MS_RUN_LENGTH * timeDelta));
		  }
		  processInput.BaseStream.WriteByte(RUN + (PING_MS_RUN_LENGTH * timeDelta) + LINE_DELIM);
		  processInput.Flush();

		  // wait for it to complete
		  int complete = 0;
		  while (complete < 1)
		  {
			 string line = processOutput.ReadLine();
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.println("ping: complete=" + complete + ", read=" + line);
			 }
			 if (complete == 0 && line.IndexOf(GENERIC_CMD_END, StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
		  }
		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.println("ping: complete");
		  }
	   }

	   private void simulationGetAddress()
	   {
		  this.fakeAddress = new sbyte[8];
		  // reset the simulated part
		  simulationReset();
		  // put the Read Rom command
		  simulationPutByte((sbyte)0x33);
		  // get the Rom ID
		  for (int i = 0; i < 8; i++)
		  {
			 this.fakeAddress[i] = simulationGetByte();
		  }
	   }

	   //--------
	   //-------- Default Main Method, for launching server with defaults
	   //--------
	   /// <summary>
	   /// A Default Main Method, for launching NetAdapterSim getting the
	   /// default adapter with the OneWireAccessProvider and listening on
	   /// the default port specified by DEFAULT_PORT.
	   /// </summary>
	   public static void Main(string[] args)
	   {
		  Debug.WriteLine("NetAdapterSim");
		  if (args.Length < 1)
		  {
			 Debug.WriteLine("");
			 Debug.WriteLine("   java com.dalsemi.onewire.adapter.NetAdapterSim <execCmd> <logFilename> <simDebug>");
			 Debug.WriteLine("");
			 Debug.WriteLine("   execCmd     - the command to start the simulator");
			 Debug.WriteLine("   logFilename - the name of the file to log output to");
			 Debug.WriteLine("   simDebug    - 'true' or 'false', turns on debug output from simulation");
			 Debug.WriteLine("");
			 Environment.Exit(1);
		  }

		  string execCmd = args[0];
		  Debug.WriteLine("   Executing: " + execCmd);
		  string logFilename = null;
		  if (args.Length > 1)
		  {
			 if (!args[1].ToLower().Equals("false"))
			 {
				logFilename = args[1];
				Debug.WriteLine("   Logging data to file: " + logFilename);
			 }
		  }
		  if (args.Length > 2)
		  {
			 NetAdapterSim.SIM_DEBUG = args[2].ToLower().Equals("true");
			 Debug.WriteLine("   Simulation Debugging is: " + (NetAdapterSim.SIM_DEBUG?"enabled":"disabled"));
		  }


		  NetAdapterSim host = new NetAdapterSim(execCmd, logFilename);
		  Debug.WriteLine("Device Address=" + Address.ToString(host.fakeAddress));

		  Debug.WriteLine("Starting Multicast Listener...");
		  host.createMulticastListener();

		  Debug.WriteLine("Starting NetAdapter Host...");
		  (new Thread(host)).Start();
		  Debug.WriteLine("NetAdapter Host Started");
	   }
	}

}