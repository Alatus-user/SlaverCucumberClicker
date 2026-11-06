using System;
using CucumberClicker.UI;
using Unity.Services.RemoteConfig;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// On launch, fetches Remote Config. If "halloween_mode" is true, the big cucumber
/// becomes a pumpkin. Runs automatically; nothing needs to be placed in the scene.
/// </summary>
public static class SeasonalSkinLoader
{
    private const string HalloweenKey = "halloween_mode";
    private static readonly Color PumpkinOrange = new Color(1f, 0.55f, 0.1f);

    // Remote Config requires these (used for targeting rules; empty is fine).
    private struct UserAttributes { }
    private struct AppAttributes { }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static async void Run()
    {
        try
        {
            await UGSManager.InitializeAsync(); // your existing init + anonymous sign-in
            RuntimeConfig config = await RemoteConfigService.Instance
                .FetchConfigsAsync(new UserAttributes(), new AppAttributes());

            bool halloween = config.GetBool(HalloweenKey, false);
            Debug.Log($"Remote Config: {HalloweenKey} = {halloween}");
            if (halloween) ApplyHalloween();
        }
        catch (Exception e)
        {
            // Offline or services down: just keep the cucumber.
            Debug.LogWarning($"Remote Config fetch failed, keeping cucumber: {e.Message}");
        }
    }

    private static void ApplyHalloween()
    {
        Sprite pumpkin = Resources.Load<Sprite>("Seasonal/pumpkin");
        if (pumpkin == null)
        {
            Debug.LogWarning("Missing Assets/_Game/Resources/Seasonal/pumpkin.png");
            return;
        }

        CucumberButton button = UnityEngine.Object.FindAnyObjectByType<CucumberButton>();
        if (button == null) return;

        Image image = button.GetComponent<Image>();
        image.sprite = pumpkin;
        image.color = PumpkinOrange; // the icon is white, so this tints it orange
    }
}