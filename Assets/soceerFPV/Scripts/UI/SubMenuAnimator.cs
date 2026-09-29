using UnityEngine;

//*****************************************
//创建人： Mezcal
//功能说明：LeanTween插件做动画效果
//***************************************** 
[RequireComponent(typeof(RectTransform))]
public class SubMenuAnimator : MonoBehaviour
{
    [SerializeField] private float slideDuration = 0.3f;
    [SerializeField] private Vector2 slideOffset = new Vector2(300, 0);
    private Vector2 originalPos;
    private bool isOpen = false;

    void Start()
    {
        originalPos = GetComponent<RectTransform>().anchoredPosition;
        gameObject.SetActive(false);//初始时默认隐藏
    }

    public void ToggleMenu()//菜单切换
    {
        if (isOpen)
            SlideOut();
        else
            SlideIn();
    }

    void SlideIn()
    {
        gameObject.SetActive(true);
        LeanTween.moveLocalX(gameObject, originalPos.x + slideOffset.x, slideDuration)
        .setEaseInOutSine();
        isOpen = true;
    }

    void SlideOut()
    {
        LeanTween.moveLocalX(gameObject, originalPos.x, slideDuration)
        .setEaseInOutSine()
        .setOnComplete(() => gameObject.SetActive(false));
        isOpen = false;
    }
}