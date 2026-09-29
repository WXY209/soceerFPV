using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
//*****************************************
//创建人：
//功能说明：AI无人机在第二地图的行为与攻防逻辑
//***************************************** 
public class Map2AIDroneMovement : MonoBehaviour
{
    [Header("飞行参数")]
    public float speed = 5f;
    public float rotateSpeed = 0.01f;
    public float liftSpeed = 1f;

    [Header("AI行为")]
    public float changeTime = 3f; //目标点更新间隔
    public float attackTime = 10f; //进攻间隔
    public bool canDefend = false;
    public float defendRange = 3f;
    public float patrolRadius = 1f;

    private Transform player;
    private Transform defendTarget;  //防御球门
    private Transform attTarget; //进攻球门
    private bool isDefending = false;
    private Vector3 savedTarget;
    private Vector3 defensePosition; //友军防御位置
    private bool enemyInAttackRange = false;

    private static List<Map2AIDroneMovement> defendingDrones = new List<Map2AIDroneMovement>();

    private Vector3[] areaPoints = new Vector3[8];//飞行区域8个顶点
    private Bounds areaBounds;//飞行区域边界框
    private Rigidbody rb;
    private Vector3 moveDir;
    private float timer;
    private float attackTimer;
    private Vector3 areaCenter;//飞行区域中心
    private bool isFlying = true;
    private Vector3 target;//当前目标点

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.useGravity = false;
        rb.drag = 2f;
        rb.angularDrag = 3f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;

        SetupArea();
        areaCenter = areaBounds.center;//计算区域中心
        SetNewTarget();//设置初始目标

        player = GameObject.FindGameObjectWithTag("Player").transform;
        SetupDefense();
    }
    // 初始化防御系统：只有敌军才启用
    void SetupDefense()
    {
        if (gameObject.CompareTag("Enemy"))
        {
            if (defendingDrones.Count < 2)
            {
                canDefend = true;
                defendingDrones.Add(this); //将当前对象加入拦截列表
                SetTargets();
            }
            else//剩余的设置为进攻
            {
                canDefend = false;
                SetTargets();
            }
        }
        else if (gameObject.CompareTag("Ally"))
        {
            canDefend = true; // 友军也启用防御功能
            SetTargets();
            GenerateDefensePosition(); // 生成初始防御位置
        }
        else
        {
            canDefend = false;
        }
    }
    // 根据玩家阵营设置防御目标球门和进攻球门
    void SetTargets()
    {
        string playerTeam = SinglePlayerContent.SelectedTeam;

        if (playerTeam == "Red")
        {
            // 玩家红方，敌军防蓝，进攻红
            GameObject defGoal = GameObject.FindGameObjectWithTag("Bring");
            GameObject attGoal = GameObject.FindGameObjectWithTag("Rring");
            if (defGoal != null) defendTarget = defGoal.transform;
            if (attGoal != null) attTarget = attGoal.transform;
        }
        else
        {
            GameObject defGoal = GameObject.FindGameObjectWithTag("Rring");
            GameObject attGoal = GameObject.FindGameObjectWithTag("Bring");
            if (defGoal != null) defendTarget = defGoal.transform;
            if (attGoal != null) attTarget = attGoal.transform;
        }
    }

    //友军防御球门
    void GenerateDefensePosition()
    {
        if (attTarget == null) return;
        Vector3 randomOffset = new Vector3(0f, 0f, 0.2f);
        defensePosition = attTarget.position + randomOffset;
    }

    //设置飞行区域
    void SetupArea()
    {
        areaPoints[0] = new Vector3(0.5f, 0.8f, -2f);
        areaPoints[1] = new Vector3(0.5f, 0.8f, 5.5f);
        areaPoints[2] = new Vector3(0.5f, 4f, -2f);
        areaPoints[3] = new Vector3(0.5f, 4f, 5.5f);
        areaPoints[4] = new Vector3(-2.8f, 0.8f, -2f);
        areaPoints[5] = new Vector3(-2.8f, 0.8f, 5.5f);
        areaPoints[6] = new Vector3(-2.8f, 4f, -2f);
        areaPoints[7] = new Vector3(-2.8f, 4f, 5.5f);
        //计算最小和最大边界
        Vector3 min = areaPoints[0];
        Vector3 max = areaPoints[0];

        foreach (Vector3 point in areaPoints)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        areaBounds = new Bounds();
        areaBounds.SetMinMax(min, max);//设置边界框
    }

    void Update()
    {
        timer += Time.deltaTime;
        // Debug.Log($"当前速度: {rb.velocity.magnitude:F2}");
        //进攻的敌军
        if (!canDefend && attTarget != null && gameObject.CompareTag("Enemy"))
        {
            attackTimer += Time.deltaTime;
            if (attackTimer >= attackTime)
            {
                AttackGoal();
                attackTimer = 0f;
            }
        }

        //友军进行防御的行为
        if (isDefending && gameObject.CompareTag("Ally"))
        {
            if (timer >= changeTime)
            {
                GenerateDefensePosition();
                target = defensePosition;
                timer = 0f;
            }
        }
        else if (!isDefending && timer >= changeTime)
        {
            SetNewTarget();
            timer = 0f;
        }
        CalcMoveDir();
        CheckBounds();
        if (canDefend && player != null)
        {
            if (gameObject.CompareTag("Enemy") && defendTarget != null)
            {
                CheckDefense();
            }
            else if (gameObject.CompareTag("Ally"))
            {
                CheckAllyDefense();
            }
        }
    }

    // 进攻球门
    void AttackGoal()
    {
        if (attTarget == null || isDefending) return;
        Vector3 fixedOffset = new Vector3(0f, 0.2f, -0.2f);
        target = attTarget.position + fixedOffset; //目标为球门位置后

        Invoke("ResumeRandomMovement", 5f);
    }
    //恢复随机运动
    void ResumeRandomMovement()
    {
        if (!isDefending)
        {
            SetNewTarget();
            timer = 0f;
        }
    }

    // 检查友军是否需要防御：检查是否有敌军在进攻范围内
    void CheckAllyDefense()
    {
        bool enemyFound = CheckForAttackingEnemy();
        if (enemyFound && !isDefending)
        {
            StartDefense();
        }
        else if (!enemyFound && isDefending)
        {
            StopDefense();
        }
        if (isDefending)
        {
            target = defensePosition;
        }
    }

    // 检查是否有敌军在进攻范围内
    bool CheckForAttackingEnemy()
    {
        if (attTarget == null) return false;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in enemies)
        {
            AIDroneMovement enemyDrone = enemyObj.GetComponent<AIDroneMovement>();
            if (enemyDrone != null && !enemyDrone.canDefend) // 只针对进攻型敌军
            {
                // 检查敌军是否在进攻范围内（距离我方球门）
                float distanceToGoal = Vector3.Distance(enemyObj.transform.position, attTarget.position);

                if (distanceToGoal < defendRange * 0.5f)
                {
                    if (!enemyInAttackRange)
                    {
                        enemyInAttackRange = true;
                    }
                    return true;
                }
            }
        }

        if (enemyInAttackRange)
        {
            enemyInAttackRange = false;
        }
        return false;
    }

    //检查是否需要防御：玩家接近球门时触发
    void CheckDefense()
    {
        float dist = Vector3.Distance(player.position, defendTarget.position);
        if (dist < defendRange && !isDefending)
        {
            StartDefense();
        }
        else if (dist > defendRange && isDefending)
        {
            StopDefense();
        }
        if (isDefending)
        {
            target = GetInterceptPos();
        }
    }
    // 开始防御：保存当前目标，设置拦截点
    void StartDefense()
    {
        isDefending = true;
        savedTarget = target;

        if (gameObject.CompareTag("Ally"))
        {
            GenerateDefensePosition();
            target = defensePosition;
        }
        else if (gameObject.CompareTag("Enemy"))
        {
            target = GetInterceptPos();
        }
    }

    void StopDefense()
    {
        isDefending = false;
        target = savedTarget;
    }
    private bool isCloseToPlayer = false; //否靠近玩家
    Vector3 GetInterceptPos()
    {
        if (player == null || defendTarget == null) return transform.position;
        Vector3 fixedOffset = new Vector3(0f, 0f, 0.2f);
        if (!isCloseToPlayer)
        {
            Vector3 pos = player.position + fixedOffset; //拦截点偏移量位置
            return pos;
        }
        else
        {
            //撞击玩家直接设置为玩家位置
            Vector3 pos = player.position + fixedOffset;
            return pos;
        }
    }
    void FixedUpdate()
    {
        if (!isFlying) return;
        Move();
    }
    //设置新的随机目标点
    void SetNewTarget()
    {
        target = new Vector3(
            Random.Range(areaBounds.min.x, areaBounds.max.x),
            Random.Range(areaBounds.min.y, areaBounds.max.y),
            Random.Range(areaBounds.min.z, areaBounds.max.z)
        );
        // Debug.Log($"[{gameObject.name}] 设置新目标点: {target}");
    }
    // 计算到目标点的方向
    void CalcMoveDir()
    {
        Vector3 toTarget = target - transform.position;
        if (toTarget.magnitude > 0.1f)
        {
            moveDir = toTarget.normalized;//标准化方向
        }
    }
    //物理移动：升降、水平移动、旋转
    void Move()
    {
        //升降控制：根据高度差调整
        float heightDiff = target.y - transform.position.y;
        float lift = Mathf.Clamp(heightDiff, -1f, 1f) * liftSpeed;
        rb.AddForce(Vector3.up * lift, ForceMode.Force);
        //水平移动
        Vector3 horForce = new Vector3(moveDir.x, 0f, moveDir.z) * speed;
        rb.AddForce(horForce, ForceMode.Force);
        //旋转朝向移动方向
        if (moveDir.magnitude > 0.1f)
        {
            Vector3 horDir = new Vector3(moveDir.x, 0f, moveDir.z);
            if (horDir.magnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(horDir);
                rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRot, rotateSpeed * Time.fixedDeltaTime));
            }
        }
    }
    //检查是否超出飞行边界
    void CheckBounds()
    {
        Vector3 pos = transform.position;
        //如果超出边界，向中心移动
        if (!areaBounds.Contains(pos))
        {
            Vector3 toCenter = areaBounds.center - pos;
            toCenter.y = 0f;

            if (toCenter.magnitude > 0.1f)
            {
                moveDir = toCenter.normalized;
            }

            if (!isDefending)
            {
                SetNewTarget();
            }
        }
        if (!isDefending)
        {
            float dist = Vector3.Distance(transform.position, target);
            if (dist < 1.5f && timer > changeTime * 0.3f)
            {
                SetNewTarget();
                timer = 0f;
            }
        }
        // 防御状态下接近目标时，提前更换防御位置
        else if (isDefending && gameObject.CompareTag("Ally"))
        {
            float dist = Vector3.Distance(transform.position, target);
            if (dist < 0.5f && timer > changeTime * 0.3f)
            {
                GenerateDefensePosition();
                target = defensePosition;
                timer = 0f;
            }
        }
    }

    void OnDestroy()
    {
        if (defendingDrones.Contains(this))
        {
            defendingDrones.Remove(this);
        }
    }
}
