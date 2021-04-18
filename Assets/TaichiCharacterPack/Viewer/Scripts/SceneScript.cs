// temp

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
    public GameObject animTest;
    public GUISkin guiSkin;
    public string boneName = "Hips";
    public bool guiOn = true;
    // Path names
    public string FBXListFile = "fbx_list_a";
    public string AnimationListFile = "animation_list_a";
    public string TitleTextFile = "title_text_a";
    private const string viewerResourcesPath = "Taichi";
    private string viewerSettingPath = viewerResourcesPath + "/Viewer Settings";
    private string viewerMaterialPath = viewerResourcesPath + "/Viewer Materials";
    private string texturePath = viewerResourcesPath + "/Textures";
    // File names (automatically updated, no need to manually change)
    private string curModelName = "";
    private string curAnimName = "";
    // Entity names (automatically loaded from paths, no need to manually change)
    private string[] animationList;
    private string[] modelList;

    private TextAsset txt; // used for resource loading

    // Not sure what these are for but they seem important
    private GameObject obj;
    private GameObject loaded;
    private SkinnedMeshRenderer SM;
    private XmlDocument xDoc;

    // Local variables (important!)
    private int curAnim = 1; // the current animation to be shown
    private int curModel = 2; // the avatar skin
    private int[] idleChangeFreq = {4, 7}; // range of seconds specifying when to refresh idle animation
    private int greetingRefreshTime = 2; // switch away from greeting animation after this time. (#todo calculate from greeting length)
    private const int curLOD = 1; // display resolution. already set to highest; avoid changing this
    private const string lodType = "_h"; // also display resolution
    private const float animSpeed = 1; // animation speed. also avoid changing this

    // Animation codes (#todo move this to a file)
    // These classify animations into categories
    Dictionary<string, int[]> animationCodes = new Dictionary<string, int[]>() {
        {"greeting", new int[] {6, 7, 8, 26, 36, 37}},
        {"negative", new int[] {22, 23, 67, 70, 71, 77, 84}},
        {"hesitant", new int[] {9, 10, 24, 30, 42, 57}},
        {"neutral", new int[] {0, 20, 21, 38, 43, 50}},
    };
    
    void Start()
    {
        txt = Resources.Load<TextAsset>(viewerSettingPath + "/" + FBXListFile);
        modelList = txt.text.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        txt = Resources.Load<TextAsset>(viewerSettingPath + "/" + AnimationListFile);
        animationList = txt.text.Split(new string[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        print("Found animations:");
        print(String.Join(" ", animationList));
        
        txt = Resources.Load<TextAsset>(viewerSettingPath + "/fbx_ctrl");
        xDoc = new XmlDocument();
        xDoc.LoadXml(txt.text);

        ModelChange(modelList[curModel] + lodType); // initialize main model

        Invoke("NewGreeting", 1); // begin the greeting action
    }

    // NewGreeting initiates a greeting animation before switching to idle animation loop
    void NewGreeting()
    {
        PickMotion("greeting");
        Invoke("NewIdleAnimation", greetingRefreshTime);
    }

    // NewIdleAnimation switches out the avatar's current idle animation at random intervals
    void NewIdleAnimation()
    {
        PickMotion("neutral");
        float nextRefreshTime = UnityEngine.Random.Range(idleChangeFreq[0], idleChangeFreq[1]);
        Invoke("NewIdleAnimation", nextRefreshTime);
    }

    void Update()
    {
        // For debugging: manually change the current animation
        if (Input.GetKeyDown("q")) IncrementMotion(-1);
        if (Input.GetKeyDown("w")) IncrementMotion(1);
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
            int i = 0;
            foreach (AnimationState anim in animTest.GetComponent<Animation>())
            {
                obj.GetComponent<Animation>().AddClip(anim.clip, anim.name);
            }
            this.SetAnimation("" + animationList[curAnim], animSpeed);
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
        SetAnimation(animationList[curAnim], animSpeed);
    }

    // Selects an animation from the specified category to perform next
    void PickMotion(string _category)
    {
        int[] possibleActions = animationCodes[_category];
        int animIndex = UnityEngine.Random.Range(0, possibleActions.Length);
        curAnim = animationCodes[_category][animIndex];
        print("Picking motion "+animationList[curAnim]+" from category "+_category);
        SetAnimation(animationList[curAnim], animSpeed);
    }

    void SetAnimation(string _name, float _speed)
    {
        if (!string.IsNullOrEmpty(_name))
        {
            print("Setting animation to: " + _name);
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

        XmlNode xNode;
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
            print("Change Texture To " + matname + " : " + property + " : " + file);
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