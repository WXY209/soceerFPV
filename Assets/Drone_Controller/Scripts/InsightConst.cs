using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Mezcal
//功能说明：无人机常量配置
//***************************************** 
public class InsightConst
{
    public static Vector3 g = Physics.gravity;
    /// <summary>
    /// 向上向下的移动速度
    /// </summary>
    public static float MoveSpeed_Y = 0.02f;
    /// <summary>
    /// 水平的旋转速度（绕Y+轴）
    /// </summary>
    public static float MoveSpeed_Y_Spin = 0.4f;
    /// <summary>
    /// 水平的移动速度
    /// </summary>
    public static float MoveSpeed_XZ = 0.01f;
    /// <summary>
    /// 水平移动时的最大倾角
    /// </summary>
    public static float MoveMaxAngle_XZ = 15;
    /// <summary>
    /// 水平移动时的最大倾角
    /// </summary>
    public static float MoveMinAngle_XZ = 10;
}
