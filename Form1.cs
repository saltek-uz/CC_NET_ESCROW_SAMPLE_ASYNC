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

        private byte[] pendingCmd = null; 


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
        }

        public void onDataReceive(byte[] data)
        {
           MessageBox.Show("Taska - callBack!");  

        }

        public void onError(byte[] data)
        {
            MessageBox.Show("Taska - callBack!");
        }

        public string onCmdDone()
        {
            MessageBox.Show("Taska - callBack!");

            return string.Empty;
        }



        public void ccNetDialogue(SerialPort spt)
        {
            while (true)
            {
                if (pendingCmd is null)
                {
                    try
                    {

                        byte[] pollCmd = ccNet.prepareCmd(cmdList.ccnetCmdPoll);
                        ccNet.sendCmd(_serialPort, pollCmd);
                        Thread.Sleep(100);
                        byte[] answr = ccNet.receiveCmd(_serialPort);
                        if (answr is null) throw (new Exception());
                        ccResponse extrData = ccNet.extractReceivedData(answr);
                        if (extrData is null) throw (new Exception());

                        vldState.isError = false;
                        vldState.lagCounter = 0;
                        vldState.lastState = extrData.cmd;
                        if (extrData.arg is null) vldState.lastParam = 0x00; else vldState.lastParam = extrData.arg[0];
                    }
                    catch
                    {
                        vldState.isError = true;
                        vldState.lagCounter++;
                    };

                    string info = $" POLL RESULTS: \r\n \r\n isError : {vldState.isError.ToString()} \r\n" +
                        $"Lags : {vldState.lagCounter.ToString()} \r\n" +
                        $"Last known state : 0x{vldState.lastState.ToString("X2")} \r\n" +
                        $"Last param : 0x{vldState.lastParam.ToString("X2")} \r\n";
                    
                    textBox1.Invoke(new Action( () => { textBox1.Text = info; } ) );
                    
                    Thread.Sleep(200);

                }
                else
                {
                    string info;
                    try
                    {
                        ccNet.sendCmd(_serialPort, pendingCmd);
                        Thread.Sleep(200);
                        byte[] answr = ccNet.receiveCmd(_serialPort);
                        if (answr is null) throw (new Exception());
                        ccResponse extrData = ccNet.extractReceivedData(answr);
                        if (extrData is null) throw (new Exception());

                        vldState.isError = false;
                        vldState.lagCounter = 0;
                        vldState.lastState = extrData.cmd;
                        if (extrData.arg is null) vldState.lastParam = 0x00; else vldState.lastParam = extrData.arg[0];
                        info = " CMD RESULTS: \r\n \r\n";
                        foreach (byte b in answr) info += b.ToString("X2") + " ";
                    }
                    catch
                    {
                        info = "Error processing cmd ";
                    };
                    
                    textBox2.Invoke(new Action( () => { textBox2.Text = info; } ) );
                    pendingCmd = null;

                    Thread.Sleep(200);                    

                };


            }
        }



        public void cmdSender(byte cmd, byte[] prm)
        {
            byte[] bt;

            if (cmd == cmdList.ccnetCmdEnBills)
            {
                bt = ccNet.prepareCmd(cmd, prm);
            }
            else
            {
                bt = ccNet.prepareCmd(cmd);
            }

            pendingCmd = bt;

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

            if (sender == button3)  cmdSender(cmdList.ccnetCmdID,    null);
            if (sender == button4)  cmdSender(cmdList.ccnetCmdTable, null);
            if (sender == button5)  cmdSender(cmdList.ccnetCmdReturn,null);
            if (sender == button6)  cmdSender(cmdList.ccnetCmdStack, null);

            //if (sender == button7)  cmdSender(cmdList.ccnetCmdACK,   null);
            //if (sender == button8)  cmdSender(cmdList.ccnetCmdNACK,  null);

            if (sender == button9)  cmdSender(cmdList.ccnetCmdEnBills, new byte[] { 0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00 });
            if (sender == button10) cmdSender(cmdList.ccnetCmdEnBills, new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

        }

        private void button11_Click(object sender, EventArgs e)
        {
            string s = portFinder();

            if (s != String.Empty)
            {
                textBox1.Text = $" PORT : {s} - found validator";
                _serialPort.PortName = s;
                _serialPort.Open();
                Task.Factory.StartNew(() => ccNetDialogue(_serialPort));
            }
            else textBox1.Text = "Not found validator";
        }


    }
    public static class vldState
    {
        public static byte lastState = 0;
        public static byte lastParam = 0;
        public static bool isError = true;
        public static int lagCounter = 0;

        public const byte
        state_powerUp = 0x10,
        state_powerUpBV = 0x11,
        state_powerUpBS = 0x12,
        state_initialize = 0x13,
        state_idling = 0x14,
        state_accepting = 0x15,
        state_stacking = 0x17,
        state_returning = 0x18,
        state_disabled = 0x19,
        state_holding = 0x1A,
        state_deviceBusy = 0x1B,
        state_rejecting = 0x1C,
        state_casseteFull = 0x41,
        state_casseteOP = 0x42,
        state_jammedVLD = 0x43,
        state_jammedCass = 0x44,
        state_cheated = 0x45,
        state_pause = 0x46,
        state_failure = 0x47,
        state_escrowPos = 0x80,
        state_escrowStd = 0x81,
        state_escrowRtn = 0x82;


    }

}
