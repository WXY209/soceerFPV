using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] public Transform target;//要追随的目标
    [SerializeField] Vector3 offset;//偏移量
    [SerializeField] float transitionSpeed = 2;//过渡的速度
    private void LateUpdate()
    {
        if (target!=null)
        {
            transform.position = Vector3.Lerp(transform.position,target.position+offset,transitionSpeed*Time.deltaTime);
        }
    }
}
