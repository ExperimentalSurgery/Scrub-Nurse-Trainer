# Installation

This package is provided through the NMY Scoped Registry.

## Registry

To install this package, follow these steps:

- Add the NMY Scoped Registry if not already done
    - ``Edit\Project Settings\Package Manager``
    - **Name:** NMY Assets
    - **URL:** http://nmyvm01:4873
    - **Scope:** com.nmy
- Open the Package Manager
    - ``Window\Package Manager``
    - Go to ``My Registries`` and select the package ``NMY Virtual Reality Training Steps``

## Dependencies

Upon installation, it also automatically installs the following packages:

- Netcode for Gameobjects - ``com.unity.netcode.gameobjects``
- TextMeshPro - ``com.unity.textmeshpro``
- Input System - ``com.unity.inputsystem``
- UniTask - ``com.nmy.external.unitask``

## Samples

This package provides the following Samples:

- **Network Manager Setup**
    - Use this sample for an easy setup with Netcode. It provides a prefab for the ``NetworkManger`` as well as a prefab
      for a simple ``PlayerPrefab`` that can be used as base.

## Further Functionalities

If you want further functionalities, please install the following packages:

- Auto Hand - ``com.nmy.external.autohand``
    - Enables: ``GrabStep`` and ``ReleaseStep`` for Auto Hand grabbable objects
- Unity Atoms Core - ``com.unity-atoms.unity-atoms-core``
    - Enables: ``AtomVariableChangedStep``, ``BoolVariableStep``, ``BoolDecicionStep``
    - Adds functionality to: ``NetworkAvatar``, ``VirtualAssistant``
- Unity Atoms Tags - ``com.unity-atoms.unity-atoms-tags``
    - Adds functionality to: ``TouchStep``, ``TriggerEvents``
- NMY Google Cloud Text To Speech - ``com.nmy.googlecloudtexttospeech``
  - Enables: ``CharacterSpeakStep``, ``VirtualAssistantSpeakStep``
  - Adds functionality to: ``Virtual Assistant``
- XR Interaction Toolkit - ``com.unity.xr.interaction.toolkit``
  - Adds functionality to: ``MoveToPositionStep``
- ParrelSync - ``com.veriorpies.parrelsync``
  - Adds functionality to: ``NetworkLauncher``
