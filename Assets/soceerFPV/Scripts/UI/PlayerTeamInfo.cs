using Mirror;
using UnityEngine;
using System.Collections.Generic;
//*****************************************
//创建人： Mezcal
//功能说明：		局域网游戏时玩家阵营同步和出生点管理
//SyncVar：同步变量，服务器修改后自动同步给所有客户端

//Command(Cmd)：客户端->服务器的远程调用

//ClientRpc(Rpc)：服务器->客户端的远程调用

//Server：只在服务器端执行的代码
//***************************************** 
public class PlayerTeamInfo : NetworkBehaviour
{
    [Header("红方组件")]
    public GameObject redComponents;

    [Header("蓝方组件")]
    public GameObject blueComponents;

    [SyncVar]
    private string team = "Red";
    //静态字典记录已占用的生成点。键：生成点Transform，值：是否被占用
    private static Dictionary<Transform, bool> occupiedSpawns = new Dictionary<Transform, bool>();
    //当前玩家占用的生成点
    private Transform mySpawnPoint;
    // 标记是否已经初始化
    private bool initialized = false;

    private void Start()
    {
        // 如果是客户端（包括主机作为客户端），保存阵营选择
        if (isClient)
        {
            if (isLocalPlayer)
            {
                //判断自己的本地玩家是主机还是客户端， 立即根据本地选择更新显示。不需要等待网络同步
                string selectedTeam = isServer ? LanPlayContent.SelectedTeam : LanPlayContent.ClientSelectedTeam;
                UpdateTeamDisplay(selectedTeam);
            }
        }
    }

    // 统一更新显示的方法
    private void UpdateTeamDisplay(string teamToShow)
    {
        if (initialized) return;
        bool isRedTeam = (teamToShow == "Red");
        //根据阵营显示/隐藏对应组件
        if (redComponents != null)
            redComponents.SetActive(isRedTeam);
        if (blueComponents != null)
            blueComponents.SetActive(!isRedTeam);
        initialized = true;
    }

    // 服务器端方法-只在服务器端执行-获取可用的生成点
    [Server]
    private Transform GetAvailableSpawnPoint()
    {
        //根据阵营选择对应的生成点标签
        string spawnTag = team == "Red" ? "RedSpawn" : "BlueSpawn";
        GameObject[] allSpawnPoints = GameObject.FindGameObjectsWithTag(spawnTag);

        if (allSpawnPoints.Length == 0)
        {
           // Debug.LogError($"未找到{team}方生成点！");
            return null;
        }

        //收集未占用的生成点
        List<Transform> availableSpawns = new List<Transform>();

        foreach (GameObject spawnObj in allSpawnPoints)
        {
            Transform spawnPoint = spawnObj.transform;

            // 检查这个生成点是否已经被占用
            if (!occupiedSpawns.ContainsKey(spawnPoint) || !occupiedSpawns[spawnPoint])
            {
                availableSpawns.Add(spawnPoint);
            }
        }

        // 如果有可用生成点，随机选择一个
        if (availableSpawns.Count > 0)
        {
            int randomIndex = Random.Range(0, availableSpawns.Count);
            Transform selectedSpawn = availableSpawns[randomIndex];

            // 标记为已占用
            occupiedSpawns[selectedSpawn] = true;
            mySpawnPoint = selectedSpawn;

          //  Debug.Log($"{team}方玩家占用生成点: {selectedSpawn.name}");
            return selectedSpawn;
        }

        // 所有生成点都被占用，随机选择一个（可能会重叠）
        //Debug.LogWarning($"所有{team}方生成点都被占用，随机选择一个");
        int fallbackIndex = Random.Range(0, allSpawnPoints.Length);
        Transform fallbackSpawn = allSpawnPoints[fallbackIndex].transform;
        mySpawnPoint = fallbackSpawn;
        return fallbackSpawn;
    }

    //玩家离开时释放占用的生成点
    [Server]
    private void ReleaseSpawnPoint()
    {
        if (mySpawnPoint != null && occupiedSpawns.ContainsKey(mySpawnPoint))
        {
            occupiedSpawns[mySpawnPoint] = false; // 释放
           // Debug.Log($"{team}方玩家释放生成点: {mySpawnPoint.name}");
            mySpawnPoint = null;
        }
    }

    // 设置生成位置
    [Server]
    private void SetSpawnPosition()
    {
        Transform spawnPoint = GetAvailableSpawnPoint();

        if (spawnPoint != null)
        {
            //服务器端设置位置
            transform.position = spawnPoint.position;
            transform.rotation = spawnPoint.rotation;
            //同步到所有客户端
            RpcSetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
        }
    }

    //网络同步-服务器调用-在所有客户端执行
    [ClientRpc]
    private void RpcSetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        //设置客户端的位置，包括主机客户端
        transform.position = position;
        transform.rotation = rotation;
        // 更新显示
        UpdateTeamDisplay(team);
    }

    // 玩家离开游戏时释放生成点
    public override void OnStopServer()
    {
        base.OnStopServer();
        ReleaseSpawnPoint();
    }

    // 主机玩家：在本地设置阵营
    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (isServer) // 主机玩家
        {
            string hostTeam = LanPlayContent.SelectedTeam;
            UpdateTeamDisplay(hostTeam);
            CmdSetTeam(hostTeam); //告诉服务器设置阵营
        }
        else // 客户端玩家
        {
            string clientTeam = LanPlayContent.ClientSelectedTeam;
            UpdateTeamDisplay(clientTeam);
            CmdSetTeam(clientTeam);
        }
    }

    [Command]//客户端调用，在服务器执行
    private void CmdSetTeam(string newTeam)
    {
        //服务器设置阵营，出生位置同步给所有客户端
        team = newTeam;
        SetSpawnPosition();
        RpcSyncTeam(team);
    }

    [ClientRpc]//同步阵营给所有客户端
    private void RpcSyncTeam(string syncedTeam)
    {
        team = syncedTeam;
        UpdateTeamDisplay(team);
    }
    //非本地玩家客户端开始时的处理
    public override void OnStartClient()
    {
        base.OnStartClient();
        if (!isLocalPlayer)
        {
            UpdateTeamDisplay(team);
        }
    }

    //重新游戏时重置所有生成点
    [Server]
    public static void ResetAllSpawnPoints()
    {
        // 创建新的列表，清空所有占用状态
        occupiedSpawns = new Dictionary<Transform, bool>();
       // Debug.Log("已重置所有生成点占用状态");
    }
}