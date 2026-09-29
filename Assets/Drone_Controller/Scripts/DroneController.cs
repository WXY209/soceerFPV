using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    //无人机实现
    public GameObject Drone;
    // 旋翼
    public Transform[] Paddles;
    private Transform droneTrans;
    private JoyStickData leftStickData;
    private JoyStickData rightStickData;
    public float speed = 1f;

    // 摇杆死区阈值
    public float joystickDeadZone = 0.1f;

    // 当前飞行状态
    private FlyState _flyState = FlyState.PowerOn;
    public FlyState flyState
    {
        get { return _flyState; }
        set
        {
            if (_flyState != value)
            {
                if (_flyState == FlyState.PowerOn && (
                    value == FlyState.FlyStart || value == FlyState.FlyingManual))
                {
                    paddleStart = true;
                    paddleStartFrame = 0;
                    paddleSpeedFrame = paddleSpeed;
                }
                if (_flyState == FlyState.PowerOn && value == FlyState.PowerOff)
                {
                    paddleStop = true;
                    paddleStopFrame = 0;
                    paddleSpeedFrame = paddleSpeed;
                }
                _flyState = value;
            }
        }
    }
    private bool paddleStart = false;
    private int paddleStartFrame = 0;
    private bool paddleStop = false;
    private int paddleStopFrame = 0;
    private float paddleSpeed = 0;
    private float paddleSpeedFrame = 0; //记录当前桨叶速度

    // 飞行转向状态
    private int _directionState = 0;
    public int directionState
    {
        get { return _directionState; }
        set
        {
            if (_directionState != value)
            {
                if (_directionState == 0)
                {
                    turnStart = true;
                    turnStartFrame = 0;
                    turnStartRot = droneTrans.rotation;
                }
                else
                {
                    if (value == 0)
                    {
                        turnStop = true;
                        turnStopFrame = 0;
                    }
                }
                _directionState = value;
            }
        }
    }
    private bool turnStart = false;
    private int turnStartFrame = 0;
    private Quaternion turnStartRot; //记录当前姿态
    private bool turnStop = false;
    private int turnStopFrame = 0;

    void Start()
    {
        droneTrans = Drone.transform;
        leftStickData = new JoyStickData();
        rightStickData = new JoyStickData();
        InsightConst.MoveSpeed_XZ = speed; // 设置新的飞行速度

        Debug.Log("飞行摇杆控制器初始化完成");
        Debug.Log("控制说明：");
        Debug.Log("左摇杆前后 - 俯仰控制");
        Debug.Log("左摇杆左右 - 偏航旋转");
        Debug.Log("右摇杆前后 - 油门控制");
        Debug.Log("右摇杆左右 - 左右平移");
    }

    private void Update()
    {
        ProcessJoystickInput();
        ProcessLeftStickState();
        ProcessRightStickState();
        directionState = rightStickData.joystickData;
        UpdateFlightStateByThrottle();

        // 调试信息
        if (Input.GetKeyDown(KeyCode.P))
        {
            Debug.Log($"左摇杆: {leftStickData.direction}, 右摇杆: {rightStickData.direction}, 油门: {rightStickData.throttle:F2}");
            Debug.Log($"飞行状态: {flyState}, 方向状态: {directionState}");
        }
    }

    private void ProcessJoystickInput()
    {
        #region 左摇杆输入处理（俯仰和旋转）
        // 左摇杆前后：俯仰控制（原W/S键功能）- 使用Y轴
        float leftY = Input.GetAxis(InsightData.LEFT_STICK_Y);
        // 左摇杆左右：偏航控制-旋转（原A/D键功能）- 使用Z轴
        float leftZ = Input.GetAxis(InsightData.LEFT_STICK_Z);

        // 应用死区过滤
        leftStickData.direction.y = Mathf.Abs(leftY) > joystickDeadZone ? leftY : 0f;
        leftStickData.direction.x = Mathf.Abs(leftZ) > joystickDeadZone ? leftZ : 0f;
        #endregion

        #region 右摇杆输入处理（油门和平移）
        // 右摇杆前后：油门控制（升降）- 使用Y轴
        float rightY = Input.GetAxis(InsightData.RIGHT_STICK_Y);
        // 右摇杆左右：左右平移 - 使用X轴
        float rightX = Input.GetAxis(InsightData.RIGHT_STICK_X);

        // 应用死区过滤并设置油门值（将[-1,1]映射到[0,1]）
        rightStickData.throttle = Mathf.Clamp01((rightY + 1f) * 0.5f);
        rightStickData.direction.y = Mathf.Abs(rightY) > joystickDeadZone ? rightY : 0f;
        rightStickData.direction.x = Mathf.Abs(rightX) > joystickDeadZone ? rightX : 0f;
        #endregion
    }

    private void ProcessLeftStickState()
    {
        // 垂直方向状态位（俯仰）
        if (Mathf.Abs(leftStickData.direction.y) <= joystickDeadZone)
        {
            if ((leftStickData.joystickData & (int)LeftJoystick.Up) > 0)
            {
                leftStickData.joystickData -= (int)LeftJoystick.Up;
            }
            if ((leftStickData.joystickData & (int)LeftJoystick.Down) > 0)
            {
                leftStickData.joystickData -= (int)LeftJoystick.Down;
            }
        }
        else
        {
            if (leftStickData.direction.y > joystickDeadZone)
            {
                if ((leftStickData.joystickData & (int)LeftJoystick.Up) <= 0)
                {
                    leftStickData.joystickData += (int)LeftJoystick.Up;
                }
                flyState = FlyState.FlyingManual;
            }
            if (leftStickData.direction.y < -joystickDeadZone)
            {
                if ((leftStickData.joystickData & (int)LeftJoystick.Down) <= 0)
                {
                    leftStickData.joystickData += (int)LeftJoystick.Down;
                }
            }
        }

        // 水平旋转状态位（偏航）
        if (Mathf.Abs(leftStickData.direction.x) <= joystickDeadZone)
        {
            if ((leftStickData.joystickData & (int)LeftJoystick.LeftSpin) > 0)
            {
                leftStickData.joystickData -= (int)LeftJoystick.LeftSpin;
            }
            if ((leftStickData.joystickData & (int)LeftJoystick.RightSpin) > 0)
            {
                leftStickData.joystickData -= (int)LeftJoystick.RightSpin;
            }
        }
        else
        {
            if (leftStickData.direction.x < -joystickDeadZone)
            {
                if ((leftStickData.joystickData & (int)LeftJoystick.LeftSpin) <= 0)
                {
                    leftStickData.joystickData += (int)LeftJoystick.LeftSpin;
                }
            }
            else if (leftStickData.direction.x > joystickDeadZone)
            {
                if ((leftStickData.joystickData & (int)LeftJoystick.RightSpin) <= 0)
                {
                    leftStickData.joystickData += (int)LeftJoystick.RightSpin;
                }
            }
        }
    }

    private void ProcessRightStickState()
    {
        if (rightStickData.direction.magnitude <= joystickDeadZone)
        {
            if ((rightStickData.joystickData & (int)RightJoystick.Forward) > 0)
            {
                rightStickData.joystickData -= (int)RightJoystick.Forward;
            }
            if ((rightStickData.joystickData & (int)RightJoystick.Backward) > 0)
            {
                rightStickData.joystickData -= (int)RightJoystick.Backward;
            }
            if ((rightStickData.joystickData & (int)RightJoystick.RightTurn) > 0)
            {
                rightStickData.joystickData -= (int)RightJoystick.RightTurn;
            }
            if ((rightStickData.joystickData & (int)RightJoystick.LeftTurn) > 0)
            {
                rightStickData.joystickData -= (int)RightJoystick.LeftTurn;
            }
            rightStickData.directDevice = Vector3.zero;
        }
        else
        {
            if (rightStickData.direction.y > joystickDeadZone)
            {
                if ((rightStickData.joystickData & (int)RightJoystick.Forward) <= 0)
                {
                    rightStickData.joystickData += (int)RightJoystick.Forward;
                    rightStickData.directDevice += droneTrans.forward;
                }
            }
            else if (rightStickData.direction.y < -joystickDeadZone)
            {
                if ((rightStickData.joystickData & (int)RightJoystick.Backward) <= 0)
                {
                    rightStickData.joystickData += (int)RightJoystick.Backward;
                    rightStickData.directDevice += -droneTrans.forward;
                }
            }
            if (rightStickData.direction.x > joystickDeadZone)
            {
                if ((rightStickData.joystickData & (int)RightJoystick.RightTurn) <= 0)
                {
                    rightStickData.joystickData += (int)RightJoystick.RightTurn;
                    rightStickData.directDevice += droneTrans.right;
                }
            }
            else if (rightStickData.direction.x < -joystickDeadZone)
            {
                if ((rightStickData.joystickData & (int)RightJoystick.LeftTurn) <= 0)
                {
                    rightStickData.joystickData += (int)RightJoystick.LeftTurn;
                    rightStickData.directDevice += -droneTrans.right;
                }
            }
        }
    }

    private void UpdateFlightStateByThrottle()
    {
        // 根据油门值自动管理飞行状态
        if (rightStickData.throttle > 0.2f && (flyState == FlyState.PowerOn || flyState == FlyState.PowerOff))
        {
            flyState = FlyState.FlyingManual;
        }
        else if (rightStickData.throttle <= 0.1f && flyState == FlyState.FlyingManual)
        {
            flyState = FlyState.PowerOn;
        }
    }

    void FixedUpdate()
    {
        // 桨叶启动动画
        if (paddleStart)
        {
            if (paddleStartFrame < 80)
            {
                var v = (paddleStartFrame++) * 1.0f / 80;
                paddleSpeed = Mathf.Lerp(paddleSpeedFrame, 100.0f, v >= 1 ? 1 : v);
            }
            foreach (var paddle in Paddles)
            {
                paddle.Rotate(Vector3.up, paddleSpeed);
            }
        }
        // 桨叶停止动画
        if (paddleStop)
        {
            if (paddleStopFrame < 50)
            {
                var v = (paddleStopFrame++) * 1.0f / 50;
                paddleSpeed = Mathf.Lerp(paddleSpeedFrame, 0.0f, v >= 1 ? 1 : v);
            }
            foreach (var paddle in Paddles)
            {
                paddle.Rotate(Vector3.up, paddleSpeed);
            }
        }

        // 左摇杆 -- 垂直轴 -- 移动（结合油门控制）
        if ((leftStickData.joystickData & ((int)LeftJoystick.Up | (int)LeftJoystick.Down)) > 0)
        {
            float verticalMove = leftStickData.direction.y * rightStickData.throttle;
            if (Drone.transform.position.y > 0 || (leftStickData.joystickData & (int)LeftJoystick.Up) > 0)
            {
                droneTrans.Translate(new Vector3(0, verticalMove, 0) * InsightConst.MoveSpeed_Y, Space.World);
            }
        }

        // 左摇杆 -- 水平轴 -- 旋转（结合油门控制）
        if ((leftStickData.joystickData & ((int)LeftJoystick.LeftSpin | (int)LeftJoystick.RightSpin)) > 0)
        {
            float rotationSpeed = leftStickData.direction.x * InsightConst.MoveSpeed_Y_Spin * rightStickData.throttle;
            droneTrans.Rotate(0, rotationSpeed, 0);
        }

        // 右摇杆 -- 前后左右方向（结合油门控制）
        if (rightStickData.joystickData > 0)
        {
            Vector3 moveDirection = rightStickData.directDevice * rightStickData.throttle;
            droneTrans.Translate(moveDirection * InsightConst.MoveSpeed_XZ, Space.World);

            if (turnStart)
            {
                var v = (turnStartFrame++) * 1.0f / 80;
                //计算平移运动时的在台倾斜的旋转轴
                var axis = Vector3.Cross(Vector3.up, moveDirection.normalized);
                var tTot = Quaternion.AngleAxis(
                    moveDirection.magnitude * (InsightConst.MoveMaxAngle_XZ - InsightConst.MoveMinAngle_XZ) + InsightConst.MoveMinAngle_XZ,
                    axis);
                droneTrans.rotation = Quaternion.Lerp(turnStartRot, tTot * turnStartRot, v);
                if (v > 1)
                {
                    turnStart = false;
                    turnStartFrame = 0;
                }
            }
        }
        else
        {
            if (turnStop)
            {
                var v = (turnStopFrame++) * 1.0f / 40;
                var lRot = droneTrans.rotation;
                droneTrans.rotation = Quaternion.Lerp(lRot, Quaternion.Euler(0, lRot.eulerAngles.y, 0), v);
                if (v > 1)
                {
                    turnStop = false;
                    turnStopFrame = 0;
                }
            }
        }
    }
}