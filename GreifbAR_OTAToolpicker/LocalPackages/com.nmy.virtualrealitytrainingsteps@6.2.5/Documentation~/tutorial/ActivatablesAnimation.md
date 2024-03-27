# Activatables Animation

Use this component if you want to animate the activatables when they get activated and deactivated.

This component works for both, acitvatables and persistant activatable objects.

<p align="center">
    <img alt="activatables-animation-propertydrawer.png" src="../images/activatables-animation-propertydrawer.png"/>
</p>

## Usage

You can add this component either to any activatable that should be animated or to the corresponding step.

The standard behaviour of the activatables is that they get activated (```GameObject.SetActive(true)```) when the step
starts
and deactivated (```GameObject.SetActive(false)```) when the step finished without room for some animations.
In order to animate the activatables (e.g. fade in and fade out), this component adjust some timeout values of the step
in order to play
the animations after the activatables are activated and before the activatables are deactivating.

The animators used for the animations should follow these instructions:

- Add a trigger parameter and name it "show" (or any other name)
- Add a trigger parameter and name it "hide" (or any other name)
- Add an initial AnimationClip with only one keyframe with the initial state of the animation (e.g. scale 0, alpha
  0, ...)
- Add a show AnimationClip that handles the animation when the activatable was activated
- Add a hide AnimationClip that handles the animation when the activatable is deactivating
- The AnimationClips for show and hide can be the same, set the speed of one of them to -1 if you want to use the
  reversed animation
- The transitions of the states should have no exit time and no transition duration or offset (set everything to 0)
- Set the conditions on the transitions to the trigger parameters you defined earlier

![activatables-animation-animator.gif](../images/activatables-animation-animator.gif)

After setting up the animator you want to use, add the component to ... for:

- Automatic Step recognition:
    - the step itself
    - the ```[Activatables]``` GameObject
    - the ```[Persistant Activatables]``` GameObject
- Manual Step Reference
    - any other GameObject outside of the Step GameObjects that should is added to the activatable lists

![activatables-animation-component.gif](../images/activatables-animation-component.gif)

It automatically fetches a list of all parameters and all AnimationClips from the animator from which you can select.

## Parameters

### Step

A reference to the step from which the (persistant) activatables should be animated.

If not set in the inspector, find the first ```BaseTrainingStep``` in parents.

### Activatables Animation

A list of animation data for the activatables.

#### Animator

A reference to the animator that should be used.

#### Step Event

The step event that should trigger the animation.

| Step Event                           | Description                                                                                                | Altered Timeout                                 |
|--------------------------------------|------------------------------------------------------------------------------------------------------------|-------------------------------------------------|
| None                                 | Don't do anything                                                                                          | -                                               |
| Activatables Activated               | Use this event if you want to add the show animation to an activatable after it was activated              | ```timeoutActivatablesActivated```              |
| Activatables Deactivating            | Use this event if you want to add the hide animation to an activatable before it is deactivating           | ```timeoutActivatablesDeactivating```           |
| Persistant Activatables Activated    | Use this event if you want to add the show animation to a persistant activatable after it was activated    | ```timeoutPersistantActivatablesActivated```    |
| Persistant Activatables Deactivating | Use this event if you want to add the hide animation to a persistant activatable before it is deactivating | ```timeoutPersistantActivatablesDeactivating``` |

#### Animation Trigger

The name of the trigger parameter of the animator.
It automatically fetches a list of all parameters from the animator from which you can select.

#### Animation Clip

The AnimationClip that is used in the animator for this animation.
It automatically fetches a list of all AnimationClips from the animator from which you can select.
The length of the AnimationClip is used to determine the timeout of the step.
Depending on the selected event, the corresponding Timeout is adjusted to the highest value.
