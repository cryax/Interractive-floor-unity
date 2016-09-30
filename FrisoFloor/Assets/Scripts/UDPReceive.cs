using UnityEngine;
using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

public enum MessageFromClient
{
    score,
    gift,
    state
}
public class UDPReceive : SingletonMono<UDPReceive>
{
    private string IP = "127.0.0.1";  
    private Thread receiveThread;
    private UdpClient client;
    private int port = 11000;
    public int x, y, z;
    public void Start()
    {
        init();
    }
    private void init()
    {

        receiveThread = new Thread(new ThreadStart(ReceiveData));
        receiveThread.IsBackground = true;
        receiveThread.Start();
        Debug.Log("Init recieve");
    }

    private void ReceiveData()
    {
        client = new UdpClient(port);
        while (true)
        {
            try
            {
                IPEndPoint anyIP = new IPEndPoint(IPAddress.Parse(IP), port);
                byte[] data = client.Receive(ref anyIP);
                string message = Encoding.UTF8.GetString(data);
                int.TryParse(message.Split(',')[0], out x);
                int.TryParse(message.Split(',')[1], out y);
                int.TryParse(message.Split(',')[2], out z);
                //Debug.LogFormat("position: {0}: {1}: {2}", x, y, z);
            }
            catch (Exception err)
            {
                print(err.ToString());
            }
        }
    }

    void OnDisable()
    {
        if (receiveThread != null)
            receiveThread.Abort();
        if (client != null) client.Close();
    }
}
