# Scrub Nurse Trainer - Prototype

![Header](https://github.com/user-attachments/assets/66aa0f57-b936-47fa-a3a7-1b56a5000a3f)

This repository contains a prototype implementation of the Scrub Nurse Trainer as explored in the paper "Surgical Instrument Trainer: Design and Evaluation of a Mixed Reality System for the Training of Operating Theatre Assistants" 
Due to very limited hardware support (This prototype can only be run on Varjo XR3 headsets) and the need for specific surgical instruments, as well as 3D-printed markers, running this project is not trivial.
If you need further assistance, please don't hesistate to contact [our team](https://www.experimental-surgery.de/digitalsurgery/)!

## Requirements

#### Software

- Unity 2022.3 LTS
- Varjo XR Software

#### Hardware

- Varjo XR3 or XR4 headset
- Surgical Instruments from [KLS Martin](https://www.klsmartin.com/en/products/surgical-instruments/) <details> <summary>List of instrument names</summary>
- Langenbeck Haken
- Ligaturklemme DeBakey
- Nadelhalter Masson
- Peritoneumklemme Miculicz
- Pinzette Mittelbreit
- Präperationsklemme Overholt Geissendoerfer
- Chirurgische Schere
- Metzenbaumschere
- Skalpellgriff Nummer 3
- Wundhaken Roux
- Schale 
</details>
- 3D printed marker cubes, with glued on Varjo Markers (please contact us if you need the 3D print files!)

## Usage

Open the project in the Unity Editor, and open the Scene ```GreifbAR_OTAToolpicker/Assets/_OTAToolpicker/Scenes/OTAToolpicker_main.unity```
The program will now guide you through the steps.

----------------------------------------------------------------------------------------------

HOTKEYS:

[D] - Toggle [marker found] and [marker lost] sound effect playback

[R] - Reset the table calibration

[T] - (hold down) Continuously scan the table marker. Fix the Table pose on button release.

[O] - Toggle the table occlusion box (for the virtual surgeon)

[S] - Toggle the virtual surgeon - move the model up and down with the arrow keys

[Z] - Reload scene

----------------------------------------------------------------------------------------------

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

## License

All parts of the code contained in this repository are licensed under the MIT license, **exept** for the for the following folders and all files contained within them:

- ```GreifbAR_OTAToolpicker/Assets/Plugins```
- ```GreifbAR_OTAToolpicker/Assets/LIV ```
- ```GreifbAR_OTAToolpicker/Assets/Missing Reference Finder ```
- ```GreifbAR_OTAToolpicker/Assets/TextMesh Pro ```
- ```GreifbAR_OTAToolpicker/LocalPackages/com.nmy.external.unitask%402.5.0 ```
- ```GreifbAR_OTAToolpicker/Packages/com.ptc.vuforia.engine-10.20.3.tgz ```

These folders have a propriatary license and you are **not allowed to publish, distribute, modify or use them commercially!** These files are only provided for compatibility purposes and to execute the program for research purposes!

## Credits

Citation coming soon
