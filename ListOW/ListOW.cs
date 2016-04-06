using System;
using System.Collections;
using System.Diagnostics;
using DSPortAdapter = com.dalsemi.onewire.adapter.DSPortAdapter;

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

using OneWireAccessProvider = com.dalsemi.onewire.OneWireAccessProvider;
using OneWireContainer = com.dalsemi.onewire.container.OneWireContainer;

/// <summary>
/// Minimal demo to list device found on default 1-Wire port
///
/// @version    0.00, 28 August 2000
/// @author     DS
/// </summary>
public class ListOW1
{
    /// <summary>
    /// Method main
    ///
    /// </summary>
    /// <param name="args">
    ///  </param>
    public static void Main1(string[] args)
    {
        OneWireContainer owd;

        try
        {
            // get the default adapter
            DSPortAdapter adapter = OneWireAccessProvider.DefaultAdapter;

            Debug.WriteLine("");
            Debug.WriteLine("Adapter: " + adapter.AdapterName + " Port: " + adapter.PortName);
            Debug.WriteLine("");

            // get exclusive use of adapter
            adapter.beginExclusive(true);

            // clear any previous search restrictions
            adapter.setSearchAllDevices();
            adapter.targetAllFamilies();
            adapter.Speed = DSPortAdapter.SPEED_REGULAR;

            // enumerate through all the 1-Wire devices found
            for (System.Collections.IEnumerator owd_enum = adapter.AllDeviceContainers; owd_enum.MoveNext();)
            {
                owd = (OneWireContainer)owd_enum.Current;

                Debug.WriteLine(owd.AddressAsString);
            }

            // end exclusive use of adapter
            adapter.endExclusive();

            // free port used by adapter
            adapter.freePort();
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
        }

        return;
    }
}