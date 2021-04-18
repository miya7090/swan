using UnityEngine;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class ServerScript : MonoBehaviour
{
    public string currentServerResponse;

	Thread receiveThread;
	UdpClient client;
	int port;

    void Start()
    {
        port = 5065;
        InitUDP();
    }

    private void InitUDP()
	{
		print ("UDP Initialized");
		receiveThread = new Thread(new ThreadStart(ReceiveData));
		receiveThread.IsBackground = true;
		receiveThread.Start();
	}

    private void ReceiveData()
	{
		client = new UdpClient(port);
		while (true)
		{
			try
			{
				IPEndPoint anyIP = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port);
				byte[] data = client.Receive(ref anyIP);
				currentServerResponse = Encoding.UTF8.GetString(data);
				print (">> " + currentServerResponse);
			} catch(Exception e)
			{
				print(e.ToString());
			}
		}
	}
}