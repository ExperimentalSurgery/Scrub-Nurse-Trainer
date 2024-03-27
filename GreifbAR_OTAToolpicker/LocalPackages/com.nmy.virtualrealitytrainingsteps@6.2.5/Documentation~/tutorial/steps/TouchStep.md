# TouchStep

A training step that requires the player to touch an object for a certain amount of time.

<p align="center">
    <img alt="TouchStep.png" src="../../images/steps/TouchStep.png"/>
</p>

## Usage

Use this step if you want a user to touch an object for a certain amount of time. 

**Example**: To determine the type of a wall, the user must touch the wall for at least three seconds.

## Parameters

### Touchable Trigger Event

An instance of the `TriggerEvent` class that is used to define an event that is raised when the player touches the object to complete the step.

### Touch Duration

The minimum duration that the player must touch the object to complete the step, in seconds.

### Sound On Touch Enter

An ``AudioSource`` that will be played when the player enters the trigger area of the object to touch.

### Sound On Touch Exit

An ``AudioSource`` that will be played when the player exits the trigger area of the object to touch.


### Sound On Touch Complete

An ``AudioSource`` that will be played when the player completes the touch interaction.

### Triggers

A list of colliders that will be considered as triggers for this step.

### Trigger Hierarchy

A list of GameObjects whose colliders in their hierarchies will be considered as triggers for this step.

### Trigger Tags

A list of atom tags whose colliders will be considered as triggers for this step.

### Trigger Hierarchy Tags

A list of atom tags whose colliders in their hierarchies will be considered as triggers for this step.