using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人：
//功能说明：无人机之间的物理碰撞与反弹
//***************************************** 
public class DroneSphereCollision : MonoBehaviour
{
    [Header("球体碰撞设置")]
    public float collisionRadius = 0.12f;
    public float collisionCooldown = 0.01f;
    
    private Rigidbody rb;
    private Dictionary<GameObject, float> collisionTimers = new Dictionary<GameObject, float>();//用于记录碰撞冷却时间

    private Vector3 debugCollisionNormal = Vector3.zero;
    private float debugTimer = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        // 确保有球形碰撞体
        SphereCollider collider = GetComponent<SphereCollider>();
        if (collider == null)
        {
            collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = collisionRadius;
            collider.isTrigger = false;
        }
    }

    void Update()
    {
        // 更新碰撞计时器
        UpdateCollisionTimers();
        
        // 调试显示
        if (debugTimer > 0f)
        {
            debugTimer -= Time.deltaTime;
        }
    }

    void UpdateCollisionTimers()
    {
        // 移除过期的碰撞计时
        List<GameObject> toRemove = new List<GameObject>();
        foreach (var pair in collisionTimers)
        {
            if (Time.time - pair.Value > collisionCooldown)//如果当前时间-碰撞时间大于冷却时间
            {
                toRemove.Add(pair.Key);//加入字典
            }
        }
        foreach (var key in toRemove)
        {
            collisionTimers.Remove(key);//从字典中移出过期记录
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        GameObject other = collision.gameObject;
        
        // 检查是否是无人机之间的碰撞
        if (IsDroneCollision(other) && !IsInCooldown(other))
        {
            HandleDroneCollision(collision);
            collisionTimers[other] = Time.time;
        }
    }

    bool IsDroneCollision(GameObject other)
    {
        return other.CompareTag("Player") || other.CompareTag("Ally") || other.CompareTag("Enemy");
    }

    bool IsInCooldown(GameObject other)
    {
        return collisionTimers.ContainsKey(other) && (Time.time - collisionTimers[other]) < collisionCooldown;
    }

    void HandleDroneCollision(Collision collision)
    {
        GameObject other = collision.gameObject;
        Rigidbody otherRb = other.GetComponent<Rigidbody>();

        if (otherRb == null) return;

        // 1. 计算碰撞法线（圆心连线方向）
        Vector3 collisionNormal = (other.transform.position - transform.position).normalized;
        debugCollisionNormal = collisionNormal;
        debugTimer = 2f;

        // 2. 分解速度向量
        Vector3 myVelocity = rb.velocity;
        Vector3 otherVelocity = otherRb.velocity;

        // 我的速度分解
        float mySpeedAlongNormal = Vector3.Dot(myVelocity, collisionNormal);
        Vector3 myVelocityAlongNormal = collisionNormal * mySpeedAlongNormal;
        Vector3 myTangentVelocity = myVelocity - myVelocityAlongNormal;

        // 对方速度分解
        float otherSpeedAlongNormal = Vector3.Dot(otherVelocity, collisionNormal);
        Vector3 otherVelocityAlongNormal = collisionNormal * otherSpeedAlongNormal;
        Vector3 otherTangentVelocity = otherVelocity - otherVelocityAlongNormal;

        // 3. 根据碰撞角度决定碰撞类型
        float dotProduct = Vector3.Dot(myVelocity.normalized, otherVelocity.normalized);

        if (dotProduct < -0.7f)
        {
            // 正面碰撞取平均值
            Vector3 averageNormalVelocity = (myVelocityAlongNormal + otherVelocityAlongNormal) * 0.5f;
            rb.velocity = averageNormalVelocity + myTangentVelocity;
            otherRb.velocity = averageNormalVelocity + otherTangentVelocity;
           // Debug.Log($"正面碰撞");
        }
        else
        {
            // 侧面碰撞：在法线方向交换部分动量，切向方向保持不变
            Vector3 newMyNormalVelocity = otherVelocityAlongNormal * 0.6f;
            Vector3 newOtherNormalVelocity = myVelocityAlongNormal * 0.6f;

            rb.velocity = newMyNormalVelocity + myTangentVelocity;
            otherRb.velocity = newOtherNormalVelocity + otherTangentVelocity;

            // 侧面碰撞添加轻微的分离力，防止卡在一起
            Vector3 separationForce = -collisionNormal * 0.2f;
            rb.AddForce(separationForce, ForceMode.Impulse);
            otherRb.AddForce(-separationForce, ForceMode.Impulse);
        }
    }
}