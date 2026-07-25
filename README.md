# VRCAviTrig

[![Latest Release](https://img.shields.io/github/release-date/Erallie/VRCAviTrig?display_date=published_at&style=for-the-badge&label=Latest%20Release)](https://github.com/Erallie/vrchat-avatar-osc/releases/latest)
![Latest Downloads](https://img.shields.io/github/downloads/Erallie/VRCAviTrig/latest/total?sort=semver&style=for-the-badge&label=Latest%20Downloads)
![All-time Downloads](https://img.shields.io/github/downloads/Erallie/VRCAviTrig/total?style=for-the-badge&label=All-time%20Downloads)
<br>
[![Our Discord](https://img.shields.io/discord/1102582171207741480?style=for-the-badge&logo=discord&logoColor=ffffff&label=Our%20Discord&color=5865F2)](https://discord.gozarproductions.com)
[![Our Other Projects](https://img.shields.io/badge/Our%20Other%20Projects-%E2%9D%A4-563294?style=for-the-badge&logo=data%3Aimage%2Fwebp%3Bbase64%2CUklGRu4DAABXRUJQVlA4WAoAAAAQAAAAHwAAHwAAQUxQSGABAAABgFtbm5volyZTA%2BtibzK2H0w5sDkmhe3GmxrwxGg0839r%2FvkkOogIBW7bKB0c4%2BARYihzIqfd6dfO%2B%2B3XtHsq4jJhlIvcDRcgNB%2FeieQETorBHgghRtUYqwDs%2B4U4IpcvUB%2BVUPSK54uEnTwsUJoar2DeMpzLxQpeG5DH8lxyyfLivVYAwPBbkWdOBg3qFlqiLy679iHy9UDKMZRXmYxpCcusayTHG01K%2FEtatYWuj7oI9hL4BxsxVwhoP2mlAJJ%2BuuAflc6%2BEUCQTCX9EV87xBR2H75NxLZSpWiwzqdIm7ZO7uB3oEgZKbD9Nt3EmHweEPH1t1GNsZUbKeisiwjyTm5fA3SO1yCrADZXrV2PZQJPL1tjN4%2BxUL9ie1mJobzOnDwSx6ILiF%2FW%2BTUR4tcHx0UaV75JXC1a4g6Ky5dLcTSuy9q4HhTieF64Hy1A3GHB8gLLK2e92feuqnbfPK8IVlA4IGgCAACwDQCdASogACAAPk0cjEQioaEb%2BqwAKATEtgBOl7v9V3sHcA2wG4A3gD0APLP9jX9n%2F2jmqv5AZRh7J%2BN2fOx22iE%2F4TUsecFmY%2BSf1r%2BAP%2BTfzT%2FXdIB7KX7MtdIGr1A8H0jmrrfZvqButwOaYcLWYNRq5QgAAP7%2F%2FmIMpiVNn67QXpM1rrDmRS8Nr%2F6dhD%2Bq5e%2BM%2BAtUP1%2FxOj85Ol5y3ebjz%2BpHoOf%2FWW8a%2F2ojUaKVDkVqof%2Bv4f0f6ud8i58wusz%2Fyrj%2F%2BwnM3q0769dvK%2F%2BQe04xL49tkb9t6ylCqqezZtZGuGLJ%2F5iUrPqdYc%2F8VbYZfP%2FOpZP%2F4X4q%2BqS4gPOxzdINOe5PGv%2F0TS%2FJRf4LlFrFkrWtxlS8n40grV%2BKUu%2FiwzdQzImvwH81FxL1bZyTSsrYwMku1Pk9StTtWNjSR8ZWEYBH9eTn%2FvBERii5XaWOPJ%2FFVXtVQGbv%2BFRW5jbo9tfFDu%2BDHHf8LbgUd%2F8W8Id1AehBtRNsLQWbADmvF1QJU8x5tw%2FtTUwIoSaa%2F2jkcvyVHkAsb2qoIh1KF1pPdae%2BZaqjydy6nUa9agjrDk1G4pMhEUhH%2BV%2FIUe49MjhR%2FuxyFmwQ8dDogMyQ%2BdcSBa56Lwt1wyJ%2F22%2F5O98r6q6wiM63HyaYONd36W7br%2F0%2F6y2DZ3irAddj%2FRxntvr%2FbbChSYXAfEbO%2FD0G%2FFbMFqTHypodt9T6dAx%2BUjJYfHzFf%2FM3Ec%2FAtwbjc2gka6urN1MlSLb2VTS9Q5r8fkDzxZz6vu1OYUPUB1UFMIhYGvMATbxxoTmVhvpovzAc%2F8nbOjw3wAAA)](https://github.com/Erallie)
[![Donate](https://img.shields.io/badge/Donate-%24-563294?style=for-the-badge&logo=ko-fi&logoColor=FFFFFF&color=FF6433)](https://www.ko-fi.com/GozarProductions)

---

A lightweight Windows utility for triggering VRChat avatar OSC parameters.

This application listens for OSC parameters sent by VRChat, keeps track of their current values, and lets you control them through simple UDP commands from external applications such as Streamer.bot, Twitch integrations, custom scripts, or your own software.

It's perfect for making Twitch redemptions trigger features on your avatar.

It also includes a built-in log viewer, parameter logging controls, configurable network ports, and the ability to save and restore avatar parameter states.

## Features

* Monitor all OSC parameters sent by VRChat, or choose which parameters to monitor
* Toggle Boolean parameters
* Set Boolean, Integer, and Float parameters directly
* Randomize Integer and Float parameters within a specified range
* Save the current state of avatar parameters and load it later
* Simple UDP command interface for automation

## Requirements

* Windows
* Software capable of sending UDP packets (e.g. [Streamer.bot](https://streamer.bot/)).
* VRChat with OSC enabled
* A VRChat avatar with parameters

## Installation

1. Go to the **[latest release](https://github.com/Erallie/VRCAviTrig/releases/latest)**.
2. Download **`GozarProductions.VRChatAvatarOSC-win-Setup.exe`** from the **Assets** section.
3. Run the installer and follow the installation prompts.
4. Launch **VRChat Avatar OSC** from the Start Menu or desktop shortcut.

Future updates can be installed directly from within the application by using the **Updates** tab.

## How It Works

The application listens for OSC messages from VRChat and automatically builds a list of every parameter it receives.

Once a parameter has been seen, its current value is remembered. External programs can then send UDP commands to this application, which forwards the appropriate OSC messages back to VRChat.

This makes it easy to connect Twitch redeems, Streamer.bot actions, hardware buttons, custom software, or other automation systems to your avatar.

**IMPORTANT NOTE:** You must *first* call `set` or `load` or **toggle the parameter manually** on your avatar for each parameter you want to *then* change with `toggle`, `random`, or `save`.

## Tabs

### Log

Displays incoming OSC parameter changes in real time.

The log automatically limits itself to prevent excessive memory usage.

### Parameter Logging

By default, every detected parameter is logged.

You can instead choose to log only selected parameters.

Features include:

* Automatically discovering parameters
* Manually adding parameters
* Selecting exactly which parameters should appear in the log

### Save Parameters

By default, the application does not save any parameters when the `save` command is used.

Instead, you choose exactly which parameters should be included in saved states.

Parameters can be specified in two ways:

- **Exact parameter names**, such as:
  ```
  Outfit/Color
  Wings/ToggledOn
  ```

- **Parameter prefixes (directories)**, such as:
  ```
  Outfit/
  FT/
  ```
  Any parameter whose name begins with one of these prefixes will be saved. This can be a directory or just a common parameter prefix.

This makes it easy to save only the parameters that should persist between sessions while ignoring temporary tracking data or other parameters that should not be restored, such as face tracking.

### Settings

Configure the application's behavior.

Settings include:

* VRChat Receive Port
* VRChat Send Port
* Command Port
* Minimize to System Tray
* Close to System Tray

Port changes take effect after restarting the application.

## Commands

Commands are sent as UTF-8 text over UDP to the configured Command Port.

### Toggle Booleans

```
toggle <parameter>
```

Example:

```
toggle Tiara
```

Toggles a Boolean parameter.

### Set Booleans, Integers, and Floats

```
set <parameter> <value>
```

Examples:
```
set Tiara false
```

```
set Outfit/Color 3
```

```
set Face/Smile 0.75
```

Sets a Boolean, Integer, or Float parameter.

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

### Save

```
save
```

Stores the current values of all configured save parameters in `saved-state.json`, so that you can load them again later.

Only Boolean, Integer, and Float parameters are supported.

The list of parameters to save is configured from the **Save Parameters** tab. Parameters can be specified individually or by using parameter prefixes (such as `Outfit/` or `FT/`).

### Load

```
load
```

Reloads the previously saved parameter values and sends them back to VRChat.

## Files

### settings.json

Stores:

* Logging preferences
* Selected parameters
* Network ports
* Window behavior settings

### saved-state.json

Stores saved avatar parameter values.

## Typical Workflow

1. Start VRChat.
2. Launch VRChat Avatar OSC.
3. Load your avatar.
4. Allow VRChat to send parameter updates.
5. Use your preferred automation software (such as [Streamer.bot](https://streamer.bot/)) to send UDP commands to this application.
6. Watch your avatar respond instantly.

## Default Ports

| Purpose              | Default Port |
| -------------------- | -----------: |
| VRChat → Application |         9001 |
| Application → VRChat |         9000 |
| Command Listener     |         8765 |

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

## Support

If you have any problems with the app or want to request a feature, please create an [issue](https://github.com/Erallie/VRCAviTrig/issues), and I will try to get to it as soon as I can!
