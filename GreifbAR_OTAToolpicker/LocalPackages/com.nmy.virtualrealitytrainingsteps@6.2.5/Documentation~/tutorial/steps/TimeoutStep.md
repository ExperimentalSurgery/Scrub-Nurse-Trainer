# TimeoutStep

A training step that waits for a given amount of time to elapse before completing.

<p align="center">
    <img alt="TimeoutStep.png" src="../../images/steps/TimeoutStep.png"/>
</p>

## Usage

Use this step if you want to wait a certain amount of time before the next step starts.

This class derives from the ``AbstractTimeBasedStep`` class and uses the concept of "delta time"
to calculate the amount of time remaining before the timeout is reached.
The delta time is the amount of time that has elapsed since the step was started on the server.

## Parameters

### Activity Timeout

The amount of time in seconds to elapse before this step finishes.