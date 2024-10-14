using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CC_NET_ESCROW_SAMPLE_ASYNC
{
    public partial class Form1 : Form
    {

        static ccNet            CCNet   = new ccNet();
        static ccNetPortWork    CCPort  = new ccNetPortWork();




        private SerialPort _serialPort;
        private CancellationTokenSource _cancellationTokenSource;

        public Form1()
        {
            InitializeComponent();
            _serialPort = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One);

            try
            {
                _serialPort.Open();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии COM-порта: {ex.Message}");
            }


        }


        enum ccNetCmdList // enum of all possible commands thru CCNet protocol
        {
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
            ccnetCmdStatic      = 0x60,
        };

        public void cmdSender(byte cmd, byte[] prm)
        {
            byte[] bt;

            if (cmd == (byte)ccNetCmdList.ccnetCmdEnBills)
            {
                bt = CCNet.prepareCmd(cmd, prm);
                CCPort.sendCmd(_serialPort, bt);
            }
            else
            {
                bt = CCNet.prepareCmd(cmd);
                CCPort.sendCmd(_serialPort, bt);
            }

            Thread.Sleep(200);
            byte[] tt = CCPort.receiveCmd(_serialPort);
            if (tt == null) return;
            string t = "";
            foreach (byte b in tt) { t += b.ToString("X2") + " "; }
            textBox1.Text = t;


        }

        private void button1_Click(object sender, EventArgs e)
        {
            //CCNet.prepareCmd((byte)0x34, new byte[] { 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00 });
            byte[] tt = CCNet.prepareCmd((byte)0x41); // reset
            CCPort.sendCmd(_serialPort,tt);
            Thread.Sleep(200);
            tt = CCPort.receiveCmd(_serialPort);

            string t = "";
            foreach (byte b in tt) { t += b.ToString("X2") +" "; }

            textBox1.Text = t;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (sender == button1)  cmdSender((byte)ccNetCmdList.ccnetCmdReset, null);
            if (sender == button2)  cmdSender((byte)ccNetCmdList.ccnetCmdPoll , null);
            if (sender == button3)  cmdSender((byte)ccNetCmdList.ccnetCmdID,    null);
            if (sender == button4)  cmdSender((byte)ccNetCmdList.ccnetCmdTable, null);
            if (sender == button5)  cmdSender((byte)ccNetCmdList.ccnetCmdReturn,null);
            if (sender == button6)  cmdSender((byte)ccNetCmdList.ccnetCmdStack, null);
            if (sender == button7)  cmdSender((byte)ccNetCmdList.ccnetCmdACK,   null);
            if (sender == button8)  cmdSender((byte)ccNetCmdList.ccnetCmdNACK,  null);

            if (sender == button9)  cmdSender((byte)ccNetCmdList.ccnetCmdEnBills, new byte[] { 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00 });
            if (sender == button10) cmdSender((byte)ccNetCmdList.ccnetCmdEnBills, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

        }
    }
}
