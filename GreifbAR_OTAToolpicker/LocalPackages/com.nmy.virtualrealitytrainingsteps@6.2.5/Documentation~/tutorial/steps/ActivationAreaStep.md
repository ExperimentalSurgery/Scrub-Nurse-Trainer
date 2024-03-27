# ActivationAreaStep

A training step that waits for a collider to enter a trigger area.

<p align="center">
    <img alt="ActivationAreaStep.png" src="../../images/steps/ActivationAreaStep.png"/>
</p>

## Usage

Use this component when you want certain trigger colliders to be at a specific place. Some examples:

- The user should move to a certain place
    - Head GameObject of the XR Origin contains a sphere collider that enters the BoxCollider of a TriggerEvent
      GameObject.
- Parts of the user (e.g. one or both hands) should be under an object
    - Each hand has a sphere collider, under the object is a triggerEvent GameObject with a SphereCollider and a
      TriggerEvent component

## Parameters

### Trigger Event

A reference to a [TriggerEvent]() component that has events for when a collider enters or exits the trigger area
defined by that component. This step registers event handlers for these events.

<p align="center">
    <img alt="ActivationAreaStep-TriggerEvent.png" src="../../images/steps/ActivationAreaStep-TriggerEvent.png"/>
</p>

