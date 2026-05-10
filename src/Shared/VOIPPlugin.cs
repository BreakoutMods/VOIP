using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using BreakoutMods.BreakoutNet;

namespace VOIP
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInDependency(BreakoutNetPlugin.PluginGuid)]
    public sealed class VOIPPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.breakoutmods.voip";
        public const string ModName = "VOIP";
        public const string ModVersion = "0.4.1";

        internal static ManualLogSource Log { get; private set; }

        private BreakoutModApp _breakoutApp;
        private GameObject _runnerObject;

        private void Awake()
        {
            Log = Logger;
            VoiceSettings.Bind(Config);
            _breakoutApp = BreakoutNet.ForPlugin(this, ModGuid).Build();

            _runnerObject = new GameObject("VOIP");
            DontDestroyOnLoad(_runnerObject);

            VoiceNetwork network = _runnerObject.AddComponent<VoiceNetwork>();
            VoiceServer server = _runnerObject.AddComponent<VoiceServer>();
            VoiceClient client = null;
            VoicePlayback playback = null;

            server.Initialize(_breakoutApp.Context);

            if (!Application.isBatchMode)
            {
                client = new VoiceClient(_breakoutApp.Context);
                playback = _runnerObject.AddComponent<VoicePlayback>();
                VoiceCapture capture = _runnerObject.AddComponent<VoiceCapture>();
                capture.Initialize(network);
                _runnerObject.AddComponent<VoiceHud>();
            }

            network.Initialize(_breakoutApp.Context, client, server, playback);

            Logger.LogInfo(ModName + " " + ModVersion + " loaded");
        }

        private void OnDestroy()
        {
            if (_runnerObject != null)
            {
                Destroy(_runnerObject);
            }

            if (_breakoutApp != null)
            {
                _breakoutApp.Dispose();
                _breakoutApp = null;
            }
        }
    }
}
