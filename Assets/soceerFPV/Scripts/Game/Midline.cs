using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人：
//功能说明：固定中线位置防止位移
//***************************************** 
public class Midline : MonoBehaviour
{
    Vector3 worldPos;
    Quaternion worldRot;
    //球门碰撞时，不允许中线变化
    void Awake()
    {
        worldPos = transform.position;
        worldRot = transform.rotation;
    }

    void LateUpdate()
    {
        transform.position = worldPos;
        transform.rotation = worldRot;
    }
}
