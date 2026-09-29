using UnityEngine;
using UnityEngine.UI;
//*****************************************
//创建人：
//功能说明：实时更新红蓝队得分与游戏时间的控制
//***************************************** 
public class GoalScore : MonoBehaviour
{
    public static GoalScore Instance;

    [Header("UI设置")]
    public Text redScoreText;
    public Text blueScoreText;
    public Text timeText;
    public GameObject replayImg;

    private int redScore = 0;
    private int blueScore = 0;
    private float timeLeft;
    private bool gameEnded = false;

    // 用于通知游戏结束的事件
    public delegate void GameEndEvent();
    public static event GameEndEvent OnGameEnd;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        timeLeft = SinglePlayerContent.SelectedGameTime;//比赛时间的引用
        gameEnded = false;
        Replay.CheckReplayImg += CheckReplayImg;
        UpdateScoreUI();
        UpdateTimeUI();
        if (replayImg != null)
        {
            replayImg.SetActive(false);
        }
    }

    private void Update()
    {
        if (!gameEnded)
        {
            if (timeLeft > 0)
            {
                timeLeft -= Time.deltaTime;
                if (timeLeft < 0) timeLeft = 0;
                UpdateTimeUI();
            }
            else
            {
                timeLeft = 0;
                gameEnded = true;
                UpdateTimeUI();
                GameOver();
            }
        }
    }

    void UpdateTimeUI()
    {
        if (timeText == null) return;
        int minutes = Mathf.FloorToInt(Mathf.Max(timeLeft, 0) / 60f);
        int seconds = Mathf.FloorToInt(Mathf.Max(timeLeft, 0) % 60f);

        timeText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

        if (timeLeft <= 30f && timeLeft > 0)
        {
            timeText.color = Color.red;
        }
    }

    void GameOver()
    {
        //Debug.Log("游戏结束");
        // 触发游戏结束事件
        OnGameEnd?.Invoke();
    }

    public void AddScore(string team, int points)
    {
        if (team == "Red")
        {
            redScore += points;
        }
        else if (team == "Blue")
        {
            blueScore += points;
        }

        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        string playerTeam = GetPlayerTeam();
        if (playerTeam == "Red")
        {
            redScoreText.color = Color.red;
            redScoreText.text = "红队: " + redScore.ToString();
            blueScoreText.color = Color.blue;
            blueScoreText.text = "蓝队: " + blueScore.ToString();
        }
        else if (playerTeam == "Blue")
        {
            redScoreText.color = Color.blue;
            redScoreText.text = "蓝队: " + blueScore.ToString();
            blueScoreText.color = Color.red;
            blueScoreText.text = "红队: " + redScore.ToString();
        }
    }

    string GetPlayerTeam()
    {
        if (SinglePlayerContent.SelectedTeam != null)
        {
            return SinglePlayerContent.SelectedTeam;
        }
        return "Red";
    }

    public bool IsGameEnded()
    {
        return gameEnded;
    }

    void CheckReplayImg(bool isReplaying)
    {
        if (replayImg != null)
        {
            replayImg.SetActive(isReplaying);
        }
    }

    void OnDestroy()
    {
        Replay.CheckReplayImg -= CheckReplayImg;
    }
}