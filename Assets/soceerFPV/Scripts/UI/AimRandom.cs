using UnityEngine;
//*****************************************
//创建人：
//功能说明：校准与定轴时准心的随机移动动画效果
//***************************************** 
public class AimRandom : MonoBehaviour
{
    public RectTransform[] items;
    public float radius = 120f;
    public float speed = 100f;

    private Vector2[] starts;
    private Vector2[] targets;
    private Transform[] grandParents; //存储每个item的父物体的父物体

    void Start()
    {
        grandParents = new Transform[items.Length];
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null)
            {
                Transform parent = items[i].transform.parent;
                if (parent != null && parent.parent != null)
                {
                    grandParents[i] = parent.parent;
                }
            }
        }

        Init();
    }

    void Init()
    {
        starts = new Vector2[items.Length];
        targets = new Vector2[items.Length];

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].gameObject.activeSelf)
            {
                starts[i] = items[i].anchoredPosition;
                NewTarget(i);
            }
        }
    }

    void Update()
    {
        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] == null || !items[i].gameObject.activeSelf)
                continue;

            // 检查该item的父物体的父物体是否激活
            bool canMove = true;
            if (grandParents[i] != null)
            {
                canMove = grandParents[i].gameObject.activeInHierarchy;
            }

            if (!canMove)
            {
                items[i].anchoredPosition = starts[i];
                continue;
            }

            // 正常运动逻辑
            RectTransform item = items[i];
            Vector2 pos = item.anchoredPosition;
            Vector2 target = targets[i];

            Vector2 newPos = Vector2.MoveTowards(pos, target, speed * Time.deltaTime);
            item.anchoredPosition = newPos;

            if (Vector2.Distance(newPos, target) < 0.1f)
            {
                NewTarget(i);
            }
        }
    }

    void NewTarget(int index)
    {
        if (grandParents[index] != null && !grandParents[index].gameObject.activeInHierarchy)
            return;

        Vector2 offset = Random.insideUnitCircle * radius;
        targets[index] = starts[index] + offset;
    }
}