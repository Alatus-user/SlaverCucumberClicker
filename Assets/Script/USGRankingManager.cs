using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;
using UnityEngine;

public class UGSRankingManager : MonoBehaviour
{
    private const string LeaderboardId = "Hight_Score";

    [Header("UI")]
    [SerializeField] private RankUIManager rankUIManager;

    [Header("Test")]
    [SerializeField] private int testScore = 1000;
    [SerializeField] private Texture2D testProfileTexture;

    private async void Start()
    {
        await UGSManager.InitializeAsync();
        await ReloadLeaderboardAsync();
    }

    public async Task SubmitScoreAsync(int score)
    {
        await LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, score);
        Debug.Log($"Score submitted: {score}");
    }

    public async Task ReloadLeaderboardAsync()
    {
        var scores = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId,
            new GetScoresOptions
            {
                Offset = 0,
                Limit = 10
            }
            );

        List<PlayerData> playerDatas = new List<PlayerData>();

        foreach(var entry in scores.Results)
        {
            Texture2D avatar = await PlayerProfileService
                .LoadAvatarAsync(entry.PlayerId);

            PlayerData playerData =
                new PlayerData(
                    entry.PlayerId,

                    entry.Rank + 1,
                    entry.PlayerName,
                    Mathf.RoundToInt((float)entry.Score),
                    avatar
                    );

            playerDatas.Add(playerData);
        }

        rankUIManager.ShowRanking(playerDatas);
    }

    [ContextMenu("Submit Test Score")]
    private async void SubmitTestScore()
    {
        await SubmitScoreAsync(testScore);
        await ReloadLeaderboardAsync();
    }

    [ContextMenu("Save Test Avatar")]
    private async void SaveTestAvatar()
    {
        await PlayerProfileService.SaveAvatarAsync(testProfileTexture);
        await ReloadLeaderboardAsync();
    }

    [ContextMenu("Reload Leaderboard")]
    private async void ReloadLeaderboard()
    {
        await ReloadLeaderboardAsync();
    }
}
 