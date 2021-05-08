Quick demo: import all files into Unity (tested on 2020.3.0f1) and run `Assets/TaichiCharacterPack/Viewed/taichi_viewer.unity`.

To include emotion recognition analysis, import the dependencies in FILE NAME to a conda environment and run `OpenCVSocket/ServerForRecognition.py` in a separate terminal concurrently.

# Table of Contents

## Unity Project
You'll need to import all of the below folders into Unity to run the project.

 - *Library/*, *Logs/*, *Packages/*, *ProjectSettings/*, *UserSettings/*: these are less human-readable, but contain important information for Unity to be able to correctly run the project.

 - *Assets/TaichiCharacterPack/*
 
   - *Resources/Taichi/*: this is where avatar resource files for Taichi are stored, e.g. animations, materials, and textures.
 
   - *Shaders/*: extra shaders for the avatar.
 
   - *Viewer/*: this folder is where the **main Unity scene** is located: `taichi_viewer.unity`. This folder also contains various setting files.
 
   - *Viewer/GUI/* DELETE THIS FOLDER, UNNECESSARY
 
   - *Viewer/Scripts/*: this is where most of the **important code** is located.
 
   1. `SceneScript.cs`: basically the main code file; is responsible for the avatar's mood-reaction logic.
 Other code files may call its public *ScheduleNewMood* or *ScheduleNewReaction* functions to request an animation scheduling. SceneScript then picks and executes an appropriate animation and may play an accompanying audio clip.
 
   2. `ServerScript.cs`: responsible for absorbing emotion recognition input from the UDP socket and passing it on to SceneScript.
 
   3. `ViewScript.cs` DELETE SINCE UNNECESSARY
 
   4. `VoiceScript.cs`: uses Unity's built-in voice recognition system to predict what the user is saying, and whether or not they are speaking. VoiceScript then performs sentiment analysis and passes its results to SceneScript.
 
   - *Viewer/Resources/Taichi/Viewer Settings/*: includes a variety of text files mapping animation and texture files to pathnames in *Assets/Resources*.
 
   - *Viewer/Resources/Taichi/Viewer BackGrounds/*: some default backgrounds to customize Taichi with.
 
   - *Viewer/Resources/simlish_audio/*: this folder contains .wav files of recorded Simlish from the Sims 4, sorted into 5 distinct response categories.

## Emotion Recognition Server

 - *OpenCVSocket/*
 
   - `ServerForRecognition.py`
 
   While running the main project in Unity, you'll need to run ServerForRecognition.py **separately** in a terminal in order to make use of emotion analysis results. This communication uses UDP port 5065. The Unity project will automatically try to connect; on successful connection, the Unity console will display a confirmation.

   ServerForRecognition.py uses a conda environment to manage its dependencies. You can create the needed environment by using INSERT YML LINK HERE ONCE EXPORTED; more instructions on how to do this are [here](https://conda.io/projects/conda/en/latest/user-guide/tasks/manage-environments.html#creating-an-environment-from-an-environment-yml-file).

   - `Emotion Recognition.py`: contains various helper functions for ServerForRecognition.py.

   - `emotion-ferplus-8.onnx`: a [pretrained](https://bleedai.com/facial-expression-recognition-emotion-recognition-with-opencv/) emotion recognition model.

Notes:

1. If you have multiple cameras, you may need to increment `default = 0 # cam source` in ServerForRecognition.py.

2. The main Unity project will still work without server input; emotion feedback simply won't be included in the avatar's mood computations.

3. Note to self: correct environment is `conda activate swan`

4. The conda environment contains dependencies such as portaudio/pyaudio that may be difficult to install with a different dependency management system such as venv

## Instructions for testing with remote participants

You can use [OBS Virtual Camera](https://obsproject.com/download) to capture a section of your screen (e.g. a Zoom feed) as a virtual camera to use with the emotion recognition server. If you see a black screen with glitching along the top in the emotion recognition visualizer, try using [this](github.com/Fenrirthviti/obs-virtual-cam) plugin (version 2.0.5).

The server should normally pass its predictions to the Unity client; you can then screenshare the game scene through Zoom or another software, thus simulating direct use.
