using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
//*****************************************
//创建人：
//功能说明：局域网游戏创建和加入的UI管理
//***************************************** 
public class LanPlayContent : MonoBehaviour
{
    [Header("内容面板")]
    [SerializeField] private GameObject creatGameContent;
    [SerializeField] private GameObject addGameContent;

    [Header("选项卡按钮")]
    [SerializeField] private Button creatGameTab;
    [SerializeField] private Button addGameTab;

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
        public string[] textOptions;
        [HideInInspector] public int currentIndex = 0; //当前文本索引
    }

    private CanvasGroup creatGameCanvasGroup;
    private CanvasGroup addGameCanvasGroup;
    private Dictionary<Text, Vector3> originalTextPositions = new Dictionary<Text, Vector3>();
    private Dictionary<Image, Color> originalImageColors = new Dictionary<Image, Color>();
    private GameObject currentContent;

    //联机处理-静态变量存储选择结果
    public static string SelectedMapScene = "LanMap1";
    public static string SelectedTeam = "Red"; // 主机阵营（保持向后兼容）
    public static string ClientSelectedTeam = "Red"; //客户端阵营
    public static int MaxPlayers = 6;//最大联机人数

    private void Start()
    {
        InitializeCanvasGroups();
        InitializeHoverEffects();
        InitializeTextSwitchers();

        creatGameTab.onClick.AddListener(() => SwitchToContent(0));
        addGameTab.onClick.AddListener(() => SwitchToContent(1));
        HideAllContents();
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

            // 根据不同的文本切换组处理
            switch (groupIndex)
            {
                case 0: //创建面板 - 阵营选择（Element 0）
                    if (group.textOptions[group.currentIndex] == "红方")
                    {
                        SelectedTeam = "Red";
                      //  Debug.Log("主机阵营设置为: 红方");
                    }
                    else if (group.textOptions[group.currentIndex] == "蓝方")
                    {
                        SelectedTeam = "Blue";
                      //  Debug.Log("主机阵营设置为: 蓝方");
                    }
                    break;

                case 1: //地图选择（Element 1）
                    switch (group.textOptions[group.currentIndex])
                    {
                        case "6*3*3":
                            SelectedMapScene = "LanMap1";
                            break;
                        case "8*4*4":
                            SelectedMapScene = "LanMap2";
                            break;
                        default:
                            SelectedMapScene = "LanMap1";
                            break;
                    }
                   // Debug.Log("地图场景设置为: " + SelectedMapScene);
                    break;

                case 3: //比赛人数（Element 3）
                    if (int.TryParse(group.textOptions[group.currentIndex], out int playerCount))
                    {
                        MaxPlayers = playerCount;
                       // Debug.Log($"比赛人数为: {MaxPlayers}");
                    }
                    else
                    {
                        MaxPlayers = 6; 
                    }
                    break;

                case 4: // 加入面板 - 阵营选择（Element 4）
                    if (group.textOptions[group.currentIndex] == "红方")
                    {
                        ClientSelectedTeam = "Red";
                        //Debug.Log("客户端阵营设置为: 红方");
                    }
                    else if (group.textOptions[group.currentIndex] == "蓝方")
                    {
                        ClientSelectedTeam = "Blue";
                       // Debug.Log("客户端阵营设置为: 蓝方");
                    }
                    break;
            }
        }
    }
    private void InitializeHoverEffects()
    {
        AddHoverEffectsToContent(creatGameContent);
        AddHoverEffectsToContent(addGameContent);
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

    public void SwitchToContent(int contentIndex)
    {
        // 先隐藏当前显示的面板
        if (currentContent != null)
        {
            if (currentContent == creatGameContent)
                HideContent(creatGameContent, creatGameCanvasGroup);
            else if (currentContent == addGameContent)
                HideContent(addGameContent, addGameCanvasGroup);
        }

        // 显示新的面板
        switch (contentIndex)
        {
            case 0:
                ShowContent(creatGameContent, creatGameCanvasGroup);
                currentContent = creatGameContent;
                break;
            case 1:
                ShowContent(addGameContent, addGameCanvasGroup);
                currentContent = addGameContent;
                break;
        }
    }

    private void ShowContent(GameObject content, CanvasGroup canvasGroup)
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

    private void HideContent(GameObject content, CanvasGroup canvasGroup)
    {
        if (content == null || canvasGroup == null) return;

        LeanTween.cancel(content);
        LeanTween.alphaCanvas(canvasGroup, 0f, fadeDuration * 0.7f)
            .setEase(LeanTweenType.easeInQuad)
            .setOnComplete(() => {
                content.SetActive(false);
            });

        SetCanvasGroupState(canvasGroup, 0f, false);

        if (currentContent == content)
            currentContent = null;
    }

    private void HideAllContents()
    {
        HideContent(creatGameContent, creatGameCanvasGroup);
        HideContent(addGameContent, addGameCanvasGroup);
        currentContent = null;
    }

    public void OnCreatGameCancelClicked()
    {
        HideContent(creatGameContent, creatGameCanvasGroup);
    }

    public void OnAddGameCancelClicked()
    {
        HideContent(addGameContent, addGameCanvasGroup);
    }

    private void InitializeCanvasGroups()
    {
        creatGameCanvasGroup = GetOrAddCanvasGroup(creatGameContent);
        addGameCanvasGroup = GetOrAddCanvasGroup(addGameContent);

        SetCanvasGroupState(creatGameCanvasGroup, 0f, false);
        SetCanvasGroupState(addGameCanvasGroup, 0f, false);
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

    private void OnDisable()
    {
        if (creatGameContent != null) LeanTween.cancel(creatGameContent);
        if (addGameContent != null) LeanTween.cancel(addGameContent);
    }
}