using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
//*****************************************
//创建人： Mezcal
//功能说明：
//***************************************** 

public class KeyControlPanel : MonoBehaviour
{
    public Button wButton, sButton, aButton, dButton, iButton, jButton, kButton, lButton;
    public AudioClip wClip, sClip, aClip, dClip, iClip, jClip, kClip, lClip;
    private AudioSource audioSource;
    private Color normalColor = Color.white; // 正常颜色
    private Color highlightedColor = Color.yellow; // 高亮颜色

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        // 初始化按钮颜色
        SetButtonColor(wButton, normalColor);
        SetButtonColor(sButton, normalColor);
        SetButtonColor(aButton, normalColor);
        SetButtonColor(dButton, normalColor);
        SetButtonColor(iButton, normalColor);
        SetButtonColor(jButton, normalColor);
        SetButtonColor(kButton, normalColor);
        SetButtonColor(lButton, normalColor);
    }

    void Update()
    {
        // 检测按键按下
        if (Input.GetKeyDown(KeyCode.W))
        {
            SetButtonColor(wButton, highlightedColor);
            audioSource.PlayOneShot(wClip);
            Debug.Log("上升");
        }
        else if (Input.GetKeyUp(KeyCode.W))
        {
            SetButtonColor(wButton, normalColor);
        }

        if (Input.GetKeyDown(KeyCode.S))
        {
            SetButtonColor(sButton, highlightedColor);
            audioSource.PlayOneShot(sClip);
            Debug.Log("下降");
        }
        else if (Input.GetKeyUp(KeyCode.S))
        {
            SetButtonColor(sButton, normalColor);
        }

        if (Input.GetKeyDown(KeyCode.A))
        {
            SetButtonColor(aButton, highlightedColor);
            audioSource.PlayOneShot(aClip);
            Debug.Log("自身右转");
        }
        else if (Input.GetKeyUp(KeyCode.A))
        {
            SetButtonColor(aButton, normalColor);
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            SetButtonColor(dButton, highlightedColor);
            audioSource.PlayOneShot(dClip);
            Debug.Log("自身左转");
        }
        else if (Input.GetKeyUp(KeyCode.D))
        {
            SetButtonColor(dButton, normalColor);
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            SetButtonColor(iButton, highlightedColor);
            audioSource.PlayOneShot(iClip);
            Debug.Log("前进");
        }
        else if (Input.GetKeyUp(KeyCode.I))
        {
            SetButtonColor(iButton, normalColor);
        }

        if (Input.GetKeyDown(KeyCode.J))
        {
            SetButtonColor(jButton, highlightedColor);
            audioSource.PlayOneShot(jClip);
            Debug.Log("左移");
        }
        else if (Input.GetKeyUp(KeyCode.J))
        {
            SetButtonColor(jButton, normalColor);
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            SetButtonColor(kButton, highlightedColor);
            audioSource.PlayOneShot(kClip);
            Debug.Log("后退");
        }
        else if (Input.GetKeyUp(KeyCode.K))
        {
            SetButtonColor(kButton, normalColor);
        }
        if (Input.GetKeyDown(KeyCode.L))
        {
            // 将指定按钮的颜色设置为高亮颜色
            SetButtonColor(lButton, highlightedColor);
            // 播放对应的音频片段
            audioSource.PlayOneShot(lClip);
            Debug.Log("右移");
        }
        else if (Input.GetKeyUp(KeyCode.L))
        {
            // 将按钮颜色恢复为默认颜色
            SetButtonColor(lButton, normalColor);
        }
    }
    // 设置按钮的显示颜色
    void SetButtonColor(Button button, Color color)
    {
        if (button != null)
        {
            // 获取按钮的Image组件
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                // 修改颜色属性
                buttonImage.color = color;
            }
        }
    }
}