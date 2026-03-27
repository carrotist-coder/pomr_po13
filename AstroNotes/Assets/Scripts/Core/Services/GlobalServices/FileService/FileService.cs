using System.Collections.Generic;
using System.IO;
using System.Xml;
using UnityEngine;

public class FileService : IFileService
{
    public string NoteFolderPath { get; private set; }

    public FileService()
    {
        InitializeFolder();
    }

    private void InitializeFolder()
    {
        string roamingPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        NoteFolderPath = Path.Combine(roamingPath, Constants.Paths.NoteFolderName);
        
        if (!Directory.Exists(NoteFolderPath))
            Directory.CreateDirectory(NoteFolderPath);
        
        Debug.Log($"Note Folder initialized at: {NoteFolderPath}");
    }

    public FileNode GetFileStructure()
    {
        if (!Directory.Exists(NoteFolderPath))
            return null;
        
        DirectoryInfo rootInfo = new DirectoryInfo(NoteFolderPath);
        FileNode rootNode = new FileNode(rootInfo.Name, rootInfo.FullName, null, true);

        BuildFileTree(rootNode);

        return rootNode;
    }

    private void BuildFileTree(FileNode parentNode)
    {
        foreach (string directory in Directory.GetDirectories(parentNode.FullPath))
        {
            DirectoryInfo dirInfo = new DirectoryInfo(directory);
            FileNode childDir = new FileNode(dirInfo.Name, dirInfo.FullName, parentNode, true);
            parentNode.Children.Add(childDir);
            
            BuildFileTree(childDir);
        }

        foreach (string extension in new[] {Constants.FileExtensions.Markdown, Constants.FileExtensions.Text})
        {
            foreach (string file in Directory.GetFiles(parentNode.FullPath, extension))
            {
                FileInfo fileInfo = new FileInfo(file);
                FileNode fileNode = new FileNode(fileInfo.Name, fileInfo.FullName, parentNode, false);
                parentNode.Children.Add(fileNode);
            }
        }
    }
}
