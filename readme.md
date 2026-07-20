# Logitech Joystick Controller

A .NET console app that reads a Logitech Extreme 3D Pro joystick on Linux (via the kernel's `joydev` interface, `/dev/input/jsN`) and renders live flight-instrument readings — pitch, roll, yaw, throttle, hat position, and button states — directly in the terminal.

## Requirements

- Linux with a `/dev/input/jsN` joystick device exposed by `joydev`
- .NET 10 SDK
- A user account in the `input` group (or root) so the device node can be opened:
  ```
  sudo usermod -aG input $USER
  ```
  (log out/in for the group change to take effect)

## Running

```
dotnet run
```

By default the app looks for a device whose name matches "Logitech Extreme 3D"; if none is found it falls back to the first available `/dev/input/jsN` device.

### Options

```
dotnet run -- [--device /dev/input/jsN] [--raw] [--list-devices]
```

- `--device /dev/input/jsN` — use a specific device path instead of auto-detecting
- `--raw` — print raw axis/button index and value changes as they occur (useful for identifying axis/button mappings on other controllers)
- `--list-devices` — list all detected `/dev/input/jsN` devices and exit
- `-h`, `--help` — show usage

On startup (non-raw mode), keep the stick centered and let go of the twist axis so the app can calibrate roll/pitch/yaw against the stick's actual resting position. Press `Ctrl+C` to quit.

## Project layout

- [Program.cs](Program.cs) — entry point, CLI arg parsing, main render loop
- [Joystick/](Joystick/) — low-level device access
  - [JoystickDevice.cs](Joystick/JoystickDevice.cs) — resolves device names to `/dev/input/jsN` paths via `/proc/bus/input/devices`
  - [JoystickReader.cs](Joystick/JoystickReader.cs) — background thread parsing the joydev binary event protocol
  - [JoystickState.cs](Joystick/JoystickState.cs) — thread-safe snapshot of current axis/button state
- [Instruments/](Instruments/) — flight-instrument model and display
  - [ControllerState.cs](Instruments/ControllerState.cs) — maps raw axes to pitch/roll/yaw/throttle/hat, per the Extreme 3D Pro's axis layout
  - [AxisCalibration.cs](Instruments/AxisCalibration.cs) — per-axis centering and auto-expanding min/max normalization
  - [ConsoleRenderer.cs](Instruments/ConsoleRenderer.cs) — in-place console redraw of the instrument readout

## Notes

The axis mapping in [AxisMap](Instruments/ControllerState.cs) is specific to the Logitech Extreme 3D Pro (USB `046d:c215`). For a different controller, use `--raw` to discover its axis/button indices.
