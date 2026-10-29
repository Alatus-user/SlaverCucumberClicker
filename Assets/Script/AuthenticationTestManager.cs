using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class AuthenticationTestManager : MonoBehaviour
{
    [Header("Test Account")]
    [SerializeField] private string username = "player01";
    [SerializeField] private string password = "Test1234!";
    [SerializeField] private string playerName = "Player01";

    private async Task InitializeAsync()
    {
        if (UnityServices.State ==
            ServicesInitializationState.Uninitialized)
        {
            await UnityServices.InitializeAsync();
        }
    }

    [ContextMenu("Sign In Anonymous")]
    private async void SignInAnonymous()
    {
        await InitializeAsync();

        if (AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning(
                "A player is already signed in."
            );

            return;
        }

        try
        {
            bool hasCachedPlayer =
                AuthenticationService.Instance.SessionTokenExists;

            await AuthenticationService.Instance
                .SignInAnonymouslyAsync();

            Debug.Log(
                $"ANONYMOUS SIGN IN\n" +
                $"Cached Player: {hasCachedPlayer}\n" +
                $"PlayerId: {AuthenticationService.Instance.PlayerId}\n" +
                $"PlayerName: {AuthenticationService.Instance.PlayerName}"
            );
        }
        catch (AuthenticationException exception)
        {
            Debug.LogException(exception);
        }
        catch (RequestFailedException exception)
        {
            Debug.LogException(exception);
        }
    }

    // ----------------------------------------------------
    // Existing Anonymous Player
    // ----------------------------------------------------

    [ContextMenu("Link Current Player")]
    private async void LinkCurrentPlayer()
    {
        await InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning(
                "No player is currently signed in."
            );

            return;
        }

        try
        {
            string playerIdBefore =
                AuthenticationService.Instance.PlayerId;

            await AuthenticationService.Instance
                .AddUsernamePasswordAsync(
                    username,
                    password
                );

            if (!string.IsNullOrWhiteSpace(playerName))
            {
                await AuthenticationService.Instance
                    .UpdatePlayerNameAsync(playerName);
            }

            Debug.Log(
                $"ACCOUNT LINKED\n" +
                $"Username: {username}\n" +
                $"PlayerId Before: {playerIdBefore}\n" +
                $"PlayerId After: {AuthenticationService.Instance.PlayerId}\n" +
                $"PlayerName: {AuthenticationService.Instance.PlayerName}"
            );
        }
        catch (AuthenticationException exception)
        {
            Debug.LogException(exception);
        }
        catch (RequestFailedException exception)
        {
            Debug.LogException(exception);
        }
    }

    // ----------------------------------------------------
    // Create New Player
    // ----------------------------------------------------

    [ContextMenu("Sign Up")]
    private async void SignUp()
    {
        await InitializeAsync();

        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
        }

        try
        {
            await AuthenticationService.Instance
                .SignUpWithUsernamePasswordAsync(
                    username,
                    password
                );

            if (!string.IsNullOrWhiteSpace(playerName))
            {
                await AuthenticationService.Instance
                    .UpdatePlayerNameAsync(playerName);
            }

            DebugCurrentPlayer("SIGN UP");
        }
        catch (AuthenticationException exception)
        {
            Debug.LogException(exception);
        }
        catch (RequestFailedException exception)
        {
            Debug.LogException(exception);
        }
    }

    // ----------------------------------------------------
    // Login Existing Player
    // ----------------------------------------------------

    [ContextMenu("Sign In")]
    private async void SignIn()
    {
        await InitializeAsync();

        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut();
        }

        try
        {
            await AuthenticationService.Instance
                .SignInWithUsernamePasswordAsync(
                    username,
                    password
                );

            DebugCurrentPlayer("SIGN IN");
        }
        catch (AuthenticationException exception)
        {
            Debug.LogException(exception);
        }
        catch (RequestFailedException exception)
        {
            Debug.LogException(exception);
        }
    }

    // ----------------------------------------------------
    // Logout
    // ----------------------------------------------------

    [ContextMenu("Sign Out")]
    private void SignOut()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("No player is signed in.");
            return;
        }

        AuthenticationService.Instance.SignOut();

        Debug.Log("SIGN OUT");
    }

    // ----------------------------------------------------
    // Debug
    // ----------------------------------------------------

    [ContextMenu("Show Current Player")]
    private void ShowCurrentPlayer()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.Log("No player is signed in.");
            return;
        }

        DebugCurrentPlayer("CURRENT PLAYER");
    }

    private void DebugCurrentPlayer(string action)
    {
        Debug.Log(
            $"{action}\n" +
            $"Username: {username}\n" +
            $"PlayerId: {AuthenticationService.Instance.PlayerId}\n" +
            $"PlayerName: {AuthenticationService.Instance.PlayerName}"
        );
    }

    [ContextMenu("DEBUG - Create New Anonymous Player")]
    private async void CreateNewAnonymousPlayer()
    {
        await InitializeAsync();

        if (AuthenticationService.Instance.IsSignedIn)
        {
            AuthenticationService.Instance.SignOut(true);
        }
        else
        {
            AuthenticationService.Instance.ClearSessionToken();
        }

        await AuthenticationService.Instance
            .SignInAnonymouslyAsync();

        DebugCurrentPlayer(
            "NEW ANONYMOUS PLAYER"
        );
    }
}