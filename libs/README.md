# Third-Party Source

This project uses the Concentus C# Opus implementation so the BepInEx plugin can ship as a single `ValheimVoip.dll`.

The Concentus source is not committed to this repository. Install it locally with:

```powershell
.\scripts\install-deps.ps1
```

That script downloads the upstream `v1.2-c#` source archive from `lostromb/concentus` and creates:

```text
libs/concentus-v1.2-csharp/
```

Do not commit downloaded NuGet packages, ZIP files, extracted source trees, or a standalone `Concentus.dll`. The plugin build compiles the installed Concentus source directly into `ValheimVoip.dll`.
