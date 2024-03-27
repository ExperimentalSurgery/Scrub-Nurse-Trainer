# VirtualAssistantMoveStep

A training step that moves the virtual assistant to a target position.

<p align="center">
    <img alt="VirtualAssistantMoveStep.png" src="../../../images/steps/VirtualAssistantMoveStep.png"/>
</p>

## Usage

Use this step to move the virtual assistant to a new position. You can specify if the virtual assistant should look
at the player during move or after move and how long the movement will last.

The virtual assistant will move according to the movement data specified in the [move data](#move-data) property.

## Parameters

### Move Data

The ``VirtualAssistantMoveData`` object that contains the details of the movement action performed by the virtual assistant.

| Fields                     | Descriptions                                                                                                                                                                                                       |
|----------------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Destination                | The object in the virtual environment that the virtual assistant should move towards. The virtual assistant will move along a straight line from its current position to this destination.                         |
| Duration                   | The duration of the movement, in seconds. This determines how long it will take for the virtual assistant to move from its current position to the destination.                                                    |
| Look At Player During Move | Specifies whether the virtual assistant should look at the player while moving. If this is true, the virtual assistant will maintain eye contact with the player throughout the movement.                          |
| Look At Player After Move  | Specifies whether the virtual assistant should look at the player after completing the movement. If this is true, the virtual assistant will turn its head to face the player once it has reached the destination. |
| Animation Curve            | The curve to use for the movement animation. This determines the speed at which the virtual assistant moves from its current position to the destination.                                                          |
