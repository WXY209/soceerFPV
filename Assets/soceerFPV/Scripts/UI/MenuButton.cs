using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.UI;
//*****************************************
//创建人：
//功能说明：按钮的悬停动画和音效
//***************************************** 
[RequireComponent(typeof(Button))]
public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Color hoverColor = new Color(1, 1, 0, 0.8f);//按钮配置
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float shakeDuration = 0.1f;
    [SerializeField] private float shakeStrength = 1f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip hoverSound; // 悬停音效
    [SerializeField] private AudioClip clickSound; // 点击音效
    [SerializeField] private float volume = 1.0f; // 音量

    private Vector3 originalPos;
    private Color originalColor;
    private Image buttonImage;
    private Button button;
    private AudioSource audioSource;

    void Start()
    {
        buttonImage = GetComponent<Image>();//获取图片组件，位置，颜色
        button = GetComponent<Button>();
        originalPos = transform.position;
        originalColor = buttonImage.color;

        // 获取或添加AudioSource组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = volume;
        audioSource.playOnAwake = false;

        // 为按钮点击添加音效
        button.onClick.AddListener(PlayClickSound);
    }

    public void OnPointerEnter(PointerEventData eventData)//移入时两个协程
    {
        StartCoroutine(FadeToColor(hoverColor));
        StartCoroutine(ShakeEffect());
        PlayHoverSound();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        StartCoroutine(FadeToColor(originalColor));
    }

    IEnumerator FadeToColor(Color targetColor)//颜色渐变
    {
        float elapsed = 0;
        Color startColor = buttonImage.color;

        while (elapsed < fadeDuration)
        {
            buttonImage.color = Color.Lerp(startColor, targetColor, elapsed / fadeDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        buttonImage.color = targetColor;
    }

    IEnumerator ShakeEffect()
    {
        float elapsed = 0;
        originalPos = transform.position;
        while (elapsed < shakeDuration)
        {
            transform.position += Random.insideUnitSphere * shakeStrength;//生成球体内部随机点？
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = originalPos;
    }

    // 播放悬停音效
    private void PlayHoverSound()
    {
        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    // 播放点击音效
    private void PlayClickSound()
    {
        if (clickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(clickSound);
        }
    }

    // 清理事件监听
    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(PlayClickSound);
        }
    }
}