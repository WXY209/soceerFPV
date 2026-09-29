using UnityEngine;
//*****************************************
//创建人：
//功能说明：控制摄像机视角运动
//***************************************** 
public class CameraControl : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotateSpeed = 50f;
    public float scrollSpeed = 5f;
    public float minHeight = 0.5f;
    public float maxHeight = 5f; 
    public float moveRange = 10f; 

    void Update()
    {
        Move();
        LimitPosition();
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
        //鼠标滚轮上下移动
        //float scroll = Input.GetAxis("Mouse ScrollWheel");
        //if (scroll != 0f)
        //{
        //    transform.position += Vector3.up * scroll * scrollSpeed;
        //}

        if (moveDir != Vector3.zero)
        {
            transform.position += moveDir.normalized * moveSpeed * Time.deltaTime;
        }
    }

    void LimitPosition()
    {
        Vector3 limitPos = transform.position;

        // 限制高度范围
        if (limitPos.y < minHeight)
        {
            limitPos.y = minHeight;
        }
        else if (limitPos.y > maxHeight)
        {
            limitPos.y = maxHeight;
        }

        // 限制水平移动范围
        Vector2 horizontalPos = new Vector2(limitPos.x, limitPos.z);
        if (horizontalPos.magnitude > moveRange)
        {
            horizontalPos = horizontalPos.normalized * moveRange;
            limitPos.x = horizontalPos.x;
            limitPos.z = horizontalPos.y;
        }

        transform.position = limitPos;
    }
}