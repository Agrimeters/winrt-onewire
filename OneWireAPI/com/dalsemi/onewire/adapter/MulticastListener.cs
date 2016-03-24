using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Networking;
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

    using com.dalsemi.onewire.logging;

	/// <summary>
	/// Generic Mulitcast broadcast listener.  Listens for a specific message and,
	/// in response, gives the specified reply.  Used by NetAdapterHost for
	/// automatic discovery of host components for the network-based DSPortAdapter.
	/// 
	/// @author SH
	/// @version 1.00
	/// </summary>
	public class MulticastListener : IDisposable
	{
	   /// <summary>
	   /// timeout for socket receive </summary>
	   private const int timeoutInSeconds = 3;

	   /// <summary>
	   /// multicast socket to receive datagram packets on </summary>
	   private DatagramSocket socket = null;

	   /// <summary>
	   /// the message we're expecting to receive on the multicast socket </summary>
	   private byte[] expectedMessage;
	   /// <summary>
	   /// the message we should reply with when we get the expected message </summary>
	   private byte[] returnMessage;

	   /// <summary>
	   /// boolean to indicate if packet handling is active </summary>
	   private volatile bool handlingPacket = false;
       /// <summary>
       /// AutoResetEvent used to stop Multicast reciever </summary>
       private AutoResetEvent waitPacketDone = new AutoResetEvent(false);


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
	   public MulticastListener(int multicastPort, string multicastGroup, byte[] expectedMessage, byte[] returnMessage)
	   {
		  this.expectedMessage = expectedMessage;
		  this.returnMessage = returnMessage;

		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
		  OneWireEventSource.Log.Debug("DEBUG: Creating Multicast Listener");
          OneWireEventSource.Log.Debug("DEBUG:    Multicast port: " + multicastPort);
          OneWireEventSource.Log.Debug("DEBUG:    Multicast group: " + multicastGroup);
          //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

          // create multicast socket
          socket = new DatagramSocket(); // MulticastSocket(multicastPort);
          socket.MessageReceived += Multicast_MessageReceived;
          socket.Control.MulticastOnly = true;
		  handlingPacket = false;

          var t = Task.Run(async () =>
          {
              // specify IP address of adapter...
              await socket.BindEndpointAsync(new HostName("localhost"), Convert.ToString(multicastPort));
          });
          t.Wait();
          

		  //join the multicast group
          socket.JoinMulticastGroup(new HostName(multicastGroup));

          //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
          OneWireEventSource.Log.Debug("DEBUG: waiting for multicast packet");
          //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
        }

        private async void Multicast_MessageReceived(DatagramSocket sender, DatagramSocketMessageReceivedEventArgs args)
        {
            handlingPacket = true;
            var reader = args.GetDataReader();

            // check to see if the received data matches the expected message
            uint length = reader.UnconsumedBufferLength;

            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            OneWireEventSource.Log.Debug("DEBUG: packet.length=" + length);
            OneWireEventSource.Log.Debug("DEBUG: expecting=" + expectedMessage.Length);
            //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
            try
            {

                if (length == expectedMessage.Length)
                {
                    bool dataMatch = true;
                    for (int i = 0; dataMatch && i < length; i++)
                    {
                        dataMatch = (expectedMessage[i] == reader.ReadByte());
                    }
                    // check to see if we received the expected message
                    if (dataMatch)
                    {
                        //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\
                        OneWireEventSource.Log.Debug("DEBUG: packet match, replying");
                        //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\

                        // send return message
                        using (var writer = new DataWriter(socket.OutputStream))
                        {
                            writer.WriteBytes(returnMessage);
                            await writer.StoreAsync();
                            // we return before the packet goes out, that's fine
                        }
                    }
                }
                else
                {
                    OneWireEventSource.Log.Critical("Unknown packet length recieved: " + length);
                }
            }
            catch (System.IO.IOException)
            {
                // drain
                ;
            }
            finally
            {
                handlingPacket = false;
                waitPacketDone.Set();
            }
        }

	   /// <summary>
	   /// Waits for datagram listener to finish, with a timeout.
	   /// </summary>
	   public virtual void stopListener()
	   {
           while (handlingPacket)
               waitPacketDone.WaitOne();

           socket.MessageReceived -= Multicast_MessageReceived;
       }

        ~MulticastListener()
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
                if (waitPacketDone != null)
                    waitPacketDone.Dispose();
                if (socket != null) 
                    socket.Dispose();
            }
        }
    }
}