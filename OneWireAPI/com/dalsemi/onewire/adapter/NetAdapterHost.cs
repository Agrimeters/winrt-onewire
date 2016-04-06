using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Connectivity;
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
    public class NetAdapterHost : IDisposable
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

        // List containing all available local HostName endpoints
        private List<LocalHostItem> localHostItems = new List<LocalHostItem>();

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
        public NetAdapterHost(DSPortAdapter adapter) : this(adapter, NetAdapterConstants.DEFAULT_PORT, false)
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
        public NetAdapterHost(DSPortAdapter adapter, bool multiThread) : this(adapter, NetAdapterConstants.DEFAULT_PORT, multiThread)
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
                netAdapterSecret = Encoding.UTF8.GetBytes(NetAdapterConstants.DEFAULT_SECRET);
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
        /// this NetAdapterHost automatically.  Uses defaults for Multicast group
        /// and port.
        /// </summary>
        public virtual void createMulticastListener()
        {
            createMulticastListener(NetAdapterConstants.DEFAULT_MULTICAST_PORT);
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
                group = NetAdapterConstants.DEFAULT_MULTICAST_GROUP;
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
                //Socket sock = null;
                //try
                //{
                //    sock = serverSocket.accept();
                //    handleConnection(sock);
                //}
                //catch (System.IO.IOException)
                //{
                //    try
                //    {
                //        if (sock != null)
                //        {
                //            sock.close();
                //        }
                //    }
                //    catch (System.IO.IOException)
                //    {
                //        ;
                //    }
                //}
            }
            hostRunning = false;
        }

        /// <summary>
        /// Stops all threads and kills the server socket.
        /// </summary>
        public virtual void stopHost()
        {
            OneWireEventSource.Log.Debug("stopHost +");

            this.hostStopped = true;
            try
            {
                this.serverSocket.ConnectionReceived -= OnConnection;
                this.serverSocket.Dispose();
            }
            catch (System.IO.IOException)
            {
                ;
            }

            // wait for run method to quit, with a timeout of 1 second
            int i = 0;
            while (hostRunning && i++ < 100)
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
                    foreach (SocketHandler socket in hashHandlers.Values)
                        socket.stopHandler();
                }
            }

            if (multicastListener != null)
            {
                multicastListener.stopListener();
            }

            // ensure that there is no exclusive use of the adapter
            adapter.endExclusive();

            OneWireEventSource.Log.Debug("stopHost -");
        }

        private void PopulateHostList()
        {
            // populate localHost List
            localHostItems.Clear();
            foreach (HostName localHostInfo in NetworkInformation.GetHostNames())
            {
                if (localHostInfo.IPInformation != null)
                {
                    LocalHostItem adapterItem = new LocalHostItem(localHostInfo);
                    localHostItems.Add(adapterItem);
                }
            }
        }

        public async void StartServer()
        {
            OneWireEventSource.Log.Debug("StartServer +");

            LocalHostItem selectedLocalHost = null;

            PopulateHostList();

            string HostNameForServer = OneWireAccessProvider.getProperty("NetAdapter.HostName");
            if (!string.ReferenceEquals(HostNameForServer, null))
            {
                HostName hostName;

                try
                {
                    hostName = new HostName(HostNameForServer);
                }
                catch (ArgumentException)
                {
                    OneWireEventSource.Log.Critical("Error: Invalid host name specified: " + HostNameForServer);
                    OneWireEventSource.Log.Critical("Available HostNames:");
                    foreach (var intf in localHostItems)
                        OneWireEventSource.Log.Critical(intf.LocalHost.CanonicalName);
                    return;
                }

                bool valid_address = false;

                if (hostName.CanonicalName.Equals("127.0.0.1"))
                {
                    selectedLocalHost = new LocalHostItem(hostName);
                    valid_address = true;
                }
                else
                {
                    foreach (var item in localHostItems)
                    {
                        if (item.LocalHost.CanonicalName.Equals(hostName.CanonicalName))
                        {
                            selectedLocalHost = item;
                            valid_address = true;
                            break;
                        }
                    }
                }

                if (!valid_address)
                {
                    OneWireEventSource.Log.Critical("Error: Invalid host name specified: " + HostNameForServer);
                    OneWireEventSource.Log.Critical("Available HostNames:");
                    foreach (var intf in localHostItems)
                        OneWireEventSource.Log.Critical(intf.DisplayString);
                    selectedLocalHost = null;
                }
            }

            try
            {
                // create the server socket
                this.serverSocket = new StreamSocketListener();
                this.serverSocket.ConnectionReceived += OnConnection;
                this.serverSocket.Control.KeepAlive = false;

                if (selectedLocalHost != null)
                {
                    await this.serverSocket.BindEndpointAsync(selectedLocalHost.LocalHost, this.serviceName);
                    OneWireEventSource.Log.Info("NetAdapter started listening on " + selectedLocalHost.LocalHost.CanonicalName + ":" + this.serviceName);
                }
                else
                {
                    await this.serverSocket.BindServiceNameAsync(this.serviceName);
                    OneWireEventSource.Log.Info("NetAdapter started listening on:");
                    foreach (var intf in localHostItems)
                        OneWireEventSource.Log.Info("\t" + intf.LocalHost.CanonicalName + ":" + this.serviceName);
                }

                hostStopped = false;
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

            OneWireEventSource.Log.Debug("StartServer -");
        }

        /// <summary>
        /// Transmits the versionUID of the current NetAdapter protocol to
        /// the client connection.  If it matches the clients versionUID,
        /// the client returns RET_SUCCESS.
        /// </summary>
        /// <param name="conn"> The connection to send/receive data. </param>
        /// <returns> <code>true</code> if the versionUID matched. </returns>
        private async Task<bool> sendVersionUID(NetAdapterConstants.Connection conn)
        {
            bool result = false;

            try
            {
                // write server version
                conn.output.WriteInt32(NetAdapterConstants.versionUID);
                await conn.output.StoreAsync();

                byte[] val = conn.ReadBlocking(conn, sizeof(byte));
                result = (val[0] == NetAdapterConstants.RET_SUCCESS);
            }
            catch (Exception e)
            {
                // If this is an unknown status it means that the error if fatal and retry will likely fail.
                if (SocketError.GetStatus(e.HResult) == SocketErrorStatus.Unknown)
                {
                    throw;
                }
            }

            return result;
        }

        /// <summary>
        /// Reads in command from client and calls the appropriate handler function.
        /// </summary>
        /// <param name="conn"> The connection to send/receive data.
        ///  </param>
        private async void processRequests(NetAdapterConstants.Connection conn)
        {
            // get the next command
            //byte cmd = 0x00;

            byte[] cmd = conn.ReadBlocking(conn, 1);
            if (cmd.Equals(null))
                return;

            OneWireEventSource.Log.Debug("\n------------------------------------------");
            //OneWireEventSource.Log.Debug("CMD received: " + cmd[0].ToString("X"));

            try
            {
                // ... and fire the appropriate method
                switch (cmd[0])
                {
                    /* Connection keep-alive and close commands */
                    case NetAdapterConstants.CMD_PINGCONNECTION:
                        // no-op, might update timer of some sort later
                        conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
                        await conn.output.StoreAsync();
                        break;

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
                        OneWireEventSource.Log.Debug("Unknown command: " + cmd[0].ToString("X"));
                        break;
                }
            }
            catch (OneWireException owe)
            {
                conn.output.WriteByte(NetAdapterConstants.RET_FAILURE);
                conn.output.WriteString(owe.ToString());
                await conn.output.StoreAsync();
            }
        }

        /// <summary>
        /// Closes the provided connection.
        /// </summary>
        /// <param name="conn"> The connection to send/receive data. </param>
        private void close(NetAdapterConstants.Connection conn)
        {
            OneWireEventSource.Log.Debug("StreamSocket close +");

            try
            {
                if (conn.sock != null)
                {
                    conn.cts.Cancel();

                    conn.input.Dispose();
                    conn.output.Dispose();
                    conn.sock.Dispose();

                    conn.input = null;
                    conn.output = null;
                    conn.sock = null;
                }
            }
            catch (System.IO.IOException)
            { //drain
                ;
            }

            // ensure that there is no exclusive use of the adapter
            adapter.endExclusive();

            OneWireEventSource.Log.Debug("StreamSocket close -");
        }

        //--------
        //-------- Finding iButton/1-Wire device options
        //--------

        private async void adapterFindFirstDevice(NetAdapterConstants.Connection conn)
        {
            bool b = adapter.findFirstDevice();

            OneWireEventSource.Log.Debug("   findFirstDevice returned " + b);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterFindNextDevice(NetAdapterConstants.Connection conn)
        {
            bool b = adapter.findNextDevice();

            OneWireEventSource.Log.Debug("   findNextDevice returned " + b);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterGetAddress(NetAdapterConstants.Connection conn)
        {
            // read in the address
            byte[] address = new byte[8];
            // call getAddress
            adapter.getAddress(address);

            OneWireEventSource.Log.Debug("   adapter.getAddress(byte[]) called, speed=" + adapter.Speed);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBytes(address);
            await conn.output.StoreAsync();
        }

        private async void adapterSetSearchOnlyAlarmingDevices(NetAdapterConstants.Connection conn)
        {
            OneWireEventSource.Log.Debug("   setSearchOnlyAlarmingDevices called, speed=" + adapter.Speed);

            adapter.setSearchOnlyAlarmingDevices();

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterSetNoResetSearch(NetAdapterConstants.Connection conn)
        {
            OneWireEventSource.Log.Debug("   setNoResetSearch called, speed=" + adapter.Speed);

            adapter.setNoResetSearch();

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterSetSearchAllDevices(NetAdapterConstants.Connection conn)
        {
            OneWireEventSource.Log.Debug("   setSearchAllDevices called, speed=" + adapter.Speed);

            adapter.setSearchAllDevices();

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterTargetAllFamilies(NetAdapterConstants.Connection conn)
        {
            OneWireEventSource.Log.Debug("   targetAllFamilies called, speed=" + adapter.Speed);

            adapter.targetAllFamilies();

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterTargetFamily(NetAdapterConstants.Connection conn)
        {
            // get the number of family codes to expect
            byte[] len = conn.ReadBlocking(conn, sizeof(Int32));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(len);

            // get the family codes
            byte[] family = conn.ReadBlocking(conn, (uint)BitConverter.ToInt32(len, 0));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(family);

            OneWireEventSource.Log.Debug("   targetFamily called, speed=" + adapter.Speed);
            OneWireEventSource.Log.Debug("      families: " + Convert.toHexString(family));

            // call targetFamily
            adapter.targetFamily(family);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterExcludeFamily(NetAdapterConstants.Connection conn)
        {
            // get the number of family codes to expect
            byte[] len = conn.ReadBlocking(conn, sizeof(Int32));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(len);

            // get the family codes
            byte[] family = conn.ReadBlocking(conn, (uint)BitConverter.ToInt32(len, 0));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(family);

            OneWireEventSource.Log.Debug("   excludeFamily called, speed=" + adapter.Speed);
            OneWireEventSource.Log.Debug("      families: " + Convert.toHexString(family));

            // call excludeFamily
            adapter.excludeFamily(family);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        //--------
        //-------- 1-Wire Network Semaphore methods
        //--------

        private async void adapterBeginExclusive(NetAdapterConstants.Connection conn)
        {
            OneWireEventSource.Log.Debug("   adapter.beginExclusive called, speed=" + adapter.Speed);

            // get blocking boolean
            byte[] res = conn.ReadBlocking(conn, sizeof(bool));
            bool blocking = BitConverter.ToBoolean(res, 0);

            // call beginExclusive
            bool b = adapter.beginExclusive(blocking);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();

            OneWireEventSource.Log.Debug("      adapter.beginExclusive returned " + b);
        }

        private async void adapterEndExclusive(NetAdapterConstants.Connection conn)
        {
            OneWireEventSource.Log.Debug("   adapter.endExclusive called, speed=" + adapter.Speed);

            // call endExclusive
            adapter.endExclusive();

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        //--------
        //-------- Primitive 1-Wire Network data methods
        //--------

        private async void adapterReset(NetAdapterConstants.Connection conn)
        {
            int i = adapter.reset();

            OneWireEventSource.Log.Debug("   reset, speed=" + adapter.Speed + ", returned " + i);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteInt32(i);
            await conn.output.StoreAsync();
        }

        private async void adapterPutBit(NetAdapterConstants.Connection conn)
        {
            // get the value of the bit
            byte[] res = conn.ReadBlocking(conn, sizeof(bool));
            bool bit = BitConverter.ToBoolean(res, 0);

            OneWireEventSource.Log.Debug("   putBit called, speed=" + adapter.Speed);
            OneWireEventSource.Log.Debug("      bit=" + bit);

            adapter.putBit(bit);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterPutByte(NetAdapterConstants.Connection conn)
        {
            // get the value of the byte
            byte[] b = conn.ReadBlocking(conn, sizeof(byte));

            OneWireEventSource.Log.Debug("   putByte called, speed=" + adapter.Speed);
            OneWireEventSource.Log.Debug("      byte=" + Convert.toHexString(b[0]));

            adapter.putByte(b[0]);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterGetBit(NetAdapterConstants.Connection conn)
        {
            bool bit = adapter.getBit;

            OneWireEventSource.Log.Debug("   getBit called, speed=" + adapter.Speed);
            OneWireEventSource.Log.Debug("      bit=" + bit);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBoolean(bit);
            await conn.output.StoreAsync();
        }

        private async void adapterGetByte(NetAdapterConstants.Connection conn)
        {
            byte b = (byte)adapter.Byte;

            OneWireEventSource.Log.Debug("   getByte called, speed=" + adapter.Speed);
            OneWireEventSource.Log.Debug("      byte=" + Convert.toHexString((byte)b));

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteByte(b);
            await conn.output.StoreAsync();
        }

        private async void adapterGetBlock(NetAdapterConstants.Connection conn)
        {
            // get the number requested
            byte[] res = conn.ReadBlocking(conn, sizeof(Int32));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);
            int len = Convert.toInt(res);

            OneWireEventSource.Log.Debug("   getBlock called, speed=" + adapter.Speed);
            OneWireEventSource.Log.Debug("      len=" + len);

            // get the bytes
            byte[] b = adapter.getBlock(len);
            OneWireEventSource.Log.Debug("      returned: " + Convert.toHexString(b));

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBytes(b);
            await conn.output.StoreAsync();
        }

        private async void adapterDataBlock(NetAdapterConstants.Connection conn)
        {
            OneWireEventSource.Log.Debug("   DataBlock called, speed=" + adapter.Speed);

            // get the number to block
            byte[] res = conn.ReadBlocking(conn, sizeof(Int32));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);
            int len = Convert.toInt(res);

            // get the bytes to block
            byte[] b = conn.ReadBlocking(conn, (uint)len);
            OneWireEventSource.Log.Debug("      " + len + " bytes");
            OneWireEventSource.Log.Debug("      Send: " + Convert.toHexString(b));

            // do the block
            adapter.dataBlock(b, 0, len);

            OneWireEventSource.Log.Debug("      Recv: " + Convert.toHexString(b));

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBytes(b);
            await conn.output.StoreAsync();
        }

        //--------
        //-------- 1-Wire Network power methods
        //--------

        private async void adapterSetPowerDuration(NetAdapterConstants.Connection conn)
        {
            // get the time factor value
            byte[] res = conn.ReadBlocking(conn, sizeof(Int32));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);
            int timeFactor = Convert.toInt(res);

            OneWireEventSource.Log.Debug("   setPowerDuration called, speed=" + adapter.Speed);
            OneWireEventSource.Log.Debug("      timeFactor=" + timeFactor);

            // call setPowerDuration
            adapter.PowerDuration = timeFactor;

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterStartPowerDelivery(NetAdapterConstants.Connection conn)
        {
            // get the change condition value
            byte[] res = conn.ReadBlocking(conn, sizeof(Int32));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);
            int changeCondition = Convert.toInt(res);

            OneWireEventSource.Log.Debug("   startPowerDelivery called, speed=" + adapter.Speed);
            OneWireEventSource.Log.Debug("      changeCondition=" + changeCondition);

            bool success = adapter.startPowerDelivery(changeCondition);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBoolean(success);
            await conn.output.StoreAsync();
        }

        private async void adapterSetProgramPulseDuration(NetAdapterConstants.Connection conn)
        {
            // get the time factor value
            byte[] res = conn.ReadBlocking(conn, sizeof(Int32));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);
            int timeFactor = Convert.toInt(res);

            OneWireEventSource.Log.Debug("   setProgramPulseDuration called, speed=" + adapter.Speed);
            OneWireEventSource.Log.Debug("      timeFactor=" + timeFactor);

            adapter.ProgramPulseDuration = timeFactor;

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterStartProgramPulse(NetAdapterConstants.Connection conn)
        {
            // get the change condition value
            byte[] res = conn.ReadBlocking(conn, sizeof(Int32));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);
            int changeCondition = Convert.toInt(res);

            OneWireEventSource.Log.Debug("   startProgramPulse called, speed=" + adapter.Speed);
            OneWireEventSource.Log.Debug("      changeCondition=" + changeCondition);

            bool success = adapter.startProgramPulse(changeCondition);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBoolean(success);
            await conn.output.StoreAsync();
        }

        private async void adapterStartBreak(NetAdapterConstants.Connection conn)
        {
            OneWireEventSource.Log.Debug("   startBreak called, speed=" + adapter.Speed);

            adapter.startBreak();

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterSetPowerNormal(NetAdapterConstants.Connection conn)
        {
            OneWireEventSource.Log.Debug("   setPowerNormal called, speed=" + adapter.Speed);

            adapter.setPowerNormal();

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        //--------
        //-------- 1-Wire Network speed methods
        //--------

        private async void adapterSetSpeed(NetAdapterConstants.Connection conn)
        {
            // get the value of the new speed
            byte[] res = conn.ReadBlocking(conn, sizeof(Int32));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(res);
            int speed = Convert.toInt(res);

            OneWireEventSource.Log.Debug("   setSpeed called, speed=" + adapter.Speed);
            OneWireEventSource.Log.Debug("      speed=" + speed);

            adapter.Speed = speed;

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            await conn.output.StoreAsync();
        }

        private async void adapterGetSpeed(NetAdapterConstants.Connection conn)
        {
            int speed = adapter.Speed;

            OneWireEventSource.Log.Debug("   getSpeed called, speed=" + adapter.Speed);
            OneWireEventSource.Log.Debug("      speed=" + speed);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteInt32(speed);
            await conn.output.StoreAsync();
        }

        //--------
        //-------- Adapter feature methods
        //--------

        private async void adapterCanOverdrive(NetAdapterConstants.Connection conn)
        {
            bool b = adapter.canOverdrive();

            OneWireEventSource.Log.Debug("   canOverdrive returned " + b);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterCanHyperdrive(NetAdapterConstants.Connection conn)
        {
            bool b = adapter.canHyperdrive();

            OneWireEventSource.Log.Debug("   canHyperDrive returned " + b);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterCanFlex(NetAdapterConstants.Connection conn)
        {
            bool b = adapter.canFlex();

            OneWireEventSource.Log.Debug("   canFlex returned " + b);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterCanProgram(NetAdapterConstants.Connection conn)
        {
            bool b = adapter.canProgram();

            OneWireEventSource.Log.Debug("   canProgram returned " + b);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterCanDeliverPower(NetAdapterConstants.Connection conn)
        {
            bool b = adapter.canDeliverPower();

            OneWireEventSource.Log.Debug("   canDeliverPower returned " + b);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterCanDeliverSmartPower(NetAdapterConstants.Connection conn)
        {
            bool b = adapter.canDeliverSmartPower();

            OneWireEventSource.Log.Debug("   canDeliverSmartPower returned " + b);

            conn.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
            conn.output.WriteBoolean(b);
            await conn.output.StoreAsync();
        }

        private async void adapterCanBreak(NetAdapterConstants.Connection conn)
        {
            bool b = adapter.canBreak();

            OneWireEventSource.Log.Debug("   canBreak returned " + b);

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
        public class SocketHandler
        {
            private readonly NetAdapterHost outerInstance;

            /// <summary>
            /// The connection that is being serviced.
            /// </summary>
            private NetAdapterConstants.Connection conn = null;

            /// <summary>
            /// indicates whether or not the handler is currently running
            /// </summary>
            private volatile bool handlerRunning = false;

            private async Task<bool> SetupConnection(NetAdapterHost outerInstance, NetAdapterConstants.Connection c)
            {
                // first thing transmitted should be version info
                bool result = await outerInstance.sendVersionUID(c);
                if (!result)
                {
                    throw new System.IO.IOException("send version failed");
                }

                // authenticate the client
                byte[] chlg = new byte[8];
                rand.NextBytes(chlg);
                c.output.WriteBytes(chlg);
                await c.output.StoreAsync();

                // compute the crc of the secret and the challenge
                int crc = CRC16.compute(outerInstance.netAdapterSecret, 0);
                crc = CRC16.compute(chlg, crc);

                byte[] res = conn.ReadBlocking(conn, sizeof(Int32));
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(res);
                int answer = BitConverter.ToInt32(res, 0);

                if (answer != crc)
                {
                    c.output.WriteByte(NetAdapterConstants.RET_FAILURE);
                    c.output.WriteString("Client Authentication Failed");
                    await c.output.StoreAsync();
                    throw new System.IO.IOException("authentication failed");
                }
                else
                {
                    c.output.WriteByte(NetAdapterConstants.RET_SUCCESS);
                    await c.output.StoreAsync();
                }

                return true;
            }

            /// <summary>
            /// Constructor for socket servicer.  Creates the input and output
            /// streams and send's the version of this host to the client
            /// connection.
            /// </summary>
            public SocketHandler(NetAdapterHost outerInstance, StreamSocket sock)
            {
                this.outerInstance = outerInstance;

                // set socket timeout to 10 seconds
                //TODO sock.SoTimeout = outerInstance.timeoutInSeconds * 1000;

                // create the connection object
                this.conn = new NetAdapterConstants.Connection();
                this.conn.sock = sock;
                //                this.conn.sock.Control.NoDelay = true;

                this.conn.cts = new CancellationTokenSource();
                this.conn.input = new DataReader(this.conn.sock.InputStream);
                this.conn.output = new DataWriter(this.conn.sock.OutputStream);

                this.conn.input.InputStreamOptions = InputStreamOptions.Partial;

                var t = Task.Run(async () => { await SetupConnection(outerInstance, this.conn); });
                t.Wait();
            }

            /// <summary>
            /// Run method for socket Servicer.
            /// </summary>
            public virtual void run()
            {
                handlerRunning = true;
                try
                {
                    var token = this.conn.cts.Token;

                    while (!outerInstance.hostStopped && this.conn.sock != null)
                    {
                        outerInstance.processRequests(this.conn);
                    }
                }
                catch (Exception t)
                {
                    OneWireEventSource.Log.Error(t.ToString());
                    OneWireEventSource.Log.Error(t.StackTrace);
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
                while (handlerRunning && i++ < timeout)
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

        /// <summary>
        /// Constructor for socket servicer.  Creates the input and output
        /// streams and send's the version of this host to the client
        /// connection.
        /// </summary>
        private void OnConnection(
            StreamSocketListener sender,
            StreamSocketListenerConnectionReceivedEventArgs args)
        {
            OneWireEventSource.Log.Debug("Connection Established with " +
                args.Socket.Information.RemoteAddress + ":" + args.Socket.Information.RemotePort);

            SocketHandler sh = new SocketHandler(this, args.Socket);
            if (singleThreaded)
            {
                // single-threaded
                sh.run();
            }
            else
            {
                // multi-threaded
                Task t = Task.Run(() => { sh.run(); });
                lock (hashHandlers)
                {
                    hashHandlers[t] = sh;
                }
            }
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

                OnDispose(this, EventArgs.Empty);
            }
        }

        public event EventHandler OnDispose = delegate { };

        /// <summary>
        /// Helper class describing a NetworkAdapter and its associated IP address
        /// </summary>
        private class LocalHostItem
        {
            public string DisplayString
            {
                get;
                private set;
            }

            public HostName LocalHost
            {
                get;
                private set;
            }

            public LocalHostItem(HostName localHostName)
            {
                if (localHostName == null)
                {
                    throw new ArgumentNullException("localHostName");
                }

                if (localHostName.IPInformation == null)
                {
                    throw new ArgumentException("Adapter information not found");
                }

                this.LocalHost = localHostName;
                this.DisplayString = "Address: " + localHostName.DisplayName +
                    " Adapter: " + localHostName.IPInformation.NetworkAdapter.NetworkAdapterId;
            }
        }
    }
}