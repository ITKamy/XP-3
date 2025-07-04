using UnityEngine;
using UnityEngine.SceneManagement;

public class ChangeScene : MonoBehaviour
{
    public static string scene;
    public static ChangeScene changeScene;
    void Start()
    {
        try
        {
            if (ChangeScene.scene == "")
            {
                ChangeScene.scene = "Start";
                ChangeScene.changeScene = this;
            }
        }
        catch { }
    
    }
    void Update()
    {
        try
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                if (scene == "Start")
                {
                    ChangeScene.ChangeTo("Game");
                }
                else
                {
                    ChangeScene.ChangeTo("Start");
                }
            }
        }
        catch { }
    }     
        
    public static void ChangeTo(string scene)
    {
        ChangeScene.scene = scene;
        SceneManager.LoadScene(scene);
    }
}