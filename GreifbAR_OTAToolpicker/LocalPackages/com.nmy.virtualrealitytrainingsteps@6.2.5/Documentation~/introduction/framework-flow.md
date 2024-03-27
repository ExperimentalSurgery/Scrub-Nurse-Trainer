# Step Framework Flow

This chapter outlines the theoretical concepts behind this framework.
This knowledge helps you better understand how to structure your training in unity.

## Terminology

|                   Terms                    | Descriptions                                                                                                                                                   |
|:------------------------------------------:|----------------------------------------------------------------------------------------------------------------------------------------------------------------|
|                    Step                    | General term for one specific task for either the system or the user to do.                                                                                    |
|                 Next Steps                 | The hierarchical successor(s) of the current step.                                                                                                             |
|                Activatables                | General term for all GameObjects that should be activated and deactivated at some point.                                                                       |
|             Step Activatables              | A list of GameObjects that are activated when their step has started and deactivated when it has finished.                                                     |
|          Persistant Activatables           | A list of GameObjects that will be activated when this step has started and remain activated after it has finished until they get deactivated by another step. |
| Persistant Activatables Deactivation Steps | A list of steps whose persistant activatables will be deactivated when this step is finished.                                                                  |

## Training Step Sequence

Let's dive into the concept on how to design a training step sequence in unity. Depending on the scope of the training,
you have do decide how to structure the application. It greatly depends whether you need only a few steps or thousand
of steps to accomplish your sequence.

A general rule of thumb is to 'divide and conquer' your flow into smaller sub parts (called chapters). Each chapter is
responsible only for their specific sequence of steps. This allows you to easily keep track of the flow and to adjust
the flow if necessary by changing the order of steps, or by adding new or remove old steps.

Lets assume we have the following instructions from a costumer:
> We want a training application about topic XYZ. The user should go through a tutorial where they get familiar with VR.
> Please show also some safety instructions afterwards. The training consists of the following: ... . At the end, let's
> wrap everything up and give a brief summary on what has been done and what has been learned.

To convert this instructions into a step flow, we could either simply use the following:

- User clicks a button
- Virtual assistant speaks 01
- Virtual assistant speaks 02
- Virtual assistant speaks 03
- User moves to position
- Virtual assistant speak 04
- User clicks a button
- ...
- ...

This structure only shows the actual steps that should be performed in the training.
But as a developer or someone who needs to implement this in unity, you could loose the overview which step is doing
what.
Introducing chapters, the flow could be structured as following:

- **Chapter: Intro**
    - **Chapter: Tutorial**
        - User clicks a button
        - **Chapter: VA Intro**
            - Virtual assistant speaks 01
            - Virtual assistant speaks 02
            - Virtual assistant speaks 03
        - User moves to position
    - **Chapter: Safety Instructions**
        - Virtual assistant speak 04
        - User clicks a button
- **Chapter: Main**
    - ...
- **Chapter: Outro**
    - ...

This structure is more readable and extensible.
The execution order is from top to bottom, starting with the intro chapter,followed by the main chapter,
and ending with the outro chapter.
Each chapter can have subchapters if necessary. This design flow is recreated within the step components.

> **Important:**  
> In Unity, we need a so called ``root node`` as starting point for a step sequence. It is a single step that
> can be a chapter or any other step from which all other steps are executed.
>
> - Root
>   - Intro
>   - Main
>   - Outro

### Hierarchy Window

The structure above is represented here as GameObjects with step components in unity.
As you can see, the execution order is from top to bottom starting by the root chapter that contains all other steps.
This is also shown in the green circles on the right side of the window.

| GameObject Names              | Descriptions                                                                                                                        |
|-------------------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| ``[Activatables]``            | Root node for this step ``Activatables`` to be activated during this step.                                                          |
| ``[Persistant Activatables]`` | Root node for this step ``Peristant Activatables`` to be activated persistently during this step. Gets deactivated by another step. |
| ``[Next Steps] ...``          | Simple GameObject as anchor point to go one step deeper into the step hierarchy                                                     |
| ``[Chapter] ...``             | ChapterStep to structure the step flow                                                                                              |
| ``[ ... ]``                   | All other steps encircled in yellow are the actual steps that are executed in order.                                                |

<p align="center">
    <img alt="HierarchyWindowExample.jpg" src="../images/HierarchyWindowExample.jpg" width="50%"/>
</p>

### Tree view

The same structure as above can also be represented by a tree structure.
Here, we use the "Depth First Preorder Traversal".
Start by visiting the root node, traverse the left subtree, then the right subtree.

![ExampleStepSequence.jpg](../images/TreeviewExample.jpg)

## Lifecycle

This framework is intended to be used for multiplayer training applications, so it implements a class
called ``BaseTrainingStep`` that derives from ``NetworkBehaviour`` and is the root implementation for all step
instances.
It can also be used for singleplayer applications when running the ``NetworkManager`` in host mode.
The steps follow the [Unity's lifecycle](https://docs.unity3d.com/Manual/ExecutionOrder.html).

When the application starts, all ``Activatables`` are deactivated. The ``StepController`` handles the entry point for
starting the step sequence.

Each step within a training sequence follows a specific flow that is shown in the following image.

![StepLifecycle.jpg](../images/StepLifecycle.jpg)
