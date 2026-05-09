# BreakoutMods VOIP

Part of the BreakoutMods modding suite.

Experimental BepInEx mod that adds proximity voice chat to Valheim using the existing Valheim network session.

The goal is simple server-hosted RP voice chat: clients capture microphone audio, the dedicated server relays voice only to nearby players, and no separate public VOIP server address is required.

## Status

Early MVP. It builds and the core voice path exists, but this is not production-polished yet.

Implemented:

- Push-to-talk voice capture, default key `V`
- Optional voice activation
- Opus encoding through embedded Concentus C# source
- BreakoutNet typed RPC transport over Valheim routed RPC
- Server-side proximity relay
- Server-authoritative voice settings sync through BreakoutNet
- Server-authoritative speaker identity and position for relayed packets
- Hardened voice packet validation with protocol versioning and sequence numbers
- Per-sender voice frame rate limiting
- Spatial Unity audio playback
- Jitter-buffered playback using streaming `AudioClip` output
- Sequence-aware packet loss diagnostics
- Rate-limited diagnostics for malformed voice packets and settings sync
- Microphone device config and push-to-talk microphone stop delay
- Minimal transmit/receive/mute/deafen HUD indicator
- Local deafen and last-speaker mute controls
- Client / Server / Shared source layout

Not done yet:

- Input device selection
- Polished player list mute/deafen UI
- Polished config migration/versioning

## Install

Install `VOIP.dll` on:

- the dedicated server
- every client that should use voice chat

VOIP depends on `BreakoutNet.dll`, which must also be installed on the server and clients.

Recommended plugin folder:

```text
BepInEx/plugins/BreakoutNet/BreakoutNet.dll
BepInEx/plugins/VOIP/VOIP.dll
```

No separate `Concentus.dll` is required. The Opus codec source is compiled directly into `VOIP.dll`.

## How It Works

Clients do not configure a separate voice server per world. When a player joins a Valheim server:

1. The client captures local microphone audio.
2. The client encodes short mono frames with Opus.
3. The client sends voice frames to the server through `BreakoutRpc.Client.SendToServer`.
4. The server receives frames and relays them only to peers within the configured proximity radius.
5. Recipients decode and play speech as spatial audio at the speaker position.

The server periodically syncs voice session settings such as radius, bitrate, sample rate, and frame size. Local client config is still used for personal settings such as push-to-talk, voice activation, and playback volume.

## Configuration

BepInEx generates the config after the first launch.

Common client settings:

- push-to-talk key
- microphone device
- microphone stop delay after push-to-talk release
- voice activation enabled/disabled
- voice activation threshold
- playback volume
- local deafen key
- mute last speaker key
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
    VOIP/
```

Build with:

```powershell
.\scripts\install-deps.ps1
.\build.ps1
```

The script:

- compiles with the .NET Framework `csc.exe`
- references assemblies from the adjacent Valheim dedicated server install
- builds and references the adjacent `Modding/BreakoutNet` project
- compiles locally installed Concentus source into the plugin DLL
- writes `VOIP.dll` to `bin/<Configuration>/net462`

Deploy with:

```powershell
.\build.ps1 -Configuration Release -Deploy
```

If the server or game has already loaded the DLL, deployment may write `VOIP.dll.pending`. Stop Valheim and rerun the build or copy the pending DLL over the loaded one.

Deployment also copies `BreakoutNet.dll` to `BepInEx/plugins/BreakoutNet`. If that DLL is already loaded, the script writes `BreakoutNet.dll.pending`.

SDK-style project builds may also work if you have a compatible .NET SDK installed:

```powershell
dotnet build .\VOIP.csproj -c Release
```

The SDK project explicitly compiles only VOIP sources and the Concentus codec core, not the upstream demo/test projects.

## Source Layout

```text
src/
  Client/
    VoiceCapture.cs
    VoiceClient.cs
    VoiceHud.cs
    VoiceMuteState.cs
    VoicePlayback.cs
  Server/
    VoiceRateLimiter.cs
    VoiceServer.cs
  Shared/
    AudioMath.cs
    OpusVoiceCodec.cs
    VOIPPlugin.cs
    VoiceNetwork.cs
    VoicePacket.cs
    VoiceRuntimeSettings.cs
    VoiceServerSettings.cs
    VoiceSettings.cs
    VoiceValidationHarness.cs
```

```text
libs/
  README.md
scripts/
  install-deps.ps1
```

`Client` owns microphone capture, local send behavior, and playback.

`Server` owns proximity relay.

`Shared` owns plugin wiring, BreakoutNet RPC registration, packet models, runtime settings, and codec/math helpers used by both sides.

More detail: [docs/architecture.md](docs/architecture.md)

## Development Notes

- Keep server authority in `src/Server`.
- Keep microphone, input, UI, and playback code in `src/Client`.
- Keep wire formats and constants in `src/Shared`.
- Keep network boilerplate in BreakoutNet where possible; VOIP should only own voice-specific validation and relay policy.
- Do not add a separate runtime codec DLL unless the loader/package plan changes.
- Avoid changing Valheim gameplay state from voice code.

## Roadmap

Near-term improvements:

- Add polished microphone device UI
- Add player-list mute/deafen controls
- Add in-game voice diagnostics panel

Longer-term ideas:

- Admin-controlled mute/deafen integration
- Optional radio/channel mode for RP groups
- Lip-sync signal export for character animation mods
- Positional occlusion or indoor dampening
