using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ServiceLocator
{
    private static ServiceLocator _globalInstance;
    private static ServiceLocator _currentInstance;
    
    private readonly Dictionary<string, IService> _services = new();

    public static ServiceLocator Global
    {
        get
        {
            if (_globalInstance == null)
                _globalInstance = new ServiceLocator();
            
            return _globalInstance;
        }
    }

    public static ServiceLocator Current
    {
        get
        {
            if (_currentInstance == null)
                _currentInstance = new ServiceLocator();
            
            return _currentInstance;
        }
    }

    public T Get<T>() where T : IService
    {
        string key = typeof(T).Name;
        
        if (!_services.ContainsKey(key))
        {
            Debug.LogError($"Service not found: {key}");
            throw new InvalidOperationException();
        }

        return (T)_services[key];
    }

    public void Register<T>(T service) where T : IService
    {
        string key = typeof(T).Name;

        if (_services.ContainsKey(key))
        {
            Debug.LogError($"Service already registered: {key}");
            return;
        }
        
        _services.Add(key, service);
    }

    public static void ResetCurrent()
    {
        _currentInstance = null;
    }

    public void ShowServices()
    {
        foreach (var service in _services)
        {
            Debug.Log(service.Key);
        }
    }
}
