# CharacterSpeakStep

A training step that involves a character speaking and animating.

<p align="center">
    <img alt="ChapterTrainingStep.png" src="../../images/steps/CharacterSpeakStep.png"/>
</p>

## Usage

Use this step when you need a character to speak to the audience. The audio clip is localized through the 
[Localized Audio Clip](#localized-audio-clip) from the ``NMY Google Cloud Text To Speech`` package. Therefore,
you need only this component when your app needs different languages. Each Localized Audio Clip contains a list
of timestamps with trigger words for the animator.

## Parameters

### Audio Source

The ``AudioSource`` component used to play the character's speech audio. Typically, this AudioSource only belongs to
the character.

### Localized Audio Clip

The ``LocalizedTextToSpeechAudioClip`` asset containing the character's speech audio and timestamps for animating the character.

### Animator

Optional. A reference to the animator component to animate the character.

### Animation Trigger Talk

Optional. If set, triggers the talk animation of the character while speaking.

### Animation Trigger Idle

Optional. If set, triggers the idle animation of the character after speaking.