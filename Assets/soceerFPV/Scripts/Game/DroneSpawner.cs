using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人：
//功能说明：按阵营生成敌军和友军
//***************************************** 
public class DroneSpawner : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject allyPrefab;
    public GameObject enemyPrefab;
    public int allyCount = 2;
    public int enemyCount = 3;

    [Header("位置设置")]
    public float allyDistance = 1f;
    public float enemyDistance = 4.5f;
    [Header("敌军X轴偏移位置")]
    public float[] enemyXOffsets = new float[] { -2f, -1f, 0f };

    void Start()
    {
        // 延迟生成，确保回放信息已加载
        StartCoroutine(DelayedSpawn());
    }

    IEnumerator DelayedSpawn()
    {
        // 等待一帧，确保GameManager和其他初始化完成
        //yield return null;
        yield return new WaitForEndOfFrame();
        //检查游戏模式，如果是单人模式则禁用脚本
        string gameMode = SinglePlayerContent.SelectedGameMode;
        if (gameMode == "单人")
        {
            // Debug.Log("单人模式");
            enabled = false; 
            yield break; 
        }

        // 检查是否是回放模式
        int shouldStartReplay = PlayerPrefs.GetInt("StartReplay", 0);
        bool isReplayMode = (shouldStartReplay == 1);

        // 获取玩家位置和前方方向
        Transform player = GameObject.FindGameObjectWithTag("Player").transform;
        Vector3 playerPos = player.position;
        Vector3 playerForward = player.forward;

        // 读取阵营信息
        string playerTeam = SinglePlayerContent.SelectedTeam;


        // 根据阵营选择生成不同的敌友
        if (playerTeam == "Red")
        {
            SpawnAllies(playerPos, playerForward, allyPrefab, "Ally");
            SpawnEnemies(playerPos, playerForward, enemyPrefab, "Enemy");
            // Debug.Log("生成红方");
        }
        else
        {
            SpawnAllies(playerPos, playerForward, enemyPrefab, "Ally");
            SpawnEnemies(playerPos, playerForward, allyPrefab, "Enemy");
            // Debug.Log("生成蓝方");
        }
    }

    void SpawnAllies(Vector3 playerPos, Vector3 playerForward, GameObject prefab, string tag)
    {
        Vector3 rightDirection = Vector3.Cross(playerForward, Vector3.up).normalized;

        // 玩家左右生成友军
        for (int i = 0; i < allyCount; i++)
        {
            float side = (i % 2 == 0) ? 1f : -1f;
            Vector3 spawnPos = playerPos + rightDirection * allyDistance * side;

            GameObject ally = Instantiate(prefab, spawnPos, Quaternion.identity);
            ally.tag = tag;
        }
    }

    void SpawnEnemies(Vector3 playerPos, Vector3 playerForward, GameObject prefab, string tag)
    {
        for (int i = 0; i < enemyCount; i++)
        {
            // 如果enemyXOffsets长度不够，使用默认值
            float xOffset = (i < enemyXOffsets.Length) ? enemyXOffsets[i] : i * 1.0f;
            Vector3 spawnPos = new Vector3(xOffset, 0.8f, enemyDistance);
            GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
            enemy.tag = tag;
            // -z的朝向
            enemy.transform.rotation = Quaternion.LookRotation(Vector3.back);
        }
    }
}