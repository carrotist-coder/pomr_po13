using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class FileManager : IFileService
{
    public string NoteFolder { get; private set; }

    public FileManager()
    {
        string roamingPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        NoteFolder = Path.Combine(roamingPath, "AstroNotes");
        
        if (!Directory.Exists(NoteFolder))
            Directory.CreateDirectory(NoteFolder);
        
        Debug.Log($"{NoteFolder}");
    }

    public FileNode GetFileStructure()
    {
        if (!Directory.Exists(NoteFolder))
            return null;

        /*
         TODO: make json for graph save
        if (!File.Exists(Path.Combine(NoteFolder, "Graph.json")))
            ScanAllDirectories();
        */
        
        DirectoryInfo rootInfo = new DirectoryInfo(NoteFolder);
        FileNode rootNode = new FileNode(rootInfo.Name, rootInfo.FullName, null, true);

        FillNodes(rootNode);
        
        return rootNode;
    }

    private void FillNodes(FileNode parentNode)
    {
        string[] directories = Directory.GetDirectories(parentNode.FullPath);

        foreach (var directory in directories)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(directory);
            FileNode childDir = new FileNode(dirInfo.Name, dirInfo.FullName, parentNode, true);
            parentNode.Children.Add(childDir);
            
            FillNodes(childDir);
        }
        
        string[] files = Directory.GetFiles(parentNode.FullPath, "*.md");

        foreach (var file in files)
        {
            FileInfo info = new FileInfo(file);
            FileNode fileNode = new FileNode(info.Name, info.FullName, parentNode, false);
            parentNode.Children.Add(fileNode);
        }
    }
}
