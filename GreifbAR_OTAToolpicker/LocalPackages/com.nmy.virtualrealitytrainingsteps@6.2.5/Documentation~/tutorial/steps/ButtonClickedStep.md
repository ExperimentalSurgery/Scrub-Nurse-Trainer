# ButtonClickedStep

A training step where the user must click a button.

<p align="center">
    <img alt="ButtonClickedStep.png" src="../../images/steps/ButtonClickedStep.png"/>
</p>

## Usage

Use this step to wait until the user clicked a button. 
For example, this button could be a confirmation, next, or okay button.

This component works only with **one** button. If you need the ability to click more than one button, and
depending on what you want to do when this button is clicked, use [ButtonDecisionStep](ButtonDecisionStep.md).

## Parameters

### Button

A reference to the button that must be clicked.

### Set Interactable At Step Start

Whether to set the interactable state of the button at the start of the step.

### Set Not Interactable At Step Finish

Whether to set the button to not be interactable at the end of the step.

### Text

Optional. A reference to a text component to display a multiplayer wait counter. If set, it shows the ratio between
finished and total clients in the form X/Y (e.g. 1/4).

