using UnityEngine;

namespace VOIP
{
    internal sealed class VoiceHud : MonoBehaviour
    {
        private const float IndicatorSeconds = 0.35f;

        private static float _transmittingUntil;
        private static float _receivingUntil;

        public static void MarkTransmitting()
        {
            _transmittingUntil = Time.time + IndicatorSeconds;
        }

        public static void MarkReceiving()
        {
            _receivingUntil = Time.time + IndicatorSeconds;
        }

        private void Update()
        {
            if (Player.m_localPlayer == null || !VoiceRuntimeSettings.Enabled)
            {
                return;
            }

            HandleInput();
        }

        private void OnGUI()
        {
            if (Player.m_localPlayer == null || !VoiceRuntimeSettings.Enabled)
            {
                return;
            }

            string status = null;
            if (VoiceMuteState.Deafened)
            {
                status = "VOIP DEAFENED";
            }
            else if (VoiceMuteState.HasLastSpeaker && VoiceMuteState.IsMuted(VoiceMuteState.LastSpeakerId))
            {
                status = "VOIP MUTED";
            }
            else if (Time.time < _transmittingUntil)
            {
                status = "VOIP TX";
            }
            else if (Time.time < _receivingUntil)
            {
                status = "VOIP RX";
            }

            if (status == null)
            {
                return;
            }

            GUI.Label(new Rect(20f, Screen.height - 60f, 160f, 30f), status);
        }

        private static void HandleInput()
        {
            if (Input.GetKeyDown(VoiceSettings.ToggleDeafenKeyCode))
            {
                VoiceMuteState.ToggleDeafen();
            }

            if (Input.GetKeyDown(VoiceSettings.ToggleMuteLastSpeakerKeyCode))
            {
                VoiceMuteState.ToggleLastSpeakerMute();
            }
        }
    }
}
