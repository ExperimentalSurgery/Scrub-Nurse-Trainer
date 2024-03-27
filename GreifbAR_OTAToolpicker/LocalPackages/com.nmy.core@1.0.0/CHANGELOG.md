# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0] - 2021-11-11
### Changed
- Version bump to 1.0.0 release version.
- Marked FindObjectsOfType<T>() as obsolete.

## [0.9.13-preview] - 2021-09-28
### Fixed
- Fixed missing UNITY_2020_1_OR_NEWER page calc logic

## [0.9.12-preview] - 2021-09-28
### Changed
- Editor utility StartupView now limited to 100 entries per page due to performance issues in large scope projects.

## [0.9.11-preview] - 2021-09-28
### Added
- Editor utility for highlighting the states of StartupBehaviours and ASBs in the inspector.
- Editor window for displaying the states of ALL StartupBehaviours and ASBs.
- Editor Preferences added: Preferences/NMY/Startup/Highlight Startups and Activatables (bool)

## [0.9.10-preview] - 2021-09-22
### Changed
- iTween, StaticUtils.SetAlphaRecursively: basic support for URP (="_BaseColor" property)

## [0.9.9-preview] - 2021-05-06
### Added
- StaticUtils: Added FindObjectsByName(), which also supports substrings (=default).
### Fixed
- RadioBehaviour.SubChild must rebuild internal dictionary indices.
- RadioBehaviour.ActivateCurrent: Fixed range check.

## [0.9.8-preview] - 2020-05-19
### Added 
- Updated documentation.
### Fixed
- Typo in package.json.
- Renamed documentation file to conform to Unity package layout.
### Changed
- Added NMY namespace to all runtime scripts in Core package (except imported ones like iTween, Console, etc).
- Added NMY.Editor namespace to all editor scripts in Core package.
### Removed
- Moved AbstractMenuRenderer_TriggerManually.cs back to _Legacy

## [0.9.7-preview] - 2020-03-27
### Changed
- Documentation for SingletonStartupBehaviour.
### Fixed
- Moved suitable common Animator assets from Samples~ to Core/Runtime.
### Removed
- Removed obsolete ObjectActivator, and associated utility.
- Removed ObjectFlasher_TriggerManually.
- Removed obsolete ActivatableTimingParameters.

## [0.9.6-preview] - 2020-02-27
### Changed
- Added static method HSVColor.Slerp().
- Updated documentation and README.
- Replaced AssetStore version of the JSONObject library by the more 
  up-to-date GitHub version (commit hash f29a13d2da107405d059e564bfda3b2ca7767e4c).

## [0.9.5-preview] - 2020-02-26
### Added
- Added JSONObject v1.7 from https://assetstore.unity.com/packages/tools/input-management/json-object-710

## [0.9.4-preview] - 2020-02-25
### Added
- Added HSVColor.cs script.
- Added extension method Color.Slerp for interpolating colors in the HSV color space.
- Added documentation to the Documentation~ folder.

## [0.9.3-preview] - 2020-02-24
### Changed
- Removed GUILayer components in all sample scenes.
- Added a simple UI to ScaleActivatable sample scene.
- Fixed SingletonStartupBehaviour sample scene.

## [0.9.2-preview] - 2020-02-10
### Changed
- Changed version to 0.9.2-preview to conform to package validation suite results.

## [0.9.1] - 2020-02-06
### Changed
- Moved the sample scenes from subfolder "Runtime/Startup/Examples" to "Samples~".
- Added sample scenes to the package.json file to make them installable via the package manager.

## [0.9.0] - 2020-02-05
### Added
- Initial development version.

