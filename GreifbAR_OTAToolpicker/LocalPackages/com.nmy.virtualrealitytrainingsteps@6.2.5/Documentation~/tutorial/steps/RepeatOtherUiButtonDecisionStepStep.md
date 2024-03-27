# RepeatOtherUiButtonDecisionStepStep

A special training step that waits for the user input to start the other step from a previous 
[ButtonDecisionStep](ButtonDecisionStep.md) if desired.

<p align="center">
    <img alt="RepeatOtherUiButtonDecisionStepStep.png" src="../../images/steps/RepeatOtherUiButtonDecisionStepStep.png"/>
</p>

## Usage

Use this step in conjunction with a previous [ButtonDecisionStep](ButtonDecisionStep.md). If the user clicks the
[Yes Button](#yes-button), the ``otherStep`` that was not executed by the 
[ButtonDecisionStep](ButtonDecisionStep.md) step will now be executed.

Note that [ButtonDecisionStep](ButtonDecisionStep.md) needs exactly two ``StepTuples`` to function.

**Example**: You have a user interface with two options - either tell the user story A or story B. 
If the user selects story A, you can ask the user if they also want to read the other story once story A is complete.

## Parameters

### Decision Step

A reference to the previous decision step from which the ``otherStep`` should be repeated.

### Yes Button

A reference to the button that must be clicked to start the other step..

### No Button

A reference to the button that must be clicked to not start the other step.