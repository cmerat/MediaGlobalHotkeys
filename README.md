# MediaGlobalHotkeys
A utility for Windows that converts specific keystrokes to Media keys for controlling music and video playback.

## Hotkeys
Here are the hotkey conversions supported by the app:
* Ctrl-Alt-Home: MediaPlayPause
* Ctrl-Alt-End: MediaStop
* Ctrl-Alt-PageUp: MediaPreviousTrack
* Ctrl-Alt-PageDown: MediaNextTrack
* Ctrl-Alt-Up: VolumeUp
* Ctrl-Alt-Down: VolumeDown

## Firefox Support
As Firefox addons do not support global hotkeys, this app will send media keystrokes directly to the Firefox process using Windows Messaging. While this is not a foolproof method, it works well enough.
