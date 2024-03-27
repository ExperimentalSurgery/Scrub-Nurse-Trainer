# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.1] - 2023-05-23
### Fixed
- AutoHideCursor: decouple from TouchManager (i.e., don't spawn one unnecessarily)
- BBInputDelegate: query Singleton without enforcing instance creation
- Unified package name in README.

## [2.0.0] - 2022-03-22
### Changed
- Added root namespace tag to .asmdefs
### Fixed
- Fixed incorrect default values for new ObjectRotaterTouch features.
- Fixed namespaces for Samples~ scripts.

## [1.0.1] - 2022-03-11
### Fixed
- Namespace fixes for examples.
- Added Package Manager documentation link.

## [1.0.0] - 2021-11-11
### Changed
- Version bump to 1.0.0 release version.
- Extended ObjectRotaterTouch for object translation.
- ObjectRotaterTouch can now rotate/scale around center hit point, instead of transform pivot. This is now the default.
- Changed "Layer required" message from Error to Warning, as it is auto-corrected.
### Removed
- Removed obsolete GUITexture cursor prefab.

## [0.9.5-preview] - 2021-06-17
### Fixed
- Resized checkerboard_4x4_repeat to a clean 512x512.
- Fix and cleanup of references materials/textures.
- Typo in package.json.
### Changed
- Consolidated ExplorerCamTouch and SmoothFollowExplorerCamTouch into a single script.
- Moved all runtime scripts into namespace NMY.TouchInteraction3D
- Moved editor script CheckForTouchableLayers into namespace TouchInteraction3D.Editor
### Removed
- Deleted now obsolete SmoothFollowExplorerCamTouch.cs.

## [0.9.4-preview] - 2020-03-27
### Changed
- Changed package maintainer.
- Unified naming scheme for hardcoded "NMY_HASSTEREO3D" to "NMY_HAS_STEREO3D"

## [0.9.3-preview] - 2020-02-25
### Changed
- Added missing com.unity.ugui dependency to package.json and the missing 
  UnityEngine.UI assembly reference to the runtime assembly definition.
- Updated com.nmy.core dependency to version 0.9.4-preview.

## [0.9.2-preview] - 2020-02-10
### Changed
- Changed version to 0.9.2-preview to conform to package validation suite results.

## [0.9.1] - 2020-02-05
### Added
- Now checking for layers "touchableObjects" and "controlObjects" on every domain reload via the new editor script CheckForTouchableLayers.cs
- Automatically adding any of the two layers to "ProjectSettings/TagManager.asset" if missing.
### Removed
- Removed the old check in BBInputDelegate.cs which was only executed in play mode and did not add layers automatically.

## [0.9.0] - 2020-02-05
### Added
- Initial development version.

