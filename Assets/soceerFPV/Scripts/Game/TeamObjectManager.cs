using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人：
//功能说明：根据阵营的选择在游戏场景显示对应队伍物体
//***************************************** 
public class TeamObjectManager : MonoBehaviour
{
    public GameObject[] redTeamObjects;

    public GameObject[] blueTeamObjects;

    private string currentTeam = "";

    void Start()
    {
        // 延迟设置，确保阵营信息已加载
        StartCoroutine(DelayedSetup());
    }

    void Update()
    {
        // 持续检查阵营变化
        CheckTeamChange();
    }

    IEnumerator DelayedSetup()
    {
        // 等待足够的时间，确保回放信息已加载
        // yield return new WaitForSeconds(0.1f);
        yield return 0;
        string playerTeam = SinglePlayerContent.SelectedTeam;
        SetupTeamObjects(playerTeam);
        currentTeam = playerTeam;
    }

    void CheckTeamChange()
    {
        string playerTeam = SinglePlayerContent.SelectedTeam;
        if (playerTeam != currentTeam)
        {
            SetupTeamObjects(playerTeam);
            currentTeam = playerTeam;

        }
    }

    void SetupTeamObjects(string team)
    {
        if (team == "Red")
        {
            SetObjectsActive(redTeamObjects, true);
            SetObjectsActive(blueTeamObjects, false);
        }
        else
        {
            SetObjectsActive(redTeamObjects, false);
            SetObjectsActive(blueTeamObjects, true);
        }
    }

    void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null) return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }
}