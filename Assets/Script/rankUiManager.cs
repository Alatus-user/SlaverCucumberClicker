using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class RankUIManager : MonoBehaviour
{
    [SerializeField] private GameObject rankDataPrefab;
    [SerializeField] private Transform rankPanel;
    private readonly List<GameObject> createdRankDatas = new();

    public void ShowRanking(List<PlayerData> playerDatas)
    {
        ClearRankData();

        foreach (PlayerData playerData in playerDatas)
        {
            GameObject rankObj = Instantiate(rankDataPrefab, rankPanel);
            RankData rankData = rankObj.GetComponent<RankData>();
            rankData.SetData(playerData);
            createdRankDatas.Add(rankObj);
        }
    }

    private void ClearRankData()
    {
        foreach (GameObject createdData in createdRankDatas)
        {
            Destroy(createdData);
        }

        createdRankDatas.Clear();
    }
}