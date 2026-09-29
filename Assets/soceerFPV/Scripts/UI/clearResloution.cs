using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人：
//功能说明：设置分辨率为1920
//注意：因为SettingsContentManager脚本ApplyResolution修改分辨率后导致部分ui不显示，用它强制恢复，后再在界面设置正确的1920分辨率，后续导出才会正常。
//***************************************** 
public class clearResloution : MonoBehaviour
{
    void Awake()
    {
        Screen.SetResolution(1920, 1080, true);
    }

    void Update()
    {
        
    }
}
