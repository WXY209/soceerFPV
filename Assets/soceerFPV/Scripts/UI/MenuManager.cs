using UnityEngine;
//*****************************************
//创建人：
//功能说明：ui场景中主菜单和子菜单的切换管理
//***************************************** 
public class MenuManager : MonoBehaviour
{
    [Header("一级菜单")]
    [SerializeField] private SubMenuAnimator singlePlayerSubMenu;
    [SerializeField] private SubMenuAnimator lanSubMenu;
    [SerializeField] private SubMenuAnimator optionMenu;        // 统一的设置菜单
    [SerializeField] private SubMenuAnimator droneSettingsSubMenu;
    [SerializeField] private SubMenuAnimator helpSubMenu;

    private SubMenuAnimator currentOpenMenu;

    // 打开一级菜单的方法
    public void OpenSinglePlayerSubMenu()
    {
        ToggleMenu(singlePlayerSubMenu);
    }

    public void OpenLanSubMenu()
    {
        ToggleMenu(lanSubMenu);
    }

    public void OpenOptionMenu()
    {
        ToggleMenu(optionMenu);
    }

    public void OpenDroneSettingsSubMenu()
    {
        ToggleMenu(droneSettingsSubMenu);
    }

    public void OpenHelpSubMenu()
    {
        ToggleMenu(helpSubMenu);
    }

    // 返回主菜单
    public void ReturnToMainMenu()
    {
        CloseAllMenus();
    }

    private void ToggleMenu(SubMenuAnimator menuToToggle)
    {
        if (currentOpenMenu == menuToToggle && menuToToggle != null)
        {
            menuToToggle.ToggleMenu();
            currentOpenMenu = null;
        }
        else
        {
            if (currentOpenMenu != null)
            {
                currentOpenMenu.ToggleMenu();
            }
            if (menuToToggle != null)
            {
                menuToToggle.ToggleMenu();
                currentOpenMenu = menuToToggle;
            }
        }
    }

    // 关闭所有菜单
    public void CloseAllMenus()
    {
        if (currentOpenMenu != null)
        {
            currentOpenMenu.ToggleMenu();
            currentOpenMenu = null;
        }
    }

    // 退出游戏
    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}