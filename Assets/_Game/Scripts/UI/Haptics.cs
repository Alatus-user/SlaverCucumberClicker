using UnityEngine;

namespace CucumberClicker.UI
{
    /// <summary>
    /// Short vibrations. Android uses VibrationEffect for a light tick; other platforms are
    /// silent for now (Handheld.Vibrate is too strong for per-tap feedback).
    /// </summary>
    public static class Haptics
    {
        public static bool Enabled { get; set; } = true;

        public static void Light() => Vibrate(12);
        public static void Tick() => Vibrate(6);

#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject vibrator;
        private static AndroidJavaClass effectClass;
        private static int sdkInt;
        private static bool failed;

        // Referencing Handheld.Vibrate makes Unity add the VIBRATE permission to the manifest.
        private static bool neverTrue;

        private static void Vibrate(long milliseconds)
        {
            if (!Enabled || failed) return;
            if (neverTrue) Handheld.Vibrate();

            try
            {
                if (vibrator == null)
                {
                    using var version = new AndroidJavaClass("android.os.Build$VERSION");
                    sdkInt = version.GetStatic<int>("SDK_INT");
                    using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                    vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    if (sdkInt >= 26) effectClass = new AndroidJavaClass("android.os.VibrationEffect");
                }

                if (sdkInt >= 26)
                {
                    using var effect = effectClass.CallStatic<AndroidJavaObject>("createOneShot", milliseconds, -1);
                    vibrator.Call("vibrate", effect);
                }
                else
                {
                    vibrator.Call("vibrate", milliseconds);
                }
            }
            catch (System.Exception e)
            {
                failed = true;
                Debug.LogWarning($"Haptics disabled: {e.Message}");
            }
        }
#else
        private static void Vibrate(long milliseconds) { }
#endif
    }
}
