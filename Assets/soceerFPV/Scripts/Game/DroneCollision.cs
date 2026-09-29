using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//*****************************************
//创建人：
//功能说明：无人机与球门，球网的碰撞效果的实现
//***************************************** 
public class DroneCollision : MonoBehaviour
{
    [Header("碰撞设置")]
    public string wallTag = "Wall";
    public string pillarTag = "Pillar";
    public string redRingTag = "Rring";
    public string blueRingTag = "Bring";

    [Header("球门设置")]
    public float pushForce = 1f;// 碰撞力大小
    public float shakeTime = 0.5f;// 晃动持续时间
    public float shakePower = 0.1f;// 晃动强度
    public float minSpeed = 1f;// 最小触发速度
    public float returnTime = 0.5f;// 返回原位时间
    public float coolDown = 0.1f;// 碰撞冷却时间

    [Header("球网设置")]
    public float wallBounce = 0.5f;// 反弹系数

    private bool isColliding = false;
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
        if (other.CompareTag(wallTag))
        {
            ApplyNetBounce(col);
           // Debug.Log($"{gameObject.name} 触发球网");
            if (gameObject.CompareTag("Player") && !isColliding)
            {
                isColliding = true;
                WallGlow(other);
                StartCoroutine(ResetFlag());
            }
            return; 
        }
        if (other.CompareTag(pillarTag) && !isColliding)
        {
            isColliding = true;
            StartCoroutine(ResetFlag());
            return;
        }

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
    //球网碰撞效果
    void ApplyNetBounce(Collision col)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) return;
        // if (rb.velocity.magnitude < 1.5f)
        if (gameObject.CompareTag("Player") && col.relativeVelocity.magnitude < 1.5f)
        {
           // Debug.Log("玩家速度不足，不触发反弹");
            return;
        }
        else if (gameObject.CompareTag("Ally") && col.relativeVelocity.magnitude < 0.5f)
        {
           // Debug.Log("友军速度不足，不触发反弹");
            return;
        }
        else if (gameObject.CompareTag("Enemy") && col.relativeVelocity.magnitude < 0.5f)
        {
          //  Debug.Log("敌军速度不足，不触发反弹");
            return;
        }
        // 停止当前所有物理运动
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        // 获取碰撞信息
        Vector3 inDir = col.relativeVelocity.normalized;  // 入射方向
        Vector3 normal = col.contacts[0].normal;          // 碰撞面法线
        // 计算反射方向（标准反射公式）
        Vector3 reflectDir = Vector3.Reflect(inDir, normal).normalized;
        // 安全检查：确保反射方向是朝外的
        if (Vector3.Dot(reflectDir, normal) < 0.1f)
        {
            // 如果反射方向仍朝内，使用法线方向
            reflectDir = normal;
        }
        // 计算反弹距离（基于碰撞速度）
        float baseDistance = 0.5f; // 基础反弹距离
        float speedFactor = Mathf.Clamp(col.relativeVelocity.magnitude * 0.2f, 0.5f, 1f);
        float bounceDistance = baseDistance * speedFactor * wallBounce;

        // 限制最大最小距离
        bounceDistance = Mathf.Clamp(bounceDistance, 0.5f, 1f);

        // Debug.Log($"碰撞反射: 方向={reflectDir}, 距离={bounceDistance}, 速度={col.relativeVelocity.magnitude}");
        // 开始向反射方向位移
        StartCoroutine(BounceMoveWithArc(reflectDir, bounceDistance, col.relativeVelocity.magnitude));
    }

    //反弹效果
    IEnumerator BounceMoveWithArc(Vector3 direction, float distance, float collisionSpeed)
    {
        Vector3 startPos = transform.position;
        // 计算总移动时间
        float moveTime = 0.25f;
        // 添加延迟时间
        float delayTime = 0.15f;
        float totalTime = delayTime + moveTime;
        float elapsed = 0f;
        // 记录延迟阶段的位置
        Vector3 delayPosition = startPos;
        // 计算目标位置
        Vector3 targetPos = startPos + direction * distance;
        // 计算抛物线参数
        float height = distance * 0.05f; // 抛物线高度
        // 计算起始和终点的Y值
        float startY = startPos.y;
        float targetY = targetPos.y;
        while (elapsed < totalTime)
        {
            elapsed += Time.deltaTime;
            if (elapsed < delayTime)
            {
                // 延迟阶段：保持在碰撞点位置
                transform.position = delayPosition;
            }
            else
            {
                // 反弹阶段：正常抛物线运动
                float t = (elapsed - delayTime) / moveTime;
                // 水平位置（线性）
                Vector3 horizontalPos = Vector3.Lerp(startPos, targetPos, t);
                // 垂直位置（抛物线）
                float verticalPos = Mathf.Lerp(startY, targetY, t);
                verticalPos += Mathf.Sin(t * Mathf.PI) * height; // 正弦波实现抛物线

                // 组合位置
                Vector3 finalPos = new Vector3(horizontalPos.x, verticalPos, horizontalPos.z);
                transform.position = finalPos;
            }

            yield return null;
        }

        transform.position = targetPos;
    }
    void WallGlow(GameObject wall)
    {
        if (gameObject.CompareTag("Player"))// 只有玩家触发发光
        {
            StartCoroutine(Glow(wall));
        }
    }

    IEnumerator Glow(GameObject wall)
    {
        Renderer rend = wall.GetComponent<Renderer>();
        if (rend != null)
        {
            Material original = rend.material;
            Material temp = new Material(original);
            rend.material = temp;

            temp.SetColor("_EmissionColor", Color.green);
            temp.SetFloat("_EmissionIntensity", 5f);
            temp.EnableKeyword("_EMISSION");
            yield return new WaitForSeconds(0.5f);
            rend.material = original;
            if (Application.isPlaying)
                Destroy(temp);
            else
                DestroyImmediate(temp);
        }
    }
    IEnumerator ResetFlag()
    {
        yield return new WaitForSeconds(0.01f);
        isColliding = false;
    }
}