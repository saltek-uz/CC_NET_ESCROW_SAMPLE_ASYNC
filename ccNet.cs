using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace CC_NET_ESCROW_SAMPLE_ASYNC
{

    internal class cmdList                    // enum of all possible commands to translate thru CCNet protocol
    {
        public const byte
        ccnetCmdACK         = 0x00,
        ccnetCmdNACK        = 0xFF,
        ccnetCmdReset       = 0x30,
        ccnetCmdStat        = 0x31,
        ccnetCmdSsecure     = 0x32,
        ccnetCmdPoll        = 0x33,
        ccnetCmdEnBills     = 0x34,
        ccnetCmdStack       = 0x35,
        ccnetCmdReturn      = 0x36,
        ccnetCmdID          = 0x37,
        ccnetCmdHold        = 0x38,
        ccnetCmdsetBcPrm    = 0x39,
        ccnetCmdExtBcData   = 0x3A,
        ccnetCmdTable       = 0x41,
        ccnetCmdDwnLoad     = 0x50,
        ccnetCmdCRC32       = 0x51,
        ccnetCmdStatic      = 0x60;
    };



    internal static class ccNet
    {

        public static byte[] prepareCmd(byte cmd) // однобайтовые комманды
        {
            byte[] arr = new byte[1];
            arr[0] = cmd;
            return ccNetLowLevel.cmdPocketAsmbr(arr);
        }

        public static byte[] prepareCmd(byte cmd, byte[] args) // с аргументом (Enable, Disable, Secure)
        {
            byte[] arr = new byte[args.Length+1];
            Array.ConstrainedCopy(args, 0, arr, 1, args.Length);
            arr[0] = cmd;
            return ccNetLowLevel.cmdPocketAsmbr(arr);
        }

        public static ccResponse extractReceivedData(byte[] data) // Обдирает входящий пакет от сопроводиловки
        {
            ccResponse cRp = null;

            if (!ccNetLowLevel.isPocketOK(data)) return null;

            byte[] body = new byte[data[2] - 5];

            Array.ConstrainedCopy( data, 3, body, 0, body.Length);

            if (body.Length == 1) cRp = new ccResponse(body[0]);
            else 
            {
                byte[] tmp = new byte[body.Length-1];
                Array.ConstrainedCopy(body,1, tmp,0, tmp.Length);
                cRp = new ccResponse(body[0], tmp);
            } 

            return cRp;
        }

        public static bool sendCmd(SerialPort sPort, byte[] data)
        {
            try
            {
                if (sPort.IsOpen)
                {
                    sPort.Write(data, 0, data.Length );
                }
                else return false;
            }
            catch { return false; }

            return true;
        }


        public static byte[] receiveCmd(SerialPort sPort)
        {

            try
            {
                if (sPort.IsOpen)
                {
                    int dataLenth = sPort.BytesToRead;
                    if (dataLenth == 0) return null;
                    byte[] data = new byte[dataLenth];
                    sPort.Read(data, 0, dataLenth);
                    return data;
                };

            }
            catch { }

            return null;
        }

    }




    public class ccResponse
    {
        public byte cmd;
        public byte[] arg;

        public ccResponse()
        {
            arg = null;
            cmd = 254;
        }

        public ccResponse(byte[] inData)
        {
            arg = new byte[inData.Length];
            Array.Copy(inData, arg, inData.Length);
        }

        public ccResponse(byte cmd, byte[] inData) : this(inData)
        {
            this.cmd = cmd;
        }

        public ccResponse(byte cmd) : this()
        {
            this.cmd = cmd;
        }

    }




    public static class ccNetLowLevel
    {
        const ushort POLYNOMIAL = 0x8408;
        public static ushort GetCRC16(byte[] bufData, int sizeData)
        {
            ushort TmpCRC, i;

            TmpCRC = 0;
            for (i = 0; i < sizeData; i++)
            {
                TmpCRC = (ushort)(TmpCRC ^ bufData[i]);
                for (int j = 0; j < 8; j++)
                {
                    if ((TmpCRC & 0x0001) != 0) { TmpCRC >>= 1; TmpCRC ^= POLYNOMIAL; }
                    else TmpCRC >>= 1;
                }
            }
            return TmpCRC;
        }


        public static byte[] cmdPocketAsmbr(byte[] cmd) // добавляет CRC16 в конец пакета для передачи, а также суффикс и длину
        {
            int arrLen = cmd.Length;

            if (arrLen == 0) return null;
            if (arrLen > 7) return null;

            byte[] cc = new byte[arrLen + 5];

            Array.ConstrainedCopy(cmd, 0, cc, 3, cmd.Length);
            cc[0] = 0x02;
            cc[1] = 0x03;
            cc[2] = (byte)(arrLen + 5);
            ushort tmpCrc = GetCRC16(cc, arrLen + 3);

            cc[arrLen + 3] = (byte)(tmpCrc & 0xFF);
            cc[arrLen + 4] = (byte)((tmpCrc >> 8) & 0xFF);

            return cc;
        }

        public static bool isPocketOK(byte[] cmd)
        {
            int arrLen = cmd.Length;

            if (arrLen < 6)         return false; // слишком короткий
            if (arrLen < cmd[2])    return false; // обкоцаный
            if (cmd[2] < 6)         return false; // странная длина ответа
            if (cmd[2] > 130)       return false; // странная длина ответа


            ushort tmpCrc = GetCRC16(cmd, arrLen - 2);

            if (
                (cmd[arrLen - 2] != (byte)(tmpCrc & 0xFF)) ||
                (cmd[arrLen - 1] != (byte)((tmpCrc >> 8) & 0xFF))
               )
                return false; // CRC16 побито
            else
                return true; 
        }





    }




}
