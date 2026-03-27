using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalServiceLocator : MonoBehaviour
{
    private IFileService _fileService;
    private SceneLoader _sceneLoader;

    private void Awake()
    {
        _fileService = new FileService();
        
        _sceneLoader = GetComponent<SceneLoader>();
        
        RegisterServices();
    }

    private void RegisterServices()
    {
        ServiceLocator.Global.Register<IFileService>(_fileService);
        ServiceLocator.Global.Register<SceneLoader>(_sceneLoader);
    }
}
