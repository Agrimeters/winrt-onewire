using System;
using System.IO;
using System.Collections;
using System.Threading;
using System.Text;
using System.Net.Sockets;
using System.Diagnostics;

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
    using System.Threading.Tasks;
    using Windows.Networking.Sockets;
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
    public class NetAdapterSim : IDisposable
    {
	   protected internal static bool SIM_DEBUG = false;

	   /// <summary>
	   /// random number generator, used to issue challenges to client </summary>
	   protected internal static readonly Random rand = new Random();

	   /// <summary>
	   /// Log file </summary>
	   protected internal StreamWriter logFile = null;

        /// <summary>
        /// exec command, command string to start the simulator </summary>
        //protected internal string execCommand;

 	    protected internal Object process; //Process
        protected internal StreamReader processOutput = null;
        protected internal StreamWriter processInput = null;


       /// <summary>
       /// fake address, returned from all search or getAddress commands </summary>
        protected internal byte[] fakeAddress = null;

       /// <summary>
       /// The server socket for listening for connections </summary>
	   protected internal StreamSocketListener serverSocket = null;

	   /// <summary>
	   /// secret for authentication with the server </summary>
	   protected internal byte[] netAdapterSecret = null;

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
       /// Timer used to get millisecond count
       /// </summary>
       private static Stopwatch stopWatch = null;

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
	   public NetAdapterSim(string execCmd, string logFilename) : this(execCmd, logFilename, NetAdapterConstants.DEFAULT_PORT, false)
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
	   /// <param name="serviceName"> the TCP/IP port to listen on for incoming connections
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterSim(string execCmd, byte[] fakeAddress, string logFile, string serviceName) : this(execCmd, logFile, serviceName, false)
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
	   public NetAdapterSim(string execCmd, string logFilename, bool multiThread) : this(execCmd, logFilename, NetAdapterConstants.DEFAULT_PORT, multiThread)
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
	   /// <param name="serviceName"> the TCP/IP port to listen on for incoming connections </param>
	   /// <param name="multiThread"> if true, multiple TCP/IP connections are allowed
	   /// to interact simulataneously with this adapter.
	   /// </param>
	   /// <exception cref="IOException"> if a network error occurs or the listen socket
	   /// cannot be created on the specified port. </exception>
	   public NetAdapterSim(string execCmd, string logFilename, string serviceName, bool multiThread)
	   {
            //// save references to file and command
            //this.execCommand = execCmd;
            //this.process = null; //TODO Runtime.Runtime.exec(execCmd);
            this.processOutput =
                new StreamReader(new FileStream(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\processOutput.txt", FileMode.Create, FileAccess.Write));

            this.processInput =
                new StreamWriter(new FileStream(Windows.Storage.ApplicationData.Current.LocalFolder.Path + "\\processInput.txt", FileMode.Create, FileAccess.Write));

            //// wait until process is ready
            //int complete = 0;
            //while (complete < 2)
            //{
            //    string line = processOutput.ReadLine();
            //    if (complete == 0 && line.IndexOf("read ok (data=17)", StringComparison.Ordinal) >= 0)
            //    {
            //        complete++;
            //        continue;
            //    }
            //    if (complete == 1 && line.IndexOf(PROMPT, StringComparison.Ordinal) >= 0)
            //    {
            //        complete++;
            //        continue;
            //    }
            //}

            System.Diagnostics.Debug.WriteLine(Windows.Storage.ApplicationData.Current.LocalFolder.Path);
            if (!string.ReferenceEquals(logFilename, null))
            {
                this.logFile = new StreamWriter(new FileStream(logFilename, FileMode.Create, FileAccess.Write));
                this.logFile.AutoFlush = true;
            }

            // Make sure we loaded the address of the device
            simulationGetAddress();

            // create the server socket
            this.serverSocket.Control.NoDelay = true;

            // set multithreaded flag
            // always multithreaded in windows
            //this.singleThreaded = !multiThread;
            //if (multiThread)
            //{
            //    this.hashHandlers = new Hashtable();
            //    this.timeoutInSeconds = 0;
            //}

            // get the shared secret
            string secret = OneWireAccessProvider.getProperty("NetAdapter.secret");
            if (!string.ReferenceEquals(secret, null))
            {
                netAdapterSecret = Encoding.UTF8.GetBytes(secret);
            }
            else
            {
                netAdapterSecret = Encoding.UTF8.GetBytes(NetAdapterConstants.DEFAULT_SECRET);
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
	   public NetAdapterSim(string execCmd, string logFilename, StreamSocketListener serverSock) : this(execCmd, logFilename, serverSock, false)
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
	   public NetAdapterSim(string execCmd, string logFilename, StreamSocketListener serverSock, bool multiThread)
	   {
            // save references to file and command
            //this.execCommand = execCmd;
            //this.process = null; //TODO Runtime.Runtime.exec(execCmd);
            //this.processOutput = new System.IO.StreamReader(this.process.InputStream);
            //this.processInput = new System.IO.StreamWriter(this.process.OutputStream);

            // wait  until process is ready
            //int complete = 0;
            //while (complete < 2)
            //{
            //    string line = processOutput.ReadLine();
            //    if (complete == 0 && line.IndexOf("read ok (data=17)", StringComparison.Ordinal) >= 0)
            //    {
            //        complete++;
            //        continue;
            //    }
            //    if (complete == 1 && line.IndexOf(PROMPT, StringComparison.Ordinal) >= 0)
            //    {
            //        complete++;
            //        continue;
            //    }
            //}

            if (!string.ReferenceEquals(logFilename, null))
            {
                this.logFile = new StreamWriter(new FileStream(logFilename, FileMode.Create, FileAccess.Write));
                this.logFile.AutoFlush = true;
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
                netAdapterSecret = Encoding.UTF8.GetBytes(secret);
            }
            else
            {
                netAdapterSecret = Encoding.UTF8.GetBytes(NetAdapterConstants.DEFAULT_SECRET);
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
			  netAdapterSecret = Encoding.UTF8.GetBytes(value);
           }
	   }

	   /// <summary>
	   /// Creates a Multicast Listener to allow NetAdapter clients to discover
	   /// this NetAdapterSim automatically.  Uses defaults for Multicast group
	   /// and port.
	   /// </summary>
	   public virtual void createMulticastListener()
	   {
		  createMulticastListener(NetAdapterConstants.DEFAULT_MULTICAST_PORT);
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
			 group = NetAdapterConstants.DEFAULT_MULTICAST_GROUP;
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
                byte[] versionBytes = Convert.toByteArray(NetAdapterConstants.versionUID);

                // this byte array is 5 because length is used to determine different
                // packet types by client
                byte[] listenPortBytes = new byte[5];
                Encoding.UTF8.GetBytes(port.ToString()).CopyTo(listenPortBytes, 0);
                listenPortBytes[4] = 0x0FF;

                multicastListener = new MulticastListener(port, group, versionBytes, listenPortBytes);
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
				//TODO sock = serverSocket.accept();
				// reset time of last command, so we don't simulate a bunch of
				// unneccessary time
				timeOfLastCommand = stopWatch.ElapsedMilliseconds;
				handleConnection(sock);
			 }
			 catch (System.IO.IOException)
			 {
				try
				{
				   if (sock != null)
				   {
                      sock.Dispose();
				   }
				}
				catch (System.IO.IOException)
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
              Task t = Task.Run(() => { sh.run(); });
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
	   public virtual async void stopHost()
	   {
		  this.hostStopped = true;
		  try
		  {
			 await this.serverSocket.CancelIOAsync();
		  }
		  catch (System.IO.IOException)
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
	   private bool sendVersionUID(NetAdapterConstants.Connection conn)
	   {
		  // write server version
		  conn.output.WriteInt32(NetAdapterConstants.versionUID);
          var t = Task.Run(async() =>
          {
              await conn.output.StoreAsync();
          });
          t.Wait();

		  byte retVal = conn.input.ReadByte();

		  return (retVal == NetAdapterConstants.RET_SUCCESS);
	   }

	   protected internal long timeOfLastCommand = 0;
	   protected internal const long IGNORE_TIME_MIN = 2;
	   protected internal const long IGNORE_TIME_MAX = 1000;
	   /// <summary>
	   /// Reads in command from client and calls the appropriate handler function.
	   /// </summary>
	   /// <param name="conn"> The connection to send/receive data.
	   ///  </param>
	   private async void processRequests(NetAdapterConstants.Connection conn)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
		  if (logFile != null)
		  {
			 logFile.WriteLine("\n------------------------------------------");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

		  // get the next command
		  byte cmd = 0x00;

		  cmd = conn.input.ReadByte();

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
		  if (logFile != null)
		  {
			 logFile.WriteLine("CMD received: " + cmd.ToString("x"));
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

		  if (cmd == NetAdapterConstants.CMD_PINGCONNECTION)
		  {
			 // no-op, might update timer of some sort later
			 simulationPing(1000);
			 conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
			 await conn.output.StoreAsync();
		  }
		  else
		  {
			 long timeDelta = stopWatch.ElapsedMilliseconds - timeOfLastCommand;
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.WriteLine("general: timeDelta=" + timeDelta);
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
				   case NetAdapterConstants.CMD_CLOSECONNECTION:
					 close(conn);
					 break;
				   /* Raw Data commands */
				   case NetAdapterConstants.CMD_RESET:
					  adapterReset(conn);
					  break;
				   case NetAdapterConstants.CMD_PUTBIT:
					  adapterPutBit(conn);
					  break;
				   case NetAdapterConstants.CMD_PUTBYTE:
					  adapterPutByte(conn);
					  break;
				   case NetAdapterConstants.CMD_GETBIT:
					  adapterGetBit(conn);
					  break;
				   case NetAdapterConstants.CMD_GETBYTE:
					  adapterGetByte(conn);
					  break;
				   case NetAdapterConstants.CMD_GETBLOCK:
					  adapterGetBlock(conn);
					  break;
				   case NetAdapterConstants.CMD_DATABLOCK:
					  adapterDataBlock(conn);
					  break;
				   /* Power methods */
				   case NetAdapterConstants.CMD_SETPOWERDURATION:
					  adapterSetPowerDuration(conn);
					  break;
				   case NetAdapterConstants.CMD_STARTPOWERDELIVERY:
					  adapterStartPowerDelivery(conn);
					  break;
				   case NetAdapterConstants.CMD_SETPROGRAMPULSEDURATION:
					  adapterSetProgramPulseDuration(conn);
					  break;
				   case NetAdapterConstants.CMD_STARTPROGRAMPULSE:
					  adapterStartProgramPulse(conn);
					  break;
				   case NetAdapterConstants.CMD_STARTBREAK:
					  adapterStartBreak(conn);
					  break;
				   case NetAdapterConstants.CMD_SETPOWERNORMAL:
					  adapterSetPowerNormal(conn);
					  break;
				   /* Speed methods */
				   case NetAdapterConstants.CMD_SETSPEED:
					  adapterSetSpeed(conn);
					  break;
				   case NetAdapterConstants.CMD_GETSPEED:
					  adapterGetSpeed(conn);
					  break;
				   /* Network Semaphore methods */
				   case NetAdapterConstants.CMD_BEGINEXCLUSIVE:
					  adapterBeginExclusive(conn);
					  break;
				   case NetAdapterConstants.CMD_ENDEXCLUSIVE:
					  adapterEndExclusive(conn);
					  break;
				   /* Searching methods */
				   case NetAdapterConstants.CMD_FINDFIRSTDEVICE:
					  adapterFindFirstDevice(conn);
					  break;
				   case NetAdapterConstants.CMD_FINDNEXTDEVICE:
					  adapterFindNextDevice(conn);
					  break;
				   case NetAdapterConstants.CMD_GETADDRESS:
					  adapterGetAddress(conn);
					  break;
				   case NetAdapterConstants.CMD_SETSEARCHONLYALARMINGDEVICES:
					  adapterSetSearchOnlyAlarmingDevices(conn);
					  break;
				   case NetAdapterConstants.CMD_SETNORESETSEARCH:
					  adapterSetNoResetSearch(conn);
					  break;
				   case NetAdapterConstants.CMD_SETSEARCHALLDEVICES:
					  adapterSetSearchAllDevices(conn);
					  break;
				   case NetAdapterConstants.CMD_TARGETALLFAMILIES:
					  adapterTargetAllFamilies(conn);
					  break;
				   case NetAdapterConstants.CMD_TARGETFAMILY:
					  adapterTargetFamily(conn);
					  break;
				   case NetAdapterConstants.CMD_EXCLUDEFAMILY:
					  adapterExcludeFamily(conn);
					  break;
				   /* feature methods */
				   case NetAdapterConstants.CMD_CANBREAK:
					  adapterCanBreak(conn);
					  break;
				   case NetAdapterConstants.CMD_CANDELIVERPOWER:
					  adapterCanDeliverPower(conn);
					  break;
				   case NetAdapterConstants.CMD_CANDELIVERSMARTPOWER:
					  adapterCanDeliverSmartPower(conn);
					  break;
				   case NetAdapterConstants.CMD_CANFLEX:
					  adapterCanFlex(conn);
					  break;
				   case NetAdapterConstants.CMD_CANHYPERDRIVE:
					  adapterCanHyperdrive(conn);
					  break;
				   case NetAdapterConstants.CMD_CANOVERDRIVE:
					  adapterCanOverdrive(conn);
					  break;
				   case NetAdapterConstants.CMD_CANPROGRAM:
					  adapterCanProgram(conn);
					  break;
				   default:
					  if (SIM_DEBUG && logFile != null)
					  {
						 logFile.WriteLine("Unkown command received: " + cmd);
					  }
					  break;
				}
			 }
			 catch (OneWireException owe)
			 {
				if (SIM_DEBUG && logFile != null)
				{
				   logFile.WriteLine("Exception: " + owe.ToString());
				}
				conn.output.WriteByte(NetAdapterConstants.RET_FAILURE);
				conn.output.WriteString(owe.ToString());
				await conn.output.StoreAsync();
			 }
			 timeOfLastCommand = stopWatch.ElapsedMilliseconds;
		  }
	   }

	   /// <summary>
	   /// Closes the provided connection.
	   /// </summary>
	   /// <param name="conn"> The connection to send/receive data. </param>
	   private void close(NetAdapterConstants.Connection conn)
	   {
		  try
		  {
			 if (conn.sock != null)
			 {
				//TODO conn.sock.Close();
			 }
		  }
		  catch (System.IO.IOException)
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

	   private async void adapterFindFirstDevice(NetAdapterConstants.Connection conn)
	   {
		  bool b = true; //adapter.findFirstDevice();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   findFirstDevice returned " + b);
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBoolean(b);
		  await conn.output.StoreAsync();
	   }

	   private async void adapterFindNextDevice(NetAdapterConstants.Connection conn)
	   {
		  bool b = false; //adapter.findNextDevice();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   findNextDevice returned " + b);
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBoolean(b);
		  await conn.output.StoreAsync();
	   }

	   private async void adapterGetAddress(NetAdapterConstants.Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.WriteLine("   adapter.getAddress(byte[]) called");
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBytes(fakeAddress);
		  await conn.output.StoreAsync();
	   }

	   private async void adapterSetSearchOnlyAlarmingDevices(NetAdapterConstants.Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.WriteLine("   setSearchOnlyAlarmingDevices called");
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   private async void adapterSetNoResetSearch(NetAdapterConstants.Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.WriteLine("   setNoResetSearch called");
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   private async void adapterSetSearchAllDevices(NetAdapterConstants.Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.WriteLine("   setSearchAllDevices called");
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   private async void adapterTargetAllFamilies(NetAdapterConstants.Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.WriteLine("   targetAllFamilies called");
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterTargetFamily(NetAdapterConstants.Connection conn)
	   {
		  // get the number of family codes to expect
		  int len = conn.input.ReadInt32();
		  // get the family codes
		  byte[] family = new byte[len];
		  conn.input.ReadBytes(family);

		  if (logFile != null)
		  {
			 logFile.WriteLine("   targetFamily called");
			 logFile.WriteLine("      families: " + Convert.toHexString(family));
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterExcludeFamily(NetAdapterConstants.Connection conn)
	   {
		  // get the number of family codes to expect
		  int len = conn.input.ReadInt32();
		  // get the family codes
		  byte[] family = new byte[len];
		  conn.input.ReadBytes(family);

		  if (logFile != null)
		  {
			 logFile.WriteLine("   excludeFamily called");
			 logFile.WriteLine("      families: " + Convert.toHexString(family));
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   //--------
	   //-------- 1-Wire Network Semaphore methods
	   //--------

	   // TODO
	   private async void adapterBeginExclusive(NetAdapterConstants.Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.WriteLine("   adapter.beginExclusive called");
		  }

		  // get blocking boolean
		  //boolean blocking = 
			 conn.input.ReadBoolean();
		  // call beginExclusive
		  bool b = true;

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBoolean(b);
          await conn.output.StoreAsync();

		  if (logFile != null)
		  {
			 logFile.WriteLine("      adapter.beginExclusive returned " + b);
		  }
	   }

	   // TODO
	   private async void adapterEndExclusive(NetAdapterConstants.Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.WriteLine("   adapter.endExclusive called");
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   //--------
	   //-------- Primitive 1-Wire Network data methods
	   //--------

	   private async void adapterReset(NetAdapterConstants.Connection conn)
	   {
		  int i = 1; // return 1 for presence pulse

		  if (logFile != null)
		  {
			 logFile.WriteLine("   reset returned " + i);
		  }

		  simulationReset();

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteInt32(i);
		  await conn.output.StoreAsync();
	   }

	   //TODO
	   private async void adapterPutBit(NetAdapterConstants.Connection conn)
	   {
		  // get the value of the bit
		  bool bit = conn.input.ReadBoolean();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   putBit called");
			 logFile.WriteLine("      bit=" + bit);
		  }

		  simulationPutBit(bit);
		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   private async void adapterPutByte(NetAdapterConstants.Connection conn)
	   {
		  // get the value of the byte
		  byte b = conn.input.ReadByte();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   putByte called");
			 logFile.WriteLine("      byte=" + Convert.toHexString(b));
		  }

		  simulationPutByte(b);

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   private async void adapterGetBit(NetAdapterConstants.Connection conn)
	   {
		  bool bit = simulationGetBit();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   getBit called");
			 logFile.WriteLine("      bit=" + bit);
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBoolean(bit);
		  await conn.output.StoreAsync();
	   }

	   private async void adapterGetByte(NetAdapterConstants.Connection conn)
	   {
		  byte b = simulationGetByte();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   getByte called");
			 logFile.WriteLine("      byte=" + Convert.toHexString((byte)b));
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteByte(b);
          await conn.output.StoreAsync();
	   }

	   private async void adapterGetBlock(NetAdapterConstants.Connection conn)
	   {
		  // get the number requested
		  int len = conn.input.ReadInt32();
		  if (logFile != null)
		  {
			 logFile.WriteLine("   getBlock called");
			 logFile.WriteLine("      len=" + len);
		  }

		  // get the bytes
		  byte[] b = new byte[len];
		  for (int i = 0; i < len; i++)
		  {
			 b[i] = simulationGetByte();
		  }

		  if (logFile != null)
		  {
			 logFile.WriteLine("      returned: " + Convert.toHexString(b));
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBytes(b);
		  await conn.output.StoreAsync();
	   }

	   private async void adapterDataBlock(NetAdapterConstants.Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.WriteLine("   DataBlock called");
		  }
		  // get the number to block
		  int len = conn.input.ReadInt32();
		  // get the bytes to block
		  byte[] b = new byte[len];
		  conn.input.ReadBytes(b);

		  if (logFile != null)
		  {
			 logFile.WriteLine("      " + len + " bytes");
			 logFile.WriteLine("      Send: " + Convert.toHexString(b));
		  }

		  // do the block
		  for (int i = 0; i < len; i++)
		  {
			 if (b[i] == 0x0FF)
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
			 logFile.WriteLine("      Recv: " + Convert.toHexString(b));
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBytes(b);
		  await conn.output.StoreAsync();
	   }

	   //--------
	   //-------- 1-Wire Network power methods
	   //--------

	   // TODO
	   private async void adapterSetPowerDuration(NetAdapterConstants.Connection conn)
	   {
		  // get the time factor value
		  int timeFactor = conn.input.ReadInt32();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   setPowerDuration called");
			 logFile.WriteLine("      timeFactor=" + timeFactor);
		  }

		  // call setPowerDuration
		  //adapter.setPowerDuration(timeFactor);

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterStartPowerDelivery(NetAdapterConstants.Connection conn)
	   {
		  // get the change condition value
		  int changeCondition = conn.input.ReadInt32();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   startPowerDelivery called");
			 logFile.WriteLine("      changeCondition=" + changeCondition);
		  }

		  // call startPowerDelivery
		  bool success = true; //adapter.startPowerDelivery(changeCondition);

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBoolean(success);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterSetProgramPulseDuration(NetAdapterConstants.Connection conn)
	   {
		  // get the time factor value
		  int timeFactor = conn.input.ReadInt32();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   setProgramPulseDuration called");
			 logFile.WriteLine("      timeFactor=" + timeFactor);
		  }

		  // call setProgramPulseDuration
		  //adapter.setProgramPulseDuration(timeFactor);

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterStartProgramPulse(NetAdapterConstants.Connection conn)
	   {
		  // get the change condition value
		  int changeCondition = conn.input.ReadInt32();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   startProgramPulse called");
			 logFile.WriteLine("      changeCondition=" + changeCondition);
		  }

		  // call startProgramPulse();
		  bool success = true; //adapter.startProgramPulse(changeCondition);

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBoolean(success);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterStartBreak(NetAdapterConstants.Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.WriteLine("   startBreak called");
		  }

		  // call startBreak();
		  //adapter.startBreak();

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterSetPowerNormal(NetAdapterConstants.Connection conn)
	   {
		  if (logFile != null)
		  {
			 logFile.WriteLine("   setPowerNormal called");
		  }

		  // call setPowerNormal
		  //adapter.setPowerNormal();

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   //--------
	   //-------- 1-Wire Network speed methods
	   //--------

	   // TODO
	   private async void adapterSetSpeed(NetAdapterConstants.Connection conn)
	   {
		  // get the value of the new speed
		  int speed = conn.input.ReadInt32();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   setSpeed called");
			 logFile.WriteLine("      speed=" + speed);
		  }

		  // do the setSpeed
		  //adapter.setSpeed(speed);

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterGetSpeed(NetAdapterConstants.Connection conn)
	   {
		  // get the adapter speed
		  int speed = 0; //adapter.getSpeed();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   getSpeed called");
			 logFile.WriteLine("      speed=" + speed);
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteInt32(speed);
		  await conn.output.StoreAsync();
	   }


	   //--------
	   //-------- Adapter feature methods
	   //--------

	   // TODO
	   private async void adapterCanOverdrive(NetAdapterConstants.Connection conn)
	   {
		  bool b = false; //adapter.canOverdrive();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   canOverdrive returned " + b);
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBoolean(b);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterCanHyperdrive(NetAdapterConstants.Connection conn)
	   {
		  bool b = false; //adapter.canHyperdrive();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   canHyperDrive returned " + b);
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBoolean(b);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterCanFlex(NetAdapterConstants.Connection conn)
	   {
		  bool b = false; //adapter.canFlex();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   canFlex returned " + b);
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBoolean(b);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterCanProgram(NetAdapterConstants.Connection conn)
	   {
		  bool b = true; //adapter.canProgram();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   canProgram returned " + b);
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBoolean(b);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterCanDeliverPower(NetAdapterConstants.Connection conn)
	   {
		  bool b = true; //adapter.canDeliverPower();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   canDeliverPower returned " + b);
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBoolean(b);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterCanDeliverSmartPower(NetAdapterConstants.Connection conn)
	   {
		  bool b = true; //adapter.canDeliverSmartPower();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   canDeliverSmartPower returned " + b);
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBoolean(b);
		  await conn.output.StoreAsync();
	   }

	   // TODO
	   private async void adapterCanBreak(NetAdapterConstants.Connection conn)
	   {
		  bool b = true; //adapter.canBreak();

		  if (logFile != null)
		  {
			 logFile.WriteLine("   canBreak returned " + b);
		  }

		  conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
		  conn.output.WriteBoolean(b);
		  await conn.output.StoreAsync();
	   }

	   //--------
	   //-------- Inner classes
	   //--------

	   /// <summary>
	   /// Private inner class for servicing new connections.
	   /// Can be run in it's own thread or in the same thread.
	   /// </summary>
	   private class SocketHandler
	   {
		   private readonly NetAdapterSim outerInstance;

		  /// <summary>
		  /// The connection that is being serviced.
		  /// </summary>
		  internal NetAdapterConstants.Connection conn = null;

		  /// <summary>
		  /// indicates whether or not the handler is currently running
		  /// </summary>
		  internal volatile bool handlerRunning = false;

		  /// <summary>
		  /// Constructor for socket servicer.  Creates the input and output
		  /// streams and send's the version of this host to the client
		  /// connection.
		  /// </summary>
		  public SocketHandler(NetAdapterSim outerInstance, System.Net.Sockets.Socket sock)
		  {
			  this.outerInstance = outerInstance;
             //TODO
			 //// set socket timeout to 10 seconds
			 //sock.SoTimeout = outerInstance.timeoutInSeconds * 1000;

			 //// create the connection object
			 //conn = new NetAdapterConstants.Connection();
			 //conn.sock = sock;
			 //conn.input = new DataInputStream(conn.sock.InputStream);
			 //if (NetAdapterConstants.BUFFERED_OUTPUT)
			 //{
				//conn.output = new DataOutputStream(new BufferedOutputStream(conn.sock.OutputStream));
			 //}
			 //else
			 //{
				//conn.output = new DataOutputStream(conn.sock.OutputStream);
			 //}

			 // first thing transmitted should be version info
			 if (!outerInstance.sendVersionUID(conn))
			 {
				throw new System.IO.IOException("send version failed");
			 }

			 // authenticate the client
			 byte[] chlg = new byte[8];
			 rand.NextBytes(chlg);
			 conn.output.WriteBytes(chlg);
             var t = Task.Run(async () =>
             {
                 await conn.output.StoreAsync();
             });
             t.Wait();

			 // compute the crc of the secret and the challenge
			 int crc = CRC16.compute(outerInstance.netAdapterSecret, 0);
			 crc = CRC16.compute(chlg, crc);
			 int answer = conn.input.ReadInt32();
			 if (answer != crc)
			 {
				conn.output.WriteByte(NetAdapterConstants.RET_FAILURE);
				conn.output.WriteString("Client Authentication Failed");
                t = Task.Run(async () =>
                {
                    await conn.output.StoreAsync();
                });
                t.Wait();
				throw new System.IO.IOException("authentication failed");
			 }
			 else
			 {
				conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
                t = Task.Run(async () =>
                {
                    await conn.output.StoreAsync();
                });
                t.Wait();
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
				   System.Diagnostics.Debug.WriteLine(t.ToString());
				   System.Diagnostics.Debug.Write(t.StackTrace);
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
				   outerInstance.hashHandlers.Remove(System.Environment.CurrentManagedThreadId);
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
			 logFile.WriteLine("reset: Writing=" + OW_RESET_CMD);
			 logFile.WriteLine("reset: Writing=" + RUN + OW_RESET_RUN_LENGTH);
		  }

          byte[] reset_cmd = Encoding.UTF8.GetBytes(OW_RESET_CMD + LINE_DELIM);
          processInput.BaseStream.Write(reset_cmd, 0, reset_cmd.Length);
          byte[] run_cmd = Encoding.UTF8.GetBytes(OW_RESET_CMD + LINE_DELIM);
		  processInput.BaseStream.Write(run_cmd, 0, run_cmd.Length);
		  processInput.Flush();

		  // wait for it to complete
		  int complete = 0;
		  while (complete < 2)
		  {
			 string line = processOutput.ReadLine();
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.WriteLine("reset: complete=" + complete + ", read=" + line);
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
			 logFile.WriteLine("reset: Complete");
		  }
	   }

	   private bool simulationGetBit()
	   {
		  bool bit = true;

		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.WriteLine("getBit: Writing=" + OW_READ_SLOT_CMD);
			 logFile.WriteLine("getBit: Writing=" + RUN + OW_READ_SLOT_RUN_LENGTH);
		  }
          byte[] read_cmd = Encoding.UTF8.GetBytes(OW_READ_SLOT_CMD + LINE_DELIM);
		  processInput.BaseStream.Write(read_cmd, 0, read_cmd.Length);
          byte[] run_cmd = Encoding.UTF8.GetBytes(RUN + OW_READ_SLOT_RUN_LENGTH + LINE_DELIM);
		  processInput.BaseStream.Write(run_cmd, 0, run_cmd.Length);
		  processInput.Flush();

		  // wait for it to complete
		  int complete = 0;
		  while (complete < 3)
		  {
			 string line = processOutput.ReadLine();
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.WriteLine("getBit: complete=" + complete + ", read=" + line);
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
			 logFile.WriteLine("getBit: Complete");
		  }
		  return bit;
	   }

	   private byte simulationGetByte()
	   {
		  byte bits = 0;

		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.WriteLine("getByte: Writing=" + OW_READ_BYTE_CMD);
			 logFile.WriteLine("getByte: Writing=" + RUN + OW_READ_BYTE_RUN_LENGTH);
		  }
          byte[] read_cmd = Encoding.UTF8.GetBytes(OW_READ_BYTE_CMD + LINE_DELIM);
		  processInput.BaseStream.Write(read_cmd, 0, read_cmd.Length);
          byte[] run_cmd = Encoding.UTF8.GetBytes(RUN + OW_READ_BYTE_RUN_LENGTH + LINE_DELIM);
          processInput.BaseStream.Write(run_cmd, 0, run_cmd.Length);
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
				   logFile.WriteLine("getByte: complete=" + complete + ", read=" + line);
				}
				if (complete == 0 && line.IndexOf(OW_READ_RESULT, StringComparison.Ordinal) >= 0)
				{
				   int i = line.IndexOf(OW_READ_RESULT, StringComparison.Ordinal) + OW_READ_RESULT.Length;
				   string bitstr = line.Substring(i, 2);
				   if (SIM_DEBUG && logFile != null)
				   {
					  logFile.WriteLine("getByte: bitstr=" + bitstr);
				   }
				   bits = (byte)(Convert.toInt(bitstr) & 0x0FF);
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
				logFile.WriteLine("getByte: complete");
			 }
		  }
		  catch (Convert.ConvertException ce)
		  {
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.WriteLine("Error during hex string conversion: " + ce);
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
				logFile.WriteLine("putBit: Writing=" + OW_WRITE_ONE_CMD);
				logFile.WriteLine("putBit: Writing=" + RUN + OW_WRITE_ONE_RUN_LENGTH);
			 }
             byte[] write_cmd = Encoding.UTF8.GetBytes(OW_WRITE_ONE_CMD + LINE_DELIM);
		     processInput.BaseStream.Write(write_cmd, 0, write_cmd.Length);
             byte[] run_cmd = Encoding.UTF8.GetBytes(RUN + OW_WRITE_ONE_RUN_LENGTH + LINE_DELIM);
             processInput.BaseStream.Write(run_cmd, 0, run_cmd.Length);
		     processInput.Flush();
		  }
		  else
		  {
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.WriteLine("putBit: Writing=" + OW_WRITE_ZERO_CMD);
				logFile.WriteLine("putBit: Writing=" + RUN + OW_WRITE_ZERO_RUN_LENGTH);
			 }
             byte[] write_cmd = Encoding.UTF8.GetBytes(OW_WRITE_ZERO_CMD + LINE_DELIM);
		     processInput.BaseStream.Write(write_cmd, 0, write_cmd.Length);
             byte[] run_cmd = Encoding.UTF8.GetBytes(RUN + OW_WRITE_ZERO_RUN_LENGTH + LINE_DELIM);
             processInput.BaseStream.Write(run_cmd, 0, run_cmd.Length);
		  }
		  processInput.Flush();

		  // wait for it to complete
		  int complete = 0;
		  while (complete < 1)
		  {
			 string line = processOutput.ReadLine();
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.WriteLine("putBit: complete=" + complete + ", read=" + line);
			 }
			 if (complete == 0 && line.IndexOf(GENERIC_CMD_END, StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
		  }
		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.WriteLine("putBit: complete");
		  }
	   }

	   private void simulationPutByte(byte b)
	   {
		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.WriteLine("putByte: Writing=" + OW_WRITE_BYTE_ARG + Convert.toHexString(b));
			 logFile.WriteLine("putByte: Writing=" + OW_WRITE_BYTE_CMD);
			 logFile.WriteLine("putByte: Writing=" + RUN + OW_WRITE_BYTE_RUN_LENGTH);
		  }
          byte[] write_arg = Encoding.UTF8.GetBytes(OW_WRITE_BYTE_ARG + Convert.toHexString(b) + LINE_DELIM);
          processInput.BaseStream.Write(write_arg, 0, write_arg.Length);
          byte[] write_cmd = Encoding.UTF8.GetBytes(OW_WRITE_BYTE_CMD + LINE_DELIM);
          processInput.BaseStream.Write(write_cmd, 0, write_cmd.Length);
          byte[] run_cmd = Encoding.UTF8.GetBytes(RUN + OW_WRITE_BYTE_RUN_LENGTH + LINE_DELIM);
          processInput.BaseStream.Write(run_cmd, 0, run_cmd.Length);
		  processInput.Flush();

		  // wait for it to complete
		  int complete = 0;
		  while (complete < 1)
		  {
			 string line = processOutput.ReadLine();
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.WriteLine("putByte: complete=" + complete + ", read=" + line);
			 }
			 if (complete == 0 && line.IndexOf(GENERIC_CMD_END, StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
		  }
		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.WriteLine("putByte: complete");
		  }
	   }

	   private void simulationPing(long timeDelta)
	   {
		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.WriteLine("ping: timeDelta=" + timeDelta);
			 logFile.WriteLine("ping: Writing=" + RUN + (PING_MS_RUN_LENGTH * timeDelta));
		  }
          byte[] run_cmd = Encoding.UTF8.GetBytes(RUN + (PING_MS_RUN_LENGTH * timeDelta) + LINE_DELIM);
          processInput.BaseStream.Write(run_cmd, 0, run_cmd.Length);
		  processInput.Flush();

		  // wait for it to complete
		  int complete = 0;
		  while (complete < 1)
		  {
			 string line = processOutput.ReadLine();
			 if (SIM_DEBUG && logFile != null)
			 {
				logFile.WriteLine("ping: complete=" + complete + ", read=" + line);
			 }
			 if (complete == 0 && line.IndexOf(GENERIC_CMD_END, StringComparison.Ordinal) >= 0)
			 {
				complete++;
				continue;
			 }
		  }
		  if (SIM_DEBUG && logFile != null)
		  {
			 logFile.WriteLine("ping: complete");
		  }
	   }

	   private void simulationGetAddress()
	   {
		  this.fakeAddress = new byte[8];
		  // reset the simulated part
		  simulationReset();
		  // put the Read Rom command
		  simulationPutByte((byte)0x33);
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
		  System.Diagnostics.Debug.WriteLine("NetAdapterSim");
		  if (args.Length < 1)
		  {
			 System.Diagnostics.Debug.WriteLine("");
			 System.Diagnostics.Debug.WriteLine("   java com.dalsemi.onewire.adapter.NetAdapterSim <execCmd> <logFilename> <simDebug>");
			 System.Diagnostics.Debug.WriteLine("");
			 System.Diagnostics.Debug.WriteLine("   execCmd     - the command to start the simulator");
			 System.Diagnostics.Debug.WriteLine("   logFilename - the name of the file to log output to");
			 System.Diagnostics.Debug.WriteLine("   simDebug    - 'true' or 'false', turns on debug output from simulation");
			 System.Diagnostics.Debug.WriteLine("");
             return;
		  }

		  string execCmd = args[0];
		  System.Diagnostics.Debug.WriteLine("   Executing: " + execCmd);
		  string logFilename = null;
		  if (args.Length > 1)
		  {
			 if (!args[1].ToLower().Equals("false"))
			 {
				logFilename = args[1];
				System.Diagnostics.Debug.WriteLine("   Logging data to file: " + logFilename);
			 }
		  }
		  if (args.Length > 2)
		  {
			 NetAdapterSim.SIM_DEBUG = args[2].ToLower().Equals("true");
			 System.Diagnostics.Debug.WriteLine("   Simulation Debugging is: " + (NetAdapterSim.SIM_DEBUG?"enabled":"disabled"));
		  }

          // start incrementing the timer
          stopWatch = new Stopwatch();
          stopWatch.Start();

          var t = Task.Run(() =>
          {

              NetAdapterSim host = new NetAdapterSim(execCmd, logFilename);
              System.Diagnostics.Debug.WriteLine("Device Address=" + Address.ToString(host.fakeAddress));

              System.Diagnostics.Debug.WriteLine("Starting Multicast Listener...");
              host.createMulticastListener();

              System.Diagnostics.Debug.WriteLine("Starting NetAdapter Host...");
          });
          t.Wait();

          System.Diagnostics.Debug.WriteLine("NetAdapter Host Started");
        }

        ~NetAdapterSim()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (processOutput != null)
                    processOutput.Dispose();
                if (processInput != null)
                    processInput.Dispose();
            }
        }
    }

}