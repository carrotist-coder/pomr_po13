using System.Collections.Generic;

//DTO for IFileService
public class FileNode
{
    public string Name;
    public string FullPath;
    public bool IsDirectory;
    
    public FileNode Parent;
    public List<FileNode> Children = new();
    
    public FileNode(string name, string fullPath, FileNode parent, bool isDirectory)
    {
        Name = name;
        FullPath = fullPath;
        Parent = parent;
        IsDirectory = isDirectory;
    }
}
