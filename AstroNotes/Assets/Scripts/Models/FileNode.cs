using System.Collections.Generic;

//DTO for IFileService
public class FileNode
{
    public string Name { get; }
    public string FullPath { get; }
    public bool IsDirectory { get; }

    public FileNode Parent { get; }
    public List<FileNode> Children { get; } = new();

    public FileNode(string name, string fullPath, FileNode parent, bool isDirectory)
    {
        Name = name;
        FullPath = fullPath;
        Parent = parent;
        IsDirectory = isDirectory;
    }
    
    public int GetLeafCount()
    {
        if (Children.Count == 0) 
            return 1;
    
        int count = 0;
        foreach (var child in Children)
        {
            count += child.GetLeafCount();
        }
        
        return count;
    }

    public bool IsRoot => Parent == null;
}
