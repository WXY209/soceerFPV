using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;
//*****************************************
//创建人：
//功能说明：回放界面中文件ui选择和管理
//***************************************** 
public class ReplayPanel : MonoBehaviour
{
    [Header("UI设置")]
    public Transform contentParent; 

    private List<ReplayFileInfo> replayFiles = new List<ReplayFileInfo>();
    private List<Transform> replayItemTransforms = new List<Transform>(); 

    void Start()
    {
        //获取所有初始化UI项
        InitializeReplayItems();
        //加载并显示回放文件
        RefreshReplayList();
        //Debug.Log($"已加载 {replayFiles.Count} 个回放文件");
    }

    void InitializeReplayItems()
    {
        replayItemTransforms.Clear();
        for (int i = 0; i < contentParent.childCount; i++)
        {
            Transform child = contentParent.GetChild(i);
            replayItemTransforms.Add(child);
        }
       // Debug.Log($"找到 {replayItemTransforms.Count} 个回放项");
    }
    void RefreshReplayList()
    {
        replayFiles = ReplayDataManager.GetAllReplayFiles();
        //为每个固定位置分配对应的回放文件
        for (int i = 0; i < replayItemTransforms.Count; i++)
        {
            Transform itemTransform = replayItemTransforms[i];
            Text timeText = FindTextComponent(itemTransform, "时间戳text");
            Button replayButton = FindButtonComponent(itemTransform, "回放button");

            if (timeText == null || replayButton == null)
            {
               // Debug.LogWarning($"回放项 {i} 缺少必要的UI组件");
                continue;
            }
            replayButton.onClick.RemoveAllListeners();

            if (i < replayFiles.Count)
            {
                ReplayFileInfo fileInfo = replayFiles[i];
                timeText.text = fileInfo.displayTime;
                replayButton.interactable = true;
                //为按钮绑定对应的文件索引
                int fileIndex = i; 
                replayButton.onClick.AddListener(() => OnReplayButtonClickedByIndex(fileIndex));
            }
            else // 没有对应的回放文件
            {
                // 显示"暂无回放"
                timeText.text = "暂无回放";

                // 禁用按钮
                replayButton.interactable = false;
            }
        }
    }

    //根据索引查找Text组件和button组件
    Text FindTextComponent(Transform parent, string name)
    {
        Transform textTransform = parent.Find(name);
        if (textTransform != null)
            return textTransform.GetComponent<Text>();
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child.GetComponent<Text>();
            Text text = child.GetComponentInChildren<Text>(true);
            if (text != null && text.name == name)
                return text;
        }
        return null;
    }
    Button FindButtonComponent(Transform parent, string name)
    {
        Transform buttonTransform = parent.Find(name);
        if (buttonTransform != null)
            return buttonTransform.GetComponent<Button>();
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child.GetComponent<Button>();
            Button button = child.GetComponentInChildren<Button>(true);
            if (button != null && button.name == name)
                return button;
        }

        return null;
    }

    void OnReplayButtonClickedByIndex(int index)
    {
        if (index >= 0 && index < replayFiles.Count)
        {
            ReplayFileInfo fileInfo = replayFiles[index];
            string fileName = fileInfo.fileName;
            string sceneName = fileInfo.sceneName; // 获取回放对应的场景名
            // 使用PlayerPrefs传递文件名
            PlayerPrefs.SetString("ReplayFileName", fileName);
            PlayerPrefs.SetInt("StartReplay", 1);
            PlayerPrefs.Save();
            // 根据回放文件的场景名加载对应的场景
            SceneManager.LoadScene(sceneName);
        }
    }
    public void OnRefreshButtonClicked()
    {
        RefreshReplayList();
    }
    //开始新游戏
    public void OnStartGameButtonClicked()
    {
        PlayerPrefs.SetInt("StartReplay", 0);
        PlayerPrefs.SetString("ReplayFileName", "");
        PlayerPrefs.Save();
        SceneManager.LoadScene("Map1");
    }

    //删除指定回放文件-暂时不需要
    public void OnDeleteReplayButtonClicked(int index)
    {
        if (index >= 0 && index < replayFiles.Count)
        {
            string fileName = replayFiles[index].fileName;
            ReplayDataManager.DeleteReplayFile(fileName);
            RefreshReplayList();
        }
    }
}