using System.Collections.Generic;

//DTO for IFileService
public class FileNode
{
    public string Name;
    public string FullPath;
    public bool IsDirectory;
    
    public List<FileNode> children = new();
    
    public FileNode(string name, string fullPath, bool isDirectory)
    {
        Name = name;
        FullPath = fullPath;
        IsDirectory = isDirectory;
    }
}
