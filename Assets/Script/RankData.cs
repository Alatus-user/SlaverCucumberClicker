using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public struct PlayerData
{
    public string playerId;
    public string playerName;
    public int rankNumber;
    public int playerScore;
    public Texture profileTexture;

    public PlayerData(
        string playerId,
        int rankNumber,
        string playerName,
        int playerScore,
        Texture profileTexture)
    {
        this.playerId = playerId;
        this.rankNumber = rankNumber;
        this.playerName = playerName;
        this.playerScore = playerScore;
        this.profileTexture = profileTexture;
    }
}

public class RankData : MonoBehaviour
{
    [SerializeField] private RawImage profileImg;
    [SerializeField] private Texture defaultProfileTexture;
    [SerializeField] private TextMeshProUGUI rankText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI scoreText;

    public void SetData(PlayerData playerData)
    {
        profileImg.texture = playerData.profileTexture != null
       ? playerData.profileTexture
       : defaultProfileTexture;

        rankText.text = playerData.rankNumber.ToString();
        playerNameText.text = playerData.playerName;
        scoreText.text = playerData.playerScore.ToString("0");
    }

}