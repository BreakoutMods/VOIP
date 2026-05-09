# Architecture

VOIP is split by runtime responsibility rather than by feature name.

## Client

Client code runs on game clients and should not make authoritative decisions.

- `VoiceCapture`: reads Unity microphone data, applies push-to-talk or voice activation, encodes frames.
- `VoiceClient`: sends encoded frames to the current Valheim server peer and accepts settings only from that server.
- `VoicePlayback`: decodes received frames into a per-speaker jitter buffer and plays spatial audio through a streaming `AudioClip`.
- `VoiceHud`: shows minimal transmit, receive, mute, and deafen state.
- `VoiceMuteState`: stores local deafen state and persisted last-speaker mute entries.

## Server

Server code runs on hosts and dedicated servers.

- `VoiceServer`: relays voice frames to nearby peers only, and periodically broadcasts authoritative voice settings.

The server never trusts a client to decide who should hear a packet. Relayed voice uses the server-known sender peer ID and server-known sender position, not the client-provided identity or position.

## Shared

Shared code is safe to use from both sides.

- `VoiceNetwork`: registers routed RPCs and dispatches packets to client/server components.
- `VoicePacket`: wire format for encoded voice frames.
- `VoiceValidationHarness`: lightweight compile-time validation cases for packet rejection behavior.
- `VoiceRuntimeSettings`: effective session settings and server sync serialization.
- `VoiceSettings`: BepInEx config bindings.
- `OpusVoiceCodec`: Concentus-backed encoder/decoder wrapper.
- `AudioMath`: small audio helpers.
- `VOIPPlugin`: BepInEx entry point and component wiring.

## Packet Flow

```text
Client microphone
  -> VoiceCapture
  -> OpusVoiceCodec.Encode
  -> VoiceClient.Send
  -> Valheim routed RPC with protocol version and sequence number
  -> VoiceServer.Relay
  -> server-authored speaker identity and position
  -> nearby client routed RPC
  -> VoiceNetwork
  -> VoicePlayback
  -> OpusVoiceCodec.Decode
  -> per-speaker jitter buffer
  -> Unity AudioSource
```

## Settings Flow

```text
Server BepInEx config
  -> VoiceRuntimeSettings.CreateServerPackage
  -> VoiceServer broadcast
  -> VoiceClient.ApplyServerSettings
  -> client runtime settings
```

Server settings override client session values during multiplayer. Personal client settings such as local push-to-talk and playback volume remain local.

## Diagnostics

Malformed voice packets, invalid settings sources, unsupported settings versions, and malformed settings packages are logged with rate limits. Settings broadcasts and received settings are also logged at low frequency so server/client sync can be diagnosed without flooding the BepInEx log.

Voice packets validate protocol version, sample rate, sample count, payload size, non-empty payloads, and finite positions. Server relay applies per-sender frame rate limiting before forwarding.
