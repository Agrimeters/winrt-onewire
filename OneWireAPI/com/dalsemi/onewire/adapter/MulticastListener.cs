using System;
using System.Threading;
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


	/// <summary>
	/// Generic Mulitcast broadcast listener.  Listens for a specific message and,
	/// in response, gives the specified reply.  Used by NetAdapterHost for
	/// automatic discovery of host components for the network-based DSPortAdapter.
	/// 
	/// @author SH
	/// @version 1.00
	/// </summary>
	public class MulticastListener : Runnable
	{
	   /// <summary>
	   /// boolean flag to turn on debug messages </summary>
	   private const bool DEBUG = false;

	   /// <summary>
	   /// timeout for socket receive </summary>
	   private const int timeoutInSeconds = 3;

	   /// <summary>
	   /// multicast socket to receive datagram packets on </summary>
	   private MulticastSocket socket = null;
	   /// <summary>
	   /// the message we're expecting to receive on the multicast socket </summary>
	   private sbyte[] expectedMessage;
	   /// <summary>
	   /// the message we should reply with when we get the expected message </summary>
	   private sbyte[] returnMessage;

	   /// <summary>
	   /// boolean to stop the thread from listening for messages </summary>
	   private volatile bool listenerStopped = false;
	   /// <summary>
	   /// boolean to check if the thread is still running </summary>
	   private volatile bool listenerRunning = false;

	   /// <summary>
	   /// Creates a multicast listener on the specified multicast port,
	   /// bound to the specified multicast group.  Whenever the byte[]
	   /// pattern specified by "expectedMessage" is received, the byte[]
	   /// pattern specifed by "returnMessage" is sent to the sender of
	   /// the "expected message".
	   /// </summary>
	   /// <param name="multicastPort"> Port to bind this listener to. </param>
	   /// <param name="multicastGroup"> Group to bind this listener to. </param>
	   /// <param name="expectedMessage"> the message to look for </param>
	   /// <param name="returnMessage"> the message to reply with </param>
	   public MulticastListener(int multicastPort, string multicastGroup, sbyte[] expectedMessage, sbyte[] returnMessage)
	   {
		  this.expectedMessage = expectedMessage;
		  this.returnMessage = returnMessage;

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
		  if (DEBUG)
		  {
			 Debug.WriteLine("DEBUG: Creating Multicast Listener");
			 Debug.WriteLine("DEBUG:    Multicast port: " + multicastPort);
			 Debug.WriteLine("DEBUG:    Multicast group: " + multicastGroup);
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

		  // create multicast socket
		  socket = new MulticastSocket(multicastPort);
		  // set timeout at 3 seconds
		  socket.SoTimeout = timeoutInSeconds * 1000;
		  //join the multicast group
		  InetAddress group = InetAddress.getByName(multicastGroup);
		  socket.joinGroup(group);
	   }

	   /// <summary>
	   /// Run method waits for Multicast packets with the specified contents
	   /// and replies with the specified message.
	   /// </summary>
	   public virtual void run()
	   {
		  sbyte[] receiveBuffer = new sbyte[expectedMessage.Length];

		  listenerRunning = true;
		  while (!listenerStopped)
		  {
			 try
			 {
				// packet for receiving messages
				DatagramPacket inPacket = new DatagramPacket(receiveBuffer, receiveBuffer.Length);
				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
				if (DEBUG)
				{
				   Debug.WriteLine("DEBUG: waiting for multicast packet");
				}
				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
				// blocks for message until timeout occurs
				socket.receive(inPacket);

				// check to see if the received data matches the expected message
				int length = inPacket.Length;

				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
				if (DEBUG)
				{
				   Debug.WriteLine("DEBUG: packet.length=" + length);
				   Debug.WriteLine("DEBUG: expecting=" + expectedMessage.Length);
				}
				//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

				if (length == expectedMessage.Length)
				{
				   bool dataMatch = true;
				   for (int i = 0; dataMatch && i < length; i++)
				   {
					  dataMatch = (expectedMessage[i] == receiveBuffer[i]);
				   }
				   // check to see if we received the expected message
				   if (dataMatch)
				   {
					  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
					  if (DEBUG)
					  {
						 Debug.WriteLine("DEBUG: packet match, replying");
					  }
					  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
					  // packet for sending messages
					  DatagramPacket outPacket = new DatagramPacket(returnMessage, returnMessage.Length, inPacket.Address, inPacket.Port);
					  // send return message
					  socket.send(outPacket);
				   }
				}
			 }
			 catch (IOException)
			 { // drain
			 }
		  }
		  listenerRunning = false;
	   }

	   /// <summary>
	   /// Waits for datagram listener to finish, with a timeout.
	   /// </summary>
	   public virtual void stopListener()
	   {
		  listenerStopped = true;
		  int i = 0;
		  int timeout = timeoutInSeconds * 100;
		  while (listenerRunning && i++<timeout)
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
}