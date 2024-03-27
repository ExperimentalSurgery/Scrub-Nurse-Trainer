# AtomVariableChangedStep

A training step that waits for a change in the value of an Atom variable.

<p align="center">
    <img alt="AtomVariableChangedStep.png" src="../../images/steps/AtomVariableChangedStep.png"/>
</p>

## Usage

This step requires ``Unity Atoms`` to be installed in the project.

Use this step if you want to wait until a atom variable changes its value from the outside.

**Example**: While the step waits for the variable change, the user is doing something that sets the Atom Variable.
Upon adding the information to the variable, this step will finish.

The value change must occur while the step executes ``ClientStepActionAsync``. This step does not safe 
previous values of the atom variable.

## Parameters

### Atom Variable

The reference to any atom variable to be monitored for changes. Does **not** work with constant variables.