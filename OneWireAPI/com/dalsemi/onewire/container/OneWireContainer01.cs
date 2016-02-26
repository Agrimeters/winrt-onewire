/*---------------------------------------------------------------------------
 * Copyright (C) 1999,2000 Dallas Semiconductor Corporation, All Rights Reserved.
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

namespace com.dalsemi.onewire.container
{

	// imports
	using com.dalsemi.onewire.adapter;


	/// <summary>
	/// <P>1-Wire&#174 container that encapsulates the functionality of the 1-Wire
	/// family type <B>01</B> (hex), Dallas Semiconductor part number: <B>DS1990A,
	/// Serial Number</B>.</P>
	/// 
	/// <P> This 1-Wire device is used as a unique serial number. </P>
	/// 
	/// <H2> Features </H2>
	/// <UL>
	///   <LI> 64 bit unique serial number
	///   <LI> Operating temperature range from -40&#176C to
	///        +85&#176C
	/// </UL>
	/// 
	/// <H2> Alternate Names </H2>
	/// <UL>
	///   <LI> DS2401
	///   <LI> DS1420 (Family 81)
	/// </UL>
	/// 
	/// <H2> DataSheets </H2>
	/// 
	///   <A HREF="http://pdfserv.maxim-ic.com/arpdf/DS1990A.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS1990A.pdf</A><br>
	///   <A HREF="http://pdfserv.maxim-ic.com/arpdf/DS2401.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS2401.pdf</A><br>
	///   <A HREF="http://pdfserv.maxim-ic.com/arpdf/DS1420.pdf"> http://pdfserv.maxim-ic.com/arpdf/DS1420.pdf</A>
	/// 
	///  @version    0.00, 28 Aug 2000
	///  @author     DS
	/// </summary>
	public class OneWireContainer01 : OneWireContainer
	{
	   /// <summary>
	   /// Create an empty container.  Must call <code>setupContainer</code> before
	   /// using this new container.<para>
	   /// 
	   /// This is one of the methods to construct a container.  The others are
	   /// through creating a OneWireContainer with parameters.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <seealso cref= #OneWireContainer01(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #OneWireContainer01(DSPortAdapter,long) </seealso>
	   /// <seealso cref= #OneWireContainer01(DSPortAdapter,String) </seealso>
	   /// <seealso cref= #setupContainer(DSPortAdapter,byte[]) </seealso>
	   /// <seealso cref= #setupContainer(DSPortAdapter,long) </seealso>
	   /// <seealso cref= #setupContainer(DSPortAdapter,String) </seealso>
	   public OneWireContainer01() : base()
	   {
	   }

	   /// <summary>
	   /// Create a container with a provided adapter object
	   /// and the address of the iButton or 1-Wire device.<para>
	   /// 
	   /// This is one of the methods to construct a container.  The other is
	   /// through creating a OneWireContainer with NO parameters.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   /// this iButton. </param>
	   /// <param name="newAddress">        address of this 1-Wire device </param>
	   /// <seealso cref= #OneWireContainer01() </seealso>
	   /// <seealso cref= com.dalsemi.onewire.utils.Address </seealso>
	   public OneWireContainer01(DSPortAdapter sourceAdapter, sbyte[] newAddress) : base(sourceAdapter, newAddress)
	   {
	   }

	   /// <summary>
	   /// Create a container with a provided adapter object
	   /// and the address of the iButton or 1-Wire device.<para>
	   /// 
	   /// This is one of the methods to construct a container.  The other is
	   /// through creating a OneWireContainer with NO parameters.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   /// this iButton. </param>
	   /// <param name="newAddress">        address of this 1-Wire device </param>
	   /// <seealso cref= #OneWireContainer01() </seealso>
	   /// <seealso cref= com.dalsemi.onewire.utils.Address </seealso>
	   public OneWireContainer01(DSPortAdapter sourceAdapter, long newAddress) : base(sourceAdapter, newAddress)
	   {
	   }

	   /// <summary>
	   /// Create a container with a provided adapter object
	   /// and the address of the iButton or 1-Wire device.<para>
	   /// 
	   /// This is one of the methods to construct a container.  The other is
	   /// through creating a OneWireContainer with NO parameters.
	   /// 
	   /// </para>
	   /// </summary>
	   /// <param name="sourceAdapter">     adapter object required to communicate with
	   /// this iButton. </param>
	   /// <param name="newAddress">        address of this 1-Wire device </param>
	   /// <seealso cref= #OneWireContainer01() </seealso>
	   /// <seealso cref= com.dalsemi.onewire.utils.Address </seealso>
	   public OneWireContainer01(DSPortAdapter sourceAdapter, string newAddress) : base(sourceAdapter, newAddress)
	   {
	   }

	   public override string Name
	   {
		   get
		   {
			  return "DS1990A";
		   }
	   }

	   public override string AlternateNames
	   {
		   get
		   {
			  return "DS2401,DS2411";
		   }
	   }

	   public override string Description
	   {
		   get
		   {
			  return "64-bit unique serial number";
		   }
	   }
	}

}