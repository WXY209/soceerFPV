using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人：
//功能说明：球门碰撞的物理与动画效果
//***************************************** 
public class RingCollision : MonoBehaviour
{
    [Header("碰撞设置")]
    public string redRingTag = "Rring";
    public string blueRingTag = "Bring";

    [Header("球门设置")]
    public float pushForce = 1f;// 碰撞力大小
    public float shakeTime = 0.5f;// 晃动持续时间
    public float shakePower = 0.1f;// 晃动强度
    public float minSpeed = 1f;// 最小触发速度
    public float returnTime = 0.5f;// 返回原位时间
    public float coolDown = 0.1f;// 碰撞冷却时间

    private Dictionary<GameObject, float> timers = new Dictionary<GameObject, float>(); // 冷却计时器

    // 球门状态记录
    private Dictionary<GameObject, Vector3> originPos = new Dictionary<GameObject, Vector3>();    // 原始位置
    private Dictionary<GameObject, Quaternion> originRot = new Dictionary<GameObject, Quaternion>(); // 原始旋转
    private Dictionary<GameObject, Coroutine> returnRoutines = new Dictionary<GameObject, Coroutine>(); // 返回协程
    private Dictionary<GameObject, float> hitTimes = new Dictionary<GameObject, float>(); // 最后碰撞时间

    void Start()
    {
        RecordAllRings();
    }
    // 记录球门的初始位置信息
    void RecordAllRings()
    {
        GameObject[] reds = GameObject.FindGameObjectsWithTag(redRingTag);
        foreach (GameObject ring in reds)
        {
            if (ring != null)
            {
                originPos[ring] = ring.transform.position;
                originRot[ring] = ring.transform.rotation;
            }
        }
        GameObject[] blues = GameObject.FindGameObjectsWithTag(blueRingTag);
        foreach (GameObject ring in blues)
        {
            if (ring != null)
            {
                originPos[ring] = ring.transform.position;
                originRot[ring] = ring.transform.rotation;
            }
        }
    }

    void Update()
    {
        // 清理过期冷却计时器
        List<GameObject> expired = new List<GameObject>();
        foreach (var item in timers)
        {
            if (Time.time - item.Value > coolDown)
            {
                expired.Add(item.Key);
            }
        }
        foreach (var key in expired)
        {
            timers.Remove(key);
        }
        // 检查哪些球门没有恢复
        List<GameObject> toReset = new List<GameObject>();
        foreach (var item in hitTimes)
        {
            GameObject ring = item.Key;
            float lastHit = item.Value;
            if (Time.time - lastHit > 1.5f)
            {
                // 检查球门是否偏离了原始位置（超过0.1个单位）
                if (originPos.ContainsKey(ring))
                {
                    Vector3 current = ring.transform.position;
                    Vector3 original = originPos[ring];

                    if (Vector3.Distance(current, original) > 0.1f)
                    {
                        toReset.Add(ring);
                    }
                }
            }
        }
        // 强制恢复超时的球门
        foreach (GameObject ring in toReset)
        {
            ForceReset(ring);
            hitTimes.Remove(ring);
        }
    }

    void ForceReset(GameObject ring)
    {
        if (!originPos.ContainsKey(ring) || !originRot.ContainsKey(ring)) return;
        Vector3 targetPos = originPos[ring];
        Quaternion targetRot = originRot[ring];
        // 停止可能正在运行的协程
        if (returnRoutines.ContainsKey(ring) && returnRoutines[ring] != null)
        {
            StopCoroutine(returnRoutines[ring]);
        }
        // 立即重置位置和旋转
        ring.transform.position = targetPos;
        ring.transform.rotation = targetRot;
        // 停止刚体运动
        Rigidbody rb = ring.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        // 清理协程引用
        if (returnRoutines.ContainsKey(ring))
        {
            returnRoutines[ring] = null;
        }
    }

    void OnCollisionEnter(Collision col)
    {
        GameObject other = col.gameObject;

        //球门碰撞处理
        if (IsRing(other) && !OnCoolDown(other))
        {
            HandleRing(col, other);
            timers[other] = Time.time;
        }
    }
    // 判断是否为球门
    bool IsRing(GameObject obj)
    {
        return obj.CompareTag(redRingTag) || obj.CompareTag(blueRingTag);
    }
    // 判断是否在冷却中
    bool OnCoolDown(GameObject obj)
    {
        return timers.ContainsKey(obj) && (Time.time - timers[obj]) < coolDown;
    }
    // 处理球门碰撞
    void HandleRing(Collision col, GameObject ring)
    {
        // 检查碰撞速度是否足够
        float speed = col.relativeVelocity.magnitude;
        if (speed < minSpeed)
        {
            return;
        }
        // 获取接触点信息
        Vector3 point = col.contacts[0].point;
        Vector3 normal = col.contacts[0].normal;
        // 球门物理响应
        RingPhysics(ring, point, normal, speed);
    }

    void RingPhysics(GameObject ring, Vector3 hitPoint, Vector3 hitNormal, float speed)
    {
        // 确保球门已记录原始位置
        if (!originPos.ContainsKey(ring))
        {
            originPos[ring] = ring.transform.position;
            originRot[ring] = ring.transform.rotation;
        }
        // 记录碰撞时间
        hitTimes[ring] = Time.time;
        Rigidbody rb = ring.GetComponent<Rigidbody>();

        if (rb == null)
        {
            rb = ring.AddComponent<Rigidbody>();
            // 设置为运动学刚体
            rb.isKinematic = true;
        }

        // 如果已有返回协程在运行，先停止它
        if (returnRoutines.ContainsKey(ring) && returnRoutines[ring] != null)
        {
            StopCoroutine(returnRoutines[ring]);
        }

        // 运动学刚体：手动计算位移和旋转
        KinematicRing(ring, hitPoint, hitNormal, speed);

        // 启动晃动效果
        StartCoroutine(Shake(ring, rb));
    }

    // 运动学刚体处理
    void KinematicRing(GameObject ring, Vector3 hitPoint, Vector3 hitNormal, float speed)
    {
        // 手动计算位移
        Vector3 dir = (hitPoint - ring.transform.position).normalized;
        float power = Mathf.Clamp(speed * pushForce * 0.1f, 0.5f, 3f); // 增加力和范围

        // 应用位移
        Vector3 move = dir * power * 0.1f; // 位移相对较小
        ring.transform.position += move;
        float baseRotation = 5f;
        float extraRotation = Mathf.Clamp(speed * 2f, 0f, 15f);
        float totalRotation = baseRotation + extraRotation;
        totalRotation = Mathf.Clamp(totalRotation, 5f, 15f);

        // 计算旋转轴：垂直于碰撞方向和向上方向
        Vector3 rotationAxis = Vector3.Cross(Vector3.up, dir).normalized;
        // 如果碰撞方向接近垂直，使用其他轴
        if (rotationAxis.magnitude < 0.1f)
        {
            rotationAxis = Vector3.Cross(Vector3.forward, dir).normalized;
        }
        // 根据碰撞点在左侧还是右侧微调旋转方向
        Vector3 localHit = ring.transform.InverseTransformPoint(hitPoint);
        if (localHit.x < 0) // 左侧碰撞,反转旋转方向
        {
            rotationAxis = -rotationAxis;
        }
        // 应用旋转
        Quaternion rot = Quaternion.AngleAxis(totalRotation, rotationAxis);
        ring.transform.rotation *= rot;
    }

    // 晃动效果
    IEnumerator Shake(GameObject ring, Rigidbody rb)
    {
        float time = 0f;

        while (time < shakeTime)
        {
            time += Time.deltaTime;
            float progress = time / shakeTime;
            float damp = 1f - progress; // 随时间衰减

            // 运动学刚体或无刚体：手动添加随机晃动
            Vector3 randOffset = new Vector3(
                Random.Range(-0.02f, 0.02f),
                Random.Range(-0.01f, 0.01f),
                Random.Range(-0.02f, 0.02f)
            ) * shakePower * damp;
            ring.transform.position += randOffset;

            // 添加小幅随机旋转晃动
            Quaternion randRot = Quaternion.Euler(
                Random.Range(-1f, 1f) * shakePower * damp,
                Random.Range(-1f, 1f) * shakePower * damp,
                Random.Range(-1f, 1f) * shakePower * damp
            );
            ring.transform.rotation *= randRot;

            yield return null;
        }
        StartReturn(ring);
    }

    // 开始返回原位
    void StartReturn(GameObject ring)
    {
        // 确保记录了原始位置
        if (!originPos.ContainsKey(ring))
        {
            originPos[ring] = ring.transform.position;
            originRot[ring] = ring.transform.rotation;
        }
        Vector3 targetPos = originPos[ring];
        Quaternion targetRot = originRot[ring];
        // 启动返回协程
        Coroutine routine = StartCoroutine(Return(ring, targetPos, targetRot));
        // 存储协程引用
        returnRoutines[ring] = routine;
    }
    // 返回原位协程
    IEnumerator Return(GameObject ring, Vector3 targetPos, Quaternion targetRot)
    {
        float time = 0f;
        Vector3 startPos = ring.transform.position;
        Quaternion startRot = ring.transform.rotation;
        Rigidbody rb = ring.GetComponent<Rigidbody>();
        bool hasRb = rb != null;

        while (time < returnTime)
        {
            time += Time.deltaTime;
            float t = time / returnTime;
            // 使用缓动函数
            float ease = t * t * (3f - 2f * t); // SmoothStep

            // 运动学或无刚体：直接设置transform
            ring.transform.position = Vector3.Lerp(startPos, targetPos, ease);
            ring.transform.rotation = Quaternion.Slerp(startRot, targetRot, ease);

            yield return null;
        }
        // 确保准确到达目标位置
        ring.transform.position = targetPos;
        ring.transform.rotation = targetRot;
        // 清理协程引用
        if (returnRoutines.ContainsKey(ring))
        {
            returnRoutines[ring] = null;
        }
        // 恢复完成后清除碰撞时间记录
        if (hitTimes.ContainsKey(ring))
        {
            hitTimes.Remove(ring);
        }
    }

}