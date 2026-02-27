using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class AppBootstrap : MonoBehaviour
{
    void Start()
    {
        GameObject globalServices = new GameObject("GlobalServices");
        DontDestroyOnLoad(globalServices);
        
        globalServices.AddComponent<SceneLoader>();
        
        globalServices.AddComponent<GlobalServiceLocator>();

        ServiceLocator.Global.ShowServices();
        
        SceneLoader.LoadScene(SceneNames.MainScene);
    }
}
