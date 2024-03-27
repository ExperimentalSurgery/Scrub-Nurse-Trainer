# ParallelExecutionStep

A training step that starts a set of steps in parallel and waits until they are finished.

<p align="center">
    <img alt="ParallelExecutionStep.png" src="../../images/steps/ParallelExecutionStep.png"/>
</p>

## Usage

Use this step if you want to run multiple steps in parallel. You can choose between three modes:
The first mode waits for any step to complete in order to finish this step. 
The seconds mode waits until all steps are finished before this step finishes. 
And the third mode lets you select which parallel steps must be completed in order to finish this step.

### Scenario: Any

You have a [ActivationAreaStep](ActivationAreaStep.md) where you want to wait until all clients are within this area.
At the same time, a character should animate the clients to hurry up,
so you want him to repeat his instructions (after some timeouts) while you also wait for the clients to enter the area.
When all users are in the activation area, the reminders of the character should stop.
After some time, when not all clients are within the activation area, the application should also continue.

The setup could look like this:

FinishingCriteria: Any

* ParallelExecutionStep
    * ActivationAreaStep
    * ChapterStep
        * TimeoutStep
        * LoopingStep
            * CharacterSpeakStep
            * TimeoutStep

The ChapterStep is a placeholder for all steps you want to include to run in parallel. With
a [LoopingStep](LoopingStep.md),
you can determine the amount of repetitions the character should loop through.
For example, you could set the [iterations](LoopingStep.md#iterations) to 2. That means:
if the user is not in the activation area within two loops of reminders, the application continues.

### Scenario: All

The user is given the task to solve several tasks, the order to solve them is not important. They can also have some
subtasks.
After all tasks are solved by the user, he is presented with a gift.

The setup could look like this:

FinishingCriteria: All

* ParallelExecutionStep
    * ChapterStep -> Task 1
    * ChapterStep -> Task 2
    * ChapterStep -> Task n
* ChapterStep -> gift

### Scenario: Selected

You want a virtual assistant to be very helpful to the user. 
In a task that he is supposed to perform, you want to give him hints if he gets stuck or doesn't know what to do after a certain time. 
However, this hint should not complete the step - only the completion of the task should do that.

The setup could look like this:

FinishingCriteria: Selected

* ParallelExecutionStep
    * ChapterStep -> VA Hints after X Seconds
    * ChapterStep -> User Task

SelectedParallelSteps -> User Task

## Parameters

### Finishing Criteria

The criteria to finish the step.

| Criteria | Description                                                                                                                                                                            |
|----------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Any      | Waits for any step within [Parallel Steps](ParallelExecutionStep.md#parallel-steps) to finish. The first completed step terminates all other parallel steps.                           |
| All      | Waits for all step within [Parallel Steps](ParallelExecutionStep.md#parallel-steps) to finish.                                                                                         |
| Selected | Waits for all selected steps from [Selected Parallel Steps](ParallelExecutionStep.md#selected-parallel-steps)  in [Parallel Steps](ParallelExecutionStep.md#parallel-steps) to finish. |

![ParallelExecutionStep_Finishing_Criteria.jpg](..%2F..%2Fimages%2Fsteps%2FParallelExecutionStep_Finishing_Criteria.jpg)

### Parallel Steps

A list of steps to run in parallel.

### Selected Parallel Steps

Only relevant if [Finishing Criteria](ParallelExecutionStep.md#finishing-criteria) is set to ```Selected```.

A list of steps contained in [Parallel Steps](ParallelExecutionStep.md#parallel-steps) that must be completed in order to finish this step.