# BoolDecisionStep

A training step that checks a Atom Bool Variable against a target value and then
selects a step to continue with.

<p align="center">
    <img alt="BoolDecisionStep.png" src="../../images/steps/BoolDecisionStep.png"/>
</p>

## Usage

This step requires ``Unity Atoms`` to be installed in the project.

Use this step to decide which substep sequence you want to go through based on the Boolean variable that was set earlier
either automatically by the system or manually by the client.

**Example:** Your system provides the ability to have a "guided" or "unguided" training. 
Depending on the ``UseGuidance_BoolVariable`` you created earlier and that is set somewhere in your application, 
one step tuple contains the substep sequence for the guided training 
and the other step tuple the sequence for an unguided sequence. 

Note that this step requires exactly **TWO** [Step Tuples](#step-tuples), with their comparables set to "true" and "
false".
Otherwise it will wait indefinitely unless the BoolVariable already has the correct value when the step starts.
Specifically, this step will NOT wait+listen for a state change of the BoolVariable when it is already running -
use the regular [BoolVariableStep](BoolVariableStep.md) for that.

## Parameters

### Step Tuples

Derived from ``DecisionStep``.
Comparable is a boolean.

### Bool Variable

The Unity Atom Bool Variable that the step will check.