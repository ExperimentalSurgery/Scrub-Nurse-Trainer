# LookAtTargetStep

A training step that waits until the user looks at a specified target for a certain amount of time.

<p align="center">
    <img alt="LookAtTargetStep.png" src="../../images/steps/LookAtTargetStep.png"/>
</p>

## Usage

Use this step if you want the user to look at a specific target for a certain amount of time. 

**Example:** An object in the opposite direction as the user is looking needs to be interacted with.
With this step you can force the user to look at this object before the next step continues.

This step assumes that the forward vector of the [target](#target) is looking in the z-axis.

## Parameters

### Camera

The camera used to determine whether the user is looking at the target.

### Target

The target that the user should look at.

### Success Delay

The amount of time that the user should take to look at the target before the step is considered complete.

### Position Threshold

A value in the range [-1, 1] that determines whether the user should stand in front or behind the target.
A value of -1 indicates that the user should stand behind the target, a value of 0 indicates that the user
should stand next to the target, and a value of 1 indicates that the user should stand in front of the target.

### Look Threshold

A value in the range [-1, 1] that determines whether the user should look at the target or in the same direction as the target.
A value of -1 indicates that the user looks in the opposite direction, a value of 0 indicates that the user
looks perpendicular, and a value of 1 indicates that the user looks in the same direction as the target.

### Debug Values

A flag indicating whether to print debug values of the calculation.