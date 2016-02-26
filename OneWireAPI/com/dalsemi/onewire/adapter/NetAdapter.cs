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
	/// <P>NetAdapter is a network-based DSPortAdapter.  It allows for the use of
	/// an actual DSPortAdapter which isn't on the local machine, but rather is
	/// connected to another device which is reachable via a TCP/IP network
	/// connection.</P>
	/// 
	/// <P>The syntax for the <code>selectPort(String)</code> command is the
	/// hostname of the computer which hosts the actual DSPortAdapter and the
	/// TCP/IP port that the host is listening on.  If the port number is not
	/// specified, a default value of 6161 is used. Here are a few examples to
	/// illustrate the syntax:
	/// <ul>
	///    <li>my.host.com:6060</li>
	///    <li>180.0.2.46:6262</li>
	///    <li>my.host.com</li>
	///    <li>180.0.2.46</li>
	/// </ul></P>
	/// 
	/// <P?The use of the NetAdapter is virtually identical to the use of any
	/// other DSPortAdapter.  The only significant changes are the necessity
	/// of the 'host' component (see NetAdapterHost)
	/// and the discovery of hosts on your network.  There are currently two
	/// techniques used for discovering all of the hosts: The look-up of each host
	/// from the onewire.properties file and the use of multicast sockets for
	/// automatic discovery.</P>
	/// 
	/// <P>In the onewire.properties file, you can add a host to your list of valid
	/// hosts by making a NetAdapter.host with an integer to distinguish the hosts.
	/// There is no limit on the number of hosts which can appear in this list, but
	/// the first one must be numbered '0'.  These hosts will then be returned in
	/// the list of valid 'ports' from the <code>selectPortNames()</code> method.
	/// Note that there do not have to be any servers returned from
	/// <code>selectPortNames()</code> for the NetAdapter to be able to connect
	/// to them (so it isn't necessary to add these entries for it to function),
	/// but applications which allow a user to automatically select an appropriate
	/// adapter and a port from a given list will not function properly without it.
	/// For example:
	/// <ul>
	///    <li>NetAdapter.host0=my.host.com:6060</li>
	///    <li>NetAdapter.host1=180.0.2.46:6262</li>
	///    <li>NetAdapter.host2=my.host.com</li>
	///    <li>NetAdapter.host3=180.0.2.46</li>
	/// </ul></P>
	/// 
	/// <P>The multicast socket technique allows you to automatically discover
	/// hosts on your subnet which are listening for multicast packets.  By
	/// default, the multicast discovery of NetAdapter hosts is disabled.
	/// When enabled, the NetAdapter creates a multicast socket and looks for servers
	/// every time you call <code>selectPortNames()</code>.  This will add a
	/// 1 second delay (due to the socket timeout) on calling the method.  If you'd
	/// like to enable this feature, add the following line to your
	/// onewire.properties file:
	/// <ul>
	///    <li>NetAdapter.MulticastEnabled=true</li>
	/// </ul>
	/// The port used and the multicast group used for multicast sockets can
	/// also be changed.  The group however, must fall withing a valid range.
	/// For more information about multicast sockets in Java, see the Java
	/// tutorial on networking at <A HREF="http://java.sun.com/docs/books/tutorial/">
	/// http://java.sun.com/docs/books/tutorial/</A>.  Change the defaults in the
	/// onewire.properties file with the following entries:
	/// <ul>
	///    <li>NetAdapter.MulticastGroup=228.5.6.7</li>
	///    <li>NetAdapter.MulticastPort=6163</li>
	/// </ul>
	/// </P>
	/// 
	/// <P>Once the NetAdapter is connected with a host, a version check is performed
	/// followed by a simple authentication step.  The authentication is dependent
	/// upon a secret shared between the NetAdapter and the host.  Both will use
	/// a default value, that each will agree with if you don't provide a secret
	/// of your own.  To set the secret, add the following line to your
	/// onewire.properties file:
	/// <ul>
	///    <li>NetAdapter.secret="This is my custom secret"</li>
	/// </ul>
	/// Optionally, the secret can be specified on a per-host basis by simply
	/// adding the secret after the port number followed by a colon.  If no port
	/// number is specified, a double-colon is required.  Here are examples:
	/// <ul>
	///    <li>my.host.com:6060:my custom secret</li>
	///    <li>180.0.2.46:6262:another custom secret</li>
	///    <li>my.host.com::the custom secret without port number</li>
	///    <li>180.0.2.46::another example of a custom secret</li>
	/// </ul></P>
	/// 
	/// <P>All of the above mentioned properties can be set on the command-line
	/// as well as being set in the onewire.properties file.  To set the
	/// properties on the command-line, use the -D option:
	/// java -DNetAdapter.Secret="custom secret" myApplication</P>
	/// 
	/// <P>The following is a list of all parameters that can be set for the
	/// NetAdapter, followed by default values where applicable.<br>
	/// <ul>
	///    <li>NetAdapter.secret=Adapter Secret Default</li>
	///    <li>NetAdapter.secret[0-MaxInt]=[no default]</li>
	///    <li>NetAdapter.host[0-MaxInt]=[no default]</li>
	///    <li>NetAdapter.MulticastEnabled=false</li>
	///    <li>NetAdapter.MulticastGroup=228.5.6.7</li>
	///    <li>NetAdapter.MulticastPort=6163</li>
	/// </ul></P>
	/// 
	/// <para>If you wanted added security on the communication channel, an SSL socket
	/// (or similar custom socket implementation) can be used by circumventing the
	/// standard DSPortAdapter's <code>selectPort(String)</code> and using the
	/// NetAdapter-specific <code>selectPort(Socket)</code>.  For example:
	/// <pre>
	///    NetAdapter na = new NetAdapter();
	/// 
	///    Socket secureSocket = // insert fancy secure socket implementation here
	/// 
	///    na.selectPort(secureSocket);
	/// <pre></P>
	/// 
	/// <P>For information on setting up the host component, see the JavaDocs
	/// for the <code>NetAdapterHost</code>
	/// 
	/// </para>
	/// </summary>
	/// <seealso cref= NetAdapterHost
	/// 
	/// @author SH
	/// @version    1.00, 9 Jan 2002 </seealso>
	public class NetAdapter : DSPortAdapter, NetAdapterConstants
	{
	   /// <summary>
	   /// Error message when neither RET_SUCCESS or RET_FAILURE are returned </summary>
	   protected internal const string UNSPECIFIED_ERROR = "An unspecified error occurred.";
	   /// <summary>
	   /// Error message when I/O failure occurs </summary>
	   protected internal const string COMM_FAILED = "IO Error: ";

	   /// <summary>
	   /// constant for no exclusive lock </summary>
	   protected internal static readonly int? NOT_OWNED = new int?(0);
	   /// <summary>
	   /// Keeps hash of current thread for exclusive lock </summary>
	   protected internal int? currentThreadHash = NOT_OWNED;

	   /// <summary>
	   /// instance for current connection, defaults to EMPTY </summary>
	   protected internal NetAdapterConstants_Connection conn = NetAdapterConstants_Fields.EMPTY_CONNECTION;

	   /// <summary>
	   /// portName For Reconnecting to Host </summary>
	   protected internal string portNameForReconnect = null;

	   /// <summary>
	   /// secret for authentication with the server </summary>
	   protected internal sbyte[] netAdapterSecret = null;

	   /// <summary>
	   /// if true, the user used a custom secret </summary>
	   protected internal bool useCustomSecret = false;

	   //-------
	   //------- Multicast variables
	   //-------

	   /// <summary>
	   /// indicates whether or not mulicast is enabled </summary>
	   protected internal bool? multicastEnabled = null;

	   /// <summary>
	   /// The multicast group to use for NetAdapter Datagram packets </summary>
	   protected internal string multicastGroup = null;

	   /// <summary>
	   /// The port to use for NetAdapter Datagram packets </summary>
	   protected internal int datagramPort = -1;

	   /// <summary>
	   /// Creates an instance of NetAdapter that isn't connected.  Must call
	   /// selectPort(String); or selectPort(Socket);
	   /// </summary>
	   public NetAdapter()
	   {
		  try
		  {
			 resetSecret();
		  }
		  catch (Exception)
		  {
			 Secret = NetAdapterConstants_Fields.DEFAULT_SECRET;
		  }
	   }

	   /// <summary>
	   /// Sets the shared secret for authenticating this NetAdapter with
	   /// a NetAdapterHost.
	   /// </summary>
	   /// <param name="secret"> the new secret for authenticating this client. </param>
	   public virtual string Secret
	   {
		   set
		   {
			  if (!string.ReferenceEquals(value, null))
			  {
				 this.netAdapterSecret = value.GetBytes();
			  }
			  else
			  {
				 resetSecret();
			  }
		   }
	   }

	   /// <summary>
	   /// Resets the secret to be the default stored in the onewire.properties
	   /// file (if there is one), or the default as defined by NetAdapterConstants.
	   /// </summary>
	   public virtual void resetSecret()
	   {
		  string secret = OneWireAccessProvider.getProperty("NetAdapter.Secret");
		  if (!string.ReferenceEquals(secret, null))
		  {
			 this.netAdapterSecret = secret.GetBytes();
		  }
		  else
		  {
			 this.netAdapterSecret = NetAdapterConstants_Fields.DEFAULT_SECRET.GetBytes();
		  }
	   }

	   /// <summary>
	   /// Checks return value from input stream.  Reads one byte.  If that
	   /// byte is not equal to RET_SUCCESS, then it tries to create an
	   /// appropriate error message.  If it is RET_FAILURE, it reads a
	   /// string representing the error message.  If it is neither, it
	   /// wraps an error message indicating that an unspecified error
	   /// occurred and attemps a reconnect.
	   /// </summary>
	   private void checkReturnValue(NetAdapterConstants_Connection conn)
	   {
		  sbyte retVal = conn.input.readByte();
		  if (retVal != NetAdapterConstants_Fields.RET_SUCCESS)
		  {
			 // an error occurred
			 string errorMsg;
			 if (retVal == NetAdapterConstants_Fields.RET_FAILURE)
			 {
				// should be a standard error message after RET_FAILURE
				errorMsg = conn.input.readUTF();
			 }
			 else
			 {
				// didn't even get RET_FAILURE
				errorMsg = UNSPECIFIED_ERROR;

				// that probably means we have a major communication error.
				// better to disconnect and reconnect.
				freePort();
				selectPort(portNameForReconnect);
			 }

			 throw new OneWireIOException(errorMsg);
		  }
	   }

	   /// <summary>
	   /// Sends a ping to the host, just to keep the connection alive.  Although
	   /// it currently is not implemented on the standard NetAdapterHost, this
	   /// command is used as a signal to the NetAdapterSim to simulate some amount
	   /// of time that has run.
	   /// </summary>
	   public virtual void pingHost()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send beginExclusive command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_PINGCONNECTION);
				conn.output.flush();

				checkReturnValue(conn);
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   //--------
	   //-------- Methods
	   //--------

	   /// <summary>
	   /// Detects adapter presence on the selected port.
	   /// </summary>
	   /// <returns>  <code>true</code> if the adapter is confirmed to be connected to
	   /// the selected port, <code>false</code> if the adapter is not connected.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> </exception>
	   /// <exception cref="OneWireException"> </exception>
	   public override bool adapterDetected()
	   {
		  lock (conn)
		  {
			 return conn != NetAdapterConstants_Fields.EMPTY_CONNECTION && conn.sock != null;
		  }
	   }

	   /// <summary>
	   /// Retrieves the name of the port adapter as a string.  The 'Adapter'
	   /// is a device that connects to a 'port' that allows one to
	   /// communicate with an iButton or other 1-Wire device.  As example
	   /// of this is 'DS9097U'.
	   /// </summary>
	   /// <returns>  <code>String</code> representation of the port adapter. </returns>
	   public override string AdapterName
	   {
		   get
		   {
			  return "NetAdapter";
		   }
	   }

	   /// <summary>
	   /// Retrieves a description of the port required by this port adapter.
	   /// An example of a 'Port' would 'serial communication port'.
	   /// </summary>
	   /// <returns>  <code>String</code> description of the port type required. </returns>
	   public override string PortTypeDescription
	   {
		   get
		   {
			  return "Network 'Hostname:Port'";
		   }
	   }

	   /// <summary>
	   /// Retrieves a version string for this class.
	   /// </summary>
	   /// <returns>  version string </returns>
	   public override string ClassVersion
	   {
		   get
		   {
			  return "" + NetAdapterConstants_Fields.versionUID;
		   }
	   }

	   //--------
	   //-------- Port Selection
	   //--------

	   /// <summary>
	   /// Retrieves a list of the platform appropriate port names for this
	   /// adapter.  A port must be selected with the method 'selectPort'
	   /// before any other communication methods can be used.  Using
	   /// a communcation method before 'selectPort' will result in
	   /// a <code>OneWireException</code> exception.
	   /// </summary>
	   /// <returns>  <code>Enumeration</code> of type <code>String</code> that contains the port
	   /// names </returns>
	   public override System.Collections.IEnumerator PortNames
	   {
		   get
		   {
			  ArrayList v = new ArrayList();
    
			  // figure out if multicast is enabled
			  if (multicastEnabled == null)
			  {
				 string enabled = null;
				 try
				 {
					enabled = OneWireAccessProvider.getProperty("NetAdapter.MulticastEnabled");
				 }
				 catch (Exception)
				 {
					 ;
				 }
				 if (!string.ReferenceEquals(enabled, null))
				 {
					multicastEnabled = Convert.ToBoolean(enabled);
				 }
				 else
				 {
					multicastEnabled = false;
				 }
			  }
    
			  // if multicasting is enabled, we'll look for servers dynamically
			  // and add them to the list
			  if (multicastEnabled.Value)
			  {
				 // figure out what the datagram listen port is
				 if (datagramPort == -1)
				 {
					string strPort = null;
					try
					{
					   strPort = OneWireAccessProvider.getProperty("NetAdapter.MulticastPort");
					}
					catch (Exception)
					{
						;
					}
					if (string.ReferenceEquals(strPort, null))
					{
					   datagramPort = NetAdapterConstants_Fields.DEFAULT_MULTICAST_PORT;
					}
					else
					{
					   datagramPort = int.Parse(strPort);
					}
				 }
    
				 // figure out what the multicast group is
				 if (string.ReferenceEquals(multicastGroup, null))
				 {
					string group = null;
					try
					{
					   group = OneWireAccessProvider.getProperty("NetAdapter.MulticastGroup");
					}
					catch (Exception)
					{
						;
					}
					if (string.ReferenceEquals(group, null))
					{
					   multicastGroup = NetAdapterConstants_Fields.DEFAULT_MULTICAST_GROUP;
					}
					else
					{
					   multicastGroup = group;
					}
				 }
    
				 MulticastSocket socket = null;
				 InetAddress group = null;
				 try
				 {
					//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
					if (NetAdapterConstants_Fields.DEBUG)
					{
					   Debug.WriteLine("DEBUG: Opening multicast on port: " + datagramPort);
					   Debug.WriteLine("DEBUG: joining group: " + multicastGroup);
					}
					//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
    
					// create the multi-cast socket
					socket = new MulticastSocket(datagramPort);
					// create the group's InetAddress
					group = InetAddress.getByName(multicastGroup);
					// join the group
					socket.joinGroup(group);
    
					// convert the versionUID to a byte[]
					sbyte[] versionBytes = Convert.toByteArray(NetAdapterConstants_Fields.versionUID);
    
					// send a packet with the versionUID
					DatagramPacket outPacket = new DatagramPacket(versionBytes, 4, group, datagramPort);
					socket.send(outPacket);
    
					// set a timeout of 1/2 second for the receive
					socket.SoTimeout = 500;
    
					sbyte[] receiveBuffer = new sbyte[32];
					for (;;)
					{
					   //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
					   if (NetAdapterConstants_Fields.DEBUG)
					   {
						  Debug.WriteLine("DEBUG: waiting for multicast packet");
					   }
					   //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
					   DatagramPacket inPacket = new DatagramPacket(receiveBuffer, receiveBuffer.Length);
					   socket.receive(inPacket);
    
					   int length = inPacket.Length;
					   sbyte[] data = inPacket.Data;
					   //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
					   if (NetAdapterConstants_Fields.DEBUG)
					   {
						  Debug.WriteLine("DEBUG: packet.length=" + length);
						  Debug.WriteLine("DEBUG: expecting=" + 5);
					   }
					   //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
					   if (length == 5 && data[4] == unchecked((sbyte)0xFF))
					   {
						  int listenPort = Convert.toInt(data, 0, 4);
						  v.Add(inPacket.Address.HostName + ":" + listenPort);
					   }
					}
				 }
				 catch (Exception)
				 { //drain
		;
				 }
				 finally
				 {
					try
					{
					   socket.leaveGroup(group);
					   socket.close();
					}
					catch (Exception)
					{ //drain
		;
					}
				 }
			  }
    
			  // get all servers from the properties file
			  string server = "";
			  try
			  {
				 for (int i = 0; !string.ReferenceEquals(server, null); i++)
				 {
					server = OneWireAccessProvider.getProperty("NetAdapter.host" + i);
					if (!string.ReferenceEquals(server, null))
					{
					   v.Add(server);
					}
				 }
			  }
			  catch (Exception)
			  {
				  ;
			  }
    
			  return v.elements();
		   }
	   }

	   /// <summary>
	   /// Specifies a platform appropriate port name for this adapter.  Note that
	   /// even though the port has been selected, it's ownership may be relinquished
	   /// if it is not currently held in a 'exclusive' block.  This class will then
	   /// try to re-aquire the port when needed.  If the port cannot be re-aquired
	   /// ehen the exception <code>PortInUseException</code> will be thrown.
	   /// </summary>
	   /// <param name="portName">  Address to connect this NetAdapter to, in the form of
	   /// "hostname:port".  For example, "shughes.dalsemi.com:6161", where 6161
	   /// is the port number to connect to.  The use of NetAdapter.DEFAULT_PORT
	   /// is recommended.
	   /// </param>
	   /// <returns> <code>true</code> if the port was aquired, <code>false</code>
	   /// if the port is not available.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> If port does not exist, or unable to communicate with port. </exception>
	   /// <exception cref="OneWireException"> If port does not exist </exception>
	   public override bool selectPort(string portName)
	   {
		  lock (conn)
		  {
			 Socket s = null;
			 try
			 {
				int port = NetAdapterConstants_Fields.DEFAULT_PORT;
				// should be of the format "hostname:port" or hostname
				int index = portName.IndexOf(':');
				if (index >= 0)
				{
				   int index2 = portName.IndexOf(':', index + 1);
				   if (index2 < 0) // no custom secret specified
				   {
					  port = int.Parse(portName.Substring(index + 1));
					  // reset the secret to default
					  resetSecret();
					  useCustomSecret = false;
				   }
				   else
				   {
					  // custom secret is specified
					  Secret = portName.Substring(index2 + 1);
					  useCustomSecret = true;
					  if (index < index2 - 1) // port number is specified
					  {
						 port = int.Parse(portName.Substring(index + 1, index2 - (index + 1)));
					  }
				   }
				   portName = portName.Substring(0, index);
				}
				else
				{
				   // reset the secret
				   resetSecret();
				   useCustomSecret = false;
				}
				s = new Socket(portName, port);
			 }
			 catch (IOException ioe)
			 {
				throw new OneWireIOException("Can't reach server: " + ioe.Message);
			 }

			 return selectPort(s);
		  }
	   }

	   /// <summary>
	   /// New method, unique to NetAdapter.  Sets the "port", i.e. the connection
	   /// to the server via an already established socket connection.
	   /// </summary>
	   /// <param name="sock"> Socket connection to NetAdapterHost
	   /// </param>
	   /// <returns> <code>true</code> if connection to host was successful
	   /// </returns>
	   /// <exception cref="OneWireIOException"> If port does not exist, or unable to communicate with port. </exception>
	   /// <exception cref="OneWireException"> If port does not exist </exception>
	   public virtual bool selectPort(Socket sock)
	   {
		  bool bSuccess = false;
		  lock (conn)
		  {
			 NetAdapterConstants_Connection tmpConn = new NetAdapterConstants_Connection();
			 tmpConn.sock = sock;

			 try
			 {
				tmpConn.input = new DataInputStream(sock.InputStream);
				if (NetAdapterConstants_Fields.BUFFERED_OUTPUT)
				{
				   tmpConn.output = new DataOutputStream(new BufferedOutputStream(sock.OutputStream));
				}
				else
				{
				   tmpConn.output = new DataOutputStream(sock.OutputStream);
				}

				// check host version
				int hostVersionUID = tmpConn.input.readInt();

				if (hostVersionUID == NetAdapterConstants_Fields.versionUID)
				{
				   // tell the server that the versionUID matched
				   tmpConn.output.writeByte(NetAdapterConstants_Fields.RET_SUCCESS);
				   tmpConn.output.flush();

				   // if the versionUID matches, we need to authenticate ourselves
				   // using the challenge from the server.
				   sbyte[] chlg = new sbyte[8];
				   tmpConn.input.read(chlg, 0, 8);

				   // compute the crc of the secret and the challenge
				   int crc = CRC16.compute(netAdapterSecret, 0);
				   crc = CRC16.compute(chlg, crc);
				   // and send it back to the server
				   tmpConn.output.writeInt(crc);
				   tmpConn.output.flush();

				   // check to see if it matched
				   checkReturnValue(tmpConn);

				   bSuccess = true;
				}
				else
				{
				   tmpConn.output.writeByte(NetAdapterConstants_Fields.RET_FAILURE);
				   tmpConn.output.flush();
				   tmpConn = null;
				}
			 }
			 catch (IOException)
			 {
				bSuccess = false;
				tmpConn = null;
			 }

			 if (bSuccess)
			 {
				portNameForReconnect = sock.InetAddress.HostName + ":" + sock.Port;
				conn = tmpConn;
			 }
		  }

		  // invalid response or version number
		  return bSuccess;
	   }

	   /// <summary>
	   /// Frees ownership of the selected port, if it is currently owned, back
	   /// to the system.  This should only be called if the recently
	   /// selected port does not have an adapter, or at the end of
	   /// your application's use of the port.
	   /// </summary>
	   /// <exception cref="OneWireException"> If port does not exist </exception>
	   public override void freePort()
	   {
		  try
		  {
			 lock (conn)
			 {
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_CLOSECONNECTION);
				conn.output.flush();
				conn.sock.close();
				conn = NetAdapterConstants_Fields.EMPTY_CONNECTION;
			 }
		  }
		  catch (Exception e)
		  {
			 throw new OneWireException(COMM_FAILED + e.Message);
		  }
	   }

	   /// <summary>
	   /// Retrieves the name of the selected port as a <code>String</code>.
	   /// </summary>
	   /// <returns>  <code>String</code> of selected port
	   /// </returns>
	   /// <exception cref="OneWireException"> if valid port not yet selected </exception>
	   public override string PortName
	   {
		   get
		   {
			  lock (conn)
			  {
				 if (!adapterDetected())
				 {
					return "Not Connected";
				 }
				 else if (useCustomSecret)
				 {
					return conn.sock.InetAddress.HostName + ":" + conn.sock.Port + ":" + StringHelperClass.NewString(this.netAdapterSecret);
				 }
				 else
				 {
					return conn.sock.InetAddress.HostName + ":" + conn.sock.Port;
				 }
			  }
		   }
	   }

	   /// <summary>
	   /// Returns whether adapter can physically support overdrive mode.
	   /// </summary>
	   /// <returns>  <code>true</code> if this port adapter can do OverDrive,
	   /// <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire
	   ///         adapter </exception>
	   public override bool canOverdrive()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send beginExclusive command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_CANOVERDRIVE);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// next parameter should be the return from beginExclusive
				return conn.input.readBoolean();
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   /// <summary>
	   /// Returns whether the adapter can physically support hyperdrive mode.
	   /// </summary>
	   /// <returns>  <code>true</code> if this port adapter can do HyperDrive,
	   /// <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire
	   ///         adapter </exception>
	   public override bool canHyperdrive()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send beginExclusive command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_CANHYPERDRIVE);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// next parameter should be the return from beginExclusive
				return conn.input.readBoolean();
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   /// <summary>
	   /// Returns whether the adapter can physically support flex speed mode.
	   /// </summary>
	   /// <returns>  <code>true</code> if this port adapter can do flex speed,
	   /// <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire
	   ///         adapter </exception>
	   public override bool canFlex()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send beginExclusive command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_CANFLEX);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// next parameter should be the return from beginExclusive
				return conn.input.readBoolean();
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   /// <summary>
	   /// Returns whether adapter can physically support 12 volt power mode.
	   /// </summary>
	   /// <returns>  <code>true</code> if this port adapter can do Program voltage,
	   /// <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire
	   ///         adapter </exception>
	   public override bool canProgram()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send beginExclusive command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_CANPROGRAM);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// next parameter should be the return from beginExclusive
				return conn.input.readBoolean();
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   /// <summary>
	   /// Returns whether the adapter can physically support strong 5 volt power
	   /// mode.
	   /// </summary>
	   /// <returns>  <code>true</code> if this port adapter can do strong 5 volt
	   /// mode, <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire
	   ///         adapter </exception>
	   public override bool canDeliverPower()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send beginExclusive command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_CANDELIVERPOWER);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// next parameter should be the return from beginExclusive
				return conn.input.readBoolean();
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   /// <summary>
	   /// Returns whether the adapter can physically support "smart" strong 5
	   /// volt power mode.  "smart" power delivery is the ability to deliver
	   /// power until it is no longer needed.  The current drop it detected
	   /// and power delivery is stopped.
	   /// </summary>
	   /// <returns>  <code>true</code> if this port adapter can do "smart" strong
	   /// 5 volt mode, <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire
	   ///         adapter </exception>
	   public override bool canDeliverSmartPower()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send beginExclusive command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_CANDELIVERSMARTPOWER);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// next parameter should be the return from beginExclusive
				return conn.input.readBoolean();
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   /// <summary>
	   /// Returns whether adapter can physically support 0 volt 'break' mode.
	   /// </summary>
	   /// <returns>  <code>true</code> if this port adapter can do break,
	   /// <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error with the adapter </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire
	   ///         adapter </exception>
	   public override bool canBreak()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send beginExclusive command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_CANBREAK);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// next parameter should be the return from beginExclusive
				return conn.input.readBoolean();
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   //--------
	   //-------- Finding iButton/1-Wire device options
	   //--------

	   /// <summary>
	   /// Returns <code>true</code> if the first iButton or 1-Wire device
	   /// is found on the 1-Wire Network.
	   /// If no devices are found, then <code>false</code> will be returned.
	   /// </summary>
	   /// <returns>  <code>true</code> if an iButton or 1-Wire device is found.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override bool findFirstDevice()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send findFirstDevice command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_FINDFIRSTDEVICE);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// return boolean from findFirstDevice
				return conn.input.readBoolean();
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   /// <summary>
	   /// Returns <code>true</code> if the next iButton or 1-Wire device
	   /// is found. The previous 1-Wire device found is used
	   /// as a starting point in the search.  If no more devices are found
	   /// then <code>false</code> will be returned.
	   /// </summary>
	   /// <returns>  <code>true</code> if an iButton or 1-Wire device is found.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override bool findNextDevice()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send findNextDevice command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_FINDNEXTDEVICE);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// return boolean from findNextDevice
				return conn.input.readBoolean();
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }


	   /// <summary>
	   /// Copies the 'current' 1-Wire device address being used by the adapter into
	   /// the array.  This address is the last iButton or 1-Wire device found
	   /// in a search (findNextDevice()...).
	   /// This method copies into a user generated array to allow the
	   /// reuse of the buffer.  When searching many iButtons on the one
	   /// wire network, this will reduce the memory burn rate.
	   /// </summary>
	   /// <param name="address"> An array to be filled with the current iButton address. </param>
	   /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
	   public override void getAddress(sbyte[] address)
	   {
		  try
		  {
			 lock (conn)
			 {
				// send getAddress command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_GETADDRESS);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// get the address
				conn.input.read(address, 0, 8);
			 }
		  }
		  catch (Exception)
		  { // drain
		  }
	   }

	   /// <summary>
	   /// Sets the 1-Wire Network search to find only iButtons and 1-Wire
	   /// devices that are in an 'Alarm' state that signals a need for
	   /// attention.  Not all iButton types
	   /// have this feature.  Some that do: DS1994, DS1920, DS2407.
	   /// This selective searching can be canceled with the
	   /// 'setSearchAllDevices()' method.
	   /// </summary>
	   /// <seealso cref= #setNoResetSearch </seealso>
	   public override void setSearchOnlyAlarmingDevices()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send setSearchOnlyAlarmingDevices command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_SETSEARCHONLYALARMINGDEVICES);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);
			 }
		  }
		  catch (Exception)
		  { // drain
		  }
	   }


	   /// <summary>
	   /// Sets the 1-Wire Network search to not perform a 1-Wire
	   /// reset before a search.  This feature is chiefly used with
	   /// the DS2409 1-Wire coupler.
	   /// The normal reset before each search can be restored with the
	   /// 'setSearchAllDevices()' method.
	   /// </summary>
	   public override void setNoResetSearch()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send setNoResetSearch command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_SETNORESETSEARCH);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);
			 }
		  }
		  catch (Exception)
		  { // drain
		  }
	   }


	   /// <summary>
	   /// Sets the 1-Wire Network search to find all iButtons and 1-Wire
	   /// devices whether they are in an 'Alarm' state or not and
	   /// restores the default setting of providing a 1-Wire reset
	   /// command before each search. (see setNoResetSearch() method).
	   /// </summary>
	   /// <seealso cref= #setNoResetSearch </seealso>
	   public override void setSearchAllDevices()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send setSearchAllDevices command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_SETSEARCHALLDEVICES);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);
			 }
		  }
		  catch (Exception)
		  { // drain
		  }
	   }


	   /// <summary>
	   /// Removes any selectivity during a search for iButtons or 1-Wire devices
	   /// by family type.  The unique address for each iButton and 1-Wire device
	   /// contains a family descriptor that indicates the capabilities of the
	   /// device. </summary>
	   /// <seealso cref=    #targetFamily </seealso>
	   /// <seealso cref=    #targetFamily(byte[]) </seealso>
	   /// <seealso cref=    #excludeFamily </seealso>
	   /// <seealso cref=    #excludeFamily(byte[]) </seealso>
	   public override void targetAllFamilies()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send targetAllFamilies command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_TARGETALLFAMILIES);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);
			 }
		  }
		  catch (Exception)
		  { // drain
		  }
	   }


	   /// <summary>
	   /// Takes an integer to selectively search for this desired family type.
	   /// If this method is used, then no devices of other families will be
	   /// found by any of the search methods.
	   /// </summary>
	   /// <param name="family">   the code of the family type to target for searches </param>
	   /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
	   /// <seealso cref=    #targetAllFamilies </seealso>
	   public override void targetFamily(int family)
	   {
		  try
		  {
			 lock (conn)
			 {
				// send targetFamily command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_TARGETFAMILY);
				conn.output.writeInt(1);
				conn.output.writeByte((sbyte)family);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);
			 }
		  }
		  catch (Exception)
		  { // drain
		  }
	   }

	   /// <summary>
	   /// Takes an array of bytes to use for selectively searching for acceptable
	   /// family codes.  If used, only devices with family codes in this array
	   /// will be found by any of the search methods.
	   /// </summary>
	   /// <param name="family">  array of the family types to target for searches </param>
	   /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
	   /// <seealso cref=    #targetAllFamilies </seealso>
	   public override void targetFamily(sbyte[] family)
	   {
		  try
		  {
			 lock (conn)
			 {
				// send targetFamily command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_TARGETFAMILY);
				conn.output.writeInt(family.Length);
				conn.output.write(family, 0, family.Length);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);
			 }
		  }
		  catch (Exception)
		  { // drain
		  }
	   }


	   /// <summary>
	   /// Takes an integer family code to avoid when searching for iButtons.
	   /// or 1-Wire devices.
	   /// If this method is used, then no devices of this family will be
	   /// found by any of the search methods.
	   /// </summary>
	   /// <param name="family">   the code of the family type NOT to target in searches </param>
	   /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
	   /// <seealso cref=    #targetAllFamilies </seealso>
	   public override void excludeFamily(int family)
	   {
		  try
		  {
			 lock (conn)
			 {
				// send excludeFamily command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_EXCLUDEFAMILY);
				conn.output.writeInt(1);
				conn.output.writeByte((sbyte)family);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);
			 }
		  }
		  catch (Exception)
		  { // drain
		  }
	   }


	   /// <summary>
	   /// Takes an array of bytes containing family codes to avoid when finding
	   /// iButtons or 1-Wire devices.  If used, then no devices with family
	   /// codes in this array will be found by any of the search methods.
	   /// </summary>
	   /// <param name="family">  array of family cods NOT to target for searches </param>
	   /// <seealso cref=   com.dalsemi.onewire.utils.Address </seealso>
	   /// <seealso cref=    #targetAllFamilies </seealso>
	   public override void excludeFamily(sbyte[] family)
	   {
		  try
		  {
			 lock (conn)
			 {
				// send excludeFamily command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_EXCLUDEFAMILY);
				conn.output.writeInt(family.Length);
				conn.output.write(family, 0, family.Length);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);
			 }
		  }
		  catch (Exception)
		  { // drain
		  }
	   }


	   //--------
	   //-------- 1-Wire Network Semaphore methods
	   //--------

	   /// <summary>
	   /// Gets exclusive use of the 1-Wire to communicate with an iButton or
	   /// 1-Wire Device.
	   /// This method should be used for critical sections of code where a
	   /// sequence of commands must not be interrupted by communication of
	   /// threads with other iButtons, and it is permissible to sustain
	   /// a delay in the special case that another thread has already been
	   /// granted exclusive access and this access has not yet been
	   /// relinquished. <para>
	   /// 
	   /// It can be called through the OneWireContainer
	   /// class by the end application if they want to ensure exclusive
	   /// use.  If it is not called around several methods then it
	   /// will be called inside each method.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="blocking"> <code>true</code> if want to block waiting
	   ///                 for an excluse access to the adapter </param>
	   /// <returns> <code>true</code> if blocking was false and a
	   ///         exclusive session with the adapter was aquired
	   /// </returns>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override bool beginExclusive(bool blocking)
	   {
		  bool bGotLocalBlock = false, bGotServerBlock = false;
		  if (blocking)
		  {
			 while (!beginExclusive())
			 {
				 try
				 {
					 Thread.Sleep(50);
				 }
				 catch (Exception)
				 {
				 }
			 }

			 bGotLocalBlock = true;
		  }
		  else
		  {
			 bGotLocalBlock = beginExclusive();
		  }

		  try
		  {
			 lock (conn)
			 {
				// send beginExclusive command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_BEGINEXCLUSIVE);
				conn.output.writeBoolean(blocking);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// next parameter should be the return from beginExclusive
				bGotServerBlock = conn.input.readBoolean();
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }

		  // if blocking, I shouldn't get here unless both are true
		  return bGotLocalBlock && bGotServerBlock;
	   }

	   /// <summary>
	   /// Gets exclusive use of the 1-Wire to communicate with an iButton or
	   /// 1-Wire Device.
	   /// This method should be used for critical sections of code where a
	   /// sequence of commands must not be interrupted by communication of
	   /// threads with other iButtons, and it is permissible to sustain
	   /// a delay in the special case that another thread has already been
	   /// granted exclusive access and this access has not yet been
	   /// relinquished. This is private and non blocking<para>
	   /// 
	   /// </para>
	   /// </summary>
	   /// <returns> <code>true</code> a exclusive session with the adapter was
	   ///         aquired
	   /// </returns>
	   /// <exception cref="OneWireException"> </exception>
	   private bool beginExclusive()
	   {
		  lock (currentThreadHash)
		  {
			 if (currentThreadHash == NOT_OWNED)
			 {
				// not owned so take
				currentThreadHash = new int?(Thread.CurrentThread.GetHashCode());

				// provided debug on standard out
				if (NetAdapterConstants_Fields.DEBUG)
				{
				   Debug.WriteLine("beginExclusive, now owned by: " + Thread.CurrentThread.Name);
				}

				return true;
			 }
			 else if (currentThreadHash.Value == Thread.CurrentThread.GetHashCode())
			 {
				// provided debug on standard out
				if (NetAdapterConstants_Fields.DEBUG)
				{
				   Debug.WriteLine("beginExclusive, already owned by: " + Thread.CurrentThread.Name);
				}

				// already own
				return true;
			 }
			 else
			 {
				// want port but don't own
				return false;
			 }
		  }
	   }

	   /// <summary>
	   /// Relinquishes exclusive control of the 1-Wire Network.
	   /// This command dynamically marks the end of a critical section and
	   /// should be used when exclusive control is no longer needed.
	   /// </summary>
	   public override void endExclusive()
	   {
		  lock (currentThreadHash)
		  {
			 // if own then release
			 if (currentThreadHash != NOT_OWNED && currentThreadHash.Value == Thread.CurrentThread.GetHashCode())
			 {
				if (NetAdapterConstants_Fields.DEBUG)
				{
				   Debug.WriteLine("endExclusive, was owned by: " + Thread.CurrentThread.Name);
				}

				currentThreadHash = NOT_OWNED;
				try
				{
				   lock (conn)
				   {
					  // send endExclusive command
					  conn.output.writeByte(NetAdapterConstants_Fields.CMD_ENDEXCLUSIVE);
					  conn.output.flush();

					  // check return value for success
					  checkReturnValue(conn);
				   }
				}
				catch (Exception)
				{ // drain
				}
			 }
		  }
	   }

	   //--------
	   //-------- Primitive 1-Wire Network data methods
	   //--------

	   /// <summary>
	   /// Sends a Reset to the 1-Wire Network.
	   /// </summary>
	   /// <returns>  the result of the reset. Potential results are:
	   /// <ul>
	   /// <li> 0 (RESET_NOPRESENCE) no devices present on the 1-Wire Network.
	   /// <li> 1 (RESET_PRESENCE) normal presence pulse detected on the 1-Wire
	   ///        Network indicating there is a device present.
	   /// <li> 2 (RESET_ALARM) alarming presence pulse detected on the 1-Wire
	   ///        Network indicating there is a device present and it is in the
	   ///        alarm condition.  This is only provided by the DS1994/DS2404
	   ///        devices.
	   /// <li> 3 (RESET_SHORT) inticates 1-Wire appears shorted.  This can be
	   ///        transient conditions in a 1-Wire Network.  Not all adapter types
	   ///        can detect this condition.
	   /// </ul>
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override int reset()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send reset command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_RESET);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// next parameter should be the return from reset
				return conn.input.readInt();
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   /// <summary>
	   /// Sends a bit to the 1-Wire Network.
	   /// </summary>
	   /// <param name="bitValue">  the bit value to send to the 1-Wire Network.
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override void putBit(bool bitValue)
	   {
		  try
		  {
			 lock (conn)
			 {
				// send putBit command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_PUTBIT);
				// followed by the bit
				conn.output.writeBoolean(bitValue);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   /// <summary>
	   /// Gets a bit from the 1-Wire Network.
	   /// </summary>
	   /// <returns>  the bit value recieved from the the 1-Wire Network.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override bool Bit
	   {
		   get
		   {
			  try
			  {
				 lock (conn)
				 {
					// send getBit command
					conn.output.writeByte(NetAdapterConstants_Fields.CMD_GETBIT);
					conn.output.flush();
    
					// check return value for success
					checkReturnValue(conn);
    
					// next parameter should be the return from getBit
					return conn.input.readBoolean();
				 }
			  }
			  catch (IOException ioe)
			  {
				 throw new OneWireException(COMM_FAILED + ioe.Message);
			  }
		   }
	   }

	   /// <summary>
	   /// Sends a byte to the 1-Wire Network.
	   /// </summary>
	   /// <param name="byteValue">  the byte value to send to the 1-Wire Network.
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override void putByte(int byteValue)
	   {
		  try
		  {
			 lock (conn)
			 {
				// send putByte command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_PUTBYTE);
				// followed by the byte
				conn.output.writeByte(byteValue);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   /// <summary>
	   /// Gets a byte from the 1-Wire Network.
	   /// </summary>
	   /// <returns>  the byte value received from the the 1-Wire Network.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override int Byte
	   {
		   get
		   {
			  try
			  {
				 lock (conn)
				 {
					// send getByte command
					conn.output.writeByte(NetAdapterConstants_Fields.CMD_GETBYTE);
					conn.output.flush();
    
					// check return value for success
					checkReturnValue(conn);
    
					// next parameter should be the return from getByte
					return conn.input.readByte() & 0x0FF;
				 }
			  }
			  catch (IOException ioe)
			  {
				 throw new OneWireException(COMM_FAILED + ioe.Message);
			  }
		   }
	   }

	   /// <summary>
	   /// Gets a block of data from the 1-Wire Network.
	   /// </summary>
	   /// <param name="len">  length of data bytes to receive
	   /// </param>
	   /// <returns>  the data received from the 1-Wire Network.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override sbyte[] getBlock(int len)
	   {
		  sbyte[] buffer = new sbyte[len];
		  getBlock(buffer,0,len);
		  return buffer;
	   }

	   /// <summary>
	   /// Gets a block of data from the 1-Wire Network and write it into
	   /// the provided array.
	   /// </summary>
	   /// <param name="arr">     array in which to write the received bytes </param>
	   /// <param name="len">     length of data bytes to receive
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override void getBlock(sbyte[] arr, int len)
	   {
		  getBlock(arr, 0, len);
	   }

	   /// <summary>
	   /// Gets a block of data from the 1-Wire Network and write it into
	   /// the provided array.
	   /// </summary>
	   /// <param name="arr">     array in which to write the received bytes </param>
	   /// <param name="off">     offset into the array to start </param>
	   /// <param name="len">     length of data bytes to receive
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override void getBlock(sbyte[] arr, int off, int len)
	   {
		  try
		  {
			 lock (conn)
			 {
				// send getBlock command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_GETBLOCK);
				// followed by the number of bytes to get
				conn.output.writeInt(len);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// next should be the bytes
				conn.input.readFully(arr, off, len);
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   /// <summary>
	   /// Sends a block of data and returns the data received in the same array.
	   /// This method is used when sending a block that contains reads and writes.
	   /// The 'read' portions of the data block need to be pre-loaded with 0xFF's.
	   /// It starts sending data from the index at offset 'off' for length 'len'.
	   /// </summary>
	   /// <param name="dataBlock"> array of data to transfer to and from the 1-Wire Network. </param>
	   /// <param name="off">       offset into the array of data to start </param>
	   /// <param name="len">       length of data to send / receive starting at 'off'
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override void dataBlock(sbyte[] dataBlock, int off, int len)
	   {
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("DataBlock called for " + len + " bytes");
		  }
		  try
		  {
			 lock (conn)
			 {
				// send dataBlock command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_DATABLOCK);
				// followed by the number of bytes to block
				conn.output.writeInt(len);
				// followed by the bytes
				conn.output.write(dataBlock, off, len);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// next should be the bytes returned
				conn.input.readFully(dataBlock, off, len);
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
		  if (NetAdapterConstants_Fields.DEBUG)
		  {
			 Debug.WriteLine("   Done DataBlocking");
		  }
	   }

	   //--------
	   //-------- 1-Wire Network power methods
	   //--------

	   /// <summary>
	   /// Sets the duration to supply power to the 1-Wire Network.
	   /// This method takes a time parameter that indicates the program
	   /// pulse length when the method startPowerDelivery().<para>
	   /// 
	   /// Note: to avoid getting an exception,
	   /// use the canDeliverPower() and canDeliverSmartPower()
	   /// </para>
	   /// method to check it's availability. <para>
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="timeFactor">
	   /// <ul>
	   /// <li>   0 (DELIVERY_HALF_SECOND) provide power for 1/2 second.
	   /// <li>   1 (DELIVERY_ONE_SECOND) provide power for 1 second.
	   /// <li>   2 (DELIVERY_TWO_SECONDS) provide power for 2 seconds.
	   /// <li>   3 (DELIVERY_FOUR_SECONDS) provide power for 4 seconds.
	   /// <li>   4 (DELIVERY_SMART_DONE) provide power until the
	   ///          the device is no longer drawing significant power.
	   /// <li>   5 (DELIVERY_INFINITE) provide power until the
	   ///          setPowerNormal() method is called.
	   /// </ul>
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override int PowerDuration
	   {
		   set
		   {
			  try
			  {
				 lock (conn)
				 {
					// send setPowerDuration command
					conn.output.writeByte(NetAdapterConstants_Fields.CMD_SETPOWERDURATION);
					// followed by the value
					conn.output.writeInt(value);
					conn.output.flush();
    
					// check return value for success
					checkReturnValue(conn);
				 }
			  }
			  catch (IOException ioe)
			  {
				 throw new OneWireException(COMM_FAILED + ioe.Message);
			  }
		   }
	   }

	   /// <summary>
	   /// Sets the 1-Wire Network voltage to supply power to a 1-Wire device.
	   /// This method takes a time parameter that indicates whether the
	   /// power delivery should be done immediately, or after certain
	   /// conditions have been met. <para>
	   /// 
	   /// Note: to avoid getting an exception,
	   /// use the canDeliverPower() and canDeliverSmartPower()
	   /// </para>
	   /// method to check it's availability. <para>
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="changeCondition">
	   /// <ul>
	   /// <li>   0 (CONDITION_NOW) operation should occur immediately.
	   /// <li>   1 (CONDITION_AFTER_BIT) operation should be pending
	   ///           execution immediately after the next bit is sent.
	   /// <li>   2 (CONDITION_AFTER_BYTE) operation should be pending
	   ///           execution immediately after next byte is sent.
	   /// </ul>
	   /// </param>
	   /// <returns> <code>true</code> if the voltage change was successful,
	   /// <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override bool startPowerDelivery(int changeCondition)
	   {
		  try
		  {
			 lock (conn)
			 {
				// send startPowerDelivery command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_STARTPOWERDELIVERY);
				// followed by the changeCondition
				conn.output.writeInt(changeCondition);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// and get the return value from startPowerDelivery
				return conn.input.readBoolean();
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   /// <summary>
	   /// Sets the duration for providing a program pulse on the
	   /// 1-Wire Network.
	   /// This method takes a time parameter that indicates the program
	   /// pulse length when the method startProgramPulse().<para>
	   /// 
	   /// Note: to avoid getting an exception,
	   /// use the canDeliverPower() method to check it's
	   /// </para>
	   /// availability. <para>
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="timeFactor">
	   /// <ul>
	   /// <li>   7 (DELIVERY_EPROM) provide program pulse for 480 microseconds
	   /// <li>   5 (DELIVERY_INFINITE) provide power until the
	   ///          setPowerNormal() method is called.
	   /// </ul>
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter </exception>
	   public override int ProgramPulseDuration
	   {
		   set
		   {
			  try
			  {
				 lock (conn)
				 {
					// send setProgramPulseDuration command
					conn.output.writeByte(NetAdapterConstants_Fields.CMD_SETPROGRAMPULSEDURATION);
					// followed by the value
					conn.output.writeInt(value);
					conn.output.flush();
    
					// check return value for success
					checkReturnValue(conn);
				 }
			  }
			  catch (IOException ioe)
			  {
				 throw new OneWireException(COMM_FAILED + ioe.Message);
			  }
		   }
	   }

	   /// <summary>
	   /// Sets the 1-Wire Network voltage to eprom programming level.
	   /// This method takes a time parameter that indicates whether the
	   /// power delivery should be done immediately, or after certain
	   /// conditions have been met. <para>
	   /// 
	   /// Note: to avoid getting an exception,
	   /// use the canProgram() method to check it's
	   /// </para>
	   /// availability. <para>
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="changeCondition">
	   /// <ul>
	   /// <li>   0 (CONDITION_NOW) operation should occur immediately.
	   /// <li>   1 (CONDITION_AFTER_BIT) operation should be pending
	   ///           execution immediately after the next bit is sent.
	   /// <li>   2 (CONDITION_AFTER_BYTE) operation should be pending
	   ///           execution immediately after next byte is sent.
	   /// </ul>
	   /// </param>
	   /// <returns> <code>true</code> if the voltage change was successful,
	   /// <code>false</code> otherwise.
	   /// </returns>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter
	   ///         or the adapter does not support this operation </exception>
	   public override bool startProgramPulse(int changeCondition)
	   {
		  try
		  {
			 lock (conn)
			 {
				// send startProgramPulse command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_STARTPROGRAMPULSE);
				// followed by the changeCondition
				conn.output.writeInt(changeCondition);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);

				// and get the return value from startPowerDelivery
				return conn.input.readBoolean();
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   /// <summary>
	   /// Sets the 1-Wire Network voltage to 0 volts.  This method is used
	   /// rob all 1-Wire Network devices of parasite power delivery to force
	   /// them into a hard reset.
	   /// </summary>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter
	   ///         or the adapter does not support this operation </exception>
	   public override void startBreak()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send startBreak command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_STARTBREAK);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);
			 }
		  }
		  catch (IOException)
		  {
			 throw new OneWireException(COMM_FAILED);
		  }
	   }

	   /// <summary>
	   /// Sets the 1-Wire Network voltage to normal level.  This method is used
	   /// to disable 1-Wire conditions created by startPowerDelivery and
	   /// startProgramPulse.  This method will automatically be called if
	   /// a communication method is called while an outstanding power
	   /// command is taking place.
	   /// </summary>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter
	   ///         or the adapter does not support this operation </exception>
	   public override void setPowerNormal()
	   {
		  try
		  {
			 lock (conn)
			 {
				// send startBreak command
				conn.output.writeByte(NetAdapterConstants_Fields.CMD_SETPOWERNORMAL);
				conn.output.flush();

				// check return value for success
				checkReturnValue(conn);
			 }
		  }
		  catch (IOException ioe)
		  {
			 throw new OneWireException(COMM_FAILED + ioe.Message);
		  }
	   }

	   //--------
	   //-------- 1-Wire Network speed methods
	   //--------

	   /// <summary>
	   /// Sets the new speed of data
	   /// transfer on the 1-Wire Network. <para>
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="speed">
	   /// <ul>
	   /// <li>     0 (SPEED_REGULAR) set to normal communciation speed
	   /// <li>     1 (SPEED_FLEX) set to flexible communciation speed used
	   ///            for long lines
	   /// <li>     2 (SPEED_OVERDRIVE) set to normal communciation speed to
	   ///            overdrive
	   /// <li>     3 (SPEED_HYPERDRIVE) set to normal communciation speed to
	   ///            hyperdrive
	   /// <li>    >3 future speeds
	   /// </ul>
	   /// </param>
	   /// <exception cref="OneWireIOException"> on a 1-Wire communication error </exception>
	   /// <exception cref="OneWireException"> on a setup error with the 1-Wire adapter
	   ///         or the adapter does not support this operation </exception>
	   public override int Speed
	   {
		   set
		   {
			  try
			  {
				 lock (conn)
				 {
					// send startBreak command
					conn.output.writeByte(NetAdapterConstants_Fields.CMD_SETSPEED);
					// followed by the value
					conn.output.writeInt(value);
					conn.output.flush();
    
					// check return value for success
					checkReturnValue(conn);
				 }
			  }
			  catch (IOException ioe)
			  {
				 throw new OneWireException(COMM_FAILED + ioe.Message);
			  }
		   }
		   get
		   {
			  try
			  {
				 lock (conn)
				 {
					// send startBreak command
					conn.output.writeByte(NetAdapterConstants_Fields.CMD_GETSPEED);
					conn.output.flush();
    
					// check return value for success
					checkReturnValue(conn);
    
					// and return the return value from getSpeed()
					return conn.input.readInt();
				 }
			  }
			  catch (Exception)
			  {
				 /* drain */
			  }
    
			  return -1;
		   }
	   }

	}
}