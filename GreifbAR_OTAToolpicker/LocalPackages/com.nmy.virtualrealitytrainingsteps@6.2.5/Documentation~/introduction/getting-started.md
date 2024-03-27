# Getting Started

This chapter gets you started to implement your first step sequence from zero to hero (pun intended :P).

However, before you perform the necessary steps, please read the following pages to understand the principles of this
framework!

- [Installation](installation.md)
- [Overview Step Flow](framework-flow.md)
- [Overview Step Components](step-components.md)

## Requirements

- Installed version of this Framework
- Installed Samples: ``Network Manager Setup``
- Some ideas on how the training can be structured in a step flow (e.g. where to place chapters, which components do I
  need, ...)
- When using ``VirtualAssistantSpeakStep`` or ``CharacterSpeakStep``, you need some ``LocalizedTextToSpeechItem`` in
  your AssetTables (not covered here)
- Optional: XR Interaction Toolkit

### Set up XR

Skip this section if you do not want to use this step framework in a VR environment.
Otherwise, set up your preferred XR Origin as you like.
In most cases, we use the XR Origin provided by the ``XR Interaction Toolkit``.

Add a ``NetworkAvatarManager`` component to a child GameObject of the root GameObject of your XR Origin
and set the references of ``Local Player``.

![network-avatar-manager.png](../images/network-avatar-manager.png)

### Add a NetworkManager

For the system to work, you need in each scene a NetworkManager.
The framework already provides a prefab, so let's use it and drag & drop it into your scene.

![network-manager.png](../images/network-manager.png)

Depending in your requirements, you can disable ``VivoxManager`` (if you do not want to use VoIP).

Go to the ``ServerManager`` GameObject. Here, you have the following options:

![network-launcher.png](../images/network-launcher.png)

Setup the components depending on your desired environment. You can always changes them if you change your mind.

|           Fields           | Singleplayer | Multiplayer |
|:--------------------------:|--------------|-------------|
|  **Auto Connect Client**   | True (tick)  | True (tick) |
| **Auto Connect Client As** | Client       | Host        |

For server builds in a multiplayer environment, use the command line args (TODO: LINK).

## Add a (Training) Step Controller

Create a new GameObject, name it ``Training Step Controller`` and add a step controller component
(e.g. ``TrainingStepController``). We will come back later to this component if we have set up our first step sequence.

![training-step-controller.png](../images/training-step-controller.png)

## Add your Step Sequence

You have successfully set up your scene to use the step framework. Now we can add the first step in the hierarchy.
Do you remember? We need a root node as starting point, so lets create a new GameObject, name it ``Root`` or name it
after the training you want to accomplish with this step (e.g. ``PEG Training``).
Add a ``ChapterTrainingStep`` component to this GameObject.

![getting-started-root-chapter-step.gif](../images/getting-started-root-chapter-step.gif)

Do you see? After adding the step component, the GameObject got prefixed with ``[Chapter]`` and new child GameObject 
were added.

Next, add three chapter steps right under ``[Next Steps]`` for the intro, main, and outro part.

![getting-started-intro-main-outro-steps.gif](../images/getting-started-intro-main-outro-steps.gif)

Add now the root node to you ``Training Step Controller`` component and hit ``Alt+A``. This automatically
sets all ``Next Steps`` of the parent GameObject to the steps that are under this GameObject. 
You do not have to click at the node, it recursively sets all new added steps in the step sequence starting by the
root node you specified in the step controller.

![getting-started-set-root-update-next-steps.gif](../images/getting-started-set-root-update-next-steps.gif)

You are now good to go. Add your steps as needed and do not forget to update the ``Next Steps`` with ``Alt+A``.
Alternatively, you can add them to the list by hand. 