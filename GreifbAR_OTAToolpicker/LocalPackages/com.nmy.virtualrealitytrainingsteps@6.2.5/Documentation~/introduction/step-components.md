# Step Components

This chapter gives you a general overview of the basic step component fields and their usage.

## Overview

Each step in Unity is represented by a step component.
There are several different steps for different tasks to be fulfilled by either the system or the user itself.
A list of all common steps with a detailed explanation can be found under the tutorial section.

When you add a step component to a GameObject, the system automatically performs the following steps:

- Creating a ``NetworkObject`` component on the same GameObject as the step component
- Prefixing the GameObject with ``[...]`` specifying which step this GameObject represents.
  You can rename the GameObject with a fitting title and add the step component to add the prefix.
- Creating ``[Activatables]`` as child GameObject. Adds this GameObject to ``StepActivatables`` of the step component
- Creating ``[Persistant Activatables]`` as child GameObject. Adds this GameObject to ``PersistantActivatables`` of the
  step component
- Creating ``[Next Steps]`` as child GameObject for creating nested step hierarchies.

As shown in the previous chapter about the [lifecycle](framework-flow.md#lifecycle) of a step, some public fields
like ``Activatables``, ``Timeouts``, and ``Next Steps`` are visible in the inspector by default.
Set the values as you need.

The numbers after the foldable title of ``Activatables`` stand for the number of added GameObjects
added to ``StepActivatables`` or ``PersistantActivatables``.

The numbers after the foldable title of ``Timeouts`` represent the set seconds of each timeout field.

Each step contains at the end of the component the step specific fields. Please check the corresponding
pages for the step you want to use.

![StepLifecycle.jpg](../images/step-component-example.png)

### Step Activatables

> A list of GameObjects that will be activated when this step has started and deactivated when it has finished.

You can either place any GameObject as child of the ``[Activatables]`` GameObject of the step to have it automatically
(de)activated during the step, or you can reference any GameObject in the hierarchy here.

At the beginning of the step sequence, all step activatables get deactivated and are only activated
when the corresponding step starts.

#### Example

This is extremely useful if you want to display some UI elements only during this step.
The UI is activated when the step has started and is automatically deactivated when it is finished.

### Persistant Activatables

> A list of GameObjects that will be activated when this step has started and remain activated after it has finished
> until they get deactivated by another step.

You can either place any GameObject as child of the ``[Activatables]`` GameObject of the step to have it automatically
activated at the start of the step, or you can reference any GameObject in the hierarchy here.
These GameObjects remain activated when the step has finished.

At the beginning of the step sequence, all persistant activatables get deactivated and are only activated when the
corresponding step starts.

Use [Persistant Activatables Deactivation Steps](#persistant-activatables-deactivation-steps) to deactivate the persistant activatables by another step.

#### Example

Persistant Activatables can be used for two main purposes:

1. Persistently activate a GameObject from the start of a step until the end of the step sequence.
   <br>**For example:** Spawn a Tool that should be activated during a step that persist until the end of the application.
2. Peristantly activate a GameObject from the start of a step until another step has finished.
   <br>**For example:** Spawn a UI at Step A, let it remain activated until Step A+n-1, deactivate the UI when Step A+n has finished.

### Persistant Activatables Deactivation Steps

> A list of steps whose persistant activatables will be deactivated when this step is finished.

You can add any BaseTrainingStep to this list. The Persistant Activatables of these steps are deactivated after
the step to which the list belongs is finished.

This behaviour is optional. You do not need to add steps if you do not specifically need this type of feature.

It works in conjunction with the [Persistant Activatables](#persistant-activatables) of other steps.

#### Example

You added a UI panel to the Persistant Activatables of Step A. This panel is activated when Step A starts. The UI
should disappear when Step N is finished, So you add Step A to the ``Persistant Activatables Deactivation Steps`` list
of Step N.

### Timeout Before Step Start

> The amount of time to wait before starting this step.

### Timeout Before Step Finish

> The amount of time to wait before finishing this step.

### Timeout Before Step Complete

> The amount of time to wait before completing this step.

### Timeout Activatables Activating

> The amount of time to wait before activating the activatable GameObjects.

### Timeout Activatables Activated

> The amount of time to wait after activating the activatable GameObjects.

### Timeout Activatables Deactivating

> The amount of time to wait before deactivating the activatable GameObjects.

### Timeout Activatables Deactivated

> The amount of time to wait after deactivating the activatable GameObjects.

### Timeout Persistant Activatables Activating

> The amount of time to wait before activating the persistant activatable GameObjects.

### Timeout Persistant Activatables Activated

> The amount of time to wait after activating the persistant activatable GameObjects.

### Timeout Persistant Activatables Deactivating

> The amount of time to wait before deactivating the persistant activatable GameObjects.

### Timeout Persistant Activatables Deactivated

> The amount of time to wait after deactivating the persistant activatable GameObjects.

### Next Steps

> A list of steps that come after this step in the training sequence.

You can add any BaseTrainingStep to this list. This list is the key feature to be able to structure the step sequences
in logical parts.

It is recommended to add **only** steps that belong to the ``Next Steps`` children GameObject of the step; otherwise,
you might loose the overview in larger step sequences. 

### Wait For All Clients To Finish

> Whether to wait for all clients to finish this step before moving on to the next step.

This flag is only relevant for multiplayer applications. If enabled, this step waits for **all** clients to do the
specific step action implemented in the derived classes of BaseTrainingStep before going to the next step in the sequence. 
Otherwise, the first client that finishes this step action will inform the server, so that the server terminates 
the step actions for all other client. Every client will continue with the next step altogether.

#### Example

When playing an audio file with different localizations (e.g. EN & DE), the length of the audio clip is typically
different. When this flag is enabled, all clients wait for the last client to finish the audio file to go to the next
step. Otherwise, the audio clip would end early, so the client would not hear everything.

In some UIs, a group is supposed to choose one of several paths (for example, when a character asks you if you want to
hear more about Topic A or Topic B). If this flag is disabled, the first client to click a button "makes" the decision
for the group. The UI disappears for everyone and the sequence continues with the decision of the client that clicked
the button.

## Helper Components

The framework comes with some helper components to debug or extend the functionality of the step.

Add the [``BaseTrainingStepDebug`` component](../tutorial/BaseTrainingStepDebug.md) to the step GameObject and it prints
debug information about the step when started.

Add the [``BaseTrainingStepUnityEvents`` component](../tutorial/BaseTrainingStepUnityEvents.md) to the step GameObject
to get access to all step events of this step in the Inspector. You can extend the functionality of the step with using
this component quite easily.

Add the [``ActivatableAnimations`` component](../tutorial/ActivatablesAnimation.md) to the step GameObject and it prints
debug information about the step when started.