using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
//*****************************************
//创建人：
//功能说明：游戏内的设置菜单，暂停和功能显示
//***************************************** 
public class SetMenu : MonoBehaviour
{
    public GameObject setCanvas;
    public GameObject[] uiComponents;

    private bool allowEsc = true;

    private void Start()
    {
        GoalScore.OnGameEnd += OnGameTimeEnd;
        Replay.OnReplayEnd += OnReplayEnd; 
        hideComponents();
    }

    void Update()
    {
        if (allowEsc && Input.GetKeyDown(KeyCode.Escape))
        {
            if (setCanvas != null)
            {
                bool isActive = setCanvas.activeSelf;
                setCanvas.SetActive(!isActive);

                if (!isActive)
                {
                    hideComponents();
                    Time.timeScale = 0f;
                    AudioListener.pause = true;
                }
                else
                {
                    Time.timeScale = 1f;
                    AudioListener.pause = false;
                }
            }
        }
    }
    void OnGameTimeEnd()
    {
        if (setCanvas != null && !setCanvas.activeSelf)
        {
            allowEsc = false;
            ShowComponents();
            setCanvas.SetActive(true);
            Time.timeScale = 0f;
            AudioListener.pause = true;
        }
    }
    void OnReplayEnd()
    {
       // Debug.Log(" طŽ           ʾ ˵ ");
        allowEsc = false; 
        ShowComponents();
        setCanvas.SetActive(true);
        Time.timeScale = 0f;
        AudioListener.pause = true;
    }

    void hideComponents()
    {
        if (uiComponents != null)
        {
            foreach (GameObject component in uiComponents)
            {
                if (component != null)
                {
                    component.SetActive(false);
                }
            }
        }
    }
    void ShowComponents()
    {
        if (uiComponents != null)
        {
            foreach (GameObject component in uiComponents)
            {
                if (component != null)
                {
                    component.SetActive(true);
                }
            }
        }
    }

    public void ReturnToMainMenu()
    {
        GoalScore.OnGameEnd -= OnGameTimeEnd;
        Replay.OnReplayEnd -= OnReplayEnd; 

        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene("soceerFPVui");
    }
    public void ReStart()
    {
        GoalScore.OnGameEnd -= OnGameTimeEnd;
        Replay.OnReplayEnd -= OnReplayEnd;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        //    ¼  س   
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void OnDestroy()
    {
        GoalScore.OnGameEnd -= OnGameTimeEnd;
        Replay.OnReplayEnd -= OnReplayEnd; 
    }
}