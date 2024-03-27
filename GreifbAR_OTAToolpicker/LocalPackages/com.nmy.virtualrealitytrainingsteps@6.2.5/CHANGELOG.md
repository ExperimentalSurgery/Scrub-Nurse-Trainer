# Changelog NMY Virtual Reality Training Steps

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [6.2.5] - 2024-01-10

### Fixed

- Fixed a problem in RepeatButtonDecisionStep where late joining clients started the sequence but did not repeat afterwards when needed

## [6.2.4] - 2024-01-10

### Fixed

- fix a StackOverflow bug in the method "IsStepInHierarchy". All external classes overriding this method must be updated to use the visited set!
- rename method "StepFinishedAction" to "StepCompletedAction

## [6.2.3] - 2024-01-10

### Fixed

- Invoke onStepChanged when TryMoveToStep method is called

## [6.2.2] - 2023-12-21

### Fixed

- Fixed an out of sync error in the ParallelExecutionStep

## [6.2.1] - 2023-12-21

### Fixed

- Fixed an issue where the server resets the stepStartedTime variable in the AbstractTimeBasedStep but a late joining client tries to get the correct value but receives -1 (the system wont react anymore)

## [6.2.0] - 2023-12-20

### Fixed

- Fixed an issue in ParallelSteps where skipping to a parallel step or a step that is in the hierarchy of a parallel step results in an asynchronous behavior.

### Updated

- Introduced a default CancellationToken for all ExecuteMoveToStepAction methods
- Added for all EventHandlers in BaseTrainingStep InvokeXXX methods so that derived classes can also invoke those events

## [6.1.11] - 2023-12-07

### Fixed

- Fixed an issue in Timeline Steps. When an animation clip has an ease out duration value at the end of the timeline to blend to some idle animation of the animator, the altered position of the object within the timeline is not blended resulting in the object out of sync for late joining clients
- Fixed some nullrefs on server-side when client (dis)connects

### Updated

- Updated UniTask Package from 2.1.3 to 2.5.0
- Updated min version of NGO and Input System

## [6.1.10] - 2023-11-17

### Fixed

- Fixed an issue with an using direction for base atoms
- Disable vivox support since old version is not downloadable anymore
- Fixed a compile error when using NGO 1.7.0

## [6.1.9] - 2023-08-22

### Fixed

- disable mesh renderer for camera fades when not needed to save draw calls
- disable inEditor preview when using camera fade sliders to prevent errors in unity 2022 LTS

## [6.1.8] - 2023-06-07

### Fixed

- Fixed what was meant in 6.1.7

## [6.1.7] - 2023-06-06

### Fixed

- Fixed an issue for the all steps within ParallelExecutionSteps where Activatables and Persistant Activatables were
  still active when linkedSource was canceled

## [6.1.6] - 2023-05-29

### Added

- Added some events to NetworkAvatar

### Fixed

- Fixed an issue where the client raised an event upton connection success but was not connected to server

## [6.1.4] - 2023-05-26

### Added

- Added the ability to set the name of a client avatar externally

## [6.1.3] - 2023-05-04

### Fixed

- Fixed the same issue from 6.1.0 but in DeactivateActivatablesAsync catch block

## [6.1.2] - 2023-05-04

### Fixed

- Fixed DefaultNetworkPrefabs reference in sample

## [6.1.1] - 2023-05-04

### Fixed

- Fixed Player Prefab reference in sample

## [6.1.0] - 2023-05-04

### Updated

- Required Netcode version is now 1.4.0

### Fixed

- Fixed an issue where Activatable GameObjects of steps got deactivated by the step system while processing the catch
  block when playmode was stopped

## [6.0.0] - 2023-04-25

### Added

- Added NetworkAvatarOffsetScriptableObject for easy exchange of settings
- Added two presets for the offsets (RPM HMD & RPM Ultraleap Handtracking)
- Added the ability to set the left hand and right hand references from outside of NetworkAvatarManager (e.g. if you
  want to support hand tracking and the hand references changes depending on the current mode)

### Removed

- Removed all offsets from NetworkAvatarRenderer and replace it with NetworkAvatarOffsetScriptableObject (please save
  your offsets before updating!)

## [5.7.3] - 2023-04-13

### Fixed

- Fixed an issue in TimelineStep where skipping a timeline step did not trigger the last frame of the timeline correctly

## [5.7.2] - 2023-04-05

### Fixed

- Blackscreen in builds when using XrFade or XrVignette and use no fade out effects at start.
- Update Shader in start to the values given in the inspector

## [5.7.1] - 2023-04-03

### Fixed

- Remove unwanted Debug.Log in XrFade

## [5.7.0] - 2023-03-31

### Added

- Added XrFade with Hemisphere (just like XrVignette but only uses alpha)

### Updated

- Updated the XrVignette behaviour to set the start fade out duration in the inspector
- Added second animation curve to XrVignette to set aperture and feathering effects separately

## [5.6.0] - 2023-03-27

### Added

- Add functionality to hide avatar hands if the tracking state of the Xr Origin controller / hands is set to false

## [5.5.1] - 2023-03-27

### Fixed

- XrVignette start behaviour when duration is 0

## [5.5.0] - 2023-03-27

### Added

- Added Xr Vignette from XRIT Samples with custom controller for fading

### Updated

- Changed LoadSceneStep to use a CameraFadeBase class for fade effects

## [5.4.11] - 2023-03-16

### Updated

- Add to all Debug logs the prefix $"{GetType()}: if not already done.

## [5.4.10] - 2023-03-09

### Fixed

- fixed error from 5.4.9

## [5.4.9] - 2023-03-09

### Fixed

- Vivox: Fixed error where the localClientId was not ready. Wait for connected client

## [5.4.8] - 2023-03-09

### Fixed

- Vivox: change Username representing now ClientId
- Vivox: fix ChannelType from Echo to Positional

## [5.4.7] - 2023-03-09

### Fixed

- Fixed an UnassignedReferenceException in NetworkAvatarRenderer while trying to access the GameObjects of the provided
  transforms

## [5.4.6] - 2023-03-09

### Updated

- Convenience update NetworkAvatarRenderer: Checks neck, head, leftHand, and rightHand whether they contain a
  ClientNetworkTransform component. If not, add it.

## [5.4.5] - 2023-03-09

### Updated

- Convenience update NetworkAvatarRenderer: if no renderer was provided, it searches for all renderers in the children
  at startup

## [5.4.4] - 2023-03-06

### Fixed

- Fixed an error where --launch-as-client did not propagate serverIP and serverPort zu the SessionManager

## [5.4.3] - 2023-03-06

### Fixed

- Fixed an error where _serverIP was set when "--serverPort" was given in command line args

## [5.4.2] - 2023-03-03

### Fixed

- Deactivate avatar name root GameObject instead of the TMP object itself

## [5.4.1] - 2023-03-03

### Fixed

- Fixed an issue where setting the NetworkAvatar directly did not trigger a change by remote clients
- Fixed an issue where the avatar was not set to the currentAvatar when it was set in the inspector

## [5.4.0] - 2023-02-28

### Updated

- Updated RepeatButtonDecisionStep to set the interactable state of the button of the traversed steps to false if needed

## [5.3.0] - 2023-02-17

### Updated

- Update ParallelExecutionStep with new finishing criteria: Selected

## [5.2.0] - 2023-02-15

### Added

- Added ParallelExecutionStep and its documentation
- Added LoopingStep and its documentation

## [5.1.4] - 2023-02-09

### Fixed

- Fixed an issue in NetworkAvatar & NetworkAvatarRenderer where the Avatar was wrong rotated and positioned when the XR
  Origin was not in the origin

## [5.1.3] - 2023-02-09

### Fixed

- Fixed a critical error where nearly all classes derived from AbstractTimeBasedStep were not executed in a Server +
  Client configuration

## [5.1.2] - 2023-02-09

### Fixed

- Fixed a NullRefException raised while StartTrainingStep was not referenced in a StepController

## [5.1.1] - 2023-02-08

### Fixed

- Fixed an error where CharacterSpeakStep returned without calling RaiseClientStepFinished

## [5.1.0] - 2023-02-08

### Added

- Add bool to automatically set the required steps in RepeatButtonDecisionStep if true

## [5.0.0] - 2023-02-07

### Fixed

- Fixed an error where an InvalidCastException in TrainingStepController was raised while trying to get the currentStep
  from the NetworkVariable

### Removed

- Removed MultipleStorylineStep since it is redundant to DecisionStep

## [4.0.15] - 2023-02-06

### Fixed

- Fixed a critical bug where BaseTrainingStep::IsStepInHierarchy formed a recursive infinite loop under certain
  conditions.

## [4.0.14] - 2023-02-01

### Fixed

- Fixed a critical error in NetworkAvatarManager and NetworkAvatar when loading a new scene

## [4.0.13] - 2023-01-19

### Changed

- Moved some hotkeys to NMY EditorTools v1.1.0: Clear Console, Toggle Inspector Lock/Mode.

## [4.0.12] - 2023-01-18

### Added

- Add own Scene Attribute adjusted from the example of NaughtyAttributes

### Fixed

- Fixed an error where DeactivateActivatablesAsync throws an exception while not necessary

## [4.0.11] - 2023-01-06

### Fixed

- Minor copy&paste bug in BaseTrainingStepUnityEventsEditor, resulting in incorrect summary display.

## [4.0.10] - 2023-01-05

### Fixed

- Fixed a bug where (persistant) activatables were not (de)activated under certain conditions when the step was
  forcefully continued.

## [4.0.9] - 2023-01-05

### Fixed

- Cleanup/removed incorrect assets Samples/ and Samples.meta, causing a Unity warning on package import.

## [4.0.8] - 2023-01-05

### Fixed

- Fixed a compiler error if vivox is not installed in the project

## [4.0.7] - 2023-01-05

### Fixed

- Fixed wrong reference in CharacterSpeakSteps
- Fixed a bug where the label of the AnimatorParam and AnimatorClip attributes were not shown

## [4.0.6] - 2023-01-05

### Added

- Added talk and idle animation trigger options to CharacterSpeakStep

## [4.0.5] - 2023-01-04

### Added

- Added custom property drawers for ActivatablesAnimation

### Fixed

- Fixed some editor script namespaces
- Fixed ActivatablesAnimation to use property drawers
- Fixed documentation for ActivatablesAnimation

## [4.0.4] - 2023-01-04

### Added

- Added documentation for ActivatablesAnimation
- Added missing events and timeout descriptions of the BaseTraining class in documentation

### Refactored

- Renamed some properties in ActivatablesAnimation

## [4.0.3] - 2023-01-04

### Fixed

- Fixed critical bug where deactivating activatables was called more than once when a step was stopped and reset for
  repeating decision steps

## [4.0.2] - 2023-01-03

### Fixed

- Fixed an issue where the reset step did not use the timeouts if specified

## [4.0.1] - 2023-01-03

### Fixed

- Fixed an issue where force continue was not working properly

## [4.0.0] - 2023-01-03

### Added

- Added new ActivatablesAnimation component for animating activatables after they get activated and before they get
  deactivated
- Added for ActivatablesAnimation new Events and timeouts to BaseTrainingStep
- Added more documentation

### Changed

- Rename some Timeouts to match the new namings "*ing vs *ed"

### Refactored

- Refactored Network Avatar scripts to own folder and namespace
- Refactored vivox

## [3.3.0] - 2022-12-22

### Fixed

- Fix a critical bug where the application crashes when steps are recursively trying to reset themself

### Removed

- Remove JumpToStep since it doesn't worked as intended for fifa

## [3.2.0] - 2022-12-22

### Added

- Add JumpToStep

## [3.1.1] - 2022-12-22

### Fixed

- Fixed an issue where some steps automatically finished when traversed twice

## [3.1.0] - 2022-12-21

### Added

- Add Documentation for the whole package. This includes most of the inline documentation as well as the
  doxfc documentation.
- Add RepeatButtonDecisionStep

### Fixed

- Fix most of LINQ features from the package

## [3.0.0] - 2022-12-02

### Added

- Added documentation structure.

### Fixed

- Fixed missing and incorrect namespaces.

## [2.2.0] - 2022-11-28

### Added

- Add AtomVariableChangedStep which checks if an atom variable has changed its value. The value change is independent of
  the type of the variable.

## [2.1.0] - 2022-11-24

### Added

- Added BoolDecisionStep, which executes Optional Steps according to a given BoolVariable.

## [2.0.0] - 2022-11-18

### Fixed

- Netcode does not support RPCs in generics, so moved all ServerRpc's from DecisionStep into all derived classes.

## [1.4.6] - 2022-11-17

### Fixed

- Fix UnassignedReferenceException on LookAtTargetStep if no target was specified

## [1.4.5] - 2022-11-17

### Fixed

- TouchStep.OnTriggerStay() was implemented, but the callback was never registered. This fixes the problem that the
  TouchStep will not trigger if the target object is already inside the collider when starting the step.
- Fixed the same for ActivationAreaStep: will now also trigger if target is already inside the area when the step
  starts.

## [1.4.4] - 2022-11-16

### Fixed

- Moved FormerlySerializedAs to correct field.

## [1.4.3] - 2022-11-16

### Fixed

- Missing using directive.

## [1.4.2] - 2022-11-16

### Fixed

- Renamed fields in BaseTraningStep need a [FormerlySerializedAs()], to keep old settings from getting lost.

## [1.4.1] - 2022-11-16

### Changed

- Alt-a: Also update any grand children recursively (e.g., for DecisionSteps).

## [1.4.0] - 2022-11-16

### Changed

- Allow the BoolVariableStep to wait for true or false.

## [1.3.0] - 2022-11-16

### Added

- Add more "time before *" thresholds in BaseTrainingStep

## [1.2.0] - 2022-11-16

### Added

- Add ToggleClickedStep
- Add options to ButtonClickedStep to not set the interactable flag at step start and step finished

## [1.1.0] - 2022-11-10

### Added

- Added TrainingStepController.TryChangeStartStep() to choose a different StartStep at runtime (but before the Step
  system initializes).

## [1.0.11] - 2022-11-09

### Fixed

- Ensure Localization has initialized before trying to load an audioClip.

## [1.0.10] - 2022-11-08

### Fixed

- Fix cancellation behaviour of the TimelineStep -> skip to the end of the director & stop the director

## [1.0.9] - 2022-11-08

### Added

- Add option to allow continue next step action in builds (TrainingStepController)
- Add Stop System Context menu to stop completely the running step system (TrainingStepController)
- Add try-catch blocks for every overriden ServerStepActionAsync
- Add some null ref checks for the controller input values (NetworkAvatar)

### Fixed

- Fix some "async void" methods to "async UniTaskVoid" and add .Forget() to the call
- Fix null ref exception if no display name text was provided (NetworkAvatarRenderer)

### Removed

- Remove catch log message from VirtualAssistant.Move
- Remove "ignoreTimeScale: false" in UnityTask.Delay calls since the default is false

## [1.0.8] - 2022-11-08

### Fixed

- Hide local avatar if needed when currentAvatar is set

## [1.0.7] - 2022-11-08

### Changed

- Refactor NetworkAvatar to support multiple avatars to select from

## [1.0.6] - 2022-11-07

### Added

- Add InputActionProperty to TrainingStepController to call ForceContinue on the current step

## [1.0.5] - 2022-11-07

### Added

- Add ForceContinue method in BaseTrainingSteps to forcefully continue the flow (skip the step action execution of this
  step)

### Fixed

- Fixed exception handling when AssetLoadAsync could not load the asset due to unexpected reasons

### Changed

- Change Log level from Log to Error for some guard messages

## [1.0.4] - 2022-11-03

### Fixed

- Fix time based steps to start properly

## [1.0.3] - 2022-11-02

### Fixed

- Fix from the fix in 1.0.2: remove all Addressables.Release commands since their produce errors (Exception: Attempting
  to use an invalid operation handle)

## [1.0.2] - 2022-11-02

### Fixed

- Fixed problem where loaded assets are not released after use
- Unify the way how CharacterSpeakStep and VirtualAssistant.Speak load addressables
- Remove some unused catch parameter
- Fix TeleportUserStep which did not override Start correctly

## [1.0.1] - 2022-11-01

### Fixed

- Fixed time based steps (TimeoutStep, TimelineStep, CharacterSpeakStep, VirtualAssistantSpeakStep) for late joining
  clients

### Changed

- Changed TriggerExpression of the Virtual Assistant from Coroutines to UniTasks

## [1.0.0] - 2022-10-28

### Changed

- Increased version number to 1.0.0.

### Fixed

- Set DefaultExecutionOrder of BaseTrainingStep (and therefore all derived classes) to -50.
- Removed now obsolete local ExecutionOrder override of TouchStep.cs.

## [0.0.1-r.22] - 2022-10-26

### Added

- Add virtual Awake method to BaseTrainingStep & Add base.Awake to inherited classes

### Fixed

- Fixed TouchStep not invoking base.Start

## [0.0.1-r.21] - 2022-10-26

### Fixed

- Fixed null colliders in trigger list of TriggerEvent if no collider was found in the GO

### Changed

- Changed ExecutionOrder of TouchStep & TriggerEvent to -50 and -51 in Order to cache all Tags
- Changed BaseTraining Awake method to Start (and all other derived classes)

## [0.0.1-r.20] - 2022-10-25

### Fixed

- If trigger list is empty, let every trigger collider in the scene be valid to invoke events

## [0.0.1-r.19] - 2022-10-25

### Changed

- add additional trigger hierarchy tags to TriggerEvent and TouchStep

## [0.0.1-r.18] - 2022-10-25

### Added

- Optional Unity Atoms Tags v4.4.5 support

### Changed

- Trigger Collider(s) in TouchStep and TriggerEvent support now non in-scene references through Atoms Tags field

### Fixed

- Missing assembly reference if google tts is not present in other projects (#ifdef SetLocalizedAudioClip)

### Removed

- compareCollider in TriggerEvent -> please use the new trigger lists
