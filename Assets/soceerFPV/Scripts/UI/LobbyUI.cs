using Mirror;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using Mirror.Examples.BenchmarkIdle;
using static Unity.Burst.Intrinsics.X86.Avx;
using Telepathy;
//*****************************************
//创建人：
//功能说明：局域网游戏界面的ip设置与ui设置
//***************************************** 
public class LobbyUI : MonoBehaviour
{
    public Button hostButton;
    public Button joinButton;
    public InputField ipAddressInputField;
    public Text IPText;


    public GameObject playerPrefab;

    void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        joinButton.onClick.AddListener(StartClient);
        string localIP = GetLocalIPAddress();
        ipAddressInputField.text = localIP;

        if (IPText != null) IPText.text = "本地IP: " + localIP;

        // 重置静态变量，避免旧数据影响
        ResetTeamSelections();
    }

    private void ResetTeamSelections()
    {
        // 确保每次启动时都有默认值
        LanPlayContent.SelectedTeam = "Red";
        LanPlayContent.ClientSelectedTeam = "Red";
    }

    public void StartHost()
    {
       //地图选择
        string targetScene = LanPlayContent.SelectedMapScene;
        // Debug.Log("目标场景: " + targetScene);
        //设置最大联机人数
        NetworkManager.singleton.maxConnections = LanPlayContent.MaxPlayers;
        Debug.Log($"设置服务器最大连接数为: {LanPlayContent.MaxPlayers}");
        // 使用通用预制体
        NetworkManager.singleton.playerPrefab = playerPrefab;

        // 开始主机
        NetworkManager.singleton.StartHost();
        // 延迟一点切换场景
        StartCoroutine(SwitchSceneAfterDelay(targetScene, 0.1f));

    }

    private IEnumerator SwitchSceneAfterDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!string.IsNullOrEmpty(sceneName))
        {
           // Debug.Log("正在切换到场景: " + sceneName);
            NetworkManager.singleton.ServerChangeScene(sceneName);
        }
    }

    public void StartClient()
    {
        if (string.IsNullOrEmpty(ipAddressInputField.text))
        {
           // Debug.LogError("IP地址不能为空！");
            return;
        }

       // Debug.Log("客户端阵营选择: " + LanPlayContent.ClientSelectedTeam);
      //  Debug.Log("正在连接到: " + ipAddressInputField.text);

        // 使用通用预制体
        NetworkManager.singleton.playerPrefab = playerPrefab;
        NetworkManager.singleton.networkAddress = ipAddressInputField.text;
        NetworkManager.singleton.StartClient();
    }

    string GetLocalIPAddress()
    {
        try //通过连接google的外部DNS服务器获取本地IP
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }
        catch//如果上面失败，遍历本机所有IP地址
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)//只取IPv4的地址
                {
                    return ip.ToString();
                }
            }
            return "无法获取IP";
        }
    }
}