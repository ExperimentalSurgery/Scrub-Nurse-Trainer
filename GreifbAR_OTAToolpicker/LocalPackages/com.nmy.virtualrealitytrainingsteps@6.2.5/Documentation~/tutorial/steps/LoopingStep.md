# LoopingStep

A training step that repeats a series of steps in a loop.

<p align="center">
    <img alt="LoopingStep.png" src="../../images/steps/LoopingStep.png"/>
</p>

## Usage

Use this step when you want to repeat a series of steps either infinitely or for a fixed number of iterations.

## Parameters

### Looping Steps

A list of steps that should run in the loop.

### Iterations

The number of iterations the loop should last. 

If the value is 0, the loop will run infinitely.
If the value is greater than 0, the loop will run x number of times before ending.