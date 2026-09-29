using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
//*****************************************
//创建人：
//功能说明：在游戏场景中管理游戏回放与场景加载
//***************************************** 
public class GameManager : MonoBehaviour
{
    void Awake()
    {
        // 检查是否需要开始回放
        int shouldStartReplay = PlayerPrefs.GetInt("StartReplay", 0);
        string replayFileName = PlayerPrefs.GetString("ReplayFileName", "");

        if (shouldStartReplay == 1 && !string.IsNullOrEmpty(replayFileName))
        {
            //Debug.Log($"准备回放文件: {replayFileName}");
            PlayerPrefs.SetInt("StartReplay", 0);
            PlayerPrefs.SetString("ReplayFileName", "");
            PlayerPrefs.Save();

            // 延迟开始回放，确保所有对象已初始化
            StartCoroutine(DelayedStartReplay(replayFileName));
        }
        else if (shouldStartReplay == 1)
        {
            PlayerPrefs.SetInt("StartReplay", 0);
            PlayerPrefs.Save();
            //如果没有指定文件名，自动查找当前场景的最新回放
            StartCoroutine(DelayedStartReplay(null));
        }
    }

    IEnumerator DelayedStartReplay(string fileName)
    {
        //yield return new WaitForEndOfFrame();
        //yield return new WaitForEndOfFrame();
        //yield return new WaitForEndOfFrame();

        string currentSceneName = SceneManager.GetActiveScene().name;

        if (!string.IsNullOrEmpty(fileName))
        {
            var (frames, teamMode, sceneName, gameMode, gameTime) = ReplayDataManager.LoadReplayDaTaSetting(fileName);
            if (frames == null || frames.Count == 0)
            {
                // Debug.LogWarning($"无法加载回放文件: {fileName}");
                yield break;
            }
            if (teamMode != null)
            {
                SinglePlayerContent.SelectedTeam = teamMode;
                // Debug.Log($"回放阵营设置为: {teamMode}");
            }
            if (!string.IsNullOrEmpty(gameMode))
            {
                SinglePlayerContent.SelectedGameMode = gameMode;
                // Debug.Log($"回放游戏模式设置为: {gameMode}");
            }
            if (gameTime > 0)
            {
                SinglePlayerContent.SelectedGameTime = gameTime;
                // Debug.Log($"回放游戏时间设置为: {gameTime}秒");
            }
        }
        else
        {
            var replayFile = ReplayDataManager.GetLatestReplayForScene(currentSceneName);
            if (replayFile == null)
            {
                // Debug.LogWarning($"未找到当前场景({currentSceneName})的回放文件");
                yield break;
            }
            var (frames, teamMode, sceneName, gameMode, gameTime) = ReplayDataManager.LoadReplayDaTaSetting(replayFile.fileName);
            if (teamMode != null)
            {
                SinglePlayerContent.SelectedTeam = teamMode;
            }
            if (!string.IsNullOrEmpty(gameMode))
            {
                SinglePlayerContent.SelectedGameMode = gameMode;
            }
            if (gameTime > 0)
            {
                SinglePlayerContent.SelectedGameTime = gameTime;
            }
        }

        // 确保Replay脚本已初始化
        while (Replay.instance == null)
        {
            yield return null;
        }

        yield return new WaitForEndOfFrame();

        // Debug.Log("开始回放");
        Replay.instance.StopRecording();
        if (!string.IsNullOrEmpty(fileName))
        {
            Replay.instance.StartReplay(fileName);
        }
        else
        {
            Replay.instance.StartReplay();
        }
    }
}