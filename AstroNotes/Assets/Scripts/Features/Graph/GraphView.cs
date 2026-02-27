using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphView : MonoBehaviour
{
    private IFileService _fileService;

    private FileNode _tree;
    void Start()
    {
        _fileService = ServiceLocator.Global.Get<IFileService>();

        if (_fileService == null) 
        {
            Debug.LogError("FileManager not found in ServiceLocator");
            return;
        }

        _tree = _fileService.GetFileStructure();
    
        if (_tree != null) 
            DebugLogStructure(_tree);
        else
            Debug.LogWarning("Empty tree.");
    }
    
    public void DebugLogStructure(FileNode node, int indent = 0)
    {
        string prefix = new string(' ', indent * 4) + (node.IsDirectory ? "Dir: " : "File: ");
    
        Debug.Log($"{prefix}{node.Name}");

        foreach (var child in node.Children)
        {
            DebugLogStructure(child, indent + 2);
        }
    }
}
