using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人： Mezcal
//功能说明：无人机数据结构和枚举
//***************************************** 
public class InsightData
{
    // 飞行摇杆轴名称常量
    public const string LEFT_STICK_X = "LeftStickX";      // 左摇杆X轴（备用）
    public const string LEFT_STICK_Y = "LeftStickY";      // 左摇杆Y轴（俯仰-前后移动）
    public const string LEFT_STICK_Z = "LeftStickZ";      // 左摇杆Z轴（偏航-旋转）

    public const string RIGHT_STICK_X = "RightStickX";    // 右摇杆X轴（左右平移）
    public const string RIGHT_STICK_Y = "RightStickY";    // 右摇杆Y轴（油门-升降）
}

// 摇杆数据容器
public class JoyStickData
{
    public Vector2 direction = Vector2.zero;    // 摇杆方向向量
    public int joystickData = 0;                // 状态位组合
    public Vector3 directDevice = Vector3.zero; // 世界空间方向
    public float throttle = 0f;                 // 油门值 (0-1)
}

// 左摇杆枚举-位标志
public enum LeftJoystick
{
    Default = 0, Up = 1, Down = 2,
    LeftSpin = 4, RightSpin = 8
}

// 右摇杆枚举  
public enum RightJoystick
{
    Default = 0, Forward = 1, Backward = 2,
    LeftTurn = 4, RightTurn = 8
}

// 飞行状态机
public enum FlyState
{
    PowerOff = 0, PowerOn = 1, FlyStart = 10,
    FlyingManual = 11, FlyFollow = 12, ReturnHome = 13
}