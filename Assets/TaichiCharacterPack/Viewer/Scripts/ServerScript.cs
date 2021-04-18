using UnityEngine;
using UnityEngine.Networking;
using System.Collections;

public class ServerScript : MonoBehaviour
{
    public string currentServerResponse;
    public int pingInterval = 5; // interval in seconds

    void Start()
    {
        print("running server upload coroutine");
        StartCoroutine(Upload());
    }

    IEnumerator Upload()
    {
        WWWForm form = new WWWForm();
        form.AddField("myField", "myData");

        while (true) { // repeatedly request from server
            using (UnityWebRequest www = UnityWebRequest.Post("http://127.0.0.1:5000/processImage", form))
            {
            
                yield return www.Send();

                if (www.isNetworkError)
                {
                    Debug.Log(www.error);
                }
                else
                {
                    currentServerResponse = www.downloadHandler.text;
                    print("POST successful: " + currentServerResponse);
                }
            }

            yield return new WaitForSeconds(pingInterval);
        }
    }
}

/*

yield return www.SendWebRequest();

if (www.result != UnityWebRequest.Result.Success)
{
    print(www.error);
}
else
{
    print("Form upload complete?");
    print(www.result);
}
*/
