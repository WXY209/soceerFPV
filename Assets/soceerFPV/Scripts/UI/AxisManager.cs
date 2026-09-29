using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
//*****************************************
//创建人：
//功能说明：摇杆进行轴的通道映射、校准和反转设置效果
//***************************************** 
public class AxisManager : MonoBehaviour
{
    [Header("当前映射配置")]       
    public AxisMapping currentMapping = new AxisMapping();

    [Header("UI组件")]
    [SerializeField] private Dropdown forwardDropdown;  //四功能下拉菜单
    [SerializeField] private Dropdown strafeDropdown;
    [SerializeField] private Dropdown rotateDropdown;
    [SerializeField] private Dropdown throttleDropdown;
    [SerializeField] private Toggle invertForwardToggle;//四功能反转单选toggle  
    [SerializeField] private Toggle invertStrafeToggle;
    [SerializeField] private Toggle invertRotateToggle;
    [SerializeField] private Toggle invertThrottleToggle;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button confirmAxisButton;
    [SerializeField] private Button calibrationAxisButton;

    [Header("定轴向导面板")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private Text confirmText;
    [SerializeField] private Button confirmButton;

    [Header("校准面板")]
    [SerializeField] private GameObject calibrationPanel;
    [SerializeField] private Text calibrationText;
    [SerializeField] private Button calibrationButton;

    private bool isInitialized = false;

    [Serializable]
    public class AxisMapping
    {
        //各功能对应的轴
        public string forwardAxis = "Axis 2";
        public string strafeAxis = "Axis 1";
        public string rotateAxis = "Axis 5";
        public string throttleAxis = "Axis 6";
        //四个功能的反转toggle
        public bool invertForward = false;
        public bool invertStrafe = false;
        public bool invertRotate = false;
        public bool invertThrottle = false;
    }

    //轴校准数据结构
    [Serializable]
    public class AxisCalibrationData
    {
        public string axisName;        //轴名称
        public float negativeMax = -1f;//负向最大值
        public float zeroOffset = 0f;  //中心偏移值
        public float positiveMax = 1f; //正向最大值

        public AxisCalibrationData(string name)
        {
            axisName = name;
            negativeMax = -1f;
            zeroOffset = 0f;
            positiveMax = 1f;
        }

        //应用校准，将原始轴值转换为校准后的值（-1到1）
        public float ApplyCalibration(float rawValue)
        {
            //如果未校准或校准数据无效，直接返回原始值
            if (Mathf.Abs(positiveMax - zeroOffset) < 0.01f ||
                Mathf.Abs(zeroOffset - negativeMax) < 0.01f)
                return rawValue;
            //公式运算
            if (rawValue >= zeroOffset)
            {
                // 正向区间：zeroOffset 到 positiveMax
                return (rawValue - zeroOffset) / (positiveMax - zeroOffset);
            }
            else
            {
                // 负向区间：negativeMax 到 zeroOffset
                return (rawValue - zeroOffset) / (zeroOffset - negativeMax);
            }
        }
    }
    // 用于防止递归调用的标志
    private bool isProcessingChange = false;
    private AxisCalibrationData[] calibrationData = new AxisCalibrationData[10];

    // 定轴状态枚举
    private enum ConfirmStep
    {
        NotStarted,
        Prepare,  //准备文本阶段
        WaitingForward,
        WaitingStrafe,
        WaitingRotate,
        WaitingThrottle,
        Completed //完成
    }

    private ConfirmStep currentStep = ConfirmStep.NotStarted;
    private Coroutine confirmCoroutine;
    private const float stepDuration = 5f;
    private const float preDuration = 2f; //准备阶段2s  

    // 检测到的轴值
    private string detectedForwardAxis = "";
    private string detectedStrafeAxis = "";
    private string detectedRotateAxis = "";
    private string detectedThrottleAxis = "";

    // 用于检测轴值变化的数据结构
    private class AxisDetectionData
    {
        public string axisName;          // 轴名称
        public float maxValueChange = 0f; // 最大变化值
        public float lastValue = 0f;      // 上一帧的值
        public float currentValue = 0f;   // 当前值

        public AxisDetectionData(string name)
        {
            axisName = name;
        }
        // 更新轴值并记录计算最大变化
        public void Update(float newValue)
        {
            currentValue = newValue;
            float change = Mathf.Abs(currentValue - lastValue);
            if (change > maxValueChange)
            {
                maxValueChange = change;
            }
            lastValue = currentValue;
        }
        // 重置检测数据
        public void Reset()
        {
            maxValueChange = 0f;
            lastValue = 0f;
            currentValue = 0f;
        }
    }
    private AxisDetectionData[] axisData = new AxisDetectionData[10]; //10个轴的检测数据
    private Coroutine calibrationCoroutine;
    private const float calibrationDuration = 5f;

    //临时存储读取到的值
    private float[,] tempAxisValues = new float[10, 3]; // [轴索引, 0:min 1:max 2:center]

    void Start()
    {
        InitializeDropdowns();
        InitializeInvertToggles();
        InitializeButtons();
        LoadSavedMapping();
        InitializeconfirmPanel();
        InitializeCalibrationPanel();

        for (int i = 0; i < 10; i++)
        {
            axisData[i] = new AxisDetectionData($"Axis {i + 1}");
        }
        for (int i = 0; i < 10; i++)
        {
            calibrationData[i] = new AxisCalibrationData($"Axis {i + 1}");
            LoadCalibrationData(i); //加载保存的校准数据
        }

        isInitialized = true;
    }
    void OnEnable()
    {
        if (isInitialized)
        {
            ResetPanelStates();
        }
    }
    void OnDisable()
    {
        ForceCloseAllPanels();
    }
    // 重置定轴和校准面板
    private void ResetPanelStates()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
        }
        if (calibrationPanel != null)
        {
            calibrationPanel.SetActive(false);
        }
        if (calibrationButton != null)
        {
            calibrationButton.gameObject.SetActive(false);
        }
        currentStep = ConfirmStep.NotStarted;

        if (confirmCoroutine != null)
        {
            StopCoroutine(confirmCoroutine);
            confirmCoroutine = null;
        }
        if (calibrationCoroutine != null)
        {
            StopCoroutine(calibrationCoroutine);
            calibrationCoroutine = null;
        }
        SetUI(true);
    }

    //关闭所有面板
    private void ForceCloseAllPanels()
    {
        if (confirmPanel != null && confirmPanel.activeSelf)
        {
            confirmPanel.SetActive(false);
            confirmButton.gameObject.SetActive(false);
        }
        if (calibrationPanel != null && calibrationPanel.activeSelf)
        {
            calibrationPanel.SetActive(false);
            calibrationButton.gameObject.SetActive(false);
        }
        // 停止所有协程
        if (confirmCoroutine != null)
        {
            StopCoroutine(confirmCoroutine);
            confirmCoroutine = null;
        }
        if (calibrationCoroutine != null)
        {
            StopCoroutine(calibrationCoroutine);
            calibrationCoroutine = null;
        }
        SetUI(true);
    }

    //初始化按钮
    void InitializeButtons()
    {
        if (resetButton != null)
        {
            resetButton.onClick.RemoveAllListeners();
            resetButton.onClick.AddListener(ResetDefault);
        }
        if (confirmAxisButton != null)
        {
            confirmAxisButton.onClick.RemoveAllListeners();
            confirmAxisButton.onClick.AddListener(StartConfirm);
        }
        if (calibrationAxisButton != null)
        {
            calibrationAxisButton.onClick.RemoveAllListeners();
            calibrationAxisButton.onClick.AddListener(StartCalibration);
        }
    }
    void InitializeCalibrationPanel()
    {
        if (calibrationPanel != null)
        {
            calibrationPanel.SetActive(false);
        }
        if (calibrationButton != null)
        {
            calibrationButton.onClick.RemoveAllListeners();
            calibrationButton.onClick.AddListener(OnCalibrationComplete);
            calibrationButton.gameObject.SetActive(false); 
        }
    }

    //开始校准
    void StartCalibration()
    {
        // 重置面板到初始状态
        if (calibrationPanel != null)
        {
            calibrationPanel.SetActive(true);
        }
        if (calibrationButton != null)
        {
            calibrationButton.gameObject.SetActive(false); // 确保开始时不显示完成按钮
        }

        //重置临时数据
        for (int i = 0; i < 10; i++)
        {
            tempAxisValues[i, 0] = float.MaxValue;  //最小值
            tempAxisValues[i, 1] = float.MinValue;  //最大值
            tempAxisValues[i, 2] = 0f;              //中心值
        }
        SetUI(false);
        //停止可能正在运行的协程
        if (calibrationCoroutine != null)
        {
            StopCoroutine(calibrationCoroutine);
        }
        calibrationCoroutine = StartCoroutine(CalibrationProcess());
    }

    //校准过程协程
    IEnumerator CalibrationProcess()
    {
        //阶段1：准备
        SetCalibrationText("准备开始校准...");
        yield return new WaitForSeconds(2f);

        //阶段2：读取最大值
        SetCalibrationText("请在5秒时间将所有摇杆移至极限");

        float startTime = Time.time;
        while (Time.time - startTime < calibrationDuration)
        {
            for (int i = 0; i < 10; i++)
            {
                float value = Input.GetAxis($"Axis {i + 1}");

                //记录最小值（负向最大值）
                if (value < tempAxisValues[i, 0])
                    tempAxisValues[i, 0] = value;

                //记录最大值（正向最大值）
                if (value > tempAxisValues[i, 1])
                    tempAxisValues[i, 1] = value;
            }
            yield return null;
        }
        //阶段3：读取中心值
        SetCalibrationText("将所有摇杆移至中心");

        //重置中心值数据
        float[] centerSamples = new float[10];
        int[] sampleCounts = new int[10];

        startTime = Time.time;
        while (Time.time - startTime < calibrationDuration)
        {
            for (int i = 0; i < 10; i++)
            {
                float value = Input.GetAxis($"Axis {i + 1}");

                //只记录接近0的值作为中心样本
                if (Mathf.Abs(value) < 0.2f)
                {
                    centerSamples[i] += value;
                    sampleCounts[i]++;
                }
            }
            yield return null;
        }

        //计算平均中心值
        for (int i = 0; i < 10; i++)
        {
            if (sampleCounts[i] > 0)
            {
                tempAxisValues[i, 2] = centerSamples[i] / sampleCounts[i];
            }
            else
            {
                //如果没有采集到足够样本，使用0
                tempAxisValues[i, 2] = 0f;
            }
        }

        //完成
        SetCalibrationText("校准完成");
        //显示确定按钮
        if (calibrationButton != null)
        {
            calibrationButton.gameObject.SetActive(true);
        }
    }

    //校准完成
    void OnCalibrationComplete()
    {
        //应用校准数据
        for (int i = 0; i < 10; i++)
        {
            calibrationData[i].negativeMax = tempAxisValues[i, 0];
            calibrationData[i].positiveMax = tempAxisValues[i, 1];
            calibrationData[i].zeroOffset = tempAxisValues[i, 2];
            //保存校准数据
            SaveCalibrationData(i);
        }
        if (calibrationPanel != null)
        {
            calibrationPanel.SetActive(false);
        }
        if (calibrationButton != null)
        {
            calibrationButton.gameObject.SetActive(false);
        }

        SetUI(true);
        calibrationCoroutine = null;
    }

    //保存校准数据
    void SaveCalibrationData(int axisIndex)
    {
        string prefix = $"Calibration_Axis{axisIndex + 1}_";
        PlayerPrefs.SetFloat(prefix + "NegativeMax", calibrationData[axisIndex].negativeMax);
        PlayerPrefs.SetFloat(prefix + "PositiveMax", calibrationData[axisIndex].positiveMax);
        PlayerPrefs.SetFloat(prefix + "ZeroOffset", calibrationData[axisIndex].zeroOffset);
        PlayerPrefs.Save();
    }

    //加载校准数据
    void LoadCalibrationData(int axisIndex)
    {
        string prefix = $"Calibration_Axis{axisIndex + 1}_";
        calibrationData[axisIndex].negativeMax = PlayerPrefs.GetFloat(prefix + "NegativeMax", -1f);
        calibrationData[axisIndex].positiveMax = PlayerPrefs.GetFloat(prefix + "PositiveMax", 1f);
        calibrationData[axisIndex].zeroOffset = PlayerPrefs.GetFloat(prefix + "ZeroOffset", 0f);
    }

    //获取校准后的轴值供其他脚本使用
    public float GetCalibratedAxisValue(string axisName)
    {
        float rawValue = Input.GetAxis(axisName);

        for (int i = 0; i < 10; i++)
        {
            if (calibrationData[i].axisName == axisName)
            {
                return calibrationData[i].ApplyCalibration(rawValue);
            }
        }

        return rawValue;
    }
    void SetCalibrationText(string text)
    {
        if (calibrationText != null)
        {
            calibrationText.text = text;
        }
    }

    //初始化定轴面板
    void InitializeconfirmPanel()
    {
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(ConfirmComplete);
            confirmButton.gameObject.SetActive(false); 
        }
    }

    void StartConfirm()
    {
        // 重置面板到初始状态
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(true);
        }
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false); 
        }

        //重置检测结果数据
        detectedForwardAxis = "";
        detectedStrafeAxis = "";
        detectedRotateAxis = "";
        detectedThrottleAxis = "";
        foreach (var data in axisData)
        {
            data.Reset();
        }

        SetUI(false);

        //停止可能正在运行的协程
        if (confirmCoroutine != null)
        {
            StopCoroutine(confirmCoroutine);
        }
        confirmCoroutine = StartCoroutine(ConfirmAxis());
    }

    //定轴过程协程
    IEnumerator ConfirmAxis()
    {
        // 步骤0：准备阶段
        currentStep = ConfirmStep.Prepare;
        SetConfirmText("准备开始确定运动摇杆...");
        yield return new WaitForSeconds(preDuration);

        // 步骤1：前后轴
        currentStep = ConfirmStep.WaitingForward;
        SetConfirmText("请移动前后运动的摇杆...");
        foreach (var data in axisData)
        {
            data.Reset();
        }
        for (int i = 0; i < 10; i++)
        {
            axisData[i].lastValue = Input.GetAxis(axisData[i].axisName);
        }
        float startTime = Time.time;
        while (Time.time - startTime < stepDuration)
        {
            // 更新所有轴当前值
            for (int i = 0; i < 10; i++)
            {
                float value = Input.GetAxis(axisData[i].axisName);
                axisData[i].Update(value);
            }
            yield return null;
        }
        // 找出变化最大的轴
        detectedForwardAxis = GetMaxAxis();

        // 步骤2：左右轴
        currentStep = ConfirmStep.WaitingStrafe;
        SetConfirmText("请移动左右运动的摇杆...");
        foreach (var data in axisData)
        {
            data.Reset();
        }
        for (int i = 0; i < 10; i++)
        {
            axisData[i].lastValue = Input.GetAxis(axisData[i].axisName);
        }
        startTime = Time.time;
        while (Time.time - startTime < stepDuration)
        {
            for (int i = 0; i < 10; i++)
            {
                float value = Input.GetAxis(axisData[i].axisName);
                axisData[i].Update(value);
            }
            yield return null;
        }
        detectedStrafeAxis = GetCancelAxis(detectedForwardAxis);

        // 步骤3：旋转轴
        currentStep = ConfirmStep.WaitingRotate;
        SetConfirmText("请移动旋转运动的摇杆...");
        foreach (var data in axisData)
        {
            data.Reset();
        }
        for (int i = 0; i < 10; i++)
        {
            axisData[i].lastValue = Input.GetAxis(axisData[i].axisName);
        }
        startTime = Time.time;
        while (Time.time - startTime < stepDuration)
        {
            for (int i = 0; i < 10; i++)
            {
                float value = Input.GetAxis(axisData[i].axisName);
                axisData[i].Update(value);
            }
            yield return null;
        }
        detectedRotateAxis = GetCancelAxis(detectedForwardAxis, detectedStrafeAxis);

        // 步骤4：上下轴
        currentStep = ConfirmStep.WaitingThrottle;
        SetConfirmText("请移动上下运动的摇杆...");
        foreach (var data in axisData)
        {
            data.Reset();
        }
        for (int i = 0; i < 10; i++)
        {
            axisData[i].lastValue = Input.GetAxis(axisData[i].axisName);
        }
        startTime = Time.time;
        while (Time.time - startTime < stepDuration)
        {
            for (int i = 0; i < 10; i++)
            {
                float value = Input.GetAxis(axisData[i].axisName);
                axisData[i].Update(value);
            }
            yield return null;
        }
        detectedThrottleAxis = GetCancelAxis(detectedForwardAxis, detectedStrafeAxis, detectedRotateAxis);
        currentStep = ConfirmStep.Completed;
        SetConfirmText("已成功设定运动摇杆");

        // 显示完成按钮
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(true);
        }
    }
    //获取变化最大的轴
    private string GetMaxAxis()
    {
        int maxIndex = 0;
        float maxChange = axisData[0].maxValueChange;
        //遍历所有轴，找到变化最大的
        for (int i = 1; i < 10; i++)
        {
            if (axisData[i].maxValueChange > maxChange)
            {
                maxChange = axisData[i].maxValueChange;
                maxIndex = i;
            }
        }
        return axisData[maxIndex].axisName;
    }
    //排除指定变化最大的轴
    private string GetCancelAxis(params string[] excludedAxes)
    {
        int maxIndex = -1;
        float maxChange = 0f;
        for (int i = 0; i < 10; i++)
        {
            //检查是否在排除列表中
            bool isExcluded = false;
            foreach (string excludedAxis in excludedAxes)
            {
                if (axisData[i].axisName == excludedAxis)
                {
                    isExcluded = true;
                    break;
                }
            }
            // 如果不在排除列表中且变化更大，则更新
            if (!isExcluded && axisData[i].maxValueChange > maxChange)
            {
                
                maxChange = axisData[i].maxValueChange;
                maxIndex = i;
            }
        }
        if (maxIndex == -1 || maxChange < 0.1f)//没有则设置为默认
        {
            switch (currentStep)
            {
                case ConfirmStep.WaitingForward:
                    return "Axis 2"; 
                case ConfirmStep.WaitingStrafe:
                    return "Axis 1";
                case ConfirmStep.WaitingRotate:
                    return "Axis 5";
                case ConfirmStep.WaitingThrottle:
                    return "Axis 6";
            }
        }

        return axisData[maxIndex].axisName;
    }

    //校准完成
    void ConfirmComplete()
    {
        // 应用检测到的轴映射
        if (!string.IsNullOrEmpty(detectedForwardAxis)) currentMapping.forwardAxis = detectedForwardAxis;
        if (!string.IsNullOrEmpty(detectedStrafeAxis)) currentMapping.strafeAxis = detectedStrafeAxis;
        if (!string.IsNullOrEmpty(detectedRotateAxis)) currentMapping.rotateAxis = detectedRotateAxis;
        if (!string.IsNullOrEmpty(detectedThrottleAxis)) currentMapping.throttleAxis = detectedThrottleAxis;

        SaveMapping();
        UpdateDropdowns();

        // 完全重置定轴面板和按钮
        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false); 
        }

        SetUI(true);

        currentStep = ConfirmStep.NotStarted;
        confirmCoroutine = null;
    }

    //设置UI可交互状态
    void SetUI(bool interactable)
    {
        //设置下拉框交互状态
        if (forwardDropdown != null) forwardDropdown.interactable = interactable;
        if (strafeDropdown != null) strafeDropdown.interactable = interactable;
        if (rotateDropdown != null) rotateDropdown.interactable = interactable;
        if (throttleDropdown != null) throttleDropdown.interactable = interactable;
        //设置反转开关交互状态
        if (invertForwardToggle != null) invertForwardToggle.interactable = interactable;
        if (invertStrafeToggle != null) invertStrafeToggle.interactable = interactable;
        if (invertRotateToggle != null) invertRotateToggle.interactable = interactable;
        if (invertThrottleToggle != null) invertThrottleToggle.interactable = interactable;
        //设置按钮交互状态
        if (resetButton != null) resetButton.interactable = interactable;
        if (confirmAxisButton != null) confirmAxisButton.interactable = interactable;
        if (calibrationAxisButton != null) calibrationAxisButton.interactable = interactable; 
    }

    //更新文本
    void SetConfirmText(string text)
    {
        if (confirmText != null)
        {
            confirmText.text = text;
        }
    }

    void InitializeInvertToggles()
    {
        SetupToggle(invertForwardToggle, InvertForwardChange);
        SetupToggle(invertStrafeToggle, InvertStrafeChange);
        SetupToggle(invertRotateToggle, InvertRotateChange);
        SetupToggle(invertThrottleToggle, InvertThrottleChange);
    }

    void SetupToggle(Toggle toggle, Action<bool> callback)
    {
        if (toggle == null) return;
        toggle.onValueChanged.RemoveAllListeners();
        toggle.onValueChanged.AddListener((isOn) => callback?.Invoke(isOn));
        toggle.isOn = false;
    }
    //反转开关的回调函数
    void InvertForwardChange(bool isOn)
    {
        currentMapping.invertForward = isOn;
        SaveMapping();
    }
    void InvertStrafeChange(bool isOn)
    {
        currentMapping.invertStrafe = isOn;
        SaveMapping();
    }
    void InvertRotateChange(bool isOn)
    {
        currentMapping.invertRotate = isOn;
        SaveMapping();
    }
    void InvertThrottleChange(bool isOn)
    {
        currentMapping.invertThrottle = isOn;
        SaveMapping();
    }

    void InitializeDropdowns()
    {
        SetupDropdown(forwardDropdown, ForwardAxisChange); //前后
        SetupDropdown(strafeDropdown, StrafeAxisChange);  //左右
        SetupDropdown(rotateDropdown, RotateAxisChange);   //旋转
        SetupDropdown(throttleDropdown, ThrottleAxisChange); //上下
    }

    void SetupDropdown(Dropdown dropdown, Action<int> callback)
    {
        if (dropdown == null) return;
        //清除旧监听器并添加新监听器
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.onValueChanged.AddListener((index) => callback?.Invoke(index));
    }

    //检查选择的轴是否已经被其他功能使用
    bool IsAxisUsed(string axisName, Dropdown currentDropdown)
    {
        if (string.IsNullOrEmpty(axisName)) return false;
        //检查轴是否被其他下拉框使用
        if (currentDropdown != forwardDropdown && axisName == currentMapping.forwardAxis)
            return true;
        if (currentDropdown != strafeDropdown && axisName == currentMapping.strafeAxis)
            return true;
        if (currentDropdown != rotateDropdown && axisName == currentMapping.rotateAxis)
            return true;
        if (currentDropdown != throttleDropdown && axisName == currentMapping.throttleAxis)
            return true;

        return false;
    }
    //获取当前下拉框正在使用的轴
    string GetUseAxis(Dropdown dropdown)
    {
        if (dropdown == forwardDropdown) return currentMapping.forwardAxis;
        if (dropdown == strafeDropdown) return currentMapping.strafeAxis;
        if (dropdown == rotateDropdown) return currentMapping.rotateAxis;
        if (dropdown == throttleDropdown) return currentMapping.throttleAxis;
        return "";
    }

    // 处理重复轴选择
    bool HandleAxisSelect(Dropdown changedDropdown, string selectedAxis, string currentAxis, Action<string> setMappingAction)
    {
        if (isProcessingChange) return false;
        if (changedDropdown == null) return false;
        if (selectedAxis == currentAxis)
        {
            return false;
        }
        // 如果选择的轴已经被其他功能使用，恢复原值并显示提示
        if (IsAxisUsed(selectedAxis, changedDropdown))
        {
            isProcessingChange = true;
            // 恢复下拉框显示为原来的值
            SetDropdownVal(changedDropdown, currentAxis);
            // Debug.Log($"轴 {selectedAxis} 已被其他功能使用，请选择其他轴。");
            isProcessingChange = false;
            return false;
        }
        //轴可用的话，更新映射
        setMappingAction?.Invoke(selectedAxis);
        SaveMapping();
        return true;
    }

    // 设置下拉框值但不触发回调（避免递归）
    void SetDropdownVal(Dropdown dropdown, string axisName)
    {
        if (dropdown == null) return;

        dropdown.onValueChanged.RemoveAllListeners();
        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (dropdown.options[i].text == axisName)
            {
                dropdown.value = i;
                break;
            }
        }
        if (dropdown == forwardDropdown)
            dropdown.onValueChanged.AddListener((index) => ForwardAxisChange(index));
        else if (dropdown == strafeDropdown)
            dropdown.onValueChanged.AddListener((index) => StrafeAxisChange(index));
        else if (dropdown == rotateDropdown)
            dropdown.onValueChanged.AddListener((index) => RotateAxisChange(index));
        else if (dropdown == throttleDropdown)
            dropdown.onValueChanged.AddListener((index) => ThrottleAxisChange(index));
    }
    //下拉框值改变时进行函数回调
    void ForwardAxisChange(int index)
    {
        if (forwardDropdown == null || forwardDropdown.options.Count <= index) return;
        string selectedAxis = forwardDropdown.options[index].text;
        string currentAxis = currentMapping.forwardAxis;
        // 处理选择
        HandleAxisSelect(forwardDropdown, selectedAxis, currentAxis,
            (axis) => currentMapping.forwardAxis = axis);
    }
    void StrafeAxisChange(int index)
    {
        if (strafeDropdown == null || strafeDropdown.options.Count <= index) return;
        string selectedAxis = strafeDropdown.options[index].text;
        string currentAxis = currentMapping.strafeAxis;
        HandleAxisSelect(strafeDropdown, selectedAxis, currentAxis,
            (axis) => currentMapping.strafeAxis = axis);
    }
    void RotateAxisChange(int index)
    {
        if (rotateDropdown != null && rotateDropdown.options.Count > index)
        {
            string selectedAxis = rotateDropdown.options[index].text;
            string currentAxis = currentMapping.rotateAxis;
            HandleAxisSelect(rotateDropdown, selectedAxis, currentAxis,
                (axis) => currentMapping.rotateAxis = axis);
        }
    }
    void ThrottleAxisChange(int index)
    {
        if (throttleDropdown != null && throttleDropdown.options.Count > index)
        {
            string selectedAxis = throttleDropdown.options[index].text;
            string currentAxis = currentMapping.throttleAxis;
            HandleAxisSelect(throttleDropdown, selectedAxis, currentAxis,
                (axis) => currentMapping.throttleAxis = axis);
        }
    }

    // 保存映射到PlayerPrefs文件
    void SaveMapping()
    {
        // 保存轴映射
        PlayerPrefs.SetString("AxisMapping_ForwardAxis", currentMapping.forwardAxis);
        PlayerPrefs.SetString("AxisMapping_StrafeAxis", currentMapping.strafeAxis);
        PlayerPrefs.SetString("AxisMapping_RotateAxis", currentMapping.rotateAxis);
        PlayerPrefs.SetString("AxisMapping_ThrottleAxis", currentMapping.throttleAxis);

        // 保存反转设置
        PlayerPrefs.SetInt("AxisMapping_InvertForward", currentMapping.invertForward ? 1 : 0);
        PlayerPrefs.SetInt("AxisMapping_InvertStrafe", currentMapping.invertStrafe ? 1 : 0);
        PlayerPrefs.SetInt("AxisMapping_InvertRotate", currentMapping.invertRotate ? 1 : 0);
        PlayerPrefs.SetInt("AxisMapping_InvertThrottle", currentMapping.invertThrottle ? 1 : 0);

        PlayerPrefs.Save();
    }

    // 加载保存的映射
    void LoadSavedMapping()
    {
        // 加载轴映射
        currentMapping.forwardAxis = PlayerPrefs.GetString("AxisMapping_ForwardAxis", "Axis 2");
        currentMapping.strafeAxis = PlayerPrefs.GetString("AxisMapping_StrafeAxis", "Axis 1");
        currentMapping.rotateAxis = PlayerPrefs.GetString("AxisMapping_RotateAxis", "Axis 5");
        currentMapping.throttleAxis = PlayerPrefs.GetString("AxisMapping_ThrottleAxis", "Axis 6");

        // 加载反转设置
        currentMapping.invertForward = PlayerPrefs.GetInt("AxisMapping_InvertForward", 0) == 1;
        currentMapping.invertStrafe = PlayerPrefs.GetInt("AxisMapping_InvertStrafe", 0) == 1;
        currentMapping.invertRotate = PlayerPrefs.GetInt("AxisMapping_InvertRotate", 0) == 1;
        currentMapping.invertThrottle = PlayerPrefs.GetInt("AxisMapping_InvertThrottle", 0) == 1;

        UpdateDropdowns();
        UpdateInvertToggles();
    }

    // 从映射配置更新下拉框显示
    void UpdateDropdowns()
    {
        SetDropdownValue(forwardDropdown, currentMapping.forwardAxis);
        SetDropdownValue(strafeDropdown, currentMapping.strafeAxis);
        SetDropdownValue(rotateDropdown, currentMapping.rotateAxis);
        SetDropdownValue(throttleDropdown, currentMapping.throttleAxis);
    }

    // 从映射配置更新反转开关显示
    void UpdateInvertToggles()
    {
        if (invertForwardToggle != null)
            invertForwardToggle.isOn = currentMapping.invertForward;
        if (invertStrafeToggle != null)
            invertStrafeToggle.isOn = currentMapping.invertStrafe;
        if (invertRotateToggle != null)
            invertRotateToggle.isOn = currentMapping.invertRotate;
        if (invertThrottleToggle != null)
            invertThrottleToggle.isOn = currentMapping.invertThrottle;
    }

    void SetDropdownValue(Dropdown dropdown, string axisName)
    {
        if (dropdown == null) return;

        for (int i = 0; i < dropdown.options.Count; i++)
        {
            if (dropdown.options[i].text == axisName)
            {
                dropdown.value = i;
                break;
            }
        }
    }

    //重置所有映射为默认值
    public void ResetDefault()
    {
        //重置轴映射
        currentMapping.forwardAxis = "Axis 1";
        currentMapping.strafeAxis = "Axis 2";
        currentMapping.rotateAxis = "Axis 3";
        currentMapping.throttleAxis = "Axis 4";

        //重置反转设置
        currentMapping.invertForward = false;
        currentMapping.invertStrafe = false;
        currentMapping.invertRotate = false;
        currentMapping.invertThrottle = false;
        //重置所有轴的校准数据
        for (int i = 0; i < 10; i++)
        {
            calibrationData[i].negativeMax = -1f;
            calibrationData[i].positiveMax = 1f;
            calibrationData[i].zeroOffset = 0f;

            SaveCalibrationData(i);
        }
        UpdateDropdowns();
        UpdateInvertToggles();
        SaveMapping();
    }
}