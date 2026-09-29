using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.SceneManagement;
//*****************************************
//创建人：
//功能说明：回放系统的各物体的位置旋转录制与播放管理
//***************************************** 
public class ObjectFrameData
{
    public Vector3 position;
    public Quaternion rotation;
    public ObjectFrameData(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
    }
}

public class FrameData
{
    public float timestamp;
    public Dictionary<GameObject, ObjectFrameData> objectsData = new Dictionary<GameObject, ObjectFrameData>();
    public FrameData(float time)
    {
        timestamp = time;
    }
}

public class Replay : MonoBehaviour
{
    public static Replay instance;
    private List<GameObject> replayObjects = new List<GameObject>();
    private List<FrameData> recordedFrames = new List<FrameData>();
    private bool isRecording = true;
    private bool isReplaying = false;
    private bool isPaused = false;
    private float startTime;
    private int currentFrame = 0;
    private Dictionary<GameObject, List<MonoBehaviour>> controlScripts = new Dictionary<GameObject, List<MonoBehaviour>>();

    public delegate void ReplayEndEvent();
    public static event ReplayEndEvent OnReplayEnd;
    public delegate void ReplayStatusEvent(bool isReplaying);
    public static event ReplayStatusEvent CheckReplayImg;
    private string currentReplayFileName;

    private int replayTimer = 0;

    void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        StartCoroutine(DelayedInitialize());
        GoalScore.OnGameEnd += OnGameEnd;
    }

    IEnumerator DelayedInitialize()
    {
        //yield return new WaitForEndOfFrame();
        //yield return new WaitForEndOfFrame();
        yield return 0;
        InitializeObjs();
        startTime = Time.time;
    }

    void InitializeObjs()
    {
        replayObjects.Clear();
        controlScripts.Clear();

        StandardizeObjectNames();

        GameObject playerDrone = GameObject.FindGameObjectWithTag("Player");
        if (playerDrone != null)
        {
            replayObjects.Add(playerDrone);
            FindScripts(playerDrone);
        }

        GameObject[] allies = GameObject.FindGameObjectsWithTag("Ally");
        foreach (GameObject ally in allies)
        {
            replayObjects.Add(ally);
            FindScripts(ally);
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            replayObjects.Add(enemy);
            FindScripts(enemy);
        }
    }

    void StandardizeObjectNames()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        for (int i = 0; i < players.Length; i++)
        {
            players[i].name = $"Player_{i}";
        }

        GameObject[] allies = GameObject.FindGameObjectsWithTag("Ally");
        System.Array.Sort(allies, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        for (int i = 0; i < allies.Length; i++)
        {
            allies[i].name = $"Ally_{i}";
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        System.Array.Sort(enemies, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i].name = $"Enemy_{i}";
        }
    }

    void FindScripts(GameObject obj)
    {
        List<MonoBehaviour> scripts = new List<MonoBehaviour>();

        singleFPVcontrol fpvControl = obj.GetComponent<singleFPVcontrol>();
        if (fpvControl != null)
            scripts.Add(fpvControl);
        AIDroneMovement aiControl = obj.GetComponent<AIDroneMovement>();
        if (aiControl != null)
        {
            scripts.Add(aiControl);
        }
        Map2AIDroneMovement map2AIControl = obj.GetComponent<Map2AIDroneMovement>();
        if (map2AIControl != null)
        {
            scripts.Add(map2AIControl);
            // Debug.Log($"找到AI脚本: {map2AIControl} 在 {obj.name}");
        }
        if (scripts.Count > 0)
        {
            controlScripts[obj] = scripts;
            // Debug.Log("存入数组中");
        }
    }

    void DisableScripts()
    {
        //  Debug.Log("禁用脚本111");
        foreach (var kvp in controlScripts)
            foreach (MonoBehaviour script in kvp.Value)
                if (script != null)
                    script.enabled = false;
        // Debug.Log("禁用脚本222");
    }

    void EnableScripts()
    {
        foreach (var kvp in controlScripts)
            foreach (MonoBehaviour script in kvp.Value)
                if (script != null)
                    script.enabled = true;
    }

    void SetRb(bool freeze)
    {
        foreach (GameObject obj in replayObjects)
        {
            if (obj == null) continue;
            Rigidbody rb = obj.GetComponent<Rigidbody>();
            if (rb == null) continue;
            if (freeze)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
            else rb.isKinematic = false;
        }
    }

    void Update()
    {
        if (isReplaying)
            keyControl();

        if (isRecording)
        {
            replayTimer++;
            if (replayTimer % 3 == 0)
            {
                RecordFrame();
            }
        }
        else if (isReplaying && !isPaused)
        {
            ReplayFrame();
        }
    }

    void RecordFrame()
    {
        if (!isRecording) return;

        float currentTime = Time.time - startTime;
        FrameData frame = new FrameData(currentTime);
        foreach (GameObject obj in replayObjects)
        {
            if (obj != null)
                frame.objectsData[obj] = new ObjectFrameData(obj.transform.position, obj.transform.rotation);
        }
        recordedFrames.Add(frame);
    }

    public void StopRecording()
    {
        isRecording = false;
    }

    public void StartRecording()
    {
        isRecording = true;
        replayTimer = 0;
    }

    void OnGameEnd()
    {
        if (isRecording)
        {
            isRecording = false;
            string currentTeam = SinglePlayerContent.SelectedTeam;
            string currentScene = SceneManager.GetActiveScene().name;
            string currentGameMode = SinglePlayerContent.SelectedGameMode;
            float currentGameTime = SinglePlayerContent.SelectedGameTime;
            
            currentReplayFileName = ReplayDataManager.SaveReplayData(recordedFrames, currentTeam, currentScene, currentGameMode, currentGameTime);
        }
    }

    float GetRecordedDuration()
    {
        if (recordedFrames.Count == 0) return 0f;
        return recordedFrames[recordedFrames.Count - 1].timestamp;
    }

    void ReplayFrame()
    {
        float currentTime = Time.time - startTime;
        while (currentFrame < recordedFrames.Count - 1 && recordedFrames[currentFrame + 1].timestamp <= currentTime)
            currentFrame++;
        if (currentFrame < recordedFrames.Count)
        {
            FrameData frame = recordedFrames[currentFrame];
            foreach (var kvp in frame.objectsData)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.transform.position = kvp.Value.position;
                    kvp.Key.transform.rotation = kvp.Value.rotation;
                }
            }
            if (currentFrame >= recordedFrames.Count - 1 && currentTime > frame.timestamp + 0.5f)
                StopReplay();
        }
    }

    void keyControl()
    {
        if (Input.GetKeyDown(KeyCode.Space)) PauseReplay();
        if (Input.GetKeyDown(KeyCode.Z)) RestartReplay();
    }

    void PauseReplay()
    {
        isPaused = !isPaused;
        if (!isPaused && currentFrame < recordedFrames.Count)
        {
            FrameData currentFrameData = recordedFrames[currentFrame];
            startTime = Time.time - currentFrameData.timestamp;
        }
    }

    void RestartReplay()
    {
        if (!isReplaying || recordedFrames.Count == 0) return;
        isPaused = false;
        currentFrame = 0;
        if (recordedFrames.Count > 0)
        {
            FrameData firstFrame = recordedFrames[0];
            foreach (var kvp in firstFrame.objectsData)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.transform.position = kvp.Value.position;
                    kvp.Key.transform.rotation = kvp.Value.rotation;
                }
            }
        }
        startTime = Time.time;
    }
    public void StartReplay(string fileName = null)
    {
        StartCoroutine(SimpleDelayedStartReplay(fileName));
    }

    IEnumerator SimpleDelayedStartReplay(string fileName)
    {
        for (int i = 0; i < 3; i++)
        {
            yield return new WaitForEndOfFrame();
        }
        List<FrameData> loadedFrames = null;
        string currentScene = SceneManager.GetActiveScene().name;

        if (!string.IsNullOrEmpty(fileName))
        {
            var (frames, teamMode, sceneName, gameMode, gameTime) = ReplayDataManager.LoadReplayDaTaSetting(fileName);
            loadedFrames = frames;
            currentReplayFileName = fileName;

            // 设置游戏模式和游戏时间
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

        if (loadedFrames == null || loadedFrames.Count == 0)
        {
            // 如果没有指定文件名，查找当前场景的最新回放
            var replayFile = ReplayDataManager.GetLatestReplayForScene(currentScene);
            if (replayFile != null)
            {
                var (frames, teamMode, sceneName, gameMode, gameTime) = ReplayDataManager.LoadReplayDaTaSetting(replayFile.fileName);
                loadedFrames = frames;
                currentReplayFileName = replayFile.fileName;
                if (!string.IsNullOrEmpty(gameMode))
                {
                    SinglePlayerContent.SelectedGameMode = gameMode;
                }
                if (gameTime > 0)
                {
                    SinglePlayerContent.SelectedGameTime = gameTime;
                }
                //Debug.Log($"加载当前场景({currentScene})的回放: {replayFile.fileName}");
            }
            else
            {
                var allReplayFiles = ReplayDataManager.GetAllReplayFiles();
                if (allReplayFiles.Count > 0)
                {
                    var (frames, teamMode, sceneName, gameMode, gameTime) = ReplayDataManager.LoadReplayDaTaSetting(allReplayFiles[0].fileName);
                    loadedFrames = frames;
                    currentReplayFileName = allReplayFiles[0].fileName;
                    if (!string.IsNullOrEmpty(gameMode))
                    {
                        SinglePlayerContent.SelectedGameMode = gameMode;
                    }
                    if (gameTime > 0)
                    {
                        SinglePlayerContent.SelectedGameTime = gameTime;
                    }
                }
            }
        }

        if (loadedFrames != null && loadedFrames.Count > 0)
        {
            recordedFrames = loadedFrames;
        }
        else if (recordedFrames.Count == 0)
        {
            //Debug.LogWarning($"未找到有效的回放文件，当前场景: {currentScene}");
        }
        InitializeObjs();
        DisableScripts();
        SetRb(true);
        isReplaying = true;
        isPaused = false;
        currentFrame = 0;
        CheckReplayImg?.Invoke(true);

        if (recordedFrames.Count > 0)
        {
            FrameData firstFrame = recordedFrames[0];
            foreach (var kvp in firstFrame.objectsData)
            {
                if (kvp.Key != null)
                {
                    kvp.Key.transform.position = kvp.Value.position;
                    kvp.Key.transform.rotation = kvp.Value.rotation;
                }
            }
        }

        startTime = Time.time;
    }

    void StopReplay()
    {
        isReplaying = false;
        isPaused = false;
        EnableScripts();
        SetRb(false);
        CheckReplayImg?.Invoke(false);
        OnReplayEnd?.Invoke();
    }

    public void ClearRecordedData()
    {
        recordedFrames.Clear();
        replayObjects.Clear();
        controlScripts.Clear();
        isRecording = true;
        isReplaying = false;
        isPaused = false;
        currentFrame = 0;
        startTime = Time.time;
        replayTimer = 0;

        string filePath = Application.persistentDataPath + "/replay_data.txt";
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }

        string replayDir = Application.persistentDataPath + "/Replays/";
        if (Directory.Exists(replayDir))
        {
            string[] replayFiles = Directory.GetFiles(replayDir, "*.replay");
            foreach (string file in replayFiles)
            {
                try
                {
                    File.Delete(file);
                }
                catch (System.Exception e)
                {
                    // Debug.LogError($"删除回放文件失败: {file}, 错误: {e.Message}");
                }
            }
        }
    }
    //暂不使用删除
    public void ClearCurrentSceneReplayData()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        ReplayDataManager.DeleteReplayFilesForScene(currentScene);
    }

    void OnDestroy()
    {
        GoalScore.OnGameEnd -= OnGameEnd;
    }
    public void GetCurrentReplayInfo()
    {
        if (string.IsNullOrEmpty(currentReplayFileName) || recordedFrames.Count == 0)
            return;

        float duration = GetRecordedDuration();
        string sceneName = SceneManager.GetActiveScene().name;

        return;
    }

    public bool HasReplayForCurrentScene()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        var replayFile = ReplayDataManager.GetLatestReplayForScene(currentScene);
        return replayFile != null;
    }
}