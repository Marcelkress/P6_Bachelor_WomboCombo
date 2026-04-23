using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AsyncSceneLoader : MonoBehaviour
{
    public static AsyncSceneLoader instance;
    public string sceneName = "scene1";

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    void Start()
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }
    public void StartLoadedScene()
    {
        asyncLoad.allowSceneActivation = true;
    }
    private bool sceneReady = false;
    private AsyncOperation asyncLoad;
    private IEnumerator LoadSceneAsync(string sceneName)
    {
         //Begin to load the Scene you specify
        asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        //Don't let the Scene activate until you allow it to
        asyncLoad.allowSceneActivation = false;
       
        //When the load is still in progress, output the Text and progress bar
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
