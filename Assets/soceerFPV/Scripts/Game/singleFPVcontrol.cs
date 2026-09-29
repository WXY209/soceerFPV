using Mirror.BouncyCastle.Bcpg;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人：
//功能说明：真正的玩家操作的无人机运动控制
//***************************************** 
public class singleFPVcontrol : MonoBehaviour
{
    [Header("运动参数")]
    public float speed = 2f;
    public float rotateSpeed = 200f;
    public float liftSpeed = 3f;
    //public float factorSmth = 0.995f;
    //private float Ffiltersmth = 0f;
    //private float Sfiltersmth = 0f;
    //private float Tfiltersmth = 0f;

    [Header("惯性设置")]
    public float acceleration = 6f;
    public float deceleration = 8f;
    private Vector3 currentVelocity = Vector3.zero;
    public float inertiaCoefficient = 0.02f;//惯性系数

    [Header("声音设置")]
    public AudioClip engineSound;
    public float minPitch = 1f;
    public float maxPitch = 2f;

    [Header("摇杆校准")]
    public float deadZone = 0.1f;
    public float TdeadZone = 0.1f;
    public float YdeadZone = 0.2f;
    private float joyOffsetX;
    private float joyOffsetY;
    private int dateCount = 0;

    [Header("轴映射设置")]
    public string forwardAxis = "Axis 2";
    public string strafeAxis = "Axis 1";
    public string rotateAxis = "Axis 5";
    public string throttleAxis = "Axis 6";

    [Header("轴反转设置")]
    public bool invertForwardAxis = false;
    public bool invertStrafeAxis = false;
    public bool invertRotateAxis = false;
    public bool invertThrottleAxis = false;

    [Header("倾角参数")]
    public float maxAngle = 15f;
    public float tiltSpeed = 30f;
    private Vector3 targetAngle = Vector3.zero;

    private AudioSource audioSource;
    private Rigidbody rb;

    public float inputForward;
    public float inputRotate;
    public float inputStrafe;
    public float inputThrottle;

    void Start()
    {
        InitializeComponents();
        // 加载保存的轴映射设置
        LoadAxisMapping();
    }

    void InitializeComponents()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.angularDrag = 3f;
        rb.drag = 0f;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.loop = true;
            audioSource.spatialBlend = 1f;
            audioSource.clip = engineSound;
        }
    }

    //从PlayerPrefs加载保存的映射
    void LoadAxisMapping()
    {
        forwardAxis = PlayerPrefs.GetString("AxisMapping_ForwardAxis", "Axis 2");
        strafeAxis = PlayerPrefs.GetString("AxisMapping_StrafeAxis", "Axis 1");
        rotateAxis = PlayerPrefs.GetString("AxisMapping_RotateAxis", "Axis 5");
        throttleAxis = PlayerPrefs.GetString("AxisMapping_ThrottleAxis", "Axis 6");
        //加载反转设置
        invertForwardAxis = PlayerPrefs.GetInt("AxisMapping_InvertForward", 0) == 1;
        invertStrafeAxis = PlayerPrefs.GetInt("AxisMapping_InvertStrafe", 0) == 1;
        invertRotateAxis = PlayerPrefs.GetInt("AxisMapping_InvertRotate", 0) == 1;
        invertThrottleAxis = PlayerPrefs.GetInt("AxisMapping_InvertThrottle", 0) == 1;
    }

    void Update()
    {
        GetInput();
        UpdateSound();
    }

    void FixedUpdate()
    {
       // Debug.Log($"当前速度: {rb.velocity.magnitude:F2}");
        MoveDrone();
    }

    void GetInput()
    {
        // 使用从PlayerPrefs加载的轴映射读取输入
        float Forward = Input.GetAxis(forwardAxis); //前后
        float Strafe = Input.GetAxis(strafeAxis);//左右平移
        inputRotate = Input.GetAxis(rotateAxis);//旋转
        inputThrottle = Input.GetAxis(throttleAxis);//升降

        //反转设置
        if (invertForwardAxis) Forward = -Forward;
        if (invertStrafeAxis) Strafe = -Strafe;
        if (invertRotateAxis) inputRotate = -inputRotate;
        if (invertThrottleAxis) inputThrottle = -inputThrottle;

        ////滤波实现惯性
        //if (Mathf.Abs(Forward) > Mathf.Abs(Ffiltersmth))
        //{
        //    Ffiltersmth = Ffiltersmth * 0.95f + Forward * 0.05f;
        //}
        //else
        //{
        //    Ffiltersmth = Ffiltersmth * factorSmth + Forward * (1 - factorSmth);
        //}
        //if (Mathf.Abs(Strafe) > Mathf.Abs(Sfiltersmth))
        //{
        //    Sfiltersmth = Sfiltersmth * 0.95f + Strafe * (1 - 0.95f);
        //}
        //else
        //{
        //    Sfiltersmth = Sfiltersmth * factorSmth + Strafe * (1 - factorSmth);
        //}
        //inputThrottle = Tfiltersmth = Tfiltersmth * 0.95f + inputThrottle * (1 - 0.95f);

        // 摇杆校准
        if (dateCount < 20)
        {
            if (Mathf.Abs(Forward) < 0.2f && Mathf.Abs(Strafe) < 0.2f)
            {
                joyOffsetX += Strafe;
                joyOffsetY += Forward;
                dateCount++;

                if (dateCount == 20)
                {
                    joyOffsetX /= 20f;
                    joyOffsetY /= 20f;
                }
            }
        }
        if (dateCount >= 20)
        {
            //inputForward = Ffiltersmth - joyOffsetY;
            //inputStrafe = Sfiltersmth - joyOffsetX;  
            inputForward = Forward - joyOffsetY;
            inputStrafe = Strafe - joyOffsetX;
        }
        else
        {
            //inputForward = Ffiltersmth;
            //inputStrafe = Sfiltersmth;
            inputForward = Forward;
            inputStrafe = Strafe;
        }

        //死区处理
        if (Mathf.Abs(inputForward) < deadZone) inputForward = 0f;
        if (Mathf.Abs(inputRotate) < YdeadZone) inputRotate = 0f;
        if (Mathf.Abs(inputStrafe) < deadZone) inputStrafe = 0f;
        if (Mathf.Abs(inputThrottle) < TdeadZone) inputThrottle = 0f;
    }

    void UpdateSound()
    {
        if (inputThrottle <= -0.99f)
        {
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
            return;
        }
        if (!audioSource.isPlaying && engineSound != null)
        {
            audioSource.clip = engineSound;
            audioSource.Play();
        }
        if (audioSource.isPlaying)
        {
            float pitch = Mathf.Lerp(minPitch, maxPitch, (inputThrottle + 1) / 2);
            audioSource.pitch = pitch;
        }
    }

    void MoveDrone()
    {
        //  //无姿态变化的倾角运动
        //Vector3 horizenForward = new Vector3(transform.forward.x, 0, transform.forward.z).normalized;
        //Vector3 horizenStrafe = new Vector3(transform.right.x, 0, transform.right.z).normalized;

        //Vector3 moveVelocity = transform.up * inputThrottle * liftSpeed +
        //                      horizenForward * inputForward * speed +
        //                      horizenStrafe * inputStrafe * speed;
        //rb.velocity = moveVelocity;

        // 计算目标速度
        Vector3 moveVelocity = transform.up * inputThrottle * liftSpeed +
                                transform.forward * inputForward * speed +
                                transform.right * inputStrafe * speed;

        //惯性设置

        Vector3 targetHorizontal = new Vector3(moveVelocity.x, 0, moveVelocity.z); 
        Vector3 currentHorizontal = new Vector3(currentVelocity.x, 0, currentVelocity.z);

        bool isInput = Mathf.Abs(inputForward) > 0.01f || Mathf.Abs(inputStrafe) > 0.01f;
        if (isInput)
        {
            float inertia = (targetHorizontal.magnitude > currentHorizontal.magnitude) ? acceleration : deceleration;
            currentHorizontal = Vector3.MoveTowards(currentHorizontal, targetHorizontal, inertia * Time.fixedDeltaTime);
        }
        else//开始滑行
        {
            if (currentHorizontal.magnitude > 0.01f)
            {
                float inertiaDis = deceleration * inertiaCoefficient;
                currentHorizontal = Vector3.MoveTowards(currentHorizontal, Vector3.zero, inertiaDis * Time.fixedDeltaTime);
            }
            else
            {
                currentHorizontal = Vector3.zero;
            }
        }
        currentVelocity = new Vector3(currentHorizontal.x, moveVelocity.y, currentHorizontal.z);
        rb.velocity = currentVelocity;

        // 旋转控制
        if (Mathf.Abs(inputRotate) > deadZone)
        {
            float rotationAmount = inputRotate * rotateSpeed * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0f, rotationAmount, 0f);
            rb.rotation = rb.rotation * deltaRotation;
        }

        // 倾角
        float targetAngleX = inputForward * maxAngle;
        float targetAngleZ = -inputStrafe * maxAngle;

        targetAngle = new Vector3(targetAngleX, 0f, targetAngleZ);
        Vector3 currentEuler = rb.rotation.eulerAngles;
        float smoothTime = tiltSpeed * Time.fixedDeltaTime;
        float newX = Mathf.LerpAngle(currentEuler.x, targetAngle.x, smoothTime);
        float newZ = Mathf.LerpAngle(currentEuler.z, targetAngle.z, smoothTime);

        Quaternion targetRotation = Quaternion.Euler(newX, currentEuler.y, newZ);
        rb.rotation = Quaternion.Slerp(rb.rotation, targetRotation, smoothTime);
    }
}