using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class ServerScript : MonoBehaviour
{
	// Link to the SceneScript to send animation commands to
    public SceneScript SceneScriptLink;

    private string currentServerResponse;
	private Thread receiveThread;
	private UdpClient client;
	private int port;

	// Expression map (#todo move this to a file)
    // Observing a certain expression (key), perform another expression (value)
	// Possible keys: ['Neutral', 'Happy', 'Surprise', 'Sad', 'Anger', 'Disgust', 'Fear', 'Contempt']
	// Possible values: ['neutral', 'distracted', ] #todo find more happy animations?! use face textures?
    Dictionary<string, string> etiquette = new Dictionary<string, string>() {
        {"Neutral", "neutral"},
        {"Happy", "neutral"},
        {"Surprise", "neutral"},
        {"Anger", "distracted"},
		{"Disgust", "distracted"},
		{"Fear", "distracted"},
		{"Contempt", "distracted"}
    };

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
			try // seems to only be called when socket receives message
			{
				IPEndPoint anyIP = new IPEndPoint(IPAddress.Parse("0.0.0.0"), port);
				byte[] data = client.Receive(ref anyIP);
				currentServerResponse = Encoding.UTF8.GetString(data);
				print (">> " + currentServerResponse + " >> " + etiquette[currentServerResponse]);
				SceneScriptLink.ScheduleNewMood(etiquette[currentServerResponse], "Socket emotion recognition");
			} catch(Exception e)
			{
				print(e.ToString());
			}
		}
	}
}