using UnityEngine;
using System.Collections.Generic;
//*****************************************
//创建人：
//功能说明：球门进球检测与得分逻辑
//***************************************** 
public class GoalTrigger : MonoBehaviour
{
    public int points = 1; 
    public GameObject frontTrigger; 
    public GameObject backTrigger; 
    public GameObject midLineTrigger; 

    private Collider frontCollider; 
    private Collider backCollider; 
    private Collider midLineCollider; 
    private HashSet<Collider> passedFront = new HashSet<Collider>(); //已通过前触发器的无人机集合
    private Dictionary<string, bool> teamCrossedMidLine = new Dictionary<string, bool>(); //记录是否已通过中线
    private Dictionary<Collider, bool> dronesInMidLine = new Dictionary<Collider, bool>(); //记录无人机是否在中线区域内

    void Start()
    {
        SetupTrigger(ref frontCollider, frontTrigger);
        SetupTrigger(ref backCollider, backTrigger);
        SetupTrigger(ref midLineCollider, midLineTrigger);
        //一开始可无视中线要求来进球
        teamCrossedMidLine["Red"] = true;
        teamCrossedMidLine["Blue"] = true;
    }

    void SetupTrigger(ref Collider target, GameObject obj)
    {
        if (obj == null) return; 
        target = obj.GetComponent<Collider>(); 
        if (target != null)
        {
            target.isTrigger = true; 

            if (obj == midLineTrigger)
            {
                var oldListener = obj.GetComponent<MidLineTriggerListener>();
                if (oldListener != null) Destroy(oldListener);
                var listener = obj.AddComponent<MidLineTriggerListener>();
                listener.goalTrigger = this;
            }
        }
    }

    void FixedUpdate()
    {
        CheckFront();
        CheckBack();
    }
    //前检测设置
    void CheckFront()
    {
        var drones = GetCollidersInTrigger(frontCollider); 
        foreach (var drone in drones)
        {
            if (IsValidDrone(drone, true) && !passedFront.Contains(drone))
            {
                passedFront.Add(drone); 
            }
        }
    }

    //后检测
    void CheckBack()
    {
        var drones = GetCollidersInTrigger(backCollider); 
        foreach (var drone in drones)
        {
            if (IsValidDrone(drone, false)) 
            {
                if (passedFront.Contains(drone)) //检查该无人机是否先通过了前触发器
                {
                    string team = GetTeam(drone.tag); 
                    //检查是否已经通过中线，是则得分，移出记录，并重置状态，等待重更新
                    if (teamCrossedMidLine.ContainsKey(team) && teamCrossedMidLine[team])
                    {
                        Score(drone);
                        passedFront.Remove(drone); 
                        teamCrossedMidLine[team] = false; 
                    }
                    else
                    {
                      // Debug.Log("无效得分");
                        passedFront.Remove(drone); 
                    }
                }
            }
        }
    }

    // 处理无人机进入中线区域的事件
    public void OnDroneEnterMidLine(Collider drone)
    {
        if (!IsValidDrone(drone, false)) return; 
        string team = GetTeam(drone.tag); 
        dronesInMidLine[drone] = true;
       // Debug.Log("中线检测");
    }

    // 处理无人机离开中线区域的事件
    public void OnDroneExitMidLine(Collider drone)
    {
        if (!IsValidDrone(drone, false)) return; 
        string team = GetTeam(drone.tag); 
        //只有当无人机曾经进入过中线区域，现在离开时才算"通过"
        if (dronesInMidLine.ContainsKey(drone) && dronesInMidLine[drone])
        {
            teamCrossedMidLine[team] = true; 
           // Debug.Log("中线检测通过");
            dronesInMidLine.Remove(drone); 
        }
    }

    //清理无人机记录
    public void CleanupDrone(Collider drone)
    {
        if (dronesInMidLine.ContainsKey(drone))
        {
            dronesInMidLine.Remove(drone); //从中线记录中移除
        }
        if (passedFront.Contains(drone))
        {
            passedFront.Remove(drone); //从通过记录中移除
        }
    }

    Collider[] GetCollidersInTrigger(Collider trigger)
    {
        if (trigger == null) return new Collider[0]; 
        var bounds = trigger.bounds; 
        //使用OverlapBox检测边界框内的所有碰撞器
        return Physics.OverlapBox(bounds.center, bounds.extents, trigger.transform.rotation);
    }

    // 验证是否是有效的无人机
    bool IsValidDrone(Collider drone, bool isFront)
    {
        if (drone == null || drone.isTrigger) return false;
        if (drone == frontCollider || drone == backCollider || drone == midLineCollider) return false;
        if (!IsDroneTag(drone.tag)) return false;
        // 如果是敌人，检查是否是进攻型
        if (drone.tag == "Enemy")
        {
            var aiMovement1 = drone.GetComponent<AIDroneMovement>();
            var aiMovement2 = drone.GetComponent<Map2AIDroneMovement>();

            if (aiMovement1 != null && aiMovement1.canDefend) //如果找到任意脚本且该敌人是防御型，则不能得分
            {
                return false;
            }
            if (aiMovement2 != null && aiMovement2.canDefend) 
            {
                return false;
            }
        }
        //如果是前触发器，需要额外检查队伍和球门方向是否匹配
        if (isFront)
        {
            string team = GetTeam(drone.tag);
            string goal = gameObject.tag; 
            // 红队只能进Bring球门，蓝队只能进Rring球门
            return (team == "Red" && goal == "Bring") || (team == "Blue" && goal == "Rring");
        }
        return true; 
    }

    // 得分处理
    void Score(Collider drone)
    {
        string goal = gameObject.tag; 
        string team = goal == "Rring" ? "Blue" : "Red"; 

        if (GoalScore.Instance != null)
        {
            GoalScore.Instance.AddScore(team, points); 
        }
    }

    bool IsDroneTag(string tag)
    {
        //return tag == "Player" || tag == "Ally" || tag == "Enemy";
        return tag == "Player" || tag == "Enemy";
    }

    string GetTeam(string droneTag)
    {
        if (droneTag == "Player")
        {
            return SinglePlayerContent.SelectedTeam ?? LanPlayContent.ClientSelectedTeam ?? "Red"; 
        }
        //else if (droneTag == "Ally")
        //{
        //    return SinglePlayerContent.SelectedTeam ?? "Red";
        //}
        else if (droneTag == "Enemy")
        {
            // 敌人标签：与玩家队伍相反
            return SinglePlayerContent.SelectedTeam == "Red" ? "Blue" : "Red";
        }
        return ""; 
    }
}

// 中线触发器监听器类：专门处理中线触发事件
public class MidLineTriggerListener : MonoBehaviour
{
    public GoalTrigger goalTrigger; 
    void OnTriggerEnter(Collider other)
    {
        if (goalTrigger != null)
        {
            goalTrigger.OnDroneEnterMidLine(other); 
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (goalTrigger != null)
        {
            goalTrigger.OnDroneExitMidLine(other); 
        }
    }
}