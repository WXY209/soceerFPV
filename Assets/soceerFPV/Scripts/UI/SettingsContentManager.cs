using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
//*****************************************
//创建人：
//功能说明：选项界面的各内容面板管理，以及内容面板的内容功能
//注意：ApplyResolution方法会导致虽然成功修改分辨率，但是ui不能自适应而可能遮挡部分ui，导致无法恢复。
//          又因为分辨率设置后是强制更改项目设置，后续导出还是会保持错误的分辨率，而陷入困境，所以暂时注释掉。或者使用clearResloution脚本。
//***************************************** 
public class SettingsContentManager : MonoBehaviour
{
    [Header("内容面板")]
    [SerializeField] private GameObject controllerContent;
    [SerializeField] private GameObject gameSettingsContent;
    [SerializeField] private GameObject graphicsContent;

    [Header("选项卡按钮")]
    [SerializeField] private Button controllerTab;
    [SerializeField] private Button gameSettingsTab;
    [SerializeField] private Button graphicsTab;

    [Header("动画设置")]
    [SerializeField] private float fadeDuration = 1f;

    [Header("悬停效果设置")]
    [SerializeField] private Color hoverImageColor = new Color(0.9f, 0.9f, 1f);
    [SerializeField] private float textMoveDistance = 10f;
    [SerializeField] private float hoverAnimationDuration = 0.2f;

    [Header("排除悬停效果的组件")]
    [SerializeField] private Text[] excludeTexts;
    [SerializeField] private Image[] excludeImages;

    [Header("多组文本切换设置")]
    [SerializeField] private TextSwitcherGroup[] textSwitcherGroups;

    [System.Serializable]
    public class TextSwitcherGroup
    {
        public Button prevButton;
        public Button nextButton;
        public Text targetText;
        public string[] textOptions;   //文本选项数组
        [HideInInspector] public int currentIndex = 0; //当前文本索引
    }

    private GameObject currentContent;
    private CanvasGroup gameSettingsCanvasGroup;
    private CanvasGroup graphicsCanvasGroup;
    private CanvasGroup controllerCanvasGroup;
    private Dictionary<Text, Vector3> originalTextPositions = new Dictionary<Text, Vector3>();
    private Dictionary<Image, Color> originalImageColors = new Dictionary<Image, Color>();

    //存储临时选择的设置
    private string selectedResolution = "";
    private string selectedFrame = "";

    private void Start()
    {
        InitializeCanvasGroups();
        InitializeHoverEffects();
        InitializeTextSwitchers();

        controllerTab.onClick.AddListener(() => SwitchToContent(0));
        gameSettingsTab.onClick.AddListener(() => SwitchToContent(1));
        graphicsTab.onClick.AddListener(() => SwitchToContent(2));
        SwitchToContent(0);

        selectedResolution = GetResoluTionText();
        selectedFrame = Application.targetFrameRate.ToString();
    }

    private void InitializeTextSwitchers()
    {
        if (textSwitcherGroups == null) return;

        for (int i = 0; i < textSwitcherGroups.Length; i++)
        {
            int groupIndex = i; //重要创建局部变量避免闭包问题

            if (textSwitcherGroups[i].prevButton != null)
                textSwitcherGroups[i].prevButton.onClick.AddListener(() => PreviousText(groupIndex));

            if (textSwitcherGroups[i].nextButton != null)
                textSwitcherGroups[i].nextButton.onClick.AddListener(() => NextText(groupIndex));

            UpdateTextDisplay(groupIndex);
        }
    }

    private void PreviousText(int groupIndex)
    {
        if (textSwitcherGroups == null || groupIndex >= textSwitcherGroups.Length) return;

        var group = textSwitcherGroups[groupIndex];
        if (group.textOptions == null || group.textOptions.Length == 0) return;

        group.currentIndex--;
        if (group.currentIndex < 0)
            group.currentIndex = group.textOptions.Length - 1;

        UpdateTextDisplay(groupIndex);
    }

    private void NextText(int groupIndex)
    {
        if (textSwitcherGroups == null || groupIndex >= textSwitcherGroups.Length) return;

        var group = textSwitcherGroups[groupIndex];
        if (group.textOptions == null || group.textOptions.Length == 0) return;

        group.currentIndex++;
        if (group.currentIndex >= group.textOptions.Length)
            group.currentIndex = 0;

        UpdateTextDisplay(groupIndex);
    }

    private void UpdateTextDisplay(int groupIndex)
    {
        if (textSwitcherGroups == null || groupIndex >= textSwitcherGroups.Length) return;

        var group = textSwitcherGroups[groupIndex];
        if (group.targetText != null && group.textOptions != null && group.textOptions.Length > 0)
        {
            group.targetText.text = group.textOptions[group.currentIndex];
            if (groupIndex == 1) //分辨率为第二个
            {
                selectedResolution = group.textOptions[group.currentIndex];
            }
            if (groupIndex == 3)//最大帧率为第四个
            {
                selectedFrame = group.textOptions[group.currentIndex];
            }
        }
    }

    /// <summary>
    /// 获取当前分辨率的文本表示
    /// </summary>
    /// <returns>当前分辨率文本（如"1920*1080"）</returns>
    private string GetResoluTionText()
    {
        return $"{Screen.width}*{Screen.height}";
    }
    //图形界面确定按钮引用，设置帧率-分辨率因为bug暂时注释掉
    public void ApplyAllSettings()
    {
        //ApplyResolution(selectedResolution);
        ApplyFrame(selectedFrame);
    }
    private void ApplyFrame(string frameRateText)
    {
        if (int.TryParse(frameRateText, out int targetFrameRate))
        {
            Application.targetFrameRate = targetFrameRate;
           // Debug.Log($"帧率: {targetFrameRate} FPS");
        }
        else
        {
            Application.targetFrameRate = 60;
        }
    }

    /// <summary>
    /// 应用分辨率设置
    /// </summary>
    /// <param name="resolutionText"></param>
    private void ApplyResolution(string resolutionText)
    {
        if (resolutionText == "最大")
        {
            //支持的所有分辨率数组的最后一个元素，即最大分辨率
            Resolution maxResolution = Screen.resolutions[Screen.resolutions.Length - 1];
            Screen.SetResolution(maxResolution.width, maxResolution.height, Screen.fullScreen);
            // Debug.Log($"分辨率最大: {maxResolution.width}*{maxResolution.height}");
        }
        else
        {
            //解析字符串‘1920’*‘1080’
            string[] dimensions = resolutionText.Split('*');
            if (dimensions.Length == 2 &&
                int.TryParse(dimensions[0], out int width) &&
                int.TryParse(dimensions[1], out int height))
            {
                Screen.SetResolution(width, height, Screen.fullScreen);
               // Debug.Log($"分辨率: {width}*{height}");
            }
            else
            {
                Screen.SetResolution(1920, 1080, Screen.fullScreen);
            }
        }
    }

    private void InitializeHoverEffects()
    {
        AddHoverEffectsToContent(gameSettingsContent);
        AddHoverEffectsToContent(graphicsContent);
        // AddHoverEffectsToContent(controllerContent);  //因设置问题，遥控器不参与悬停
    }

    private void AddHoverEffectsToContent(GameObject content)
    {
        if (content == null) return;
        Text[] allTexts = content.GetComponentsInChildren<Text>(true);
        foreach (Text textComponent in allTexts)
        {
            if (textComponent != null && !IsExcluded(textComponent, excludeTexts))
            {
                originalTextPositions[textComponent] = textComponent.rectTransform.anchoredPosition;
                AddHoverEventsToText(textComponent);
            }
        }
        Image[] allImages = content.GetComponentsInChildren<Image>(true);
        foreach (Image imageComponent in allImages)
        {
            if (imageComponent != null && !IsExcluded(imageComponent, excludeImages))
            {
                originalImageColors[imageComponent] = imageComponent.color;
                AddHoverEventsToImage(imageComponent);
            }
        }
    }

    private bool IsExcluded(Text textComponent, Text[] excludeArray)
    {
        if (excludeArray == null) return false;
        foreach (Text excludedText in excludeArray)
        {
            if (excludedText == textComponent) return true;
        }
        return false;
    }

    private bool IsExcluded(Image imageComponent, Image[] excludeArray)
    {
        if (excludeArray == null) return false;
        foreach (Image excludedImage in excludeArray)
        {
            if (excludedImage == imageComponent) return true;
        }
        return false;
    }

    private void AddHoverEventsToText(Text textComponent)
    {
        if (textComponent == null) return;
        EventTrigger eventTrigger = textComponent.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = textComponent.gameObject.AddComponent<EventTrigger>();
        }
        eventTrigger.triggers.Clear();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { OnTextPointerEnter(textComponent); });
        eventTrigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { OnTextPointerExit(textComponent); });
        eventTrigger.triggers.Add(entryExit);
    }

    private void AddHoverEventsToImage(Image imageComponent)
    {
        if (imageComponent == null) return;
        EventTrigger eventTrigger = imageComponent.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = imageComponent.gameObject.AddComponent<EventTrigger>();
        }
        eventTrigger.triggers.Clear();

        EventTrigger.Entry entryEnter = new EventTrigger.Entry();
        entryEnter.eventID = EventTriggerType.PointerEnter;
        entryEnter.callback.AddListener((data) => { OnImagePointerEnter(imageComponent); });
        eventTrigger.triggers.Add(entryEnter);

        EventTrigger.Entry entryExit = new EventTrigger.Entry();
        entryExit.eventID = EventTriggerType.PointerExit;
        entryExit.callback.AddListener((data) => { OnImagePointerExit(imageComponent); });
        eventTrigger.triggers.Add(entryExit);
    }

    private void OnTextPointerEnter(Text textComponent)
    {
        if (textComponent == null || !originalTextPositions.ContainsKey(textComponent)) return;
        LeanTween.cancel(textComponent.gameObject);
        Vector2 originalPos = originalTextPositions[textComponent];
        Vector2 newPosition = new Vector2(originalPos.x + textMoveDistance, originalPos.y);
        LeanTween.move(textComponent.rectTransform, newPosition, hoverAnimationDuration)
            .setEase(LeanTweenType.easeOutQuad);
    }

    private void OnTextPointerExit(Text textComponent)
    {
        if (textComponent == null || !originalTextPositions.ContainsKey(textComponent)) return;
        LeanTween.cancel(textComponent.gameObject);
        LeanTween.move(textComponent.rectTransform, originalTextPositions[textComponent], hoverAnimationDuration)
            .setEase(LeanTweenType.easeOutQuad);
    }

    private void OnImagePointerEnter(Image imageComponent)
    {
        if (imageComponent == null || !originalImageColors.ContainsKey(imageComponent)) return;
        LeanTween.cancel(imageComponent.gameObject);
        LeanTween.color(imageComponent.rectTransform, hoverImageColor, hoverAnimationDuration)
            .setEase(LeanTweenType.easeOutQuad);
    }

    private void OnImagePointerExit(Image imageComponent)
    {
        if (imageComponent == null || !originalImageColors.ContainsKey(imageComponent)) return;
        LeanTween.cancel(imageComponent.gameObject);
        LeanTween.color(imageComponent.rectTransform, originalImageColors[imageComponent], hoverAnimationDuration)
            .setEase(LeanTweenType.easeOutQuad);
    }

    private void InitializeCanvasGroups()
    {
        gameSettingsCanvasGroup = GetOrAddCanvasGroup(gameSettingsContent);
        graphicsCanvasGroup = GetOrAddCanvasGroup(graphicsContent);
        controllerCanvasGroup = GetOrAddCanvasGroup(controllerContent);

        SetCanvasGroupState(gameSettingsCanvasGroup, 0f, false);
        SetCanvasGroupState(graphicsCanvasGroup, 0f, false);
        SetCanvasGroupState(controllerCanvasGroup, 0f, false);
    }

    private CanvasGroup GetOrAddCanvasGroup(GameObject target)
    {
        if (target == null) return null;
        CanvasGroup canvasGroup = target.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = target.AddComponent<CanvasGroup>();
        }
        return canvasGroup;
    }

    private void SetCanvasGroupState(CanvasGroup canvasGroup, float alpha, bool interactable)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.interactable = interactable;
            canvasGroup.blocksRaycasts = interactable;
        }
    }

    public void SwitchToContent(int contentIndex)
    {
        if (currentContent != null)
        {
            FadeOutContent(GetCanvasGroupByContent(currentContent));
        }

        switch (contentIndex)
        {
            case 0:
                FadeInContent(controllerContent, controllerCanvasGroup);
                break;
            case 1:
                FadeInContent(gameSettingsContent, gameSettingsCanvasGroup);
                break;
            case 2:
                FadeInContent(graphicsContent, graphicsCanvasGroup);
                break;
        }
    }

    private void FadeInContent(GameObject content, CanvasGroup canvasGroup)
    {
        if (content == null || canvasGroup == null) return;
        content.SetActive(true);
        content.transform.localScale = Vector3.one * 0.9f;
        LeanTween.cancel(content);
        LeanTween.alphaCanvas(canvasGroup, 1f, fadeDuration)
            .setEase(LeanTweenType.easeOutQuad);
        LeanTween.scale(content, Vector3.one, fadeDuration)
            .setEase(LeanTweenType.easeOutBack);

        currentContent = content;
        SetCanvasGroupState(canvasGroup, 1f, true);
    }

    private void FadeOutContent(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) return;
        LeanTween.cancel(canvasGroup.gameObject);
        LeanTween.alphaCanvas(canvasGroup, 0f, fadeDuration * 0.7f)
            .setEase(LeanTweenType.easeInQuad)
            .setOnComplete(() => {
                if (canvasGroup.gameObject != currentContent)
                {
                    canvasGroup.gameObject.SetActive(false);
                }
            });

        SetCanvasGroupState(canvasGroup, 0f, false);
    }

    private CanvasGroup GetCanvasGroupByContent(GameObject content)
    {
        if (content == gameSettingsContent) return gameSettingsCanvasGroup;
        if (content == graphicsContent) return graphicsCanvasGroup;
        if (content == controllerContent) return controllerCanvasGroup;
        return null;
    }
    public void ShowControllerSettings() => SwitchToContent(0);
    public void ShowGameSettings() => SwitchToContent(1);
    public void ShowGraphicsSettings() => SwitchToContent(2);

    private void OnEnable()
    {
        // 每次界面激活时都显示默认面板
        SwitchToContent(0);
    }

    private void OnDisable()
    {
        if (gameSettingsContent != null) LeanTween.cancel(gameSettingsContent);
        if (graphicsContent != null) LeanTween.cancel(graphicsContent);
        if (controllerContent != null) LeanTween.cancel(controllerContent);
    }
}