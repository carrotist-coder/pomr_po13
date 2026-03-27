using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AngularNavigationStrategy : INavigationStrategy
{
    private readonly IGraphView _graphView;
    private readonly float _selectionThreshold;
    
    public AngularNavigationStrategy(IGraphView graphView, float selectionThreshold = 0.8f)
    {
        _graphView = graphView;
        _selectionThreshold = selectionThreshold;
    }
    
    public FileNode FindBestMatch(FileNode currentNode, Vector2 direction)
    {
        if (currentNode == null)
        {
            Debug.LogError("CurrentNode is null in FindBestMatch");
            return null;
        }
        
        var candidates = new List<FileNode>();
        
        if (currentNode.Children != null)
        {
            candidates.AddRange(currentNode.Children.Where(child => child != null));
        }
        
        if (currentNode.Parent != null)
        {
            candidates.Add(currentNode.Parent);
        }
        
        if (candidates.Count == 0)
            return null;
        
        FileNode bestMatch = null;
        float bestDot = -1f;
        
        foreach (var node in candidates)
        {
            if (node == null)
            {
                Debug.LogWarning("Null node found in candidates");
                continue;
            }
            
            var nodeDirection = _graphView.GetDirectionToNode(node);
            
            if (nodeDirection == Vector2.zero)
            {
                Debug.LogWarning($"Node {node.Name} returned zero direction");
                continue;
            }
            
            float dot = Vector2.Dot(direction, nodeDirection);
            
            if (dot > bestDot)// && dot > _selectionThreshold)
            {
                bestDot = dot;
                bestMatch = node;
            }
        }
        
        foreach (var node in candidates)
        {
            var nodeDirection = _graphView.GetDirectionToNode(node);
            float dot = Vector2.Dot(direction, nodeDirection);
        
            Debug.Log($"CANDIDATE: {node.Name} | DirToNode: {nodeDirection} | Dot: {dot:F3} | Threshold: {_selectionThreshold}");
        
            if (dot > bestDot && dot > _selectionThreshold)
            {
                bestDot = dot;
                bestMatch = node;
            }
        }
    
        Debug.Log($"Best match: {(bestMatch != null ? bestMatch.Name : "null")} with dot {bestDot:F3}");
        return bestMatch;
    }
}