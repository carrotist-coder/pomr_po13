using UnityEngine;

public interface IGraphView
{
    public Vector2 GetDirectionToNode(FileNode node);
    public void SetNodeColor(FileNode node, Color color);
    public Vector3 GetNodePosition(FileNode node);
}