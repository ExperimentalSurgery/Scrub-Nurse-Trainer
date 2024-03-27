# RepeatButtonDecisionStep

A training step that repeats this step until the required steps from [Required Steps](#required-steps) are traversed.

<p align="center">
    <img alt="ButtonClickedStep.png" src="../../images/steps/RepeatButtonDecisionStep.png"/>
</p>

## Usage

Use this step if you want to repeat several steps before continuing.

The order in which the steps are traversed is not important.

It adds this step to ``Next Steps`` until

**Example:** A character wants to tell you something about topic A, topic B, and topic C. Each topic is represented by
a step that is added to [Required Steps](#required-steps). The user sees three buttons corresponding to each topic. 
They click on one button, all buttons disappear and the character tells the corresponding topic. 
After finishing this topic, the user sees the three buttons again. They can pick the same topic again or another one.
This will be repeated until each topic was at least traversed once.

**Example:** You have a dialog with two topics and one "I dont give a f*** about your topics" button. 
Add only this step to [Required Steps](#required-steps) in order to quickly continue the sequence without hearing
the topics.

## Parameters

### Step Tuples

See [here](ButtonDecisionStep.md#step-tuples).

### Required Steps

A list of required steps that must be traversed in this loop. These steps must also be set in [Step Tuples](#step-tuples).

If not set, it will be automatically filled with the steps from [Step Tuples](#step-tuples).

If only one step was added, this step is responsible that the sequence does not end in an infinite loop.