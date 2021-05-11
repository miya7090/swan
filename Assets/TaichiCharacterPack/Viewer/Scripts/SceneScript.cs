using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using UnityEngine;

public class SceneScript : MonoBehaviour
{
    // Public information
    public VoiceScript VoiceScriptLink; // only need the variable indicating whether avatar currently speaking
    public GameObject animTest;
    public GUISkin guiSkin;
    public AudioSource avatarAudioSource;
    
    // Path names (no need to change)
    private const string FBXListFile = "fbx_list";
    private const string AnimationListFile = "animation_list";
    private const string viewerResourcesPath = "Taichi";
    private const string viewerSettingPath = viewerResourcesPath + "/Viewer Settings";
    private const string viewerMaterialPath = viewerResourcesPath + "/Viewer Materials";
    private const string texturePath = viewerResourcesPath + "/Textures";
    // Miscellaneous settings (no need to change)
    private const int curLOD = 1; // display resolution. already set to highest; avoid changing this
    private const string lodType = "_h"; // also display resolution
    private const float animSpeed = 1; // animation speed. also avoid changing this
    private const string boneName = "Hips";
    // File names (automatically updated, no need to manually change)
    private string curModelName = "";
    private string curAnimName = "";
    // Entity names (automatically loaded from paths, no need to manually change)
    private string[] animationList;
    private string[] modelList;
    // Not sure what these are for but they seem important (no need to change)
    private TextAsset txt;
    private GameObject obj;
    private GameObject loaded;
    private SkinnedMeshRenderer SM;
    private XmlDocument xDoc;

    // Animation-controlling variables (important!)
    // Refer to "animation picking diagram.jpg" for more details
    private int curAnim; // the current animation to be shown
    private int lastAnim;
    private int curModel; // the avatar skin

    private string scheduledMood_category; // *externally modifiable thru helper functions
    private string scheduledReaction_category; // *externally modifiable thru helper functions
    
    private const int idleAnimationLength = 2; // length per each animation episode before refresh
    // ^ #todo SPLIT THIS SEPARATELY BASED ON REACTION AND MOOD ANIMATION LENGTHS WHICH ARE DIFFERENT (1 VERSUS 2)?
    private const int moodCooldown = 4; // cooldowns are in number of episodes
    private const int reactionCooldown = 4;
    private const int reactionInitiation = 6;
    private int moodCooldownTimer;
    private int reactionCooldownTimer = 0;
    private int reactionInitiationTimer;

    // Codes for sound files to play
    Dictionary<string, AudioClip[]> voiceCodes = new Dictionary<string, AudioClip[]>();
    
    // Animation codes (#todo move this to a file)
    // These classify animations into categories
    Dictionary<string, int[]> moodCodes = new Dictionary<string, int[]>() {
        {"neutral", new int[] {30, 50}},
        {"distracted", new int[] {0, 42, 43, 49}},
    };

    /* moods [[[when input into dictionary, subtract one for zero offset]]]
    31: pose_03: 14 seconds, listening with crossed arms
    51: idle_11: totally neutral, staring straight

    1: idle_00: 14 seconds, neutral with scratching neck, staring into distance
    43: crossarms_00: 14 seconds, looking around/down with crossed arms
    44: thinking_00: 10 seconds, arms crossed alternate looking down/up
    50: idle_10: 12 seconds, looking around distractedly */

    Dictionary<string, int[]> reactionCodes = new Dictionary<string, int[]>() {
        {"greeting", new int[] {6}},
        {"nod", new int[] {20, 21}}, // <= generated reactions currently default to "nod" category
        {"shake", new int[] {22, 23}},
    };

    /* reactions [[[when input into dictionary, subtract one for zero offset]]]
    7: greet_01: 2.5 seconds, wave
    21: nod_00: 1 second, nods once
    22: nod_01: 2 seconds, nod and casual fist pump
    23: refuse_00: 1.5 seconds, head shake
    24: refuse_01: 2 seconds, head shake and cross arms */

    // maps animation codes to how long to wait before producing sound
    Dictionary<int, float> soundDelay = new Dictionary<int, float>() {
        {30, 7}, {50, 9999},
        {0, 1}, {42, 1}, {43, 1}, {49, 1},
        {6, 0.5f},
        {20, 0}, {21, 0},
        {22, 0}, {23, 0}
    };
    
    /* other interesting
    10: embar_00: 8 seconds, look down scratch head
    11: embar_01: 5 seconds, scratch head
    2: idle_01: 2 seconds, neutral with blinking (same as idle_11?)
    78: kick_24: 1 second, high kick to face
    27: wink_00: 1.5 seconds, winks once */
    
    void Start()
    {
        avatarAudioSource = GetComponent<AudioSource>();

        txt = Resources.Load<TextAsset>(viewerSettingPath + "/" + FBXListFile);
        modelList = txt.text.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        txt = Resources.Load<TextAsset>(viewerSettingPath + "/" + AnimationListFile);
        animationList = txt.text.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        print("Found animations:"); // #todo convert these to Debug.Log commands
        print(String.Join(" ", animationList));
        
        txt = Resources.Load<TextAsset>(viewerSettingPath + "/fbx_ctrl");
        xDoc = new XmlDocument();
        xDoc.LoadXml(txt.text);

        // load audio clips for taichi to speak
        foreach (string audioFolder in new List<string>(){"greeting", "positive", "confident-ahh", "disappoint-ahh", "thinking-hesitant"})
        {
            List<AudioClip> my_Clips_list = new List<AudioClip>();
            foreach (UnityEngine.Object myClip_obj in Resources.LoadAll("simlish_audio/"+audioFolder))
            {
                my_Clips_list.Add((AudioClip)myClip_obj);
            }
            voiceCodes.Add(audioFolder, my_Clips_list.ToArray());
        }

        print("voice codes initialization complete: " + voiceCodes.ToString());

        ModelChange(modelList[curModel] + lodType); // initialize main model

        curAnim = 0; // will later be updated to "greeting" through cooldown
        lastAnim = 0;
        curModel = 2;
        scheduledMood_category = null;
        scheduledReaction_category = "greeting";
        moodCooldownTimer = 0;
        reactionCooldownTimer = reactionCooldown; // force greeting reaction to be animated
        reactionInitiationTimer = 0;
        Invoke("UpdateLoop", idleAnimationLength);
    }

    // MOST IMPORTANT FUNCTIONS ----------------------------------------------------------------------

    // Refer to "animation picking diagram.jpg" for more details
    // CallerID is a string specifying where this function was called from for readability
    public void ScheduleNewMood(string animationType, string callerID)
    {
        scheduledMood_category = animationType;
        print(callerID + " scheduled new mood of category " + scheduledMood_category);
    }

    public void ScheduleNewReaction(string animationType, string callerID)
    {
        scheduledReaction_category = animationType;
        print(callerID + " scheduled new reaction of category " + scheduledReaction_category);
    }

    // UpdateLoop switches out the avatar's current idle animation for the scheduled animation
    void UpdateLoop()
    {
        string debugOutput = "M:"+scheduledMood_category+" R:"+scheduledReaction_category+" ";
        debugOutput += moodCooldownTimer+"/"+moodCooldown+" ";
        debugOutput += reactionCooldownTimer+"/"+reactionCooldown+" ";
        debugOutput += reactionInitiationTimer+"/"+reactionInitiation+" ";

        bool generateRandomReact = (UnityEngine.Random.Range(1, 21) <= 1); // 5% of time, randomly nod
        if (generateRandomReact) { debugOutput += "|random react!!| "; }
        
        // Avatar logic~
        // First, check if there's a scheduled reaction and we're not on reaction cooldown
        if (scheduledReaction_category != null && reactionCooldownTimer >= reactionCooldown) {
            debugOutput += "[switch to react] ";
            commandAnimationSwitch(scheduledReaction_category, reactionCodes);
            scheduledReaction_category = null;
            scheduledMood_category = "neutral"; // immediately refresh to neutral mood after completion

            moodCooldownTimer = moodCooldown;
            reactionCooldownTimer = 0;
            reactionInitiationTimer = 0;
        }
        // Otherwise, check if there's a scheduled mood and we're not on mood cooldown
        else if (scheduledMood_category != null && moodCooldownTimer >= moodCooldown) {
            debugOutput += "[switch to mood] ";
            commandAnimationSwitch(scheduledMood_category, moodCodes);
            scheduledMood_category = null;
            moodCooldownTimer = 0;
            reactionCooldownTimer += 1;
            reactionInitiationTimer = 0;
        }
        // Otherwise, check if there's no scheduled reaction and we can initiate reaction
        else if (scheduledMood_category == null && (reactionInitiationTimer >= reactionInitiation || generateRandomReact)) {
            debugOutput += "[generate react] ";
            commandAnimationSwitch("nod", reactionCodes);
            moodCooldownTimer += 1;
            reactionCooldownTimer = 0;
            reactionInitiationTimer = 0;

            scheduledMood_category = "neutral"; // immediately refresh to neutral mood after completion
            moodCooldownTimer = moodCooldown;
        } else {
            debugOutput += "[idle] ";
            moodCooldownTimer += 1;
            reactionCooldownTimer += 1;
            reactionInitiationTimer += 1;
        }
        
        // Update timers!
        print(debugOutput);
        Invoke("UpdateLoop", idleAnimationLength);
    }

    // Pick animation from category using provided dictionary, and switch to it
    void commandAnimationSwitch(string categoryName, Dictionary<string, int[]> codeDictionary) {
        if (codeDictionary.ContainsKey(categoryName)) {
            int[] possibleActions = codeDictionary[categoryName];
            int animIndex = UnityEngine.Random.Range(0, possibleActions.Length);
            curAnim = codeDictionary[categoryName][animIndex];
            print("Performing motion "+animationList[curAnim]+" from category "+categoryName + ", [code " + curAnim + "]");
            if (curAnim != lastAnim) {
                SetAnimation(animationList[curAnim], animSpeed, curAnim);
            } else {
                print("animation already "+animationList[curAnim]+" , will not change");
            }
            lastAnim = curAnim;        
        } else {
            print("error: category "+categoryName+" doesn't exist, no motion scheduled");
        }

        // play a sound, maybe
        if (VoiceScriptLink.isUserSpeaking) {
            avatarAudioSource.volume = 0.5f;
            print("avatar will be quieter because the user is speaking~");
        } else {
            avatarAudioSource.volume = 1.0f;
        }

        playSound(categoryName);
    }

    // HELPER FUNCTIONS (no need to change) -----------------------------------------------------------

    void Update()
    {
        // For debugging: manually change the current animation
        if (Input.GetKeyDown("q")) IncrementMotion(-1);
        if (Input.GetKeyDown("w")) IncrementMotion(1);
    }

    void playSound(string soundType)
    {
        // "neutral", "distracted", "greeting", "nod", "shake"
        if (soundType == "greeting") {
            avatarAudioSource.clip = voiceCodes["greeting"][UnityEngine.Random.Range(0, voiceCodes["greeting"].Length)];
        } else if (soundType == "neutral") {
            avatarAudioSource.clip = voiceCodes["thinking-hesitant"][UnityEngine.Random.Range(0, voiceCodes["thinking-hesitant"].Length)];
        } else if (soundType == "distracted") {
            avatarAudioSource.clip = voiceCodes["confident-ahh"][UnityEngine.Random.Range(0, voiceCodes["confident-ahh"].Length)];
        } else if (soundType == "nod") {
            avatarAudioSource.clip = voiceCodes["confident-ahh"][UnityEngine.Random.Range(0, voiceCodes["confident-ahh"].Length)];
        } else if (soundType == "shake") {
            avatarAudioSource.clip = voiceCodes["disappoint-ahh"][UnityEngine.Random.Range(0, voiceCodes["disappoint-ahh"].Length)];
        } else {
            print("261 - error processing sound type");
        }

        print(" - playing sound for current animation "+curAnim+" with sound type "+soundType);
        avatarAudioSource.PlayDelayed(soundDelay[curAnim]);
    }

    // Initialize the avatar model
    // _name    the name of the model
    void ModelChange(string _name)
    {
        if (!string.IsNullOrEmpty(_name))
        {
            print("Instantiating new model : " + _name);
            curModelName = Path.GetFileNameWithoutExtension(_name);
            var loaded = Resources.Load<GameObject>(_name);

            Destroy(obj); // delete any existing object
            obj = Instantiate(loaded) as GameObject;

            // extra avatar settings
            SM = obj.GetComponentInChildren(typeof(SkinnedMeshRenderer)) as SkinnedMeshRenderer;
            SM.quality = SkinQuality.Bone4;
            SM.updateWhenOffscreen = true;

            // animation things? not sure what for loop does
            foreach (AnimationState anim in animTest.GetComponent<Animation>())
            {
                obj.GetComponent<Animation>().AddClip(anim.clip, anim.name);
            }
            this.SetAnimation(animationList[curAnim], animSpeed, curAnim);
        }
    }

    void SetAnimationSpeed(float _speed)
    {
        foreach (AnimationState state in obj.GetComponent<Animation>())
        {
            state.speed = _speed;
        }
    }

    // For debugging, increment the current animation
    void IncrementMotion(int _add)
    {
        curAnim += _add;
        if (animationList.Length <= curAnim)
        {
            curAnim = 0;
        }
        else if (curAnim < 0)
        {
            curAnim = animationList.Length - 1;
        }
        SetAnimation(animationList[curAnim], animSpeed, curAnim);
    }

    void SetAnimation(string _name, float _speed, int originalAnimNumber)
    {
        if (!string.IsNullOrEmpty(_name))
        {
            //print("Setting animation to: " + _name + " ("+originalAnimNumber+")");
            curAnimName = "" + _name;
            obj.GetComponent<Animation>().Play(curAnimName);

            SetFixedFbx(xDoc, obj, curModelName, curAnimName);
        }
        SetAnimationSpeed(_speed);
    }

    void SetFixedFbx(XmlDocument _xDoc, GameObject _obj, string _model, string _anim)
    {
        if (_xDoc == null) return;
        if (_obj == null) return;

        XmlNode xNodeTex;
        XmlNode xNodeAni;
        string t;

        t = "Root/Animation[@Lod=''or@Lod='" + curLOD + "'][Info[@Model=''or@Model='" + _model + "'][@Ani=''or@Ani='" + _anim + "']]";
        xNodeAni = _xDoc.SelectSingleNode(t);

        if (xNodeAni != null)
        {
            string ani = xNodeAni.Attributes["File"].InnerText;
            curAnimName = ani;
            print("Change Animation To " + curAnimName);
            _obj.GetComponent<Animation>().Play(curAnimName);
        }
        t = "Root/Texture[@Lod=''or@Lod='" + curLOD + "'][Info[@Model=''or@Model='" + _model + "'][@Ani=''or@Ani='" + _anim + "']]";
        xNodeTex = _xDoc.SelectSingleNode(t);

        if (xNodeTex != null)
        {
            string matname = xNodeTex.Attributes["Material"].InnerText;
            string property = xNodeTex.Attributes["Property"].InnerText;
            string file = xNodeTex.Attributes["File"].InnerText;
            //print("Change Texture To " + matname + " : " + property + " : " + file);
            foreach (Material mat in SM.GetComponent<Renderer>().sharedMaterials)
            {
                if (mat)
                {
                    if (mat.name == matname)
                    {
                        Texture2D tex = Resources.Load<Texture2D>(file);
                        mat.SetTexture(property, tex);
                    }
                }
            }
        }

        //init Position
        Vector3 pos;
        Vector3 rot;
        t = "Root/Position[@Ani=''or@Ani='" + _anim + "']";
        xNodeAni = _xDoc.SelectSingleNode(t);
        if (xNodeAni != null)
        {
            pos.x = float.Parse(xNodeAni.Attributes["PosX"].InnerText);
            pos.y = float.Parse(xNodeAni.Attributes["PosY"].InnerText);
            pos.z = float.Parse(xNodeAni.Attributes["PosZ"].InnerText);
            rot.x = float.Parse(xNodeAni.Attributes["RotX"].InnerText);
            rot.y = float.Parse(xNodeAni.Attributes["RotY"].InnerText);
            rot.z = float.Parse(xNodeAni.Attributes["RotZ"].InnerText);

            obj.transform.position = pos;
            obj.transform.eulerAngles = rot;
        }

    }
}