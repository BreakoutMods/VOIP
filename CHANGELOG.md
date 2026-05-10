# Changelog

## 0.4.0

- Updated VOIP to use BreakoutNet `0.2.0` app/context APIs.
- Replaced VOIP's local world-left polling with BreakoutNet core hooks.
- Added local VOIP extension events for settings application and relayed voice packets.

## 0.3.0

- Migrated VOIP voice frame transport to BreakoutNet typed RPCs.
- Migrated server-authoritative settings sync to BreakoutNet.
- Added BreakoutNet as a runtime/package dependency.
- Updated build and deploy scripts to build and copy `BreakoutNet.dll`.

## 0.2.0

- Added hardened voice packet validation, server-authored speaker identity/position, rate limiting, microphone lifecycle improvements, and playback UX polish.
