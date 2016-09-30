using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    class UdpSender
    {

        public Action<String> ServerStatus;
        int port = 11000;
        String host = "127.0.0.1";


        //x,y,1

        public void SenMessage(String mess)
        {
            //  IPEndPoint ep = new IPEndPoint(IPAddress.Parse(host), port);
            
            Task.Run(() =>
            {
                sender = CreateUdp(host, port);
                try
                {
                    sender.Connect(host, port);
                    Byte[] senddata = Encoding.ASCII.GetBytes(mess);
                    sender.Send(senddata, senddata.Length);
                    
                    /*  var receivedData = sender.Receive(ref ep);
                String valueRes = Encoding.ASCII.GetString(receivedData, 0, receivedData.Length);
                status.Text = valueRes;*/

                    //Console.WriteLine("This message was sent " + mess + "");

                    OnStatus("Connected");
                }
                catch (Exception ex)
                {
                    OnStatus("[Exception]" + ex.Message);
                }
            });
        }

        private void test()
        {
            Console.WriteLine("Receiver");
            // This constructor arbitrarily assigns the local port number.
            UdpClient udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, port));
            try
            {
                //IPEndPoint object will allow us to read datagrams sent from any source.
                IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);

                string message = String.Empty;
                do
                {

                    // Blocks until a message returns on this socket from a remote host.
                    Byte[] receiveBytes = udpClient.Receive(ref RemoteIpEndPoint);
                    message = Encoding.ASCII.GetString(receiveBytes);

                    // Uses the IPEndPoint object to determine which of these two hosts responded.
                    Debug.WriteLine("This is the message you received: " +
                                    message);
                    //Console.WriteLine("This message was sent from " +
                    //                            RemoteIpEndPoint.Address.ToString() +
                    //                            " on their port number " +
                    //                            RemoteIpEndPoint.Port.ToString());
                } while (message != "exit");
                udpClient.Close();
                //udpClientB.Close();

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Debug.WriteLine("Press Any Key to Continue");
        }

        private UdpClient CreateUdp(String ahost, int aport)
        {
            return new UdpClient(ahost, aport);



        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            Thread.Sleep(1000);
            for (int i = 0; i < 10000; i++)
            {
                //  Thread.Sleep(50);
                Thread.Sleep(50);
                SenMessage(new Random().Next(900000).ToString());

            }

            // test();
        }

        private void OnStatus(String messErr)
        {
            if (ServerStatus != null)
            {
                ServerStatus(messErr);
            }
        }

        private UdpClient sender;
        private void Form1_Load(object se, EventArgs e)
        {

        }
    }
    
}
