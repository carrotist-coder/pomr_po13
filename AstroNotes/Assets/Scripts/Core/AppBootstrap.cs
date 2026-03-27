using UnityEngine;

public class AppBootstrap : MonoBehaviour
{
    [SerializeField] private GraphViewConfig _graphViewConfig;
    
    private void Start()
    {
        InitializeGlobalServices();
        LoadMainScene();
    }
    
    private void InitializeGlobalServices()
    {
        var globalServicesObject = new GameObject("GlobalServices");
        DontDestroyOnLoad(globalServicesObject);
        
        var sceneLoader = globalServicesObject.AddComponent<SceneLoader>();
        
        var fileService = new FileService();
        ServiceLocator.Global.Register<IFileService>(fileService);
        
        var udpService = globalServicesObject.AddComponent<UdpService>();
        ServiceLocator.Global.Register<IUdpService>(udpService);
        
        ServiceLocator.Global.Register(sceneLoader);
        
        Debug.Log("Global services initialized");
    }
    
    private void LoadMainScene()
    {
        SceneLoader.LoadScene(SceneNames.MainScene);
    }
}