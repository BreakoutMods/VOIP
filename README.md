# BreakoutMods VOIP

Part of the BreakoutMods Valheim modding suite.

Experimental BepInEx mod that adds proximity voice chat to Valheim using the existing Valheim network session.

The goal is simple server-hosted RP voice chat: clients capture microphone audio, the dedicated server relays voice only to nearby players, and no separate public VOIP server address is required.

## Status

Early MVP. It builds and the core voice path exists, but this is not production-polished yet.

Implemented:

- Push-to-talk voice capture, default key `V`
- Optional voice activation
- Opus encoding through embedded Concentus C# source
- Valheim routed RPC transport
- Server-side proximity relay
- Server-authoritative voice settings sync
- Spatial Unity audio playback
- Jitter-buffered playback using streaming `AudioClip` output
- Rate-limited diagnostics for malformed voice packets and settings sync
- Client / Server / Shared source layout

Not done yet:

- Input device selection
- HUD transmit/receive indicator
- Player mute/deafen UI
- Polished config migration/versioning

## Install

Install `ValheimVoip.dll` on:

- the dedicated server
- every client that should use voice chat

Recommended plugin folder:

```text
BepInEx/plugins/ValheimVoip/ValheimVoip.dll
```

No separate `Concentus.dll` is required. The Opus codec source is compiled directly into `ValheimVoip.dll`.

## How It Works

Clients do not configure a separate voice server per world. When a player joins a Valheim server:

1. The client captures local microphone audio.
2. The client encodes short mono frames with Opus.
3. The client sends voice frames to `ZNet.GetServerPeer()` using Valheim routed RPC.
4. The server receives frames and relays them only to peers within the configured proximity radius.
5. Recipients decode and play speech as spatial audio at the speaker position.

The server periodically syncs voice session settings such as radius, bitrate, sample rate, and frame size. Local client config is still used for personal settings such as push-to-talk, voice activation, and playback volume.

## Configuration

BepInEx generates the config after the first launch.

Common client settings:

- push-to-talk key
- voice activation enabled/disabled
- voice activation threshold
- playback volume
- jitter buffer target duration
- maximum jitter buffer duration

Common server settings:

- voice enabled/disabled
- proximity radius
- full-volume radius
- Opus bitrate
- sample rate
- frame duration

Server settings are authoritative during multiplayer sessions.

## Build

This project targets `.NET Framework 4.6.2` because Valheim/BepInEx run on Unity Mono.

Expected local layout for `build.ps1`:

```text
Valheim dedicated server/
  BepInEx/
  valheim_server_Data/
  Modding/
    valheim-voip/
```

Build with:

```powershell
.\scripts\install-deps.ps1
.\build.ps1
```

The script:

- compiles with the .NET Framework `csc.exe`
- references assemblies from the adjacent Valheim dedicated server install
- compiles locally installed Concentus source into the plugin DLL
- deploys `ValheimVoip.dll` to `BepInEx/plugins/ValheimVoip`

If the server or game has already loaded the DLL, deployment may write `ValheimVoip.dll.pending`. Stop Valheim and rerun the build or copy the pending DLL over the loaded one.

SDK-style project builds may also work if you have a compatible .NET SDK installed:

```powershell
dotnet build .\ValheimVoip.csproj
```

The SDK build path is mainly for IDE support. `build.ps1` is the reference local build path.

## Source Layout

```text
src/
  Client/
    VoiceCapture.cs
    VoiceClient.cs
    VoicePlayback.cs
  Server/
    VoiceServer.cs
  Shared/
    AudioMath.cs
    OpusVoiceCodec.cs
    ValheimVoipPlugin.cs
    VoiceNetwork.cs
    VoicePacket.cs
    VoiceRuntimeSettings.cs
    VoiceSettings.cs
```

```text
libs/
  README.md
scripts/
  install-deps.ps1
```

`Client` owns microphone capture, local send behavior, and playback.

`Server` owns proximity relay and server settings broadcast.

`Shared` owns plugin wiring, RPC registration/routing, packet models, runtime settings, and codec/math helpers used by both sides.

More detail: [docs/architecture.md](docs/architecture.md)

## Development Notes

- Keep server authority in `src/Server`.
- Keep microphone, input, UI, and playback code in `src/Client`.
- Keep wire formats and constants in `src/Shared`.
- Do not add a separate runtime codec DLL unless the loader/package plan changes.
- Avoid changing Valheim gameplay state from voice code.

## Roadmap

Near-term improvements:

- Add microphone device selection
- Add transmit/receive HUD indicator
- Add per-player mute/deafen controls
- Add packet sequence numbers and loss statistics

Longer-term ideas:

- Admin-controlled mute/deafen integration
- Optional radio/channel mode for RP groups
- Lip-sync signal export for character animation mods
- Positional occlusion or indoor dampening
