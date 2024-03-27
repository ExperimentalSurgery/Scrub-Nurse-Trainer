# NMY Core

The NMY Core package contains a number of basic scripts and components.

## Console.cs
Console.cs contains a console to display Unity's debug logs in-game. You can 
use it by adding it to any game object in the scene.
The console can then be activated by pressing the configured toggleKey or by 
painting a circle on the screen using touch or the mouse.

The script is based on https://github.com/mminer/consolation
It was extended by the "paint circle to activate" functionality taken from 
https://assetstore.unity.com/packages/tools/utilities/ingame-logs-viewer-mt-141657

## Easing.cs
Contains a variety of standalone easing functions.

## iTween.cs
A modified and optimized version of the tweening library. The original library 
can be found in the Asset Store or at 
http://www.pixelplacement.com/itween/gettingstarted.php.

## StaticUtils.cs
A collection of useful static utitlity methods.

## HSVColor 
A struct representing a color in the HSV colorspace.
Contains an extension method `Slerp()` for the `Color` class to interpolate 
colors in the HSV colorspace.
Example: 
```C#
Color c = Color.white.Slerp(Color.black, 0.5f);
```
There is also a static method `Slerp()` on the `HSVColor` class so that the 
call to Slerp is somehow analogous to other Lerp calls:
```C#
Color c = HSVColor.Slerp(Color.white, Color.black, 0.5f);
```
Note that we cannot extend the `Color` class with the static `Slerp()` method
because C# does not allow adding static extension methods to a class.

## JSONObject
A C# class for encoding/decoding JSON data into a usable runtime data structure.
This is taken from https://assetstore.unity.com/packages/tools/input-management/json-object-710
and integrated into the Core package until JSON Object itself is available via the
package manager.
Documentation can be found in Runtime/JSON/readme.txt or online at
https://github.com/mtschoen/JSONObject

## StartupBehaviour and ActivatableStartupBehaviour
Todo. For now have a look at the sample scenes.

## Contact
NMY Mixed-Reality Communication GmbH
p.eschler@nmy.de
https://www.nmy.de