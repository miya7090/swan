using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using System.Collections.Generic;

/// code source: https://github.com/LightBuzz/Speech-Recognition-Unity/blob/master/SpeechRecognitionUnity/Assets/Open%20Dictation%20Mode/DictationEngine.cs
/// https://www.w3.org/TR/speech-grammar/

public class VoiceScript : MonoBehaviour
{
    // Link to the SceneScript to send animation commands to
    public SceneScript SceneScriptLink;
    protected DictationRecognizer dictationRecognizer;

    // #todo replace with sentiment analysis
    private List<string> positiveWords = new List<string>(){"good", "great", "easy", "nice", "happy", "interesting", "cool", "fun"};
    private List<string> negativeWords = new List<string>(){"sad", "stress", "stressing", "stresses", "stressful", "frustrating", "stuck", "broken", "angry", "hard", "difficult", "confusing"};

    public string CurrentText;
    public bool isUserSpeaking;

    void Start()
    {
        StartDictationEngine();
    }

    private void DictationRecognizer_OnDictationHypothesis(string text)
    {
        //Debug.LogFormat("Dictation hypothesis: {0}", text);
        CurrentText = text;
        if (isUserSpeaking == false) { isUserSpeaking = true; }

        List<string> wordsSpoken = new List<string>(CurrentText.Split(' '));

        // first check for mute keywords
        if (wordsSpoken.Contains("swan") || wordsSpoken.Contains("hi") || wordsSpoken.Contains("hey")) {
            SceneScriptLink.UnmuteSwan();
            CurrentText = "";
        }
        else if (wordsSpoken.Contains("thanks") || wordsSpoken.Contains("thank")) {
            SceneScriptLink.MuteSwan();
            CurrentText = "";
        }

        foreach (string wordSpoke in wordsSpoken)
        {
            if (positiveWords.Contains(wordSpoke))
            {
                print("positive word detected! " + wordSpoke);
                SceneScriptLink.ScheduleNewReaction("nod", "Voice recognition - positive");
                CurrentText = "";
            }
            else if (negativeWords.Contains(wordSpoke))
            {
                print("negative word detected! " + wordSpoke);
                SceneScriptLink.ScheduleNewReaction("shake", "Voice recognition - negative");
                CurrentText = "";
            }
        }
    }

    // no need to change functions below this line ------------------------------------------------------------------------

    /// thrown when engine has some messages, that are not specifically errors
    private void DictationRecognizer_OnDictationComplete(DictationCompletionCause completionCause)
    {
        if (completionCause != DictationCompletionCause.Complete)
        {
            Debug.LogWarningFormat("Dictation completed unsuccessfully: {0}.", completionCause);

            switch (completionCause)
            {
                case DictationCompletionCause.TimeoutExceeded:
                case DictationCompletionCause.PauseLimitExceeded:
                    //we need a restart
                    CloseDictationEngine();
                    StartDictationEngine();
                    break;

                case DictationCompletionCause.UnknownError:
                case DictationCompletionCause.AudioQualityFailure:
                case DictationCompletionCause.MicrophoneUnavailable:
                case DictationCompletionCause.NetworkFailure:
                    CloseDictationEngine();
                    break;
                case DictationCompletionCause.Canceled: //happens when focus moved to another application
                case DictationCompletionCause.Complete:
                    CloseDictationEngine();
                    StartDictationEngine();
                    break;
            }
        }
    }

    /// Resulted complete phrase will be determined once the person stops speaking. the best guess from the PC will go on the result.
    private void DictationRecognizer_OnDictationResult(string text, ConfidenceLevel confidence)
    {
        /* // Don't waste processing on this function, process live only, don't wait until stop speaking (#todo can this function be removed entirely) 
        Debug.LogFormat("Dictation result: {0}", text);
        if (ResultedText) ResultedText.text += text + "\n";
        */
        if (isUserSpeaking == true)
        {
            isUserSpeaking = false;
            CurrentText = ""; // clear text since done
            //OnPhraseRecognized.Invoke(text);
        }
    }

    private void DictationRecognizer_OnDictationError(string error, int hresult)
    {
        Debug.LogErrorFormat("Dictation error: {0}; HResult = {1}.", error, hresult);
    }

    private void OnApplicationQuit()
    {
        CloseDictationEngine();
    }

    private void StartDictationEngine()
    {
        isUserSpeaking = false;

        dictationRecognizer = new DictationRecognizer();

        dictationRecognizer.DictationHypothesis += DictationRecognizer_OnDictationHypothesis;
        dictationRecognizer.DictationResult += DictationRecognizer_OnDictationResult;
        dictationRecognizer.DictationComplete += DictationRecognizer_OnDictationComplete;
        dictationRecognizer.DictationError += DictationRecognizer_OnDictationError;

        dictationRecognizer.Start();
    }

    private void CloseDictationEngine()
    {
        if (dictationRecognizer != null)
        {
            dictationRecognizer.DictationHypothesis -= DictationRecognizer_OnDictationHypothesis;
            dictationRecognizer.DictationComplete -= DictationRecognizer_OnDictationComplete;
            dictationRecognizer.DictationResult -= DictationRecognizer_OnDictationResult;
            dictationRecognizer.DictationError -= DictationRecognizer_OnDictationError;

            if (dictationRecognizer.Status == SpeechSystemStatus.Running)
                dictationRecognizer.Stop();
            
            dictationRecognizer.Dispose();
        }
    }
}