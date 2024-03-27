# ToggleClickedStep

A training step that allows users to click a toggle to finish the step.

<p align="center">
    <img alt="ToggleClickedStep.png" src="../../images/steps/ToggleClickedStep.png"/>
</p>

## Usage

Use this step to wait until the user clicked a button.

**Example:** This toggle could be a tab in a UI menu.

## Parameters

### Toggle

A reference to the toggle that must be clicked.

### Set Toggle Interactable At Step Start

Whether to set the interactable state of the toggle at the start of the step.

### Set Toggle Not Interactable at Step Finished

Whether to set the toggle to not be interactable at the end of the step.

### Text
Optional. A reference to a text component to display a multiplayer wait counter. If set, it shows the ratio between
finished and total clients in the form X/Y (e.g. 1/4).