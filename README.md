# VRChat Avatar OSC

A lightweight Windows utility for monitoring, controlling, and saving VRChat avatar OSC parameters.

This application listens for OSC parameters sent by VRChat, keeps track of their current values, and lets you control them through simple UDP commands from external applications such as Streamer.bot, Twitch integrations, custom scripts, or your own software.

It also includes a built-in log viewer, parameter logging controls, configurable network ports, and the ability to save and restore avatar parameter states.

---

## Features

* Monitor all OSC parameters sent by VRChat, or choose which parameters to monitor
* Toggle Boolean parameters
* Set Integer and Float parameters
* Randomize Integer and Float parameters within a specified range
* Save the current state of avatar parameters and load it later
* Simple UDP command interface for automation

---

## Requirements

* Windows
* VRChat with OSC enabled
* A VRChat avatar with parameters

---

## How It Works

The application listens for OSC messages from VRChat and automatically builds a list of every parameter it receives.

Once a parameter has been seen, its current value is remembered. External programs can then send UDP commands to this application, which forwards the appropriate OSC messages back to VRChat.

This makes it easy to connect Twitch redeems, Streamer.bot actions, hardware buttons, custom software, or other automation systems to your avatar.

---

## Tabs

### Log

Displays incoming OSC parameter changes in real time.

The log automatically limits itself to prevent excessive memory usage.

---

### Parameter Logging

By default, every detected parameter is logged.

You can instead choose to log only selected parameters.

Features include:

* Automatically discovering parameters
* Manually adding parameters
* Selecting exactly which parameters should appear in the log

---

### Settings

Configure the application's behavior.

Settings include:

* VRChat Receive Port
* VRChat Send Port
* Command Port
* Minimize to System Tray
* Close to System Tray

Port changes take effect after restarting the application.

---

## Commands

Commands are sent as UTF-8 text over UDP to the configured Command Port.

### Toggle Booleans

```
toggle <parameter>
```

Example:

```
toggle Wings/Enabled
```

Toggles a Boolean parameter.

---

### Set Floats and Integers

```
set <parameter> <value>
```

Examples:

```
set Outfit/Color 3
```

```
set Face/Smile 0.75
```

Sets an Integer or Float parameter.

---

### Randomize Integers or Floats

```
random <parameter> <minimum> <maximum>
```

Examples:

```
random Outfit/Color 1 4
```

```
random Face/Smile 0.0 1.0
```

Selects a random value within the specified range.

For integers, the current value is never selected again if another value is available.

---

### Save

```
save
```

Stores the current avatar parameter values in `saved-state.json`, so that you can load them again later.

Only Boolean, Integer, and Float parameters are saved.

---

### Load

```
load
```

Reloads the previously saved parameter values and sends them back to VRChat.

---

## Files

### settings.json

Stores:

* Logging preferences
* Selected parameters
* Network ports
* Window behavior settings

---

### saved-state.json

Stores saved avatar parameter values.

---

## Typical Workflow

1. Start VRChat.
2. Launch VRChat Avatar OSC.
3. Load your avatar.
4. Allow VRChat to send parameter updates.
5. Use your preferred automation software (such as Streamer.bot) to send UDP commands to this application.
6. Watch your avatar respond instantly.

---

## Default Ports

| Purpose              | Default Port |
| -------------------- | -----------: |
| VRChat → Application |         9001 |
| Application → VRChat |         9000 |
| Command Listener     |         8765 |

---

## Example Streamer.bot Integration

Configure a **UDP Broadcast** action that sends text such as:

```
toggle Wings/Enabled
```

or

```
random Outfit/Color 1 4
```

to the application's Command Port.

No OSC knowledge is required—Streamer.bot only needs to send plain text commands.
