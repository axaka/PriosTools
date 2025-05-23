## [1.2.1] - 2024-04-30
###  Spreadsheet improvements
- Full spreadsheet url should now be pasted instead of just the id
- Added toggle to save class file

## [1.2.0] - 2024-04-30
###  Added support for google spreadsheets
- Google spreadsheet tabs can now be downloaded as .json files
- Based on https://assetstore.unity.com/packages/tools/utilities/google-sheet-to-json-90369 from Trung Dong
- A matching .cs class file will be generated that has has the ability to load the data

## [1.1.3] - 2024-04-30
### PriosUIAnimator bugfix
- preventIfRunning and preventIfAlreadyCorrect should no longer prevent the initial value from being set

## [1.1.2] - 2024-04-30
### PriosEvent bugfix
- Prevent instance creation when exiting play mode

## [1.1.1] - 2024-04-30
### Used Keys in the scene is now shown
- A list of all known used keys in the current scene is now shown on the bottom of PriosEventReciever and PriosEventTrigger

## [1.1.0] - 2024-04-30
### Added PriosSceneManager
- Use this to change to a new scene or restart current scene.

## [1.0.3] - 2024-04-29
### Changed the default slide animation
- The default slide animation now has under- and over-shoot

## [1.0.2] - 2024-04-29
### Anchor Around UI Object
- Added editor script to anchor UI Object perfectly around it
- Default Keybind is CTRL + SHIFT + Q

## [1.0.1] - 2024-04-29
### Slider support
- Added Slider support for PriosEventReciever

## [1.0.0] - 2024-04-29
### First Release
- Initial release of the project with basic functionality.
- PriosEvents
- PriosEventTrigger
- PriosEventListener
- PriosUIAnimator