using System;
using System.Collections;
using System.Threading;
using System.Diagnostics;

/*---------------------------------------------------------------------------
 * Copyright (C) 2001-2003 Dallas Semiconductor Corporation, All Rights Reserved.
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

	//using gnu.io;
	//import javax.comm.*;

	/// <summary>
	///  <para>The SerialService class provides serial IO services to the
	///  USerialAdapter class. </para>
	/// 
	///  @version    1.00, 1 Sep 2003
	///  @author     DS
	/// 
	/// </summary>
	public class SerialService : SerialPortEventListener
	{
	   private const bool DEBUG = false;
	   /// <summary>
	   /// The serial port name of this object (e.g. COM1, /dev/ttyS0) </summary>
	   private readonly string comPortName;
	   /// <summary>
	   /// The serial port object for setting serial port parameters </summary>
	   private SerialPort serialPort = null;
	   /// <summary>
	   /// The input stream, for reading data from the serial port </summary>
	   private System.IO.Stream serialInputStream = null;
	   /// <summary>
	   /// The output stream, for writing data to the serial port </summary>
	   private System.IO.Stream serialOutputStream = null;
	   /// <summary>
	   /// The hash code of the thread that currently owns this serial port </summary>
	   private int currentThreadHash = 0;
	   /// <summary>
	   /// temporary array, used for converting characters to bytes </summary>
	   private sbyte[] tempArray = new sbyte[128];
	   /// <summary>
	   /// used to end the Object.wait loop in readWithTimeout method </summary>
	   [NonSerialized]
	   private bool dataAvailable = false;

	   /// <summary>
	   /// Vector of thread hash codes that have done an open but no close </summary>
	   private readonly ArrayList users = new ArrayList(4);

	   /// <summary>
	   /// Flag to indicate byte banging on read </summary>
	   private readonly bool byteBang;

	   /// <summary>
	   /// Vector of serial port ID strings (i.e. "COM1", "COM2", etc) </summary>
	   private static readonly ArrayList vPortIDs = new ArrayList(2);
	   /// <summary>
	   /// static list of threadIDs to the services they are using </summary>
	   private static Hashtable knownServices = new Hashtable();
	   /// <summary>
	   /// static list of all unique SerialService classes </summary>
	   private static Hashtable uniqueServices = new Hashtable();


	   /// <summary>
	   /// Cleans up the resources used by the thread argument.  If another
	   /// thread starts communicating with this port, and then goes away,
	   /// there is no way to relinquish the port without stopping the
	   /// process. This method allows other threads to clean up.
	   /// </summary>
	   /// <param name="thread"> that may have used a <code>USerialAdapter</code> </param>
	   public static void CleanUpByThread(Thread t)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.WriteLine("SerialService.CleanUpByThread(Thread)");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  try
		  {
			 SerialService temp = (SerialService) knownServices[t];
			 if (temp == null)
			 {
			   return;
			 }

			 lock (temp)
			 {
				if (t.GetHashCode() == temp.currentThreadHash)
				{
					//then we need to release the lock...
					temp.currentThreadHash = 0;
				}
			 }

			 temp.closePortByThreadID(t);
			 knownServices.Remove(t);
		  }
		  catch (System.Exception e)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.WriteLine("Exception cleaning: " + e.ToString());
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  }
	   }

	   /// <summary>
	   /// do not use default constructor
	   /// user getSerialService(String) instead.
	   /// </summary>
	   private SerialService()
	   {
		  this.comPortName = null;
		  this.byteBang = false;
	   }

	   /// <summary>
	   /// this constructor only for use in the static method:
	   /// getSerialService(String)
	   /// </summary>
	   protected internal SerialService(string strComPort)
	   {
		  this.comPortName = strComPort;

		  // check to see if need to byte-bang the reads
		  string prop = com.dalsemi.onewire.OneWireAccessProvider.getProperty("onewire.serial.bytebangread");
		  if (!string.ReferenceEquals(prop, null))
		  {
			 if (prop.IndexOf("true", StringComparison.Ordinal) != -1)
			 {
				byteBang = true;
			 }
			 else
			 {
				byteBang = false;
			 }
		  }
		  else
		  {
			 byteBang = false;
		  }
	   }

	   public static SerialService getSerialService(string strComPort)
	   {
		  lock (uniqueServices)
		  {
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			 if (DEBUG)
			 {
				Debug.WriteLine("SerialService.getSerialService called: strComPort=" + strComPort);
			 }
			 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//

			 string strLowerCaseComPort = strComPort.ToLower();
			 object o = uniqueServices[strLowerCaseComPort];
			 if (o != null)
			 {
				return (SerialService)o;
			 }
			 else
			 {
				SerialService sps = new SerialService(strComPort);
				uniqueServices[strLowerCaseComPort] = sps;
				return sps;
			 }
		  }
	   }

	   /// <summary>
	   /// SerialPortEventListener method.  This just calls the notify
	   /// method on this object, so that all blocking methods are kicked
	   /// awake whenever a serialEvent occurs.
	   /// </summary>
	   public virtual void serialEvent(SerialPortEvent spe)
	   {
		  dataAvailable = true;
		  if (DEBUG)
		  {
			 switch (spe.EventType)
			 {
				case SerialPortEvent.BI:
				   Debug.WriteLine("SerialPortEvent: Break interrupt.");
				   break;
				case SerialPortEvent.CD:
				   Debug.WriteLine("SerialPortEvent: Carrier detect.");
				   break;
				case SerialPortEvent.CTS:
				   Debug.WriteLine("SerialPortEvent: Clear to send.");
				   break;
				case SerialPortEvent.DATA_AVAILABLE:
				   Debug.WriteLine("SerialPortEvent: Data available at the serial port.");
				   break;
				case SerialPortEvent.DSR:
				   Debug.WriteLine("SerialPortEvent: Data set ready.");
				   break;
				case SerialPortEvent.FE:
				   Debug.WriteLine("SerialPortEvent: Framing error.");
				   break;
				case SerialPortEvent.OE:
				   Debug.WriteLine("SerialPortEvent: Overrun error.");
				   break;
				case SerialPortEvent.OUTPUT_BUFFER_EMPTY:
				   Debug.WriteLine("SerialPortEvent: Output buffer is empty.");
				   break;
				case SerialPortEvent.PE:
				   Debug.WriteLine("SerialPortEvent: Parity error.");
				   break;
				case SerialPortEvent.RI:
				   Debug.WriteLine("SerialPortEvent: Ring indicator.");
				   break;
			 }
			 Debug.WriteLine("SerialService.SerialEvent: oldValue=" + spe.OldValue);
			 Debug.WriteLine("SerialService.SerialEvent: newValue=" + spe.NewValue);
		  }
		  //try
		  //{
		  //   serialInputStream.notifyAll();
		  //}
		  //catch(Exception e)
		  //{
		  //   e.printStackTrace();
		  //}
	   }


	   public virtual void openPort()
	   {
		   lock (this)
		   {
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 Debug.WriteLine("SerialService.openPort() called");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  openPort(null);
		   }
	   }

	   public virtual void openPort(SerialPortEventListener spel)
	   {
		   lock (this)
		   {
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 Debug.WriteLine("SerialService.openPort: Thread.currentThread()=" + Thread.CurrentThread);
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  // record this thread as an owner
			  if (users.IndexOf(Thread.CurrentThread) == -1)
			  {
				 users.Add(Thread.CurrentThread);
			  }
        
			  if (PortOpen)
			  {
				 return;
			  }
        
			  CommPortIdentifier port_id;
			  try
			  {
				 port_id = CommPortIdentifier.getPortIdentifier(comPortName);
			  }
			  catch (NoSuchPortException nspe)
			  {
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				 if (DEBUG)
				 {
					Debug.WriteLine("SerialService.openPort: No such port (" + comPortName + "). " + nspe);
				 }
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				 throw new System.IO.IOException("No such port (" + comPortName + "). " + nspe);
			  }
        
			  // check if the port is currently used
			  if (port_id.CurrentlyOwned)
			  {
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				 if (DEBUG)
				 {
					Debug.WriteLine("SerialService.openPort: Port In Use (" + comPortName + ")");
				 }
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				 throw new System.IO.IOException("Port In Use (" + comPortName + ")");
			  }
        
			  // try to aquire the port
			  try
			  {
				 // get the port object
				 serialPort = (SerialPort)port_id.open("Dallas Semiconductor", 2000);
        
				 //serialPort.setInputBufferSize(4096);
				 //serialPort.setOutputBufferSize(4096);
        
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				 if (DEBUG)
				 {
					Debug.WriteLine("SerialService.openPort: getInputBufferSize = " + serialPort.InputBufferSize);
					Debug.WriteLine("SerialService.openPort: getOutputBufferSize = " + serialPort.OutputBufferSize);
				 }
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
        
				 if (spel != null)
				 {
					serialPort.addEventListener(spel);
				 }
				 else
				 {
					serialPort.addEventListener(this);
				 }
				 serialPort.notifyOnOutputEmpty(true);
				 serialPort.notifyOnDataAvailable(true);
        
				 // flow i/o
				 serialPort.FlowControlMode = SerialPort.FLOWCONTROL_NONE;
        
				 serialInputStream = serialPort.InputStream;
				 serialOutputStream = serialPort.OutputStream;
				 // bug workaround
				 serialOutputStream.WriteByte(0);
        
				 // settings
				 serialPort.disableReceiveFraming();
				 serialPort.disableReceiveThreshold();
				 serialPort.enableReceiveTimeout(1);
        
				 // set baud rate
				 serialPort.setSerialPortParams(9600, SerialPort.DATABITS_8, SerialPort.STOPBITS_1, SerialPort.PARITY_NONE);
        
				 serialPort.DTR = true;
				 serialPort.RTS = true;
        
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				 if (DEBUG)
				 {
					Debug.WriteLine("SerialService.openPort: Port Openend (" + comPortName + ")");
				 }
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  }
			  catch (System.Exception e)
			  {
				 // close the port if we have an object
				 if (serialPort != null)
				 {
					serialPort.close();
				 }
        
				 serialPort = null;
        
				 throw new System.IO.IOException("Could not open port (" + comPortName + ") :" + e);
			  }
		   }
	   }

	   public virtual bool NotifyOnDataAvailable
	   {
		   set
		   {
			   lock (this)
			   {
				  serialPort.notifyOnDataAvailable(value);
			   }
		   }
	   }

	   public static System.Collections.IEnumerator SerialPortIdentifiers
	   {
		   get
		   {
			  lock (vPortIDs)
			  {
				 if (vPortIDs.Count == 0)
				 {
					System.Collections.IEnumerator e = CommPortIdentifier.PortIdentifiers;
					while (e.MoveNext())
					{
					   CommPortIdentifier portID = (CommPortIdentifier)e.Current;
					   if (portID.PortType == CommPortIdentifier.PORT_SERIAL)
					   {
						  vPortIDs.Add(portID.Name);
					   }
					}
				 }
				 return vPortIDs.GetEnumerator();
			  }
		   }
	   }

	   public virtual string PortName
	   {
		   get
		   {
			   lock (this)
			   {
				  return comPortName;
			   }
		   }
	   }

	   public virtual bool PortOpen
	   {
		   get
		   {
			   lock (this)
			   {
				  return serialPort != null;
			   }
		   }
	   }

	   public virtual bool DTR
	   {
		   get
		   {
			   lock (this)
			   {
				  return serialPort.DTR;
			   }
		   }
		   set
		   {
			   lock (this)
			   {
				  serialPort.DTR = value;
			   }
		   }
	   }


	   public virtual bool RTS
	   {
		   get
		   {
			   lock (this)
			   {
				  return serialPort.RTS;
			   }
		   }
		   set
		   {
			   lock (this)
			   {
				  serialPort.RTS = value;
			   }
		   }
	   }


	   /// <summary>
	   /// Send a break on this serial port
	   /// </summary>
	   /// <param name="duration"> - break duration in ms </param>
	   public virtual void sendBreak(int duration)
	   {
		   lock (this)
		   {
			  serialPort.sendBreak(duration);
		   }
	   }

	   public virtual int BaudRate
	   {
		   get
		   {
			   lock (this)
			   {
				  return serialPort.BaudRate;
			   }
		   }
		   set
		   {
			   lock (this)
			   {
				  if (!PortOpen)
				  {
					 throw new System.IO.IOException("Port Not Open");
				  }
            
				  try
				  {
					 // set baud rate
					 serialPort.setSerialPortParams(value, SerialPort.DATABITS_8, SerialPort.STOPBITS_1, SerialPort.PARITY_NONE);
            
				  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				  if (DEBUG)
				  {
					 Debug.WriteLine("SerialService.setBaudRate: baudRate=" + value);
				  }
				  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				  }
				  catch (UnsupportedCommOperationException uncoe)
				  {
					 throw new System.IO.IOException("Failed to set baud rate: " + uncoe);
				  }
            
			   }
		   }
	   }


	   /// <summary>
	   /// Close this serial port.
	   /// </summary>
	   /// <exception cref="IOException"> - if port is in use by another application </exception>
	   public virtual void closePort()
	   {
		   lock (this)
		   {
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 Debug.WriteLine("SerialService.closePort");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  closePortByThreadID(Thread.CurrentThread);
		   }
	   }

	   public virtual void flush()
	   {
		   lock (this)
		   {
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 Debug.WriteLine("SerialService.flush");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
        
			  if (!PortOpen)
			  {
				 throw new System.IO.IOException("Port Not Open");
			  }
        
			  serialOutputStream.Flush();
			  while (serialInputStream.available() > 0)
			  {
				 serialInputStream.Read();
			  }
		   }
	   }
	   // ------------------------------------------------------------------------
	   // BeginExclusive/EndExclusive Mutex Methods
	   // ------------------------------------------------------------------------
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
	   /// </para>
	   /// </summary>
	   /// <param name="blocking"> <code>true</code> if want to block waiting
	   ///                 for an excluse access to the adapter </param>
	   /// <returns> <code>true</code> if blocking was false and a
	   ///         exclusive session with the adapter was aquired
	   ///  </returns>
	   public virtual bool beginExclusive(bool blocking)
	   {
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (DEBUG)
		  {
			 Debug.WriteLine("SerialService.beginExclusive(bool)");
		  }
		  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		  if (blocking)
		  {
			 while (!beginExclusive())
			 {
				 try
				 {
					 Thread.Sleep(50);
				 }
				 catch (System.Exception)
				 {
				 }
			 }

			 return true;
		  }
		  else
		  {
			 return beginExclusive();
		  }
	   }

	   /// <summary>
	   /// Relinquishes exclusive control of the 1-Wire Network.
	   /// This command dynamically marks the end of a critical section and
	   /// should be used when exclusive control is no longer needed.
	   /// </summary>
	   public virtual void endExclusive()
	   {
		   lock (this)
		   {
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 Debug.WriteLine("SerialService.endExclusive");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  // if own then release
			  if (currentThreadHash == Thread.CurrentThread.GetHashCode())
			  {
					currentThreadHash = 0;
			  }
			  knownServices.Remove(Thread.CurrentThread);
		   }
	   }

	   /// <summary>
	   /// Check if this thread has exclusive control of the port.
	   /// </summary>
	   public virtual bool haveExclusive()
	   {
		   lock (this)
		   {
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 Debug.WriteLine("SerialService.haveExclusive");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  return (currentThreadHash == Thread.CurrentThread.GetHashCode());
		   }
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
	   /// <exception cref="IOException"> </exception>
	   private bool beginExclusive()
	   {
		   lock (this)
		   {
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 Debug.WriteLine("SerialService.beginExclusive()");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (currentThreadHash == 0)
			  {
				 // not owned so take
				 currentThreadHash = System.Environment.CurrentManagedThreadId.GetHashCode();
				 knownServices[System.Environment.CurrentManagedThreadId] = this;
        
				 return true;
			  }
			  else if (currentThreadHash == System.Environment.CurrentManagedThreadId.GetHashCode())
			  {
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
	   /// Allows clean up port by thread
	   /// </summary>
	   private void closePortByThreadID(Thread t)
	   {
		   lock (this)
		   {
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 Debug.WriteLine("SerialService.closePortByThreadID(Thread), Thread=" + t);
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
        
			  // added singleUser object for case where one thread creates the adapter
			  // (like the main thread), and another thread closes it (like the AWT event)
			  bool singleUser = (users.Count == 1);
        
			  // remove this thread as an owner
			  users.Remove(t);
        
			  // if this is the last owner then close the port
			  if (singleUser || users.Count == 0)
			  {
				 // if don't own a port then just return
				 if (!PortOpen)
				 {
					return;
				 }
        
				 // close the port
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				 if (DEBUG)
				 {
					Debug.WriteLine("SerialService.closePortByThreadID(Thread): calling serialPort.removeEventListener() and .close()");
				 }
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				 serialPort.removeEventListener();
				 serialPort.close();
				 serialPort = null;
				 serialInputStream = null;
				 serialOutputStream = null;
			  }
			  else
			  {
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				 if (DEBUG)
				 {
					Debug.WriteLine("SerialService.closePortByThreadID(Thread): can't close port, owned by another thread");
				 }
			  }
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
		   }
	   }


	   // ------------------------------------------------------------------------
	   // Standard InputStream methods
	   // ------------------------------------------------------------------------

	   public virtual int available()
	   {
		   lock (this)
		   {
			  if (!PortOpen)
			  {
				 throw new System.IO.IOException("Port Not Open");
			  }
        
			  return serialInputStream.available();
		   }
	   }

	   public virtual int read()
	   {
		   lock (this)
		   {
			  if (!PortOpen)
			  {
				 throw new System.IO.IOException("Port Not Open");
			  }
        
			  return serialInputStream.Read();
		   }
	   }

	   public virtual int read(sbyte[] buffer)
	   {
		   lock (this)
		   {
			  if (!PortOpen)
			  {
				 throw new System.IO.IOException("Port Not Open");
			  }
        
			  return read(buffer, 0, buffer.Length);
		   }
	   }

	   public virtual int read(sbyte[] buffer, int offset, int length)
	   {
		   lock (this)
		   {
			  if (!PortOpen)
			  {
				 throw new System.IO.IOException("Port Not Open");
			  }
        
			  return serialInputStream.Read(buffer, offset, length);
		   }
	   }

	   public virtual int readWithTimeout(sbyte[] buffer, int offset, int length)
	   {
		   lock (this)
		   {
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 Debug.WriteLine("SerialService.readWithTimeout(): length=" + length);
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
        
			  if (!PortOpen)
			  {
				 throw new System.IO.IOException("Port Not Open");
			  }
        
			  // set max_timeout to be very long
			  long max_timeout = DateTimeHelperClass.CurrentUnixTimeMillis() + length * 20 + 800;
			  int count = 0;
        
			  // check which mode of reading
			  if (byteBang)
			  {
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
				 if (DEBUG)
				 {
					Debug.WriteLine("SerialService.readWithTimeout(): byte-banging read");
				 }
				 //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
        
				 int new_byte;
				 do
				 {
					new_byte = serialInputStream.Read();
        
					if (new_byte != -1)
					{
					   buffer[count + offset] = (sbyte)new_byte;
					   count++;
					}
					else
					{
					   // check for timeout
					   if (DateTimeHelperClass.CurrentUnixTimeMillis() > max_timeout)
					   {
						  break;
					   }
        
					   // no bytes available yet so yield
					   Thread.@yield();
        
					   //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
					   if (DEBUG)
					   {
						  Debug.Write("y");
					   }
					   //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
					}
				 } while (length > count);
			  }
			  else
			  {
				 do
				 {
					int get_num = serialInputStream.available();
					//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
					//if(DEBUG)
					//   System.out.println("SerialService.readWithTimeout(): get_num=" + get_num + ", ms left=" + (max_timeout - System.currentTimeMillis()));
					//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
					if (get_num > 0)
					{
					   // check for block bigger then buffer
					   if ((get_num + count) > length)
					   {
						  get_num = length - count;
					   }
        
					   // read the block
					   count += serialInputStream.Read(buffer, count + offset, get_num);
					}
					else
					{
					   // check for timeout
					   if (DateTimeHelperClass.CurrentUnixTimeMillis() > max_timeout)
					   {
						  length = 0;
					   }
					   Thread.@yield();
					}
				 } while (length > count);
			  }
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 Debug.WriteLine("SerialService.readWithTimeout: read " + count + " bytes");
				 Debug.WriteLine("SerialService.readWithTimeout: " + com.dalsemi.onewire.utils.Convert.toHexString(buffer, offset, count));
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
        
			  // return the number of characters found
			  return count;
		   }
	   }

	   public virtual char[] readWithTimeout(int length)
	   {
		   lock (this)
		   {
			  sbyte[] buffer = new sbyte[length];
        
			  int count = readWithTimeout(buffer, 0, length);
        
			  if (length != count)
			  {
				 throw new System.IO.IOException("readWithTimeout, timeout waiting for return bytes (wanted " + length + ", got " + count + ")");
			  }
        
			  char[] returnBuffer = new char[length];
			  for (int i = 0; i < length; i++)
			  {
				 returnBuffer[i] = (char)(buffer[i] & 0x00FF);
			  }
        
			  return returnBuffer;
		   }
	   }

	   // ------------------------------------------------------------------------
	   // Standard OutputStream methods
	   // ------------------------------------------------------------------------
	   public virtual void write(int data)
	   {
		   lock (this)
		   {
			  if (!PortOpen)
			  {
				 throw new System.IO.IOException("Port Not Open");
			  }
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 Debug.WriteLine("SerialService.write: write 1 byte");
				 Debug.WriteLine("SerialService.write: " + com.dalsemi.onewire.utils.Convert.toHexString((sbyte)data));
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
        
			  try
			  {
				 serialOutputStream.WriteByte(data);
				 serialOutputStream.Flush();
			  }
			  catch (System.IO.IOException e)
			  {

                    // drain IOExceptions that are 'Interrrupted' on Linux
                    // convert the rest to IOExceptions
                    if (!((System.Environment.GetEnvironmentVariable("os.name").IndexOf("Linux") != -1) && (e.ToString().IndexOf("Interrupted", StringComparison.Ordinal) != -1)))
				 {
					throw new System.IO.IOException("write(char): " + e);
				 }
			  }
		   }
	   }
	   public virtual void write(sbyte[] data, int offset, int length)
	   {
		   lock (this)
		   {
			  if (!PortOpen)
			  {
				 throw new System.IO.IOException("Port Not Open");
			  }
        
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 Debug.WriteLine("SerialService.write: write " + length + " bytes");
				 Debug.WriteLine("SerialService.write: " + com.dalsemi.onewire.utils.Convert.toHexString(data, offset, length));
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
        
			  try
			  {
				 serialOutputStream.Write(data, offset, length);
				 serialOutputStream.Flush();
			  }
			  catch (IOException e)
			  {
        
				 // drain IOExceptions that are 'Interrrupted' on Linux
				 // convert the rest to IOExceptions
				 if (!((System.Environment.GetEnvironmentVariable("os.name").IndexOf("Linux") != -1) && (e.ToString().IndexOf("Interrupted", StringComparison.Ordinal) != -1)))
				 {
					throw new System.IO.IOException("write(char): " + e);
				 }
			  }
		   }
	   }

	   public virtual void write(sbyte[] data)
	   {
		   lock (this)
		   {
			 write(data, 0, data.Length);
		   }
	   }
	   public virtual void write(string data)
	   {
		   lock (this)
		   {
			  sbyte[] dataBytes = data.GetBytes();
			  write(dataBytes, 0, dataBytes.Length);
		   }
	   }

	   public virtual void write(char data)
	   {
		   lock (this)
		   {
			  write((int)data);
		   }
	   }

	   public virtual void write(char[] data)
	   {
		   lock (this)
		   {
			  write(data, 0, data.Length);
		   }
	   }

	   public virtual void write(char[] data, int offset, int length)
	   {
		   lock (this)
		   {
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
			  if (DEBUG)
			  {
				 Debug.WriteLine("SerialService.write: write " + length + " chars");
			  }
			  //\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//\\//
        
			  if (length > tempArray.Length)
			  {
				 tempArray = new sbyte[length];
			  }
        
			  for (int i = 0; i < length; i++)
			  {
				 tempArray[i] = (sbyte)data[i];
			  }
        
			  write(tempArray, 0, length);
		   }
	   }
	}
}