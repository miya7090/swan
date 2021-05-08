using UnityEngine;
using System.Collections;
using System.Threading;
using UnityEngine.UI;
using UnitySentiment;

public class SendTextToAnalyse : MonoBehaviour {

	// Link to the SceneScript to send animation commands to
    public SceneScript SceneScriptLink;
	public SentimentAnalysis predictionObject;
	VoiceScript myVoiceScript;

	private bool responseFromThread = false;
	private bool threadStarted = false;
	public Vector3 SentimentAnalysisResponse;

	public float SENTIMENT_ANALYSIS_FREQUENCY = 1.5f; // in seconds

	void Start() 
	{
		Application.runInBackground = true;
		predictionObject.Initialize();
		SentimentAnalysis.OnAnlysisFinished += GetAnalysisFromThread;
		SentimentAnalysis.OnErrorOccurs += Errors;
		myVoiceScript = GetComponent<VoiceScript>();

		UpdateLoop();
	}

	void UpdateLoop()
	{
		string sendText = myVoiceScript.CurrentText;
		if (string.IsNullOrEmpty(sendText)) {
			sendText = "null";
		}
		print("sending "+sendText+" to sentiment prediction object...");
		predictionObject.PredictSentimentText(myVoiceScript.CurrentText);
		StartCoroutine(WaitResponseFromThread());
	}

	// Sentiment Analysis Thread
	private void GetAnalysisFromThread(Vector3 analysisResult)
	{		
		SentimentAnalysisResponse = analysisResult;
		responseFromThread = true;
	}

	private IEnumerator WaitResponseFromThread()
	{
		while(!responseFromThread) // Waiting For the response
		{
			yield return null;
		}
		// Main Thread Action
		PrintAnalysis();
		ActUponAnalysis();
		yield return new WaitForSeconds(SENTIMENT_ANALYSIS_FREQUENCY);

		// Reset
		responseFromThread = false;
		threadStarted = false;
		UpdateLoop();
	}

	private void ActUponAnalysis()
	{
		if (SentimentAnalysisResponse.x > 0.7)
		{
			print("positive sentiment detected! " + SentimentAnalysisResponse.x.ToString());
			SceneScriptLink.ScheduleNewReaction("nod", "Voice sentiment - positive");
		}
		else if (SentimentAnalysisResponse.y > 0.7)
		{
			print("negative sentiment detected! " + SentimentAnalysisResponse.y.ToString());
			SceneScriptLink.ScheduleNewReaction("shake", "Voice sentiment - negative");
		}
	}

	private void PrintAnalysis()
	{
		string sentAnalysisTxt = "";
		sentAnalysisTxt += SentimentAnalysisResponse.x + " % : Positive"; 
		sentAnalysisTxt += SentimentAnalysisResponse.y + " % : Negative";
		sentAnalysisTxt += SentimentAnalysisResponse.z + " % : Neutral";
		
		print(sentAnalysisTxt);
	}

	// Sentiment Analysis Thread
	private void Errors(int errorCode, string errorMessage)
	{
		Debug.Log(errorMessage + "\nCode: " + errorCode);
	}
}