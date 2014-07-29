// <author>Filip Ryborz</author>
// <date>07/21/2014</date>
// <summary>Program handling file operations when interfacing embedded system by serial port.</summary>
// <website>simply-embedded.blogspot.com</website>
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SerialToFS
{
    public partial class Form1 : Form
    {
        private Byte[] rxBuf;
        private Byte[] txBuf;
        private Byte[] fileBuf;
        private int fileIdx;
        private int fileSize;
        private System.IO.FileStream file;

        public Form1()
        {
            InitializeComponent();
            rxBuf = new Byte[1024];
            txBuf = new Byte[1024];
            fileBuf = new Byte[4096];
            file = null;
            fileIdx = 0;
            fileSize = 0;
            foreach (string s in System.IO.Ports.SerialPort.GetPortNames())
            {
                cbPort1.Items.Add(s);
            }
        }

        private void bConnect_Click(object sender, EventArgs e)
        {
            string sTmp = tbBaud.Text;
            try
            {
                sp1.PortName = cbPort1.Items[cbPort1.SelectedIndex].ToString();
                sp1.BaudRate = int.Parse(sTmp.Substring(0, sTmp.IndexOf(',')));
                sTmp = sTmp.Substring(sTmp.IndexOf(',') + 1);
                sp1.DataBits = int.Parse(sTmp.Substring(0, sTmp.IndexOf(',')));
                sTmp = sTmp.Substring(sTmp.IndexOf(',') + 1);
                switch (sTmp.Substring(0, sTmp.IndexOf(',')))
                {
                    case "N":
                    case "n":
                        sp1.Parity = System.IO.Ports.Parity.None;
                        break;
                    case "E":
                    case "e":
                        sp1.Parity = System.IO.Ports.Parity.Even;
                        break;
                    case "O":
                    case "o":
                        sp1.Parity = System.IO.Ports.Parity.Odd;
                        break;
                    case "M":
                    case "m":
                        sp1.Parity = System.IO.Ports.Parity.Mark;
                        break;
                    case "S":
                    case "s":
                        sp1.Parity = System.IO.Ports.Parity.Space;
                        break;
                    default:
                        sp1.Parity = System.IO.Ports.Parity.None;
                        break;
                }
                sTmp = sTmp.Substring(sTmp.IndexOf(',') + 1);
                switch (sTmp)
                {
                    case "0":
                        sp1.StopBits = System.IO.Ports.StopBits.None;
                        break;
                    case "1":
                        sp1.StopBits = System.IO.Ports.StopBits.One;
                        break;
                    case "1.5":
                    case "1,5":
                        sp1.StopBits = System.IO.Ports.StopBits.OnePointFive;
                        break;
                    case "2":
                        sp1.StopBits = System.IO.Ports.StopBits.Two;
                        break;
                    default:
                        sp1.StopBits = System.IO.Ports.StopBits.One;
                        break;
                }
                sp1.Open();
                cbPort1.Enabled = false;
                tbBaud.Enabled = false;
                bConnect.Enabled = false;
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                sp1.Close();
                file.Close();
            }
            catch (Exception ex) { ex.ToString(); }
        }

        private void sp1_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            int rxLen = sp1.BytesToRead;
            int tmp = 0;


            if (rxLen > 0)
            {
                if (fileSize > 0)
                {

                    if (rxLen > fileSize)
                    {
                        tmp = rxLen;
                        rxLen = fileSize;
                    }
                    sp1.Read(fileBuf, fileIdx, rxLen);
                    fileIdx += rxLen;
                    fileSize -= rxLen;
                    rxLen = tmp;
                }
                if (fileSize == 0)
                {
                    //rxLen = sp1.BytesToRead; 
                    sp1.Read(rxBuf, 0, rxLen);
                    rtb1.Invoke(new EventHandler(delegate { rtb1.AppendText(System.Text.Encoding.ASCII.GetString(rxBuf, 0, rxLen)); }));
                }

            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            String sFile = "";
            String sTmp = "";
            int size = 0;
            int readSize = 0;

            if (rtb1.Lines.Length > 0)
            {
                sTmp = rtb1.Lines[rtb1.Lines.Length - 1];
                // open file
                if (sTmp.IndexOf("fOpen(") > -1)
                {
                    sFile = sTmp.Substring(sTmp.IndexOf('(') + 1, sTmp.IndexOf(',') - sTmp.IndexOf('(') - 1);
                    try
                    {
                        file = System.IO.File.Open(sFile, System.IO.FileMode.OpenOrCreate);
                    }
                    catch (Exception ex) { ex.ToString(); }
                    if (file != null)
                    {
                        txBuf[0] = 0;
                        txBuf[1] = 0;
                        sp1.Write(txBuf, 0, 2);
                        rtb1.AppendText("\n\rFile opened!\n\r");
                    }
                    else
                    {
                        txBuf[0] = 0xFF;
                        txBuf[1] = 0xFF;
                        sp1.Write(txBuf, 0, 2);
                        rtb1.AppendText("\n\rFile not opened!\n\r");
                    }
                }
                // read file
                if ((sTmp.IndexOf("fRead") > -1) && (file != null))
                {
                    readSize = int.Parse(sTmp.Substring(sTmp.IndexOf(", ") + 1, sTmp.IndexOf('|') - sTmp.IndexOf(',') - 1));
                    size = (int)file.Length;
                    txBuf[0] = (Byte)((size & 0xFF00) >> 8);
                    txBuf[1] = (Byte)(size & 0x00FF);
                    sp1.Write(txBuf, 0, 2);
                    rtb1.AppendText("\n\rReading file!\n\r");
                    if (readSize < size)
                    {
                        size = readSize;
                    }
                    file.Read(fileBuf, 0, size);
                    sp1.Write(fileBuf, 0, size);
                }
                // write file
                if ((sTmp.IndexOf("fWrite") > -1) && (file != null))
                {
                    fileSize = int.Parse(sTmp.Substring(sTmp.IndexOf(", ") + 1, sTmp.IndexOf('|') - sTmp.IndexOf(',') - 1));
                    sp1.Write(txBuf, 0, 1); // send ACK
                    rtb1.AppendText("\n\rWriting file!\n\r");
                }
                // close file
                if ((sTmp.IndexOf("fClose") > -1) && (file != null))
                {
                    file.Position = 0;
                    file.Write(fileBuf, 0, fileIdx);
                    fileIdx = 0;

                    file.Close();
                    sp1.Write(txBuf, 0, 1); // send ACK
                    rtb1.AppendText("\n\rFile closed!\n\r");
                }
            }

        }

        private void bGetCoverage_Click(object sender, EventArgs e)
        {
            if (tbTrigger.Text.Length > 0)
            {
                txBuf[0] = (byte)tbTrigger.Text[0];
                sp1.Write(txBuf, 0, 1); // send ACK
            }
        }
    }
}
