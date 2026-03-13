using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalServiceLocator : MonoBehaviour
{
    private IFileService _fileManager;
    private SceneLoader _sceneLoader;

    private void Awake()
    {
        _fileManager = new FileManager();
        
        _sceneLoader = GetComponent<SceneLoader>();
        
        RegisterServices();
    }

    private void RegisterServices()
    {
        ServiceLocator.Global.Register<IFileService>(_fileManager);
        ServiceLocator.Global.Register<SceneLoader>(_sceneLoader);
    }
}
