using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonUI : MonoBehaviour
{
    public void NewGameScene()
    {
        SceneManager.LoadScene("Scene_Game");
    }
}
