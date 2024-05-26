using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadPlayfabClientHome : MonoBehaviour
{
    public string sceneName = "Home_PlayfabClientDemo";
    public void LoadHomeScene()
    {
        SceneManager.LoadScene(sceneName);
    }
}
