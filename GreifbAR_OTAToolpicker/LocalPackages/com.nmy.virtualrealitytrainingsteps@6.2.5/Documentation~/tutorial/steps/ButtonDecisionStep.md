# ButtonDecisionStep

A training step that waits for a UI ``Button.onClick`` event to add the corresponding step into the step sequence.

<p align="center">
    <img alt="ButtonDecisionStep.png" src="../../images/steps/ButtonDecisionStep.png"/>
</p>

## Usage

Use this step if you want to branch your training depending on certain button decisions.

**Example**: A character wants to tell the user two stories, A and B. It lets the user decide which story they want to 
hear by pressing the corresponding button. Only the step that are associated with the clicked button will be added
to ``NextSteps``.

## Parameters

### Step Tuples

A list of tuples that contains the possible steps that can be taken based on the button that can be clicked.
