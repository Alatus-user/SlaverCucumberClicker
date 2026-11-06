// Real notifications need the Mobile Notifications package (MOBILE_NOTIFICATIONS comes from the
// asmdef's versionDefines) and a phone. In the Editor and on desktop every call is a safe no-op.
#if MOBILE_NOTIFICATIONS && (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
#define NOTIFICATIONS_ON
#endif

using System;
using System.Collections.Generic;
using UnityEngine;
#if NOTIFICATIONS_ON
using Unity.Notifications;
#endif

namespace CucumberClicker
{
    /// <summary>
    /// Tells the player when jars finish while the game is in the background.
    /// Notifications are scheduled when the app is paused and cancelled when it comes back,
    /// so they never fire while the player is looking at the jars. Created at runtime, so the
    /// scene doesn't need rebuilding.
    /// </summary>
    public class FermentationNotifier : MonoBehaviour
    {
        private const string ChannelId = "fermentation";
        private const string GroupId = "fermentation";
        private const int TestId = 1;
        private const int FirstJarId = 1000; // Jar notifications use FirstJarId + 0, 1, 2, ...

        /// <summary>Jars finishing this close together share one notification.</summary>
        private const double MergeWindowSeconds = 60;

        public static FermentationNotifier Instance { get; private set; }

        /// <summary>False in the Editor, on desktop, or without the package.</summary>
        public static bool IsSupported
        {
            get
            {
#if NOTIFICATIONS_ON
                return true;
#else
                return false;
#endif
            }
        }

        private GameManager game;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void CreateInGame()
        {
            if (GameManager.Instance == null || Instance != null) return;
            Instance = new GameObject(nameof(FermentationNotifier)).AddComponent<FermentationNotifier>();
        }

        private void Awake()
        {
            game = GameManager.Instance;
#if NOTIFICATIONS_ON
            NotificationCenterArgs args = NotificationCenterArgs.Default;
            args.AndroidChannelId = ChannelId;
            args.AndroidChannelName = Strings.NotificationChannelName;
            args.AndroidChannelDescription = Strings.NotificationChannelDescription;
            NotificationCenter.Initialize(args);
            CancelJarNotifications();
#endif
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Asks for permission the first time a jar is running, when the reason is obvious.</summary>
        private void Update()
        {
            if (!IsSupported || game == null)
            {
                enabled = false;
                return;
            }

            long now = TimeService.UtcNowTicks;
            bool anyRunning = false;
            foreach (JarState jar in game.Pickling.Jars) anyRunning |= jar.StatusAt(now) == JarStatus.Running;
            if (!anyRunning) return;

            enabled = false; // Only Update needs this; pause callbacks still arrive.
#if NOTIFICATIONS_ON
            NotificationCenter.RequestPermission(); // Android 13+ and iOS show a dialog once; older Android allows it.
#endif
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) ScheduleJarNotifications();
            else CancelJarNotifications();
        }

        /// <summary>Schedules one notification per group of jars that finish together. Returns how many.</summary>
        public int ScheduleJarNotifications()
        {
            if (!IsSupported || game == null) return 0;
            CancelJarNotifications();

            List<(double seconds, JarState jar)> finishes = NextFinishes();
            int scheduled = 0;
            for (int start = 0; start < finishes.Count; scheduled++)
            {
                int end = start + 1;
                while (end < finishes.Count && finishes[end].seconds - finishes[start].seconds <= MergeWindowSeconds) end++;

                string text = end - start == 1
                    ? Strings.JarReadyText(NumberFormatter.Format(finishes[start].jar.amount), RecipeName(finishes[start].jar))
                    : Strings.JarsReadyText(end - start);
                // The last jar of the group decides the time, so every jar in it is Ready when the player taps.
                Schedule(FirstJarId + scheduled, Strings.JarReadyTitle, text, finishes[end - 1].seconds, false);
                start = end;
            }
            return scheduled;
        }

        /// <summary>Debug: sends a notification after a delay, shown even if the game is open.</summary>
        public bool SendTest(double delaySeconds)
        {
            return Schedule(TestId, Strings.TestNotificationTitle, Strings.TestNotificationText, delaySeconds, true);
        }

        /// <summary>Running jars, soonest first, with seconds left.</summary>
        private List<(double seconds, JarState jar)> NextFinishes()
        {
            long now = TimeService.UtcNowTicks;
            var finishes = new List<(double, JarState)>();
            foreach (JarState jar in game.Pickling.Jars)
                if (jar.StatusAt(now) == JarStatus.Running)
                    finishes.Add((jar.RemainingSecondsAt(now), jar));
            finishes.Sort((a, b) => a.Item1.CompareTo(b.Item1));
            return finishes;
        }

        private string RecipeName(JarState jar) => game.Pickling.FindRecipe(jar.recipeId)?.displayName ?? Strings.PicklesFallbackName;

        private bool Schedule(int id, string title, string text, double delaySeconds, bool showInForeground)
        {
#if NOTIFICATIONS_ON
            var notification = new Notification
            {
                Identifier = id,
                Title = title,
                Text = text,
                Group = GroupId,
                ShowInForeground = showInForeground,
            };
            // Uses the real clock, not TimeService, so a debug time offset doesn't move it.
            TimeSpan delay = TimeSpan.FromSeconds(Math.Max(1, delaySeconds));
            NotificationCenter.ScheduleNotification(notification, new NotificationIntervalSchedule(delay));
            return true;
#else
            Debug.Log($"[Notifications] Not supported here. Would send in {delaySeconds:0}s: {title} / {text}");
            return false;
#endif
        }

        /// <summary>Removes pending and shown jar notifications. Leaves the test one alone.</summary>
        private void CancelJarNotifications()
        {
#if NOTIFICATIONS_ON
            if (game == null) return;
            for (int i = 0; i < game.Pickling.Jars.Count; i++)
            {
                NotificationCenter.CancelScheduledNotification(FirstJarId + i);
                NotificationCenter.CancelDeliveredNotification(FirstJarId + i);
            }
#endif
        }
    }
}
