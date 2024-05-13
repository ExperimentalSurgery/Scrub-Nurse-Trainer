OTA Tool Picker - Demonstrator

HOTKEYS:
[] - 

HOW TO:
Calibrate Instrument Marker Pose

![image](https://github.com/ExperimentalSurgery/OTA_Tool_Picker/assets/12296181/abc664b3-cd9a-4731-a29e-d82d6efc8a6c)

1. Open in the hierarchy tab the root object [InstrumentMarkerController-Varjo] and select one of the child objects.
2. In the inspector tab, scroll down to the [OTA Varjo Marker] component. Here are two values of interest: [Marker Anker Pos. Offset] and [Marker Anker Rot. Offset]
3. Enter [Play] mode and go to level 1 (learning). Present the marker to the Varjo and begin tweaking the values until the model matches.
4. Right-click the [OTA Varjo Marker] component titlebar and select [Copy Component].
5. Leave the [Play] mode and right-click the [OTA Varjo Marker] component again. This time select [Paste Component Values]
6. Repeat for each instrument and save the scene.

![image](https://github.com/ExperimentalSurgery/OTA_Tool_Picker/assets/12296181/70fe0662-d2a7-4608-a340-f025aad0f3be)
