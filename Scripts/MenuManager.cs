
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{

    public void OnStartButtonClick()//当“开始游戏”按钮被按下后
    {
        SceneManager.LoadScene(1);//开始游戏，进入日记场景
    }

    public void OnExitButtonClick()//当“退出游戏”按钮被按下后
    {
        Application.Quit();//退出游戏
    }
}
