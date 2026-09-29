using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using UnityEngine.UI;
//*****************************************
//创建人：
//功能说明：局域网游戏内的暂停菜单
//***************************************** 
public class LanSetMenu : MonoBehaviour
{
    public GameObject setCanvas;
    public Button quitButton;

    void Start()
    {
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitNetworkGame);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            ToggleMenu();
    }

    void ToggleMenu()
    {
        if (setCanvas == null) return;
        bool showMenu = !setCanvas.activeSelf;
        setCanvas.SetActive(showMenu);
        Time.timeScale = showMenu ? 0f : 1f;
        AudioListener.pause = showMenu;
    }

    public void QuitNetworkGame()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        if (NetworkClient.isConnected)
        {
            if (NetworkServer.active)
                NetworkManager.singleton.StopHost();
            else
                NetworkManager.singleton.StopClient();
        }
        else
        {
            SceneManager.LoadScene("soceerFPVui");
        }
        setCanvas?.SetActive(false);
    }

    public void ContinueGame()
    {
        setCanvas?.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }
}