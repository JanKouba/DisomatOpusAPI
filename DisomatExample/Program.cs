using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DisomatOpusAPI;


namespace DisomatExample
{
    class Program
    {
        static void Main(string[] args)
        {
            /* Create connection to Disomat Opus device via RS232
             * using parameters according to the device settings
             */
            MinProz device = new MinProz(Port.COM1, 9600, System.IO.Ports.Parity.Even, System.IO.Ports.StopBits.One, 8);
            
            device.ResponseReceived += Device_ResponseReceived; //Add event handling for response receive

            while (true) //Repeat request every 1s
            {
                device.CreateRequest();
                Thread.Sleep(1000);
            }    
        }

        private static void Device_ResponseReceived(object sender, EventArgs e)
        {
            /*
             * Expected response structure:
             * 01#TG#netto#tara#dg/dt#status#
             * 
             * Error message structure:
             * 99#
             * 
             * This code target is to show NETTO and TARE in kilograms
             * 
             * Because device returns values in Tons (two decimal precision)
             * is needed to multiply by 1000 to get kgs
             */

            SchenckEventArgs schenckEventArgs = (SchenckEventArgs)e;

            try
            {
                string response = schenckEventArgs.Response;

                if (response != "99#")
                {
                    string valueNetto = schenckEventArgs.Response.Split('#')[2];
                    string valueTara = schenckEventArgs.Response.Split('#')[3];

                    double currentNetto = Double.Parse(valueNetto) * 1000;
                    double currentTara = Double.Parse(valueTara) * 1000;

                    Console.WriteLine("Netto:{0}kg\nTare:{1}kg", currentNetto, currentTara);
                }
                else
                    Console.WriteLine("Device can not undestand the request");
            }
            catch { Console.WriteLine("Unexpected error"); }
        }
    }
}
