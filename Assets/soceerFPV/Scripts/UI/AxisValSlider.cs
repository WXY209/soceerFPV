using UnityEngine;
using UnityEngine.UI;
//*****************************************
//创建人：
//功能说明：动态显示10个摇杆轴值的进度条的运动效果
//***************************************** 
public class AxisValSlider : MonoBehaviour
{
    [SerializeField] private Slider[] axisBars = new Slider[10];
    [SerializeField] private float updateRate = 0.1f;

    //引用到AxisManager以获取反转设置
    [SerializeField] private AxisManager axisManager;

    private float timer;

    void Start()
    {
        // 初始化滑动条
        foreach (var slider in axisBars)
        {
            if (slider != null)
            {
                slider.minValue = -1f;
                slider.maxValue = 1f;
                slider.value = 0f;
                slider.interactable = false;
            }
        }
        if (axisManager == null)
        {
            axisManager = FindObjectOfType<AxisManager>();
        }
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= updateRate)
        {
            timer = 0f;
            UpdateAxisBars();
        }
    }

    void UpdateAxisBars()
    {
        for (int i = 0; i < 10; i++)
        {
            if (axisBars[i] == null) continue;

            float value = Input.GetAxis($"Axis {i + 1}");
            string axisName = $"Axis {i + 1}";
           // Debug.Log($"axisName:{axisName},value{value}");

            //检查是否需要反转显示
            if (axisManager != null && axisManager.currentMapping != null)
            {
                //检查当前轴是否被任何功能使用且设置了反转
                if (axisManager.currentMapping.forwardAxis == axisName &&
                    axisManager.currentMapping.invertForward)
                    value = -value;
                else if (axisManager.currentMapping.strafeAxis == axisName &&
                         axisManager.currentMapping.invertStrafe)
                    value = -value;
                else if (axisManager.currentMapping.rotateAxis == axisName &&
                         axisManager.currentMapping.invertRotate)
                    value = -value;
                else if (axisManager.currentMapping.throttleAxis == axisName &&
                         axisManager.currentMapping.invertThrottle)
                    value = -value;
            }

            axisBars[i].value = value;
        }
    }
}