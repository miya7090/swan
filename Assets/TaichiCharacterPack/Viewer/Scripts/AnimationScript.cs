// temp

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Text.RegularExpressions;
using UnityEngine;

public class AnimationScript : MonoBehaviour
{
    // Link to the SceneScript to send animation commands to
    public SceneScript SceneScriptLink;
    // Link to the ServerScript to read signals from
    public ServerScript ServerScriptLink;

    void Start()
    {
        print("animation script running!");
        // #todo
    }

    void Update()
    {
    }
}