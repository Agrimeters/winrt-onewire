using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.Streams;

namespace com.dalsemi.onewire.adapter
{
    public static class Ds2490
    {
        public class Pipe
        {
            public const UInt32 InterruptInPipeIndex = 0;
            public const UInt32 BulkInPipeIndex = 0;
            public const UInt32 BulkOutPipeIndex = 0;
        }

        public const UInt16 DeviceVid = 0x04FA;
        public const UInt16 DevicePid = 0x2490;

        // Error codes
        public class ERR
        {
            public const byte NOERROR = 0;   // an error has not yet been encountered
            public const byte GETLASTERROR = 1;   // use GetLastError() for more information
            public const byte RESULTREGISTERS = 2;   // use DS2490COMM_GetLastResultRegister() for more info
            public const byte USBDEVICE = 3;   // error from USB device driver
            public const byte READWRITE = 4;   // an I/O error occurred while communicating w/ device
            public const byte TIMEOUT = 5;   // an operation timed out before completion
            public const byte INCOMPLETEWRITE = 6;   // not all data could be sent for output
            public const byte INCOMPLETEREAD = 7;   // not all data could be received for an input
            public const byte INITTOUCHBYTE = 8;   // the touch byte thread could not be started
        }

        public class CMD_TYPE
        {
            // Request byte, Command Type Code Constants 
            public const byte CONTROL = 0x00;
            public const byte COMM = 0x01;
            public const byte MODE = 0x02;
            public const byte TEST = 0x03;
        }

        //
        // Value field, Control commands
        //
        public class CTL
        {
            // Control Command Code Constants 
            public const UInt16 RESET_DEVICE = 0x0000;
            public const UInt16 START_EXE = 0x0001;
            public const UInt16 RESUME_EXE = 0x0002;
            public const UInt16 HALT_EXE_IDLE = 0x0003;
            public const UInt16 HALT_EXE_DONE = 0x0004;
            public const UInt16 CANCEL_CMD = 0x0005;
            public const UInt16 CANCEL_MACRO = 0x0006;
            public const UInt16 FLUSH_COMM_CMDS = 0x0007;
            public const UInt16 FLUSH_RCV_BUFFER = 0x0008;
            public const UInt16 FLUSH_XMT_BUFFER = 0x0009;
            public const UInt16 GET_COMM_CMDS = 0x000A;
        }

        //
        // Value field COMM Command options
        //
        public class COMM
        {
            // COMM Bits (bitwise or into COMM commands to build full value byte pairs)
            // Byte 1
            public const UInt16 TYPE = 0x0008;
            public const UInt16 SE = 0x0008;
            public const UInt16 D = 0x0008;
            public const UInt16 Z = 0x0008;
            public const UInt16 CH = 0x0008;
            public const UInt16 SM = 0x0008;
            public const UInt16 R = 0x0008;
            public const UInt16 IM = 0x0001;

            // Byte 2
            public const UInt16 PS = 0x4000;
            public const UInt16 PST = 0x4000;
            public const UInt16 CIB = 0x4000;
            public const UInt16 RTS = 0x4000;
            public const UInt16 DT = 0x2000;
            public const UInt16 SPU = 0x1000;
            public const UInt16 F = 0x0800;
            public const UInt16 ICP = 0x0200;
            public const UInt16 RST = 0x0100;

            // Read Straight command, special bits 
            public const UInt16 READ_STRAIGHT_NTF = 0x0008;
            public const UInt16 READ_STRAIGHT_ICP = 0x0004;
            public const UInt16 READ_STRAIGHT_RST = 0x0002;
            public const UInt16 READ_STRAIGHT_IM = 0x0001;

            //
            // Value field COMM Command options (0-F plus assorted bits)
            //
            public const UInt16 ERROR_ESCAPE = 0x0601;
            public const UInt16 SET_DURATION = 0x0012;
            public const UInt16 BIT_IO = 0x0020;
            public const UInt16 PULSE = 0x0030;
            public const UInt16 _1_WIRE_RESET = 0x0042;
            public const UInt16 BYTE_IO = 0x0052;
            public const UInt16 MATCH_ACCESS = 0x0064;
            public const UInt16 BLOCK_IO = 0x0074;
            public const UInt16 READ_STRAIGHT = 0x0080;
            public const UInt16 DO_RELEASE = 0x6092;
            public const UInt16 SET_PATH = 0x00A2;
            public const UInt16 WRITE_SRAM_PAGE = 0x00B2;
            public const UInt16 WRITE_EPROM = 0x00C4;
            public const UInt16 READ_CRC_PROT_PAGE = 0x00D4;
            public const UInt16 READ_REDIRECT_PAGE_CRC = 0x21E4;
            public const UInt16 SEARCH_ACCESS = 0x00F4;
        }

        // Mode Command Code
        // Enable Pulse
        public const byte ENABLEPULSE_PRGE = 0x01;  // strong pull-up
        public const byte ENABLEPULSE_SPUE = 0x02;  // programming pulse

        // 1Wire Bus Speed Setting
        public const byte ONEWIREBUSSPEED_REGULAR = 0x00;
        public const byte ONEWIREBUSSPEED_FLEXIBLE = 0x01;
        public const byte ONEWIREBUSSPEED_OVERDRIVE = 0x02;

        public class Mode
        {
            //
            // Value field Mode Commands options
            //
            public const UInt16 PULSE_EN = 0x0000;
            public const UInt16 SPEED_CHANGE_EN = 0x0001;
            public const UInt16 _1WIRE_SPEED = 0x0002;
            public const UInt16 STRONG_PU_DURATION = 0x0003;
            public const UInt16 PULLDOWN_SLEWRATE = 0x0004;
            public const UInt16 PROG_PULSE_DURATION = 0x0005;
            public const UInt16 WRITE1_LOWTIME = 0x0006;
            public const UInt16 DSOW0_TREC = 0x0007;
        }

        //
        // This is the status structure as returned by the Interrupt Pipe
        //
        public class UsbStatusPacket
        {
            public void UpdateUsbStatusPacket(DataReader m, uint length)
            {
                EnableFlags = m.ReadByte();
                OneWireSpeed = m.ReadByte();
                StrongPullUpDuration = m.ReadByte();
                ProgPulseDuration = m.ReadByte();
                PullDownSlewRate = m.ReadByte();
                Write1LowTime = m.ReadByte();
                DSOW0RecoveryTime = m.ReadByte();
                Reserved1 = m.ReadByte();
                StatusFlags = m.ReadByte();
                CurrentCommCmd1 = m.ReadByte();
                CurrentCommCmd2 = m.ReadByte();
                CommBufferStatus = m.ReadByte();
                WriteBufferStatus = m.ReadByte();
                ReadBufferStatus = m.ReadByte();
                Reserved2 = m.ReadByte();
                Reserved3 = m.ReadByte();

                var len = length - 16;
                if (len > 0)
                {
                    byte[] buff = new byte[len];
                    for (int i = 0; i < len; i++)
                        buff[i] = m.ReadByte();
                    CommResultCodes = buff;
                }
                else
                {
                    CommResultCodes = null;
                }

                Updated = true;
            }

            public byte EnableFlags { get; set; }
            public byte OneWireSpeed { get; set; }
            public byte StrongPullUpDuration { get; set; }
            public byte ProgPulseDuration { get; set; }
            public byte PullDownSlewRate { get; set; }
            public byte Write1LowTime { get; set; }
            public byte DSOW0RecoveryTime { get; set; }
            public byte Reserved1 { get; set; }
            public byte StatusFlags { get; set; }
            public byte CurrentCommCmd1 { get; set; }
            public byte CurrentCommCmd2 { get; set; }
            public byte CommBufferStatus { get; set; }  // Buffer for COMM commands
            public byte WriteBufferStatus { get; set; } // Buffer we write to
            public byte ReadBufferStatus { get; set; }  // Buffer we read from
            public byte Reserved2 { get; set; }
            public byte Reserved3 { get; set; }

            public byte[] CommResultCodes { get; set; }

            public bool Updated { get; set; }
        }

        //
        // STATUS FLAGS
        //
        // Enable Flags
        public const byte ENABLEFLAGS_SPUE = 0x01;  // if set Strong Pull-up to 5V enabled
        public const byte ENABLEFLAGS_PRGE = 0x02;  // if set 12V programming pulse enabled
        public const byte ENABLEFLAGS_SPCE = 0x04;  // if set a dynamic 1-Wire bus speed change through Comm. Cmd. enabled

        // Device Status Flags
        public const byte STATUSFLAGS_SPUA = 0x01;  // if set Strong Pull-up is active
        public const byte STATUSFLAGS_PRGA = 0x02;  // if set a 12V programming pulse is being generated
        public const byte STATUSFLAGS_12VP = 0x04;  // if set the external 12V programming voltage is present
        public const byte STATUSFLAGS_PMOD = 0x08;  // if set the DS2490 powered from USB and external sources
        public const byte STATUSFLAGS_HALT = 0x10;  // if set the DS2490 is currently halted
        public const byte STATUSFLAGS_IDLE = 0x20;  // if set the DS2490 is currently idle

        // Result Registers
        public const byte ONEWIREDEVICEDETECT = 0xA5;  // 1-Wire device detected on bus
        public const byte COMMCMDERRORRESULT_NRS = 0x01;  // if set 1-WIRE RESET did not reveal a Presence Pulse or SET PATH did not get a Presence Pulse from the branch to be connected
        public const byte COMMCMDERRORRESULT_SH = 0x02;  // if set 1-WIRE RESET revealed a short on the 1-Wire bus or the SET PATH couln not connect a branch due to short
        public const byte COMMCMDERRORRESULT_APP = 0x04;  // if set a 1-WIRE RESET revealed an Alarming Presence Pulse
        public const byte COMMCMDERRORRESULT_VPP = 0x08;  // if set during a PULSE with TYPE=1 or WRITE EPROM command the 12V programming pulse not seen on 1-Wire bus
        public const byte COMMCMDERRORRESULT_CMP = 0x10;  // if set there was an error reading confirmation byte of SET PATH or WRITE EPROM was unsuccessful
        public const byte COMMCMDERRORRESULT_CRC = 0x20;  // if set a CRC occurred for one of the commands: WRITE SRAM PAGE, WRITE EPROM, READ EPROM, READ CRC PROT PAGE, or READ REDIRECT PAGE W/CRC
        public const byte COMMCMDERRORRESULT_RDP = 0x40;  // if set READ REDIRECT PAGE WITH CRC encountered a redirected page
        public const byte COMMCMDERRORRESULT_EOS = 0x80;  // if set SEARCH ACCESS with SM=1 ended sooner than expected with too few ROM IDs

        // Strong Pullup 
        public const UInt16 SPU_MULTIPLE_MS = 128;
        public const UInt16 SPU_DEFAULT_CODE = 512 / SPU_MULTIPLE_MS;   // default Strong pullup value

        // Programming Pulse 
        public const UInt16 PRG_MULTIPLE_US = 8;               // Programming Pulse Time Multiple (Time = PRG_MULTIPLE_US * DurationCode)
        public const UInt16 PRG_DEFAULT_CODE = 512 / PRG_MULTIPLE_US;   // default Programming pulse value


        public const byte g_DS2490COMM_LAST_SHORTEDBUS = 9;   // 1Wire bus shorted on
        public const byte g_DS2490COMM_LAST_EMPTYBUS = 10;  // 1Wire bus empty

        public const uint IOCTL_INBUF_SIZE = 512;
        public const uint IOCTL_OUTBUF_SIZE = 512;
        public const byte TIMEOUT_PER_BYTE = 15;    //1000 modified 4/27/00 BJV

    }
}
