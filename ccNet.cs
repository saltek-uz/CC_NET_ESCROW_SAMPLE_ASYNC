﻿using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;

namespace CC_NET_ESCROW_SAMPLE_ASYNC
{
    enum vldStates                  // enum of all possible states to translate thru CCNet protocol
    {
        state_powerUp       = 0x10,
        state_powerUpBV     = 0x11,
        state_powerUpBS     = 0x12,
        state_initialize    = 0x13,
        state_idling        = 0x14,
        state_accepting     = 0x15,
        state_stacking      = 0x17,
        state_returning     = 0x18,
        state_disabled      = 0x19,
        state_holding       = 0x1A,
        state_deviceBusy    = 0x1B,
        state_rejecting     = 0x1C,
        state_casseteFull   = 0x41,      
        state_casseteOP     = 0x42,      
        state_jammedVLD     = 0x43,      
        state_jammedCass    = 0x44,     
        state_cheated       = 0x45,      
        state_pause         = 0x46,
        state_failure       = 0x47,      
        state_escrowPos     = 0x80,
        state_escrowStd     = 0x81,
        state_escrowRtn     = 0x82
    };


    public class ccNetPortWork
    {

        public string searchBV()
        {
            string s = string.Empty ;


            string[] ports = SerialPort.GetPortNames();

            foreach (string port in ports) 
            {
                try
                { 
                    SerialPort _tmpPort = new SerialPort(port,9600,Parity.None,8, StopBits.One);
                    _tmpPort.Open();
                    // find cash
                    _tmpPort.Close();
                }
                catch { continue; };
            
            }

            return s ;
        }


        public bool sendCmd(SerialPort sPort, byte[] data)
        {
            try
            {
                if (sPort.IsOpen) 
                {
                    sPort.Write(data, 0, data.Length);
                }
                else return false;
            }
            catch { return false; }

            return true;
        }


        public byte[] receiveCmd(SerialPort sPort)
        {

            try
            {
                if (sPort.IsOpen)
                {
                    int dataLenth = sPort.BytesToRead;
                    if (dataLenth == 0) return null ;
                    byte[] data = new byte[dataLenth];
                    sPort.Read(data,0,dataLenth);
                    return data ;
                };
            
            }
            catch {}

            return null ;
        }


    }



    internal class ccNet
    {

        ccNetLowLevel cLL = new ccNetLowLevel();

        public int glState = 0;
        public int glDrops = 0;
        


        public byte[] prepareCmd(byte cmd) // однобайтовые комманды
        {
            byte[] arr = new byte[1];
            arr[0] = cmd;
            return cLL.cmdPocketAsmbr(arr);
        }

        public byte[] prepareCmd(byte cmd, byte[] args) // с аргументом (Enable, Disable, Secure)
        {
            byte[] arr = new byte[args.Length+1];
            Array.ConstrainedCopy(args, 0, arr, 1, args.Length);
            arr[0] = cmd;
            return cLL.cmdPocketAsmbr(arr);
        }

        public ccResponse extractReceivedData(byte[] data) // Обдирает входящий пакет от сопроводиловки
        {
            ccResponse cRp = null;

            if (!cLL.isPocketOK(data)) return null;

            byte[] body = new byte[data[2] - 5];

            Array.ConstrainedCopy(body,0, data, 3, body.Length);

            if (body.Length == 1) cRp = new ccResponse(body[0]);
            else 
            {
                byte[] tmp = new byte[body.Length-1];
                Array.ConstrainedCopy(body,1, tmp,0, tmp.Length);
                cRp = new ccResponse(body[0], tmp);
            } 

            return cRp;
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




    public class ccNetLowLevel
    {
        const ushort POLYNOMIAL = 0x8408;
        public ushort GetCRC16(byte[] bufData, int sizeData)
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


        public byte[] cmdPocketAsmbr(byte[] cmd) // добавляет CRC16 в конец пакета для передачи, а также суффикс и длину
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

        public bool isPocketOK(byte[] cmd)
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