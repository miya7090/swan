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
	// Possible values: ['greeting', 'negative', 'hesitant', 'neutral'] #todo find more happy animations?!
    Dictionary<string, string> etiquette = new Dictionary<string, string>() {
        {"Neutral", "neutral"},
        {"Happy", "neutral"},
        {"Surprise", "neutral"},
        {"Anger", "hesitant"},
		{"Disgust", "negative"},
		{"Fear", "hesitant"},
		{"Contempt", "negative"}
    };

	// for more easily testable results
	Dictionary<string, string> testEtiquette = new Dictionary<string, string>() {
        {"Neutral", "neutral"},
        {"Happy", "negative"},
        {"Surprise", "hesitant"},
        {"Anger", "neutral"},
		{"Disgust", "neutral"},
		{"Fear", "neutral"},
		{"Contempt", "neutral"}
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
				print (">> " + currentServerResponse + " >> " + testEtiquette[currentServerResponse]);
				SceneScriptLink.ScheduleNewMood(testEtiquette[currentServerResponse], "Socket emotion recognition");
			} catch(Exception e)
			{
				print(e.ToString());
			}
		}
	}
}