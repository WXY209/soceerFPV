using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
//*****************************************
//创建人：
//功能说明：回放数据的二进制序列化存储与加载
//***************************************** 
[System.Serializable]
public class SerializableObjectFrameData
{
    public string position;
    public string rotation;

    public SerializableObjectFrameData(Vector3 pos, Quaternion rot)
    {
        position = $"{pos.x},{pos.y},{pos.z}";
        rotation = $"{rot.x},{rot.y},{rot.z},{rot.w}";
    }

    public Vector3 GetPosition()
    {
        string[] parts = position.Split(',');
        return new Vector3(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]));
    }

    public Quaternion GetRotation()
    {
        string[] parts = rotation.Split(',');
        return new Quaternion(float.Parse(parts[0]), float.Parse(parts[1]), float.Parse(parts[2]), float.Parse(parts[3]));
    }
}

[System.Serializable]
public class SerializableFrameData
{
    public float timestamp;
    public SerializableObjectData[] objectsData;
    public string saveTime;
    public string teamMode;
    public string sceneName;
    //对游戏模式和游戏时间字段进行记录
    public string gameMode;
    public float gameTime;

    [System.Serializable]
    public class SerializableObjectData
    {
        public string objectName;
        public SerializableObjectFrameData frameData;
    }
}

[System.Serializable]
public class ReplayFileInfo
{
    public string fileName;
    public string saveTime;
    public string displayTime;
    public string sceneName;
    public int index;
}

public static class ReplayDataManager
{
    private static string SaveDirectory => Application.persistentDataPath + "/Replays/";
    private const int MAX_REPLAY_FILES = 10;
    private const string FILE_EXTENSION = ".replay";

    public static List<ReplayFileInfo> GetAllReplayFiles()
    {
        List<ReplayFileInfo> replayFiles = new List<ReplayFileInfo>();

        if (!Directory.Exists(SaveDirectory))
            return replayFiles;

        string[] files = Directory.GetFiles(SaveDirectory, "*" + FILE_EXTENSION);

        foreach (string file in files)
        {
            try
            {
                Wrapper wrapper = LoadBinaryData(file);
                if (wrapper != null && wrapper.frames != null && wrapper.frames.Length > 0)
                {
                    replayFiles.Add(new ReplayFileInfo
                    {
                        fileName = Path.GetFileName(file),
                        saveTime = wrapper.frames[0].saveTime,
                        displayTime = FormatDisplayTime(wrapper.frames[0].saveTime),
                        sceneName = wrapper.frames[0].sceneName
                    });
                }
            }
            catch (Exception e)
            {
                // Debug.LogWarning($"加载回放文件信息失败: {e.Message}");
            }
        }

        replayFiles = replayFiles
            .OrderByDescending(f => f.saveTime)
            .ToList();

        if (replayFiles.Count > MAX_REPLAY_FILES)
        {
            for (int i = MAX_REPLAY_FILES; i < replayFiles.Count; i++)
            {
                DeleteReplayFile(replayFiles[i].fileName);
            }
            replayFiles = replayFiles.Take(MAX_REPLAY_FILES).ToList();
        }

        return replayFiles;
    }
    public static List<ReplayFileInfo> GetReplayFilesForScene(string sceneName)
    {
        var allFiles = GetAllReplayFiles();
        return allFiles.Where(f => f.sceneName == sceneName).ToList();
    }
    public static ReplayFileInfo GetLatestReplayForScene(string sceneName)
    {
        var sceneFiles = GetReplayFilesForScene(sceneName);
        return sceneFiles.OrderByDescending(f => f.saveTime).FirstOrDefault();
    }

    private static string FormatDisplayTime(string saveTime)
    {
        if (DateTime.TryParse(saveTime, out DateTime time))
        {
            return time.ToString("yyyy-MM-dd HH:mm:ss");
        }
        return saveTime;
    }

    private static string GenerateNewFileName(string sceneName = null)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string scenePrefix = string.IsNullOrEmpty(sceneName) ? "" : $"{sceneName}_";
        return $"{scenePrefix}replay_{timestamp}" + FILE_EXTENSION;
    }

    //保存回放数据-添加了游戏模式与游戏时间
    public static string SaveReplayData(List<FrameData> recordedFrames, string teamMode, string sceneName, string gameMode = null, float gameTime = 0f)
    {
        if (!Directory.Exists(SaveDirectory))
            Directory.CreateDirectory(SaveDirectory);

        SerializableFrameData[] serializableFrames = new SerializableFrameData[recordedFrames.Count];
        string saveTime = DateTime.Now.ToString("o");

        for (int i = 0; i < recordedFrames.Count; i++)
        {
            FrameData frame = recordedFrames[i];
            List<SerializableFrameData.SerializableObjectData> objectDataList =
                new List<SerializableFrameData.SerializableObjectData>();

            foreach (var kvp in frame.objectsData)
            {
                if (kvp.Key == null) continue;
                objectDataList.Add(new SerializableFrameData.SerializableObjectData
                {
                    objectName = kvp.Key.name,
                    frameData = new SerializableObjectFrameData(kvp.Value.position, kvp.Value.rotation)
                });
            }
            serializableFrames[i] = new SerializableFrameData
            {
                timestamp = frame.timestamp,
                saveTime = saveTime,
                objectsData = objectDataList.ToArray(),
                teamMode = teamMode,
                sceneName = sceneName,
                gameMode = gameMode ?? SinglePlayerContent.SelectedGameMode,
                gameTime = gameTime > 0 ? gameTime : SinglePlayerContent.SelectedGameTime
            };
        }
        CleanupOldFiles();
        string fileName = GenerateNewFileName(sceneName);
        string filePath = SaveDirectory + fileName;
        SaveBinaryData(new Wrapper { frames = serializableFrames }, filePath);
        //Debug.Log($"回放数据已保存至: {filePath}，场景: {sceneName}，游戏模式: {gameMode}，游戏时间: {gameTime}");
        return fileName;
    }

    //public static string SaveReplayData(List<FrameData> recordedFrames, string teamMode, string sceneName)
    //{
    //    return SaveReplayData(recordedFrames, teamMode, sceneName, SinglePlayerContent.SelectedGameMode, SinglePlayerContent.SelectedGameTime);
    //}

    private static void CleanupOldFiles()
    {
        if (!Directory.Exists(SaveDirectory))
            return;

        var files = Directory.GetFiles(SaveDirectory, "*" + FILE_EXTENSION)
            .Select(f => new FileInfo(f))
            .OrderByDescending(f => f.LastWriteTime)
            .ToList();

        for (int i = MAX_REPLAY_FILES - 1; i < files.Count; i++)
        {
            try
            {
                files[i].Delete();
            }
            catch (Exception e)
            {
                // Debug.LogWarning($"删除旧回放文件失败: {e.Message}");
            }
        }
    }

    public static List<FrameData> LoadReplayData(string fileName)
    {
        var (frames, teamMode, sceneName, gameMode, gameTime) = LoadReplayDaTaSetting(fileName);
        return frames;
    }

    public static (List<FrameData>, string, string) LoadReplayDataWithTeamAndScene(string fileName)
    {
        var (frames, teamMode, sceneName, gameMode, gameTime) = LoadReplayDaTaSetting(fileName);
        return (frames, teamMode, sceneName);
    }

    //加载方法
    public static (List<FrameData>, string, string, string, float) LoadReplayDaTaSetting(string fileName)
    {
        string filePath = SaveDirectory + fileName;

        if (!File.Exists(filePath))
        {
            // Debug.LogWarning($"回放文件不存在: {filePath}");
            return (null, null, null, null, 0f);
        }

        Wrapper wrapper = LoadBinaryData(filePath);
        if (wrapper == null || wrapper.frames == null || wrapper.frames.Length == 0)
        {
            //Debug.LogWarning($"回放文件格式错误: {filePath}");
            return (null, null, null, null, 0f);
        }

        string teamMode = wrapper.frames[0].teamMode;
        string sceneName = wrapper.frames[0].sceneName;
    
        string gameMode = wrapper.frames[0].gameMode;
        float gameTime = wrapper.frames[0].gameTime;

        List<FrameData> frames = new List<FrameData>();

        Dictionary<string, GameObject> objectMap = BuildObjectMap();

        foreach (SerializableFrameData sFrame in wrapper.frames)
        {
            FrameData frame = new FrameData(sFrame.timestamp);

            if (sFrame.objectsData != null)
            {
                foreach (var objData in sFrame.objectsData)
                {
                    GameObject obj = FindGameObjectByName(objData.objectName, objectMap);
                    if (obj != null)
                    {
                        Vector3 pos = objData.frameData.GetPosition();
                        Quaternion rot = objData.frameData.GetRotation();
                        frame.objectsData[obj] = new ObjectFrameData(pos, rot);
                    }
                }
            }
            frames.Add(frame);
        }

        return (frames, teamMode, sceneName, gameMode, gameTime);
    }

    //public static (List<FrameData>, string) LoadReplayDataWithTeam(string fileName)
    //{
    //    var (frames, teamMode, sceneName, gameMode, gameTime) = LoadReplayDaTaSetting(fileName);
    //    return (frames, teamMode);
    //}

    private static Dictionary<string, GameObject> BuildObjectMap()
    {
        Dictionary<string, GameObject> map = new Dictionary<string, GameObject>();

        string[] tags = { "Player", "Ally", "Enemy" };

        foreach (string tag in tags)
        {
            GameObject[] objects = GameObject.FindGameObjectsWithTag(tag);
            foreach (GameObject obj in objects)
            {
                string standardizedName = GetStandardizedName(obj, tag);
                map[standardizedName] = obj;

                if (!map.ContainsKey(obj.name))
                {
                    map[obj.name] = obj;
                }
            }
        }

        return map;
    }

    private static string GetStandardizedName(GameObject obj, string tag)
    {
        if (obj.name.StartsWith(tag + "_"))
        {
            return obj.name;
        }

        GameObject[] sameTagObjects = GameObject.FindGameObjectsWithTag(tag);
        System.Array.Sort(sameTagObjects, (a, b) => a.transform.position.x.CompareTo(b.transform.position.x));

        for (int i = 0; i < sameTagObjects.Length; i++)
        {
            if (sameTagObjects[i] == obj)
            {
                return $"{tag}_{i}";
            }
        }

        return obj.name;
    }

    private static GameObject FindGameObjectByName(string objectName, Dictionary<string, GameObject> objectMap)
    {
        if (objectMap.ContainsKey(objectName))
        {
            return objectMap[objectName];
        }

        string cleanName = objectName.Replace("(Clone)", "").Trim();
        if (objectMap.ContainsKey(cleanName))
        {
            return objectMap[cleanName];
        }

        string[] nameParts = objectName.Split('_');
        if (nameParts.Length >= 2)
        {
            string baseName = nameParts[0];
            string standardizedName = $"{baseName}_{nameParts[1]}";
            if (objectMap.ContainsKey(standardizedName))
            {
                return objectMap[standardizedName];
            }
        }

        return GameObject.Find(objectName);
    }

    public static void DeleteReplayFile(string fileName)
    {
        string filePath = SaveDirectory + fileName;
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
    //删除方案-暂时不用
    public static void DeleteReplayFilesForScene(string sceneName)
    {
        var sceneFiles = GetReplayFilesForScene(sceneName);
        foreach (var fileInfo in sceneFiles)
        {
            DeleteReplayFile(fileInfo.fileName);
        }
    }

    private static void SaveBinaryData(Wrapper data, string filePath)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(filePath, FileMode.Create))
        {
            formatter.Serialize(stream, data);
        }
    }

    private static Wrapper LoadBinaryData(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        BinaryFormatter formatter = new BinaryFormatter();
        using (FileStream stream = new FileStream(filePath, FileMode.Open))
        {
            try
            {
                return (Wrapper)formatter.Deserialize(stream);
            }
            catch (Exception e)
            {
                //Debug.LogError($"加载回放文件失败: {e.Message}");
                return null;
            }
        }
    }

    [System.Serializable]
    private class Wrapper
    {
        public SerializableFrameData[] frames;
    }
}