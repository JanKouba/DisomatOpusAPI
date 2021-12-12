using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace DisomatOpusAPI
{
    public class MinProz
    {

        public event EventHandler ResponseReceived;         //Event handler for receiving response
        public event EventHandler WriteTimeOutOccured;      //Event handler for writing data error
        public event EventHandler ReadTimeOutOccured;       //Event handler for reading data error

        SerialPort serialPort = new SerialPort();           //SerialPort instance

        string receivedData = string.Empty;                 //Response string
        StringBuilder response = new StringBuilder();       

        const char CR = (char)13;                           //<CR> charactee

        private Status status;                              //Communication status -> see enum Status                      
        public Status Status { get => status; }

        /// <summary>
        /// Create new connection
        /// </summary>
        /// <param name="port">Port as DisomatOpusAPI.Port</param>
        /// <param name="baudRate">Baudrate in kbps as Int32</param>
        /// <param name="parity">Parity type as System.IO.Ports.Parity</param>
        /// <param name="stopBits">Stop Bits type as System.IO.Ports.StopBits</param>
        /// <param name="dataBits">Number of data bits as Int32</param>
        public MinProz(Port port, int baudRate, Parity parity, StopBits stopBits, int dataBits)
        {
            OpenPort(port.ToString(), baudRate, parity, stopBits, dataBits);
        }

        /// <summary>
        /// Create new connection
        /// </summary>
        /// <param name="port">Port as String</param>
        /// <param name="baudRate">Baudrate in kbps as Int32</param>
        /// <param name="parity">Parity type as System.IO.Ports.Parity</param>
        /// <param name="stopBits">Stop Bits type as System.IO.Ports.StopBits</param>
        /// <param name="dataBits">Number of data bits as Int32</param>
        public MinProz(string port, int baudRate, Parity parity, StopBits stopBits, int dataBits)
        {
            OpenPort(port, baudRate, parity, stopBits, dataBits);
        }

        public void OpenPort(string port, int baudRate, Parity parity, StopBits stopBits, int dataBits)
        {
            /*
             * Port settings must fits to device settings
             * The common one is 
             * 9600 9E1 (9600 kbps, EVEN parity, ONE stop bit, 8 data bits
             */
            if (!serialPort.IsOpen)
            {
                status = Status.NotReady;

                serialPort.PortName = port;
                serialPort.BaudRate = baudRate;
                serialPort.Parity = parity;
                serialPort.StopBits = stopBits;
                serialPort.DataBits = dataBits;
                serialPort.Handshake = Handshake.None;
                serialPort.Encoding = ASCIIEncoding.ASCII;
                serialPort.WriteTimeout = 300000;
                serialPort.ReadTimeout = 300000;

                serialPort.DataReceived += SerialPort_DataReceived;
                serialPort.Open();

                status = Status.Ready;
            }
            else
            {
                status = Status.NotReady;
                throw new Exception("Port " + port + " is not ready. Maybe is already OPEN");
            }
        }

        public void CreateRequest()
        {
            /*
             * This code implementing only one request type
             * for requesting current values
             * Any other could be added if needed
             */
            if (status != Status.NotReady || status != Status.Busy)
            {
                status = Status.Busy;
                RequestCurrenValue();
            }
        }

        private void RequestCurrenValue()
        {
            /*
             * Request current value
             * <PAYLOAD>
             * Command for return current value is 01#TG#
             * where 01 is device idetifier - should fits to device setting
             */
            SendCommand("01#TG#");
            response.Clear();
        }

        private void SendCommand(string command)
        {
            /*
             * Structure is
             * <PAYLOAD><CR>
             */
            try
            {
                serialPort.Write(command + CR);
                status = Status.Ready;
            }
            catch
            {
                WriteTimeOutOccured?.Invoke(this, EventArgs.Empty);
                status = Status.Error;
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            /*
             * Response structure is
             * 01#TG#netto#tara#dg/dt#status#<CR>
             * We must append all characters up to <CR>
             * When <CR> is reached ResponseReceived event is fired
             */
            try
            {
                receivedData = serialPort.ReadExisting();

                foreach (char c in receivedData)
                {
                    if (c == CR)
                        ResponseReceived?.Invoke(this, new SchenckEventArgs(response.ToString()));
                    else
                        response.Append(c);
                }

                status = Status.Ready;
            }
            catch
            {
                receivedData = string.Empty;
                ReadTimeOutOccured?.Invoke(this, EventArgs.Empty);
                status = Status.Error;
            }
        }

        public void ClosePort()
        {
            if (serialPort.IsOpen)
                serialPort.Close();
            else
            {
                status = Status.NotReady;
                throw new Exception("Port is already CLOSED");
            }
        }
    }

    public class SchenckEventArgs : EventArgs
    {
        public string Response { get; private set; }

        public SchenckEventArgs(string response)
        {
            Response = response;
        }
    }

    public enum Port
    {
        COM1,
        COM2,
        COM3,
        COM4,
        COM5,
        COM6,
        COM7,
        COM8,
        COM9
    };

    public enum Status
    {
        Busy = 1,
        Ready = 0,
        NotReady = -1,
        Error = -4
    }

}

