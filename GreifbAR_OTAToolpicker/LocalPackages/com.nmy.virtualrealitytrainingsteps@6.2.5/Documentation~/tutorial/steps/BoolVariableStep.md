# BoolVariableStep

A training step that waits for a Unity Atom Bool Variable to match a target value.

<p align="center">
    <img alt="BoolVariableStep.png" src="../../images/steps/BoolVariableStep.png"/>
</p>

## Usage

This step requires ``Unity Atoms`` to be installed in the project.

Use this step to wait until something happened in your application. This "something" can be anything
that can alter the Bool Condition. 
For example, the user grabbed/released something and the corresponding event changes the value. The system
finished loading some resources which changed the condition. User added something somewhere, ...

## Parameters

### Bool Condition

The boolean variable that this step will wait for.

### Target Value

The boolean value that represents the target value to match.