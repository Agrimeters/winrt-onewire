using System;

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
            /// <summary>
            /// Reset Device
            /// </summary>
            public const UInt16 RESET_DEVICE = 0x0000;

            /// <summary>
            /// Start Execution
            /// </summary>
            public const UInt16 START_EXE = 0x0001;

            /// <summary>
            /// Resume Execution
            /// </summary>
            public const UInt16 RESUME_EXE = 0x0002;

            /// <summary>
            /// Halt Execution when Idle
            /// </summary>
            public const UInt16 HALT_EXE_IDLE = 0x0003;

            /// <summary>
            /// Halt Execution when Done
            /// </summary>
            public const UInt16 HALT_EXE_DONE = 0x0004;

            /// <summary>
            /// Cancel Command
            /// </summary>
            public const UInt16 CANCEL_CMD = 0x0005;

            /// <summary>
            /// Cancel Macro
            /// </summary>
            public const UInt16 CANCEL_MACRO = 0x0006;

            /// <summary>
            /// The DS2490 must be in a halted state before the
            /// FLUSH_COMM_CMDS command can be processed.
            /// </summary>
            public const UInt16 FLUSH_COMM_CMDS = 0x0007;

            /// <summary>
            /// The DS2490 must be in a halted state before the
            /// FLUSH_DATA_RCV_BUFFER command can be processed.
            /// </summary>
            public const UInt16 FLUSH_DATA_RCV_BUFFER = 0x0008;

            /// <summary>
            /// The DS2490 must be in a halted state before the
            /// FLUSH_DATA_XMT_BUFFER command can be processed.
            /// </summary>
            public const UInt16 FLUSH_DATA_XMT_BUFFER = 0x0009;

            /// <summary>
            /// The DS2490 must be in a halted state before the
            /// GET_COMM_CMDS command can be processed.
            /// </summary>
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

            /// <summary>
            /// SE = 1 enable the speed change on the 1-Wire bus.
            /// SE = 0 disable the speed change on the 1-Wire bus.
            /// </summary>
            public const UInt16 SE = 0x0008;

            /// <summary>
            /// Data bit value to be written to the 1-Wire bus.
            /// </summary>
            public const UInt16 D = 0x0008;

            /// <summary>
            /// Z = 1 checks if the 0-bits in the byte to be written are 0-bits in the
            /// byte read back form the device.
            /// Z = 0 checks if the byte to be written is identical to the one read back
            /// from the device.
            /// </summary>
            public const UInt16 Z = 0x0008;

            /// <summary>
            /// CH = 1 follows the chain if the page is redirected.
            /// CH = 0 stops reading if the page is redirected.
            /// </summary>
            public const UInt16 CH = 0x0008;

            /// <summary>
            /// SM = 1 searches for and reports ROM Ids without really accessing a
            /// particular device.
            /// SM = 0 makes a “Strong Access” to a particular device.
            /// </summary>
            public const UInt16 SM = 0x0008;

            /// <summary>
            /// R = 1 performs a read function.
            /// R = 0 performs a write function.
            /// </summary>
            public const UInt16 R = 0x0008;

            /// <summary>
            /// IM = 1 enables immediate execution of the command. Assumes that all 1-Wire
            /// device data required by the command has been received at EP2.
            /// IM = 0 prevents immediate execution of the command; execution must be started
            /// through a control function command.
            /// </summary>
            public const UInt16 IM = 0x0001;

            // Byte 2
            /// <summary>
            /// PS = 1 reduces the preamble size to 2 bytes (rather than 3).
            /// PS = 0 sets preamble size to 3 bytes.
            /// </summary>
            public const UInt16 PS = 0x4000;

            /// <summary>
            /// PST = 1 continuously generate 1-Wire Reset sequences until a
            /// presence pulse is discovered.
            /// PST = 0 generate only one 1-Wire Reset sequence.
            /// </summary>
            public const UInt16 PST = 0x4000;

            /// <summary>
            /// CIB = 1 prevents a strong pullup to 5V if SPU = 1 and the bit read
            /// back from the 1Wire bus is 1.
            /// CIB = 0 generally enables the strong pullup to 5V.
            /// </summary>
            public const UInt16 CIB = 0x4000;

            /// <summary>
            /// RTS = 1 returns the discrepancy information to the host if SM = 1 and
            /// there are more devices than could be discovered in the current pass.
            /// RTS = 0 does not return discrepancy information.
            /// </summary>
            public const UInt16 RTS = 0x4000;

            /// <summary>
            /// DT = 1 activates/selects the CRC16 generator
            /// DT = 0 specifies no CRC.
            /// </summary>
            public const UInt16 DT = 0x2000;

            /// <summary>
            /// SPU = 1 inserts a strong pullup to 5V after a Bit or Byte or Block I/O or
            /// Do & Release command.
            /// SPU = 0 no strong pullup.
            /// </summary>
            public const UInt16 SPU = 0x1000;

            /// <summary>
            /// F = 1 clears the buffers in case an error occurred during the execution of
            /// the previous command; requires that ICP = 0 in the previous command.
            /// F = 0 prevents the buffers from being cleared.
            /// </summary>
            public const UInt16 F = 0x0800;

            /// <summary>
            /// NTF = 1 always generate communication command processing result feedback if
            /// ICP = 0 NTF = 0 generate communication command processing result feedback
            /// only if an error occurs and ICP = 0. If ICP = 1 command result feedback is
            /// suppressed for either case, see the ICP bit above.
            /// </summary>
            public const UInt16 NTF = 0x0400;

            /// <summary>
            /// ICP = 1 indicates that the command is not the last one of a macro;
            /// as a consequence command processing result feedback messages are suppressed.
            /// ICP = 0 indicates that the command is the last one of a macro or single command
            /// operation; enables command processing result feedback signaling.
            /// </summary>
            public const UInt16 ICP = 0x0200;

            /// <summary>
            /// RST = 1 inserts a 1-Wire Reset before executing the command.
            /// RST = 0 no 1-Wire Reset inserted.
            /// </summary>
            public const UInt16 RST = 0x0100;

            // Read Straight command, special bits
            public const UInt16 READ_STRAIGHT_NTF = 0x0008;

            public const UInt16 READ_STRAIGHT_ICP = 0x0004;
            public const UInt16 READ_STRAIGHT_RST = 0x0002;
            public const UInt16 READ_STRAIGHT_IM = 0x0001;

            //
            // Value field COMM Command options (0-F plus assorted bits)
            //
            public const UInt16 SET_DURATION = 0x0012;

            public const UInt16 PULSE = 0x0030;
            public const UInt16 ONEWIRE_RESET = 0x0042;
            public const UInt16 BIT_IO = 0x0020;
            public const UInt16 BYTE_IO = 0x0052;
            public const UInt16 BLOCK_IO = 0x0074;
            public const UInt16 MATCH_ACCESS = 0x0064;
            public const UInt16 READ_STRAIGHT = 0x0080;
            public const UInt16 DO_RELEASE = 0x6092;
            public const UInt16 SET_PATH = 0x00A2;
            public const UInt16 WRITE_SRAM_PAGE = 0x00B2;
            public const UInt16 READ_CRC_PROT_PAGE = 0x00D4;
            public const UInt16 READ_REDIRECT_PAGE_CRC = 0x21E4;
            public const UInt16 SEARCH_ACCESS = 0x00F4;

            public const UInt16 ERROR_ESCAPE = 0x0601;
            public const UInt16 WRITE_EPROM = 0x00C4;
        }

        // Mode Command Code
        // Enable Pulse
        public const byte ENABLEPULSE_PRGE = 0x01;  // programming pulse

        public const byte ENABLEPULSE_SPUE = 0x02;  // strong pull-up

        public class Mode
        {
            //
            // Value field Mode Commands options
            //
            public const UInt16 PULSE_EN = 0x0000;

            public const UInt16 SPEED_CHANGE_EN = 0x0001;
            public const UInt16 ONEWIRE_SPEED = 0x0002;
            public const UInt16 STRONG_PU_DURATION = 0x0003;
            public const UInt16 PULLDOWN_SLEWRATE = 0x0004;
            public const UInt16 PROG_PULSE_DURATION = 0x0005;
            public const UInt16 WRITE1_LOWTIME = 0x0006;
            public const UInt16 DSOW0_TREC = 0x0007;
        }

        //
        // STATUS FLAGS
        //

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