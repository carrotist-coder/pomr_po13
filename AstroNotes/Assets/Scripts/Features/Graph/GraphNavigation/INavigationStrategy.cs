using UnityEngine;

public interface INavigationStrategy
{
    public FileNode FindBestMatch(FileNode currentNode, Vector2 direction);
}
