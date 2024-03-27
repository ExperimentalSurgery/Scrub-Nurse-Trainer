# ReleaseGrabStep

A training step that waits for a grabbable object to be released. Optionally, it waits until the object is resting.

<p align="center">
    <img alt="ReleaseGrabStep.png" src="../../images/steps/ReleaseGrabStep.png"/>
</p>

## Usage

This step requires ``Auto Hands`` to be installed in the project.

Use this step if you want the user to release a previously grabbed object.

Works in conjunction with [GrabStep](GrabStep.md).

## Parameters

### Grabbable

The grabbable object to be released.

### Grabbable Rigidbody

The ``Rigidbody`` of the grabbable to be released.

### Wait For Resting

Whether to wait for the grabbable object to be resting before completing the step.