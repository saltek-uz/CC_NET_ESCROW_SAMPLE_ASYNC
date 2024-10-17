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

        private SerialPort _serialPort;
        private CancellationTokenSource _cancellationTokenSource;




            static string portName = " ";


            public static string portFinder()
            {
                string[] ports = SerialPort.GetPortNames();

                byte[] pollCmd = ccNet.prepareCmd(cmdList.ccnetCmdPoll);

                foreach (string port in ports)
                {
                    try
                    {
                        SerialPort sp = new SerialPort(port, 9600, Parity.None, 8, StopBits.One);
                        sp.Open();
                        Thread.Sleep(50);
                        ccNet.sendCmd(sp, pollCmd);
                        Thread.Sleep(200);
                        byte[] answr = ccNet.receiveCmd(sp);
                        sp.Close();
                        if ( answr is null ) continue;
                        if (ccNet.extractReceivedData(answr) is null) continue;
                        return port; // купюрник ответил правильным пакетом

                    }
                    catch { continue; };

                }

                return string.Empty;
            }






        public Form1()
        {
            InitializeComponent();
            _serialPort = new SerialPort("COM1", 9600, Parity.None, 8, StopBits.One);
          //  Task.Factory.StartNew( () => ccNetDialogue(_serialPort) );

        }

        public void onDataReceive(byte[] data)
        {
           MessageBox.Show("Taska - callBack!");  

        }

        public void onError(byte[] data)
        {
            MessageBox.Show("Taska - callBack!");
        }


        

        public void ccNetDialogue(SerialPort spt)
        {
            //try
            //{
            //    _serialPort.Open();
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show($"Ошибка при открытии COM-порта: {ex.Message}");
            //}



            while (true) 
            {
                Thread.Sleep(1000);
                onDataReceive(null);
               
                
            }
        
        }




        public void cmdSender(byte cmd, byte[] prm)
        {
            byte[] bt;

            if (cmd == cmdList.ccnetCmdEnBills)
            {
                bt = ccNet.prepareCmd(cmd, prm);
                ccNet.sendCmd(_serialPort, bt);
            }
            else
            {
                bt = ccNet.prepareCmd(cmd);
                ccNet.sendCmd(_serialPort, bt);
            }

            Thread.Sleep(200);
            byte[] tt = ccNet.receiveCmd(_serialPort);
            if (tt == null) return;
            string t = "";
            foreach (byte b in tt) { t += b.ToString("X2") + " "; }
            textBox1.Text = t;


        }

        private void button1_Click(object sender, EventArgs e)
        {
            byte[] tt = ccNet.prepareCmd((byte)0x41); // reset
            ccNet.sendCmd(_serialPort,tt);
            Thread.Sleep(200);
            tt = ccNet.receiveCmd(_serialPort);

            string t = "";
            foreach (byte b in tt) { t += b.ToString("X2") +" "; }

            textBox1.Text = t;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            if (sender == button1)  cmdSender(cmdList.ccnetCmdReset, null);
            if (sender == button2)  cmdSender(cmdList.ccnetCmdPoll , null);
            if (sender == button3)  cmdSender(cmdList.ccnetCmdID,    null);
            if (sender == button4)  cmdSender(cmdList.ccnetCmdTable, null);
            if (sender == button5)  cmdSender(cmdList.ccnetCmdReturn,null);
            if (sender == button6)  cmdSender(cmdList.ccnetCmdStack, null);
            if (sender == button7)  cmdSender(cmdList.ccnetCmdACK,   null);
            if (sender == button8)  cmdSender(cmdList.ccnetCmdNACK,  null);

            if (sender == button9)  cmdSender(cmdList.ccnetCmdEnBills, new byte[] { 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00 });
            if (sender == button10) cmdSender(cmdList.ccnetCmdEnBills, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

        }

        private void button11_Click(object sender, EventArgs e)
        {
            string s = portFinder();

            if (s != String.Empty) textBox1.Text = $" PORT : {s} - found validator";
            else textBox1.Text = "Not found validator";
        }
    }
}
