using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
//*****************************************
//创建人： Mezcal
//功能说明：无人机自动控制脚本，包含手动控制与自动任务执行功能
//***************************************** 
public class AutoDroneController : MonoBehaviour
{
    // 无人机实现
    public GameObject Drone;
    // 旋翼
    public Transform[] Paddles;
    private Transform droneTrans;
    //存储摇杆操作
    private JoyStickData leftStickData;
    private JoyStickData rightStickData;
    public float speed = 1f;
    // 当前飞行状态
    private FlyState _flyState = FlyState.PowerOn;
    //封装属性，状态变更逻辑
    public FlyState flyState
    {
        get { return _flyState; }
        set
        {
            if (_flyState != value)
            {
                // 处理从 PowerOn 状态切换到 FlyStart 或 FlyingManual 状态
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
    //启动螺旋翼的相关变量
    private bool paddleStart = false;
    private int paddleStartFrame = 0;
    private bool paddleStop = false;
    private int paddleStopFrame = 0;
    private float paddleSpeed = 0;
    private float paddleSpeedFrame = 0;
    public TMPro.TMP_Text DroneHeight;

    // 飞行转向状态
    private int _directionState = 0;
    //封装属性，状态变更逻辑
    public int directionState
    {
        get { return _directionState; }
        set
        {
            if (_directionState != value)
            {
                //转向开始
                if (_directionState == 0)
                {
                    turnStart = true;
                    turnStartFrame = 0;
                    turnStartRot = droneTrans.rotation;
                }
                else
                {
                    //转向停止
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
    private Quaternion turnStartRot;
    private bool turnStop = false;
    private int turnStopFrame = 0;

    // 自动任务队列相关变量
    private Queue<AutoTask> autoTaskQueue = new Queue<AutoTask>();
    private bool isAutoTaskRunning = false;

    void Start()
    {
        droneTrans = Drone.transform;
        //初始化左右遥感数据
        leftStickData = new JoyStickData();
        rightStickData = new JoyStickData();
        InsightConst.MoveSpeed_XZ = speed;

        // 设置自动任务队列
        SetupAutoTasks();
    }

    // 设置自动任务队列
    private void SetupAutoTasks()
    {
        // 在自动任务开始前启动螺旋桨
        paddleStart = true;
        paddleStartFrame = 0;
        paddleSpeedFrame = paddleSpeed;
        // 1. 悬停至目标高度（1米）
        autoTaskQueue.Enqueue(new AutoTask(() => HoverToHeight(1f), "升空至1"));

        // 2. 慢速水平旋转360度
        autoTaskQueue.Enqueue(new AutoTask(() => RotateHorizontal360(), "旋转360"));

        // 3. 水平飞行8字
        autoTaskQueue.Enqueue(new AutoTask(() => FlyHorizontalFigureEight(), "8字飞行"));

        // 4. 在最后位置定点降落
        autoTaskQueue.Enqueue(new AutoTask(() => LandAtPoint(), "归位"));

        // 启动自动任务
        StartCoroutine(RunAutoTasks());
    }

    // 运行自动任务队列
    private IEnumerator RunAutoTasks()
    {
        isAutoTaskRunning = true;

        while (autoTaskQueue.Count > 0)
        {
            AutoTask task = autoTaskQueue.Dequeue();
            Debug.Log("Starting task: " + task.TaskName);

            // 执行任务
            yield return StartCoroutine(task.TaskAction());

            Debug.Log("Finished task: " + task.TaskName);

            // 任务之间短暂等待
            yield return new WaitForSeconds(1f);
        }

        isAutoTaskRunning = false;
        Debug.Log("完成所有任务");
        // 在所有任务完成后停止螺旋桨
        paddleStop = true;
        paddleStopFrame = 0;
    }

    // 悬停至指定高度
    public IEnumerator HoverToHeight(float targetHeight)
    {
        float duration = 2f; // 飞行持续时间
        float elapsedTime = 0f;
        Vector3 startPosition = Drone.transform.position;

        while (elapsedTime < duration)
        {
            // 通过线性插值移动无人机
            float t = elapsedTime / duration;
            Drone.transform.position = Vector3.Lerp(startPosition, new Vector3(startPosition.x, targetHeight, startPosition.z), t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // 慢速水平旋转360度
    public IEnumerator RotateHorizontal360()
    {
        float duration = 10f;
        float elapsedTime = 0f;
        float originalYRotation = Drone.transform.eulerAngles.y;
        float targetRotation = originalYRotation + 360f;

        while (elapsedTime < duration)
        {
            //旋转功能
            float t = elapsedTime / duration;
            float currentRotation = Mathf.Lerp(originalYRotation, targetRotation, t);
            Drone.transform.rotation = Quaternion.Euler(Drone.transform.eulerAngles.x, currentRotation, Drone.transform.eulerAngles.z);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    // 水平飞行8字
    public IEnumerator FlyHorizontalFigureEight()
    {
        float duration = 15f; // 飞行持续时间
        float radius = 5f; // 飞行半径
        Vector3 startPosition = Drone.transform.position; // 从当前位置开始
        float angle = 0f;
        float angleSpeed = (2 * Mathf.PI) / duration; // 角速度

        for (float t = 0f; t < duration; t += Time.fixedDeltaTime)
        {
            angle += angleSpeed * Time.fixedDeltaTime;
            // 使用平滑插值计算位置
            float x = startPosition.x + radius * Mathf.Cos(angle);
            float z = startPosition.z + radius * Mathf.Sin(angle) * Mathf.Cos(angle);

            // 平滑移动到目标位置
            Drone.transform.position = Vector3.Lerp(
                Drone.transform.position,
                new Vector3(x, startPosition.y, z),
                Time.fixedDeltaTime * 10f // 调整插值速度
            );

            // 确保旋翼动画与飞行状态同步
            paddleStart = true;
            paddleSpeed = Mathf.Lerp(paddleSpeed, 100.0f, 0.1f); // 保持旋翼转动

            yield return new WaitForFixedUpdate(); // 等待物理更新
        }

        // 任务完成后恢复初始旋翼速度
        paddleSpeed = 100.0f;
    }

    // 定点降落
    public IEnumerator LandAtPoint()
    {
        float duration = 5f; // 降落持续时间
        float elapsedTime = 0f;
        Vector3 startPosition = Drone.transform.position;
        Vector3 landingPosition = new Vector3(0, 0, 0); // 降落点位置

        while (elapsedTime < duration)
        {
            float t = elapsedTime / duration;
            Drone.transform.position = Vector3.Lerp(startPosition, landingPosition, t);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
    }

    private void Update()
    {
        // 如果正在运行自动任务，则禁用手动控制
        if (!isAutoTaskRunning)
        {
            #region 左摇杆
            if (Input.GetKeyDown(KeyCode.W))
            {
                leftStickData.direction.y = 1;
                Debug.Log("left joystick work: up! " + leftStickData.direction.x + "," + leftStickData.direction.y);
            }

            if (Input.GetKeyUp(KeyCode.W))
            {
                leftStickData.direction.y = 0;
                Debug.Log("left joystick work: up cancel! " + leftStickData.direction.x + "," + leftStickData.direction.y);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                leftStickData.direction.y = -1;
                Debug.Log("left joystick work: down! " + leftStickData.direction.x + "," + leftStickData.direction.y);
            }

            if (Input.GetKeyUp(KeyCode.S))
            {
                leftStickData.direction.y = 0;
                Debug.Log("left joystick work: down cancel! " + leftStickData.direction.x + "," + leftStickData.direction.y);
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                leftStickData.direction.x = -1;
                Debug.Log("left joystick work: left spin! " + leftStickData.direction.x + "," + leftStickData.direction.y);
            }

            if (Input.GetKeyUp(KeyCode.A))
            {
                leftStickData.direction.x = 0;
                Debug.Log("left joystick work: left spin cancel! " + leftStickData.direction.x + "," + leftStickData.direction.y);
            }

            if (Input.GetKeyDown(KeyCode.D))
            {
                leftStickData.direction.x = 1;
                Debug.Log("left joystick work: right spin! " + leftStickData.direction.x + "," + leftStickData.direction.y);
            }

            if (Input.GetKeyUp(KeyCode.D))
            {
                leftStickData.direction.x = 0;
                Debug.Log("left joystick work: right spin cancel! " + leftStickData.direction.x + "," + leftStickData.direction.y);
            }
            #endregion

            #region 右摇杆
            if (Input.GetKeyDown(KeyCode.I))
            {
                rightStickData.direction.y = 1;
                Debug.Log("right joystick work: foward! " + rightStickData.direction.x + "," + rightStickData.direction.y);
            }

            if (Input.GetKeyUp(KeyCode.I))
            {
                rightStickData.direction.y = 0;
                Debug.Log("right joystick work: up cancel! " + rightStickData.direction.x + "," + rightStickData.direction.y);
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                rightStickData.direction.y = -1;
                Debug.Log("right joystick work: down! " + rightStickData.direction.x + "," + rightStickData.direction.y);
            }

            if (Input.GetKeyUp(KeyCode.K))
            {
                rightStickData.direction.y = 0;
                Debug.Log("right joystick work: down cancel! " + rightStickData.direction.x + "," + rightStickData.direction.y);
            }

            if (Input.GetKeyDown(KeyCode.J))
            {
                rightStickData.direction.x = -1;
                Debug.Log("right joystick work: left spin! " + rightStickData.direction.x + "," + rightStickData.direction.y);
            }

            if (Input.GetKeyUp(KeyCode.J))
            {
                rightStickData.direction.x = 0;
                Debug.Log("right joystick work: left spin cancel! " + rightStickData.direction.x + "," + rightStickData.direction.y);
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                rightStickData.direction.x = 1;
                Debug.Log("right joystick work: right spin! " + rightStickData.direction.x + "," + rightStickData.direction.y);
            }

            if (Input.GetKeyUp(KeyCode.L))
            {
                rightStickData.direction.x = 0;
                Debug.Log("right joystick work: right spin cancel! " + rightStickData.direction.x + "," + rightStickData.direction.y);
            }
            #endregion
            // 更新左摇杆的 joystickData 状态
            if (Mathf.Abs(leftStickData.direction.y) <= 0.1f)
            {
                // 清除垂直方向的操作状态
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
                //重新更新垂直方向的操作状态
                if (leftStickData.direction.y > 0.1f)
                {
                    if ((leftStickData.joystickData & (int)LeftJoystick.Up) <= 0)
                    {
                        leftStickData.joystickData += (int)LeftJoystick.Up;
                    }
                    flyState = FlyState.FlyingManual;
                }
                if (leftStickData.direction.y < -0.1f)
                {
                    if ((leftStickData.joystickData & (int)LeftJoystick.Down) <= 0)
                    {
                        leftStickData.joystickData += (int)LeftJoystick.Down;
                    }
                }
            }
            //左摇杆
            if (Mathf.Abs(leftStickData.direction.x) <= 0.1f)
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
                if (leftStickData.direction.x < -0.1f)
                {
                    if ((leftStickData.joystickData & (int)LeftJoystick.LeftSpin) <= 0)
                    {
                        leftStickData.joystickData += (int)LeftJoystick.LeftSpin;
                    }
                }
                else if (leftStickData.direction.x > 0.1f)
                {
                    if ((leftStickData.joystickData & (int)LeftJoystick.RightSpin) <= 0)
                    {
                        leftStickData.joystickData += (int)LeftJoystick.RightSpin;
                    }
                }
            }
            // 更新右摇杆的 joystickData 状态和 directDevice 方向
            if (rightStickData.direction.magnitude <= 0.1f)
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
            // 更新操作状态和方向向量
            else
            {
                if (rightStickData.direction.y > 0.1f)
                {
                    if ((rightStickData.joystickData & (int)RightJoystick.Forward) <= 0)
                    {
                        rightStickData.joystickData += (int)RightJoystick.Forward;
                        rightStickData.directDevice += droneTrans.forward;
                    }
                }
                else if (rightStickData.direction.y < -0.1f)
                {
                    if ((rightStickData.joystickData & (int)RightJoystick.Backward) <= 0)
                    {
                        rightStickData.joystickData += (int)RightJoystick.Backward;
                        rightStickData.directDevice += -droneTrans.forward;
                    }
                }
                if (rightStickData.direction.x > 0.1f)
                {
                    if ((rightStickData.joystickData & (int)RightJoystick.RightTurn) <= 0)
                    {
                        rightStickData.joystickData += (int)RightJoystick.RightTurn;
                        rightStickData.directDevice += droneTrans.right;
                    }
                }
                else if (rightStickData.direction.x < -0.1f)
                {
                    if ((rightStickData.joystickData & (int)RightJoystick.LeftTurn) <= 0)
                    {
                        rightStickData.joystickData += (int)RightJoystick.LeftTurn;
                        rightStickData.directDevice += -droneTrans.right;
                    }
                }

            }
            directionState = rightStickData.joystickData;
        }

        UpdateHeight();
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

        // 左摇杆 -- 垂直轴 -- 移动
        if (!isAutoTaskRunning && (leftStickData.joystickData & ((int)LeftJoystick.Up | (int)LeftJoystick.Down)) > 0)
        {
            if (Drone.transform.position.y > 0 || (leftStickData.joystickData & (int)LeftJoystick.Up) > 0)
            {
                droneTrans.Translate(new Vector3(0, leftStickData.direction.y, 0) * InsightConst.MoveSpeed_Y, Space.World);
            }
        }

        // 左摇杆 -- 垂直轴 -- 旋转
        if (!isAutoTaskRunning && (leftStickData.joystickData & ((int)LeftJoystick.LeftSpin | (int)LeftJoystick.RightSpin)) > 0)
        {
            droneTrans.Rotate(0, leftStickData.direction.x * InsightConst.MoveSpeed_Y_Spin, 0);
        }

        // 右摇杆 -- 前后左右方向
        if (!isAutoTaskRunning && rightStickData.joystickData > 0)
        {
            droneTrans.Translate(rightStickData.directDevice * InsightConst.MoveSpeed_XZ, Space.World);

            if (turnStart)
            {
                var v = (turnStartFrame++) * 1.0f / 80;
                var axis = Vector3.Cross(Vector3.up, rightStickData.directDevice.normalized);
                var tTot = Quaternion.AngleAxis(
                    rightStickData.directDevice.magnitude * (InsightConst.MoveMaxAngle_XZ - InsightConst.MoveMinAngle_XZ) + InsightConst.MoveMinAngle_XZ,
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

    void UpdateHeight()
    {
        var h = Drone.transform.position.y;
        DroneHeight.text = $"Height: {h:0.00}";
        DroneHeight.color = h < 1 && ((leftStickData.joystickData & (int)LeftJoystick.Down) > 0) ? Color.red : Color.green;
    }
    //绘制8字飞行路线
    private void OnDrawGizmos()
    {
        float radius = 5f;
        Vector3 startPosition = transform.position;
        Gizmos.color = Color.cyan;
        for (float angle = 0; angle < 2 * Mathf.PI; angle += 0.1f)
        {
            float x = startPosition.x + radius * Mathf.Cos(angle);
            float z = startPosition.z + radius * Mathf.Sin(angle) * Mathf.Cos(angle);
            Gizmos.DrawSphere(new Vector3(x, startPosition.y, z), 0.1f);
        }
    }
}


// 自动任务辅助类
public class AutoTask
{
    //执行协程
    public System.Func<IEnumerator> TaskAction { get; private set; }
    public string TaskName { get; private set; }

    public AutoTask(System.Func<IEnumerator> action, string name)
    {
        TaskAction = action;
        TaskName = name;
    }

}


