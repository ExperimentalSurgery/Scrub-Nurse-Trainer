# LoadSceneStep

A training step that loads a scene when this step is executed.

<p align="center">
    <img alt="LoadSceneStep.png" src="../../images/steps/LoadSceneStep.png"/>
</p>

## Usage

Use this step when you want to load a scene after your step sequence is finished.

**Example:** your application is structured in different training scenarios through scenes. Maybe you have a "hub" scene
where you can choose your training. After your selected training is finished, load the hub scene again so that the
user can choose another scene.

## Parameters

### Scene Name

the name of the scene to be loaded when this step is executed. 
The scene name should be the name of the scene as it appears in the build settings.

If ``NaughtyAttributes`` is installed through ``AutoHand``, you have a Scene-Dropdown to select the scene; otherwise,
you have to type the exact scene name.

### Camera Fader

Optional. A reference to the Camera Fader component on the head of the rig. 
It is used to  control the fade effect when loading the scene.

### Fade Duration

The duration of the fade effect when loading the scene in seconds.

### Fade Alpha

The alpha value of the fade effect when loading the scene.