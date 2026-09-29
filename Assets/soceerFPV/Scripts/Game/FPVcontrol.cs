using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
//*****************************************
//创建人：
//功能说明：测试联网功能时无法使用手柄，故使用键盘。只在联网时使用，真正控制脚本不应该是它
//***************************************** 
public class FPVcontrol : NetworkBehaviour
{
    /*同一台电脑脚本读取的是同一个全局轴值，所以手柄控制时会同步运动，建议使用两台电脑+手柄测试*/
    //[Header("飞行参数")]
    //public float speed = 6f;
    //public float rotateSpeed = 60f;
    //public float liftSpeed = 3f;



    //[Header("音效设置")]
    //public AudioClip engineSound;
    //public float minPitch = 1f;
    //public float maxPitch = 2f;

    //[Header("摇杆设置")]
    //public float deadZone = 0.1f;
    //public float TdeadZone = 0.1f;
    //public float YdeadZone = 0.2f;
    //private float joyOffsetX;
    //private float joyOffsetY;
    //private int dateCount = 0;

    //private AudioSource audioSource;
    //private Rigidbody rb;

    //private float inputForward;
    //private float inputRotate;
    //private float inputStrafe;
    //private float inputThrottle;

    ////private string debugText = "";
    ////private GUIStyle debugStyle;

    //void Start()
    //{
    //    rb = GetComponent<Rigidbody>();
    //    if (rb == null)
    //    {
    //        rb = gameObject.AddComponent<Rigidbody>();
    //    }
    //    rb.useGravity = false;
    //    rb.angularDrag = 3f;

    //    //debugStyle = new GUIStyle();
    //    //debugStyle.normal.textColor = Color.red;
    //    //debugStyle.fontSize = 50;

    //    audioSource = GetComponent<AudioSource>();
    //    if (audioSource == null)
    //    {
    //        audioSource = gameObject.AddComponent<AudioSource>();
    //        audioSource.loop = true;
    //        audioSource.spatialBlend = 1f;
    //        audioSource.clip = engineSound;
    //    }
    //}

    //void Update()
    //{
    //    if (!isLocalPlayer) return;
    //    GetInput();
    //    UpdateSound();
    //    //UpdateDebug();
    //}

    //void FixedUpdate()
    //{
    //    if (!isLocalPlayer) return;

    //    MoveDrone();
    //}

    //void GetInput()
    //{
    //    float Forward = Input.GetAxis("Vertical");//前后
    //    float Strafe = Input.GetAxis("Horizontal");//左右
    //    inputRotate = Input.GetAxis("YRotation"); //旋转
    //    inputThrottle = Input.GetAxis("ZAxis");//上下

    //    if (dateCount < 20)
    //    {
    //        if (Mathf.Abs(Forward) < 0.2f && Mathf.Abs(Strafe) < 0.2f)
    //        {
    //            joyOffsetX += Strafe;
    //            joyOffsetY += Forward;
    //            dateCount++;

    //            if (dateCount == 20)
    //            {
    //                joyOffsetX /= 20f;
    //                joyOffsetY /= 20f;
    //            }
    //        }
    //    }

    //    if (dateCount >= 20)
    //    {
    //        inputForward = Forward - joyOffsetY;
    //        inputStrafe = Strafe - joyOffsetX;
    //    }
    //    else
    //    {
    //        inputForward = Forward;
    //        inputStrafe = Strafe;
    //    }

    //    // 死区处理
    //    if (Mathf.Abs(inputForward) < deadZone) inputForward = 0f;
    //    if (Mathf.Abs(inputRotate) < YdeadZone) inputRotate = 0f;
    //    if (Mathf.Abs(inputStrafe) < deadZone) inputStrafe = 0f;
    //    if (Mathf.Abs(inputThrottle) < TdeadZone) inputThrottle = 0f;
    //}

    //void UpdateSound()
    //{
    //    if (inputThrottle <= -0.99f)
    //    {
    //        // Debug.Log("1");
    //        if (audioSource.isPlaying)
    //        {
    //            audioSource.Stop();

    //        }
    //        return;
    //    }

    //    if (!audioSource.isPlaying && engineSound != null)
    //    {
    //        audioSource.clip = engineSound;
    //        audioSource.Play();
    //    }

    //    if (audioSource.isPlaying)
    //    {
    //        float pitch = Mathf.Lerp(minPitch, maxPitch, (inputThrottle + 1) / 2);
    //        audioSource.pitch = pitch;
    //    }
    //}

    //void MoveDrone()
    //{
    //    // 前后移动
    //    Vector3 moveVelocity = transform.up * inputThrottle * liftSpeed +
    //                          transform.forward * inputForward * speed +
    //                          transform.right * inputStrafe * speed;

    //    rb.velocity = moveVelocity;
    //    // 左右旋转
    //    if (Mathf.Abs(inputRotate) > deadZone)
    //    {
    //        float rotationAmount = inputRotate * rotateSpeed * Time.fixedDeltaTime;
    //        Quaternion deltaRotation = Quaternion.Euler(0f, rotationAmount, 0f);
    //        rb.rotation = rb.rotation * deltaRotation;
    //    }
    //}

    ////void UpdateDebug()
    ////{
    ////    debugText = $"输入值:\n" +
    ////               $"前后: {inputForward:F2} 旋转: {inputRotate:F2}\n" +
    ////               $"平移: {inputStrafe:F2} 油门: {inputThrottle:F2}\n\n" +
    ////               $"速度: {rb.velocity.magnitude:F1}\n" +
    ////               $"无人机位置:\n" +
    ////               $"X: {transform.position.x:F1} Y: {transform.position.y:F1} Z: {transform.position.z:F1}\n";
    ////}

    ////void OnGUI()
    ////{
    ////    GUI.Label(new Rect(10, 10, 600, 400), debugText, debugStyle);
    ////}



    public float moveSpeed = 5f;
    public float rotateSpeed = 50f;
    public float scrollSpeed = 5f;
    public float minHeight = 0.5f;
    public float maxHeight = 5f;
    public float moveRange = 10f;

    void Update()
    {
        if (!isLocalPlayer) return;
        Move();
    }

    void Move()
    {
        Vector3 moveDir = Vector3.zero;

        //前后移动
        if (Input.GetKey(KeyCode.W))
            moveDir += transform.forward;
        if (Input.GetKey(KeyCode.S))
            moveDir -= transform.forward;

        //左右移动
        if (Input.GetKey(KeyCode.D))
            moveDir += transform.right;
        if (Input.GetKey(KeyCode.A))
            moveDir -= transform.right;

        //左右旋转
        if (Input.GetKey(KeyCode.E))
            transform.Rotate(0, rotateSpeed * Time.deltaTime, 0, Space.World);
        if (Input.GetKey(KeyCode.Q))
            transform.Rotate(0, -rotateSpeed * Time.deltaTime, 0, Space.World);

        // 鼠标滚轮上下移动
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0f)
        {
            transform.position += Vector3.up * scroll * scrollSpeed;
        }

        if (moveDir != Vector3.zero)
        {
            transform.position += moveDir.normalized * moveSpeed * Time.deltaTime;
        }
    }

}