using System;

namespace VOIP
{
    internal static class AudioMath
    {
        public static float Rms(float[] samples, int count)
        {
            if (count <= 0)
            {
                return 0f;
            }

            double total = 0;
            for (int i = 0; i < count; i++)
            {
                total += samples[i] * samples[i];
            }

            return (float)Math.Sqrt(total / count);
        }
    }
}
