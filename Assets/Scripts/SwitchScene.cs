using UnityEngine;
using UnityEngine.SceneManagement;

public class SwitchScene : MonoBehaviour
{
    public void LoadScene()
    {
        SceneManager.LoadScene("PlayScene");
    }
}