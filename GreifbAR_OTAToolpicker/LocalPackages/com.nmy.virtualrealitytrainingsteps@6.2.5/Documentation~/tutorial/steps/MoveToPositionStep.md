# MoveToPositionStep

A training step that waits until the user moved to a specified position.

<p align="center">
    <img alt="MoveToPositionStep.png" src="../../images/steps/MoveToPositionStep.png"/>
</p>

## Usage

Use this step if a user should move to a specific position in your application. Please provide the user with
additional hints where they should move to.

## Parameters

### Source Position

A Transform that specifies the position that should be moved to the [desired position](#desired-position) during this step.
This could be the position of the user's camera or some other game object.

### Desired Position

A Transform that specifies the desired position that the [source position](#source-position) should be moved to.
This could be a game object representing a target location in the game world.

### Activation Radius

A value that specifies the radius in meters around the desired position within which the user must be to complete this step.