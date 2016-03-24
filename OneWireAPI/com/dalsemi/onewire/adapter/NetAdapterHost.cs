using System;
using System.Collections;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

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
    using com.dalsemi.onewire.logging;
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
    public class NetAdapterHost : NetAdapterConstants, IDisposable
    {
        /// <summary>
        /// random number generator, used to issue challenges to client </summary>
        protected internal static readonly Random rand = new Random();

        /// <summary>
        /// The adapter this NetAdapter will proxy too </summary>
        protected internal DSPortAdapter adapter = null;

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
        /// The connection that is being serviced.
        /// </summary>
        protected internal NetAdapterConstants_Connection conn;

        /// <summary>
        /// The port we are listening on.
        /// </summary>
        protected internal string serviceName;

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
        /// <param name="serviceName"> the TCP/IP port to listen on for incoming connections
        /// </param>
        /// <exception cref="IOException"> if a network error occurs or the listen socket
        /// cannot be created on the specified port. </exception>
        public NetAdapterHost(DSPortAdapter adapter, string serviceName) : this(adapter, serviceName, false)
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
        /// <param name="serviceName"> the TCP/IP port to listen on for incoming connections </param>
        /// <param name="multiThread"> if true, multiple TCP/IP connections are allowed
        /// to interact simulataneously with this adapter.
        /// </param>
        /// <exception cref="IOException"> if a network error occurs or the listen socket
        /// cannot be created on the specified port. </exception>
        public NetAdapterHost(DSPortAdapter adapter, string serviceName, bool multiThread)
        {
            //save reference to adapter
            this.adapter = adapter;

            // save the port number for later use
            this.serviceName = serviceName;

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
                netAdapterSecret = Encoding.UTF8.GetBytes(NetAdapterConstants_Fields.DEFAULT_SECRET);
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
        public NetAdapterHost(DSPortAdapter adapter, StreamSocketListener serverSock) : this(adapter, serverSock, false)
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
        public NetAdapterHost(DSPortAdapter adapter, StreamSocketListener serverSock, bool multiThread)
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
                netAdapterSecret = Encoding.UTF8.GetBytes(secret);
            }
            else
            {
                netAdapterSecret = Encoding.UTF8.GetBytes(NetAdapterConstants_Fields.DEFAULT_SECRET);
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
                byte[] versionBytes = Convert.toByteArray(NetAdapterConstants_Fields.versionUID);

                // this byte array is 5 because length is used to determine different
                // packet types by client
                byte[] listenPortBytes = new byte[5];
                Encoding.UTF8.GetBytes(port.ToString()).CopyTo(listenPortBytes, 0);
                listenPortBytes[4] = 0x0FF;

                multicastListener = new MulticastListener(port, group, versionBytes, listenPortBytes);
            }
        }


        ///// <summary>
        ///// Run method for threaded NetAdapterHost.  Maintains server socket which
        ///// waits for incoming connections.  Whenever a connection is received
        ///// launches it services the socket or (optionally) launches a new thread
        ///// for servicing the socket.
        ///// </summary>
        //public virtual void run()
        //{
        //hostRunning = true;
        //while (!hostStopped)
        //{
        //Socket sock = null;
        //try
        //{
        //sock = serverSocket.accept();
        //handleConnection(sock);
        //}
        //catch (System.IO.IOException)
        //{
        //try
        //{
        //   if (sock != null)
        //   {
        //	  sock.close();
        //   }
        //}
        //catch (System.IO.IOException)
        //{
        //	;
        //}
        //}
        //}
        //hostRunning = false;
        //}

        /// <summary>
        /// Stops all threads and kills the server socket.
        /// </summary>
        // public virtual void stopHost()
        // {
        // this.hostStopped = true;
        // try
        // {
        // this.serverSocket.close();
        // }
        // catch (System.IO.IOException)
        // {
        //  ;
        // }

        // // wait for run method to quit, with a timeout of 1 second
        // int i = 0;
        // while (hostRunning && i++<100)
        // {
        // try
        // {
        // Thread.Sleep(10);
        // }
        //catch (Exception)
        //{
        // ;
        //}
        // }

        // if (!singleThreaded)
        // {
        // lock (hashHandlers)
        // {
        //	System.Collections.IEnumerator e = hashHandlers.Values.GetEnumerator();
        //	while (e.MoveNext())
        //	{
        //	   ((SocketHandler)e.Current).stopHandler();
        //	}
        // }
        // }

        // if (multicastListener != null)
        // {
        // multicastListener.stopListener();
        // }

        // // ensure that there is no exclusive use of the adapter
        // adapter.endExclusive();
        // }

        public async void StartServer()
        {
            // create the server socket
            this.serverSocket = new StreamSocketListener();
            this.serverSocket.ConnectionReceived += OnConnection;
            this.serverSocket.Control.KeepAlive = false;

            // Start listen operation.
            try
            {
                //if (BindToAny.IsChecked == true)
                //{
                // Don't limit traffic to an address or an adapter.
                await this.serverSocket.BindServiceNameAsync(this.serviceName);
                OneWireEventSource.Log.Info("Listening");
                //}
                //else if (BindToAddress.IsChecked == true)
                //{
                //    // Try to bind to a specific address.
                //    await listener.BindEndpointAsync(selectedLocalHost.LocalHost, ServiceNameForListener.Text);
                //Debug.WriteLine(
                //        "Listening on address " + selectedLocalHost.LocalHost.CanonicalName);
                //}
                //else if (BindToAdapter.IsChecked == true)
                //{
                //    // Try to limit traffic to the selected adapter.
                //    // This option will be overridden by interfaces with weak-host or forwarding modes enabled.
                //    NetworkAdapter selectedAdapter = selectedLocalHost.LocalHost.IPInformation.NetworkAdapter;

                //    // For demo purposes, ensure that we use the same adapter in the client connect scenario.
                //    CoreApplication.Properties.Add("adapter", selectedAdapter);

                //    await listener.BindServiceNameAsync(
                //        ServiceNameForListener.Text,
                //        SocketProtectionLevel.PlainSocket,
                //        selectedAdapter);

                //    Debug.WriteLine(
                //        "Listening on adapter " + selectedAdapter.NetworkAdapterId);
                //}
            }
            catch (Exception exception)
            {
                // If this is an unknown status it means that the error is fatal and retry will likely fail.
                if (SocketError.GetStatus(exception.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }

                OneWireEventSource.Log.Critical("Start listening failed with error: " + exception.Message);
            }
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
            conn.output.WriteInt32(NetAdapterConstants_Fields.versionUID);
            var t = Task.Run(async () => { await conn.output.StoreAsync(); });

            byte retVal = conn.input.ReadByte();

            return (retVal == NetAdapterConstants_Fields.RET_SUCCESS);
        }

        /// <summary>
        /// Reads in command from client and calls the appropriate handler function.
        /// </summary>
        /// <param name="conn"> The connection to send/receive data.
        ///  </param>
        private async void processRequests(NetAdapterConstants_Connection conn)
        {
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("\n------------------------------------------");
            }
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

            // get the next command
            byte cmd = 0x00;

            cmd = conn.input.ReadByte();

            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("CMD received: " + cmd.ToString("x"));
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
                        conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
                        await conn.output.StoreAsync();
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
                conn.output.WriteByte(NetAdapterConstants_Fields.RET_FAILURE);
                conn.output.WriteString(owe.ToString());
                await conn.output.StoreAsync();
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
                    conn.output.DetachStream();
                    conn.output.Dispose();
                    conn.input.DetachStream();
                    conn.input.Dispose();
                    conn.sock.Dispose();
                    serverSocket.Dispose();
                }
            }
            catch (System.IO.IOException)
            { //drain
                ;
            }

            conn.sock = null;
            conn.input = null;
            conn.output = null;
            serverSocket = null;

            // ensure that there is no exclusive use of the adapter
            adapter.endExclusive();
        }

        //--------
        //-------- Finding iButton/1-Wire device options
        //--------

        private async void adapterFindFirstDevice(NetAdapterConstants_Connection conn)
        {
            bool b = adapter.findFirstDevice();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   findFirstDevice returned " + b);
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterFindNextDevice(NetAdapterConstants_Connection conn)
        {
            bool b = adapter.findNextDevice();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   findNextDevice returned " + b);
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterGetAddress(NetAdapterConstants_Connection conn)
        {
            // read in the address
            byte[] address = new byte[8];
            // call getAddress
            adapter.getAddress(address);

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   adapter.getAddress(byte[]) called, speed=" + adapter.Speed);
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBytes(address);
            await conn.output.StoreAsync();
        }

        private async void adapterSetSearchOnlyAlarmingDevices(NetAdapterConstants_Connection conn)
        {
            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   setSearchOnlyAlarmingDevices called, speed=" + adapter.Speed);
            }

            adapter.setSearchOnlyAlarmingDevices();

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterSetNoResetSearch(NetAdapterConstants_Connection conn)
        {
            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   setNoResetSearch called, speed=" + adapter.Speed);
            }

            adapter.setNoResetSearch();

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterSetSearchAllDevices(NetAdapterConstants_Connection conn)
        {
            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   setSearchAllDevices called, speed=" + adapter.Speed);
            }

            adapter.setSearchAllDevices();

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterTargetAllFamilies(NetAdapterConstants_Connection conn)
        {
            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   targetAllFamilies called, speed=" + adapter.Speed);
            }

            adapter.targetAllFamilies();

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterTargetFamily(NetAdapterConstants_Connection conn)
        {
            // get the number of family codes to expect
            int len = conn.input.ReadInt32();
            // get the family codes
            byte[] family = new byte[len];
            conn.input.ReadBytes(family);

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   targetFamily called, speed=" + adapter.Speed);
                OneWireEventSource.Log.Debug("      families: " + Convert.toHexString(family));
            }

            // call targetFamily
            adapter.targetFamily(family);

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterExcludeFamily(NetAdapterConstants_Connection conn)
        {
            // get the number of family codes to expect
            int len = conn.input.ReadInt32();
            // get the family codes
            byte[] family = new byte[len];
            conn.input.ReadBytes(family);

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   excludeFamily called, speed=" + adapter.Speed);
                OneWireEventSource.Log.Debug("      families: " + Convert.toHexString(family));
            }

            // call excludeFamily
            adapter.excludeFamily(family);

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        //--------
        //-------- 1-Wire Network Semaphore methods
        //--------

        private async void adapterBeginExclusive(NetAdapterConstants_Connection conn)
        {
            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   adapter.beginExclusive called, speed=" + adapter.Speed);
            }

            // get blocking boolean
            bool blocking = conn.input.ReadBoolean();
            // call beginExclusive
            bool b = adapter.beginExclusive(blocking);

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("      adapter.beginExclusive returned " + b);
            }
        }

        private async void adapterEndExclusive(NetAdapterConstants_Connection conn)
        {
            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   adapter.endExclusive called, speed=" + adapter.Speed);
            }

            // call endExclusive
            adapter.endExclusive();

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        //--------
        //-------- Primitive 1-Wire Network data methods
        //--------

        private async void adapterReset(NetAdapterConstants_Connection conn)
        {
            int i = adapter.reset();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   reset, speed=" + adapter.Speed + ", returned " + i);
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteInt32(i);
            await conn.output.StoreAsync();
        }

        private async void adapterPutBit(NetAdapterConstants_Connection conn)
        {
            // get the value of the bit
            bool bit = conn.input.ReadBoolean();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   putBit called, speed=" + adapter.Speed);
                OneWireEventSource.Log.Debug("      bit=" + bit);
            }

            adapter.putBit(bit);

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterPutByte(NetAdapterConstants_Connection conn)
        {
            // get the value of the byte
            byte b = conn.input.ReadByte();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   putByte called, speed=" + adapter.Speed);
                OneWireEventSource.Log.Debug("      byte=" + Convert.toHexString(b));
            }

            adapter.putByte(b);

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterGetBit(NetAdapterConstants_Connection conn)
        {
            bool bit = adapter.getBit; //adapter

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   getBit called, speed=" + adapter.Speed);
                OneWireEventSource.Log.Debug("      bit=" + bit);
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBoolean(bit);
            await conn.output.StoreAsync();
        }

        private async void adapterGetByte(NetAdapterConstants_Connection conn)
        {
            byte b = (byte)adapter.Byte;

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   getByte called, speed=" + adapter.Speed);
                OneWireEventSource.Log.Debug("      byte=" + Convert.toHexString((byte)b));
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteByte(b);
            await conn.output.StoreAsync();
        }
        private async void adapterGetBlock(NetAdapterConstants_Connection conn)
        {
            // get the number requested
            int len = conn.input.ReadInt32();
            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   getBlock called, speed=" + adapter.Speed);
                OneWireEventSource.Log.Debug("      len=" + len);
            }

            // get the bytes
            byte[] b = adapter.getBlock(len);

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("      returned: " + Convert.toHexString(b));
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBytes(b);
            await conn.output.StoreAsync();
        }

        private async void adapterDataBlock(NetAdapterConstants_Connection conn)
        {
            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   DataBlock called, speed=" + adapter.Speed);
            }
            // get the number to block
            int len = conn.input.ReadInt32();
            // get the bytes to block
            byte[] b = new byte[len];
            conn.input.ReadBytes(b);

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("      " + len + " bytes");
                OneWireEventSource.Log.Debug("      Send: " + Convert.toHexString(b));
            }

            // do the block
            adapter.dataBlock(b, 0, len);

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("      Recv: " + Convert.toHexString(b));
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBytes(b);
            await conn.output.StoreAsync();
        }

        //--------
        //-------- 1-Wire Network power methods
        //--------

        private async void adapterSetPowerDuration(NetAdapterConstants_Connection conn)
        {
            // get the time factor value
            int timeFactor = conn.input.ReadInt32();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   setPowerDuration called, speed=" + adapter.Speed);
                OneWireEventSource.Log.Debug("      timeFactor=" + timeFactor);
            }

            // call setPowerDuration
            adapter.PowerDuration = timeFactor;

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterStartPowerDelivery(NetAdapterConstants_Connection conn)
        {
            // get the change condition value
            int changeCondition = conn.input.ReadInt32();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   startPowerDelivery called, speed=" + adapter.Speed);
                OneWireEventSource.Log.Debug("      changeCondition=" + changeCondition);
            }

            // call startPowerDelivery
            bool success = adapter.startPowerDelivery(changeCondition);

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBoolean(success);
            await conn.output.StoreAsync();
        }

        private async void adapterSetProgramPulseDuration(NetAdapterConstants_Connection conn)
        {
            // get the time factor value
            int timeFactor = conn.input.ReadInt32();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   setProgramPulseDuration called, speed=" + adapter.Speed);
                OneWireEventSource.Log.Debug("      timeFactor=" + timeFactor);
            }

            // call setProgramPulseDuration
            adapter.ProgramPulseDuration = timeFactor;

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterStartProgramPulse(NetAdapterConstants_Connection conn)
        {
            // get the change condition value
            int changeCondition = conn.input.ReadInt32();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   startProgramPulse called, speed=" + adapter.Speed);
                OneWireEventSource.Log.Debug("      changeCondition=" + changeCondition);
            }

            // call startProgramPulse();
            bool success = adapter.startProgramPulse(changeCondition);

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBoolean(success);
            await conn.output.StoreAsync();
        }

        private async void adapterStartBreak(NetAdapterConstants_Connection conn)
        {
            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   startBreak called, speed=" + adapter.Speed);
            }

            // call startBreak();
            adapter.startBreak();

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterSetPowerNormal(NetAdapterConstants_Connection conn)
        {
            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   setPowerNormal called, speed=" + adapter.Speed);
            }

            // call setPowerNormal
            adapter.setPowerNormal();

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        //--------
        //-------- 1-Wire Network speed methods
        //--------

        private async void adapterSetSpeed(NetAdapterConstants_Connection conn)
        {
            // get the value of the new speed
            int speed = conn.input.ReadInt32();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   setSpeed called, speed=" + adapter.Speed);
                OneWireEventSource.Log.Debug("      speed=" + speed);
            }

            // do the setSpeed
            adapter.Speed = speed;

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterGetSpeed(NetAdapterConstants_Connection conn)
        {
            // get the adapter speed
            int speed = adapter.Speed;

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   getSpeed called, speed=" + adapter.Speed);
                OneWireEventSource.Log.Debug("      speed=" + speed);
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteInt32(speed);
            await conn.output.StoreAsync();
        }


        //--------
        //-------- Adapter feature methods
        //--------

        private async void adapterCanOverdrive(NetAdapterConstants_Connection conn)
        {
            bool b = adapter.canOverdrive();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   canOverdrive returned " + b);
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterCanHyperdrive(NetAdapterConstants_Connection conn)
        {
            bool b = adapter.canHyperdrive();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   canHyperDrive returned " + b);
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterCanFlex(NetAdapterConstants_Connection conn)
        {
            bool b = adapter.canFlex();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   canFlex returned " + b);
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterCanProgram(NetAdapterConstants_Connection conn)
        {
            bool b = adapter.canProgram();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   canProgram returned " + b);
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterCanDeliverPower(NetAdapterConstants_Connection conn)
        {
            bool b = adapter.canDeliverPower();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   canDeliverPower returned " + b);
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterCanDeliverSmartPower(NetAdapterConstants_Connection conn)
        {
            bool b = adapter.canDeliverSmartPower();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   canDeliverSmartPower returned " + b);
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterCanBreak(NetAdapterConstants_Connection conn)
        {
            bool b = adapter.canBreak();

            if (NetAdapterConstants_Fields.DEBUG)
            {
                OneWireEventSource.Log.Debug("   canBreak returned " + b);
            }

            conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        /// <summary>
        /// Constructor for socket servicer.  Creates the input and output
        /// streams and send's the version of this host to the client
        /// connection.
        /// </summary>
        private async void OnConnection(
            StreamSocketListener sender,
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            // create the connection object
            conn = new NetAdapterConstants_Connection();
            conn.sock = args.Socket;

            conn.input = new DataReader(args.Socket.InputStream);
            conn.output = new DataWriter(args.Socket.OutputStream);

            // first thing transmitted should be version info
            if (!sendVersionUID(conn))
            {
                throw new System.IO.IOException("send version failed");
            }

            // authenticate the client
            byte[] chlg = new byte[8];
            rand.NextBytes(chlg);
            conn.output.WriteBytes(chlg);
            await conn.output.StoreAsync();

            // compute the crc of the secret and the challenge
            int crc = CRC16.compute(netAdapterSecret, 0);
            crc = CRC16.compute(chlg, crc);
            int answer = conn.input.ReadInt32();
            if (answer != crc)
            {
                conn.output.WriteByte(NetAdapterConstants_Fields.RET_FAILURE);
                conn.output.WriteString("Client Authentication Failed");
                await conn.output.StoreAsync();
                throw new System.IO.IOException("authentication failed");
            }
            else
            {
                conn.output.WriteByte(NetAdapterConstants_Fields.RET_SUCCESS);
                await conn.output.StoreAsync();
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
            DSPortAdapter adapter = OneWireAccessProvider.DefaultAdapter;

            NetAdapterHost host = new NetAdapterHost(adapter, true);

            OneWireEventSource.Log.Info("Starting Multicast Listener");
            host.createMulticastListener();

            OneWireEventSource.Log.Info("Starting NetAdapter Host");
            host.StartServer();

            while (true) { ; }

            //if(System.in!=null)
            //{
            //   System.out.println("\nPress Enter to Shutdown");
            //   (new BufferedReader(new InputStreamReader(System.in))).readLine();
            //   host.stopHost();
            //   System.exit(1);
            //}
        }

        ~NetAdapterHost()
        {
            Dispose(false);
        }

        public void Close()
        {
            Dispose();
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
                if (multicastListener != null)
                {
                    multicastListener.Dispose();
                    multicastListener = null;
                }
            }
        }

    }
}