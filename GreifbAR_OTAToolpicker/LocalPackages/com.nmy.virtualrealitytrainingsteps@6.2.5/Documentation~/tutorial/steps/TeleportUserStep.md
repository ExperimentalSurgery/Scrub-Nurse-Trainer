# TeleportUserStep

A training step that teleports the user to a specified destination.

<p align="center">
    <img alt="TeleportUserStep.png" src="../../images/steps/TeleportUserStep.png"/>
</p>

## Usage

This step requires ``XR Interaction Toolkit`` to be installed in the project.

Use this step if you want to teleport the user to a specific destination.

## Parameters

### Destination

The destination to teleport the user to.

### Teleportation Provider

The teleportation provider component used to initiate the teleportation.

### Match Orientation

The orientation matching mode to use when teleporting the user.

|    Matching mode     | Description                                                                                                                        |
|:--------------------:|------------------------------------------------------------------------------------------------------------------------------------|
|     WorldSpaceUp     | After teleporting the XR Origin will be positioned such that its up vector matches world space up.                                 |
|       TargetUp       | After teleporting the XR Origin will be positioned such that its up vector matches target up.                                      |
|  TargetUpAndForward  | After teleporting the XR Origin will be positioned such that its up and forward vectors match target up and forward, respectively. |
|         None         | After teleporting the XR Origin will not attempt to match any orientation.                                                         |
