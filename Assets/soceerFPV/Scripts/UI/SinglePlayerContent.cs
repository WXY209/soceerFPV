using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Collections;
//*****************************************
//创建人：
//功能说明：单人游戏界面的内容面板的各功能实现
//***************************************** 
public class SinglePlayerContent : MonoBehaviour
{
    [Header("内容面板")]
    [SerializeField] private GameObject gameMapContent;

    [Header("选项卡按钮")]
    [SerializeField] private Button gameMap_1;
    [SerializeField] private Button gameMap_2;
    [SerializeField] private Button gameMap_3;

    [Header("加载设置")]
    [SerializeField] private string[] sceneNames;

    [Header("按钮图片设置")]
    [SerializeField] private ButtonImageSet[] buttonImageSets;

    [Header("地图显示文本")]
    [SerializeField] private Text selectedMapDisplayText;
    [SerializeField]
    private string[] mapDisplayTexts = new string[]
{
    "6*3*3地图",
    "8*4*4地图",
    "地图3"
};

    /// <summary>
    /// 自定义按钮图片设置类 - 用于存储每个按钮的图片配置
    /// </summary>
    [System.Serializable]
    public class ButtonImageSet
    {
        public Button targetButton;
        public Sprite normalSprite;
        public Sprite hoverSprite;
    }

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

    /// <summary>
    /// 文本切换器组类 - 用于管理左右按钮切换文本的功能
    /// </summary>
    [System.Serializable]
    public class TextSwitcherGroup
    {
        public Button prevButton;
        public Button nextButton;
        public Text targetText;
        public string[] textOptions;
        [HideInInspector] public int currentIndex = 0; //当前选中的文本索引
    }

    private CanvasGroup gameMapCanvasGroup;
    private Dictionary<Text, Vector3> originalTextPositions = new Dictionary<Text, Vector3>();
    private Dictionary<Image, Color> originalImageColors = new Dictionary<Image, Color>();
    private Dictionary<Button, Sprite> originalButtonSprites = new Dictionary<Button, Sprite>();
    private Dictionary<Button, ButtonImageSet> buttonImageMap = new Dictionary<Button, ButtonImageSet>();

    private int currentSelectedMapIndex = 0;
    private bool isLoading = false;

    public static string SelectedTeam = "Red";
    public static string SelectedGameMode = "人机";
    public static float SelectedGameTime = 360f;

    private void Start()
    {
        InitializeCanvasGroups();
        InitializeHoverEffects();
        InitializeTextSwitchers();
        InitializeButtonHoverEffects();

        if (gameMapContent != null)
            gameMapContent.SetActive(false);


        gameMap_1.onClick.AddListener(() => OnTabClicked(0));
        gameMap_2.onClick.AddListener(() => OnTabClicked(1));
        gameMap_3.onClick.AddListener(() => OnTabClicked(2));

        if (selectedMapDisplayText != null)
        {
            selectedMapDisplayText.text = " ";
        }
    }
    public void OnCancelButtonClicked()
    {
        HideContentPanel();
    }
    public void OnConfirmButtonClicked()
    {
        if (!isLoading)
        {
            StartGame();
        }
    }
    public void SetTeamForReplay(string team)
    {
        SelectedTeam = team;

        // 如果有文本切换器，更新显示
        if (textSwitcherGroups != null && textSwitcherGroups.Length > 0)
        {
            var teamGroup = textSwitcherGroups[0]; //第一个是阵营选择器
            if (teamGroup != null && teamGroup.textOptions != null)
            {
                // 找到对应的索引
                for (int i = 0; i < teamGroup.textOptions.Length; i++)
                {
                    if ((team == "Red" && teamGroup.textOptions[i] == "红方") ||
                        (team == "Blue" && teamGroup.textOptions[i] == "蓝方"))
                    {
                        teamGroup.currentIndex = i;
                        teamGroup.targetText.text = teamGroup.textOptions[i];
                        break;
                    }
                }
            }
        }
    }
    private void StartGame()
    {
        isLoading = true;

        if (sceneNames != null && currentSelectedMapIndex < sceneNames.Length)
        {
            string targetScene = sceneNames[currentSelectedMapIndex];
            if (!string.IsNullOrEmpty(targetScene))
            {
                SceneManager.LoadScene(targetScene);
            }
            else
            {
                isLoading = false;
            }
        }
        else
        {
            isLoading = false;
        }
    }
    /// <summary>
    /// 异步加载场景协程 - 控制加载界面显示时间并跳转场景
    /// </summary>
    /// <param name="sceneName">要加载的场景名称</param>
    /// <returns>IEnumerator协程</returns>

    private void HideContentPanel()
    {
        if (gameMapContent != null && gameMapCanvasGroup != null)
        {
            FadeOutContent(gameMapCanvasGroup);
        }
    }
    /// <summary>
    /// 选项卡点击事件处理 - 记录选中的地图并显示内容面板
    /// </summary>
    /// <param name="mapIndex">地图索引（0-2）</param>
    private void OnTabClicked(int mapIndex)
    {
        currentSelectedMapIndex = mapIndex;

        ResetAllTextSwitchers();
        ShowContentPanel();
        UpdateMapDisplay();

        switch (mapIndex)
        {
            case 0:
                //  Debug.Log("6*3*3地图");
                break;
            case 1:
                //  Debug.Log("8*4*4地图");
                break;
            case 2:
                // Debug.Log("地图3");
                break;
        }
    }

    /// <summary>
    /// 更新地图显示文本
    /// </summary>
    private void UpdateMapDisplay()
    {
        if (selectedMapDisplayText == null)
        {
            return;
        }

        if (mapDisplayTexts != null && currentSelectedMapIndex < mapDisplayTexts.Length)
        {
            selectedMapDisplayText.text = $"{mapDisplayTexts[currentSelectedMapIndex]}";
        }
        else if (selectedMapDisplayText != null)
        {
            selectedMapDisplayText.text = $"{currentSelectedMapIndex + 1}";
        }
    }
    private void ResetAllTextSwitchers()
    {
        if (textSwitcherGroups == null) return;

        foreach (var group in textSwitcherGroups)
        {
            if (group != null)
            {
                group.currentIndex = 0;
                if (group.targetText != null && group.textOptions != null && group.textOptions.Length > 0)
                {
                    group.targetText.text = group.textOptions[0];
                }
            }
        }
    }
    private void ShowContentPanel()
    {
        if (gameMapContent != null && gameMapCanvasGroup != null)
        {
            gameMapContent.SetActive(true);
            FadeInContent(gameMapContent, gameMapCanvasGroup);
        }
    }
    private void FadeOutContent(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) return;

        LeanTween.cancel(canvasGroup.gameObject);
        LeanTween.alphaCanvas(canvasGroup, 0f, fadeDuration * 0.7f)
            .setEase(LeanTweenType.easeInQuad)
            .setOnComplete(() => {
                if (gameMapContent != null)
                {
                    gameMapContent.SetActive(false);
                }
            });

        SetCanvasGroupState(canvasGroup, 0f, false);
    }
    private void InitializeButtonHoverEffects()
    {
        if (buttonImageSets != null)
        {
            foreach (var imageSet in buttonImageSets)
            {
                if (imageSet.targetButton != null)
                {
                    buttonImageMap[imageSet.targetButton] = imageSet;

                    Image buttonImage = imageSet.targetButton.GetComponent<Image>();
                    if (buttonImage != null && imageSet.normalSprite != null)
                    {
                        buttonImage.sprite = imageSet.normalSprite;
                    }
                }
            }
        }

        AddButtonHoverEffect(gameMap_1);
        AddButtonHoverEffect(gameMap_2);
        AddButtonHoverEffect(gameMap_3);
    }
    private void AddButtonHoverEffect(Button button)
    {
        if (button == null) return;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null)
        {
            originalButtonSprites[button] = buttonImage.sprite;

            EventTrigger eventTrigger = button.GetComponent<EventTrigger>();
            if (eventTrigger == null)
            {
                eventTrigger = button.gameObject.AddComponent<EventTrigger>();
            }

            eventTrigger.triggers.Clear();

            EventTrigger.Entry entryEnter = new EventTrigger.Entry();
            entryEnter.eventID = EventTriggerType.PointerEnter;
            entryEnter.callback.AddListener((data) => { OnButtonPointerEnter(button); });
            eventTrigger.triggers.Add(entryEnter);

            EventTrigger.Entry entryExit = new EventTrigger.Entry();
            entryExit.eventID = EventTriggerType.PointerExit;
            entryExit.callback.AddListener((data) => { OnButtonPointerExit(button); });
            eventTrigger.triggers.Add(entryExit);
        }
    }
    private void OnButtonPointerEnter(Button button)
    {
        if (buttonImageMap.ContainsKey(button) && buttonImageMap[button].hoverSprite != null)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = buttonImageMap[button].hoverSprite;
            }
        }
    }
    private void OnButtonPointerExit(Button button)
    {
        if (buttonImageMap.ContainsKey(button) && buttonImageMap[button].normalSprite != null)
        {
            Image buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = buttonImageMap[button].normalSprite;
            }
        }
    }
    private void InitializeTextSwitchers()
    {
        if (textSwitcherGroups == null) return;

        for (int i = 0; i < textSwitcherGroups.Length; i++)
        {
            int groupIndex = i;

            if (textSwitcherGroups[i].prevButton != null)
                textSwitcherGroups[i].prevButton.onClick.AddListener(() => PreviousText(groupIndex));

            if (textSwitcherGroups[i].nextButton != null)
                textSwitcherGroups[i].nextButton.onClick.AddListener(() => NextText(groupIndex));

            UpdateTextDisplay(groupIndex);
        }
    }

    /// <summary>
    /// 切换到上一个文本选项
    /// </summary>
    /// <param name="groupIndex">文本切换组索引</param>
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

            if (groupIndex == 0) //阵营选择是索引第一个
            {
                if (group.textOptions[group.currentIndex] == "红方")
                {
                    SelectedTeam = "Red";
                    // Debug.Log("阵营设置为: 红方");
                }
                else if (group.textOptions[group.currentIndex] == "蓝方")
                {
                    SelectedTeam = "Blue";
                    // Debug.Log("阵营设置为: 蓝方");
                }
            }
            //游戏模式是第二个
            else if (groupIndex == 1)
            {
                SelectedGameMode = group.textOptions[group.currentIndex];
                //Debug.Log($"游戏模式: {SelectedGameMode}");
            }
            //比赛时间是第三个
            else if (groupIndex == 2)
            {
                string selectedTime = group.textOptions[group.currentIndex];
                switch (selectedTime)
                {
                    case "6分钟":
                        SelectedGameTime = 360f; 
                        break;
                    case "3分钟":
                        SelectedGameTime = 180f; 
                        break;
                    case "1分钟":
                        SelectedGameTime = 60f; 
                        break;
                    case "30秒":
                        SelectedGameTime = 30f; 
                        break;
                    default:
                        SelectedGameTime = 360f; 
                        break;
                }
            }
        }
    }
    private void InitializeHoverEffects()
    {
        AddHoverEffectsToContent(gameMapContent);
    }

    /// <summary>
    /// 为指定内容面板添加悬停效果
    /// </summary>
    /// <param name="content">内容面板对象</param>
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

    /// <summary>
    /// 检查图片组件是否在排除列表中
    /// </summary>
    /// <param name="imageComponent">图片组件</param>
    /// <param name="excludeArray">排除数组</param>
    /// <returns>是否被排除</returns>
    private bool IsExcluded(Image imageComponent, Image[] excludeArray)
    {
        if (excludeArray == null) return false;
        foreach (Image excludedImage in excludeArray)
        {
            if (excludedImage == imageComponent) return true;
        }
        return false;
    }

    /// <summary>
    /// 为文本组件添加悬停事件
    /// </summary>
    /// <param name="textComponent">文本组件</param>
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
    /// <summary>
    /// 文本指针进入事件 - 文本右移动画
    /// </summary>
    /// <param name="textComponent">文本组件</param>
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
    /// <summary>
    /// 图片指针进入事件 - 图片颜色变化
    /// </summary>
    /// <param name="imageComponent">图片组件</param>
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
        gameMapCanvasGroup = GetOrAddCanvasGroup(gameMapContent);
        SetCanvasGroupState(gameMapCanvasGroup, 0f, false);
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
    /// <summary>
    /// 淡入显示内容面板
    /// </summary>
    /// <param name="content">内容面板对象</param>
    /// <param name="canvasGroup">CanvasGroup组件</param>
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

        SetCanvasGroupState(canvasGroup, 1f, true);
    }

    //当脚本禁用时取消所有动画
    private void OnDisable()
    {
        if (gameMapContent != null) LeanTween.cancel(gameMapContent);
    }
}