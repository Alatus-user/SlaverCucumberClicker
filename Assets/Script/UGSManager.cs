using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public static class UGSManager
{
    public static async Task InitializeAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        Debug.Log($"Player ID: {AuthenticationService.Instance.PlayerId}");
    }

    public static async Task SetPlayerNameAsync(string playerName)
    {
        await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
        Debug.Log($"Player Name: {AuthenticationService.Instance.PlayerName}");
    }

    public static string PlayerId => AuthenticationService.Instance.PlayerId;

    public static string PlayerName => AuthenticationService.Instance.PlayerName;
}