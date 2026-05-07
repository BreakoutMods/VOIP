using BepInEx;
using BepInEx.Logging;
using UnityEngine;

namespace ValheimVoip
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public sealed class ValheimVoipPlugin : BaseUnityPlugin
    {
        public const string ModGuid = "de.valheim.voip";
        public const string ModName = "Valheim VOIP";
        public const string ModVersion = "0.1.0";

        internal static ManualLogSource Log { get; private set; }

        private GameObject _runnerObject;

        private void Awake()
        {
            Log = Logger;
            VoiceSettings.Bind(Config);

            _runnerObject = new GameObject("Valheim VOIP");
            DontDestroyOnLoad(_runnerObject);

            VoiceNetwork network = _runnerObject.AddComponent<VoiceNetwork>();
            VoiceServer server = _runnerObject.AddComponent<VoiceServer>();
            VoiceClient client = new VoiceClient();
            VoicePlayback playback = _runnerObject.AddComponent<VoicePlayback>();
            VoiceCapture capture = _runnerObject.AddComponent<VoiceCapture>();

            network.Initialize(client, server, playback);
            capture.Initialize(network);

            Logger.LogInfo(ModName + " " + ModVersion + " loaded");
        }

        private void OnDestroy()
        {
            if (_runnerObject != null)
            {
                Destroy(_runnerObject);
            }
        }
    }
}
