using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class GraphView : MonoBehaviour
{
    private IFileService _fileService;
    private UdpReciever _udpReciever;

    private FileNode _tree;
    private FileNode _currentNode;
    private Dictionary<FileNode, GameObject> _nodeObjects = new Dictionary<FileNode, GameObject>();
    
    public GameObject folderPrefab;
    public GameObject filePrefab;
    public Material lineMaterial;
    public float radiusStep = 3.0f;

    [Header("Selection Settings")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    [Range(0.5f, 1f)]
    public float selectionThreshold = 0.8f;

    void Start()
    {
        _fileService = ServiceLocator.Global.Get<IFileService>();
        _udpReciever = FindObjectOfType<UdpReciever>();

        if (_fileService == null) 
        {
            Debug.LogError("FileManager not found in ServiceLocator");
            return;
        }

        _tree = _fileService.GetFileStructure();

        if (_tree != null)
        {
            _currentNode = _tree;
            DrawNode(_tree, 0f, 360f, 0);
            HighlightNode(_currentNode);
        }
    }

    void Update()
    {
        if (_udpReciever == null || _currentNode == null) return;

        Vector2 inputDir = _udpReciever.CurrentDirection;

        if (inputDir.sqrMagnitude > 0.01f)
        {
            CheckNavigation(inputDir);
        }
    }

    private void CheckNavigation(Vector2 fingerDir)
    {
        FileNode bestMatch = null;
        float maxDot = -1f;

        List<FileNode> candidates = new List<FileNode>(_currentNode.Children);
        if (_currentNode.Parent != null) candidates.Add(_currentNode.Parent);

        foreach (var candidate in candidates)
        {
            if (!_nodeObjects.ContainsKey(candidate)) continue;

            Vector3 worldDir = _nodeObjects[candidate].transform.position - _nodeObjects[_currentNode].transform.position;
            Vector2 directionToCandidate = new Vector2(worldDir.x, worldDir.y).normalized;

            float dot = Vector2.Dot(fingerDir, directionToCandidate);

            if (dot > maxDot)
            {
                maxDot = dot;
                bestMatch = candidate;
            }
        }

        if (bestMatch != null && maxDot > selectionThreshold)
        {
            if (bestMatch != _currentNode)
            {
                ChangeSelection(bestMatch);
            }
        }
    }

    private void ChangeSelection(FileNode newNode)
    {
        SetNodeVisualState(_currentNode, normalColor, 1.0f);
        
        _currentNode = newNode;
        
        SetNodeVisualState(_currentNode, selectedColor, 1.2f);
        
        Debug.Log($"Selected: {_currentNode.Name}");
    }

    private void SetNodeVisualState(FileNode node, Color color, float scale)
    {
        if (_nodeObjects.TryGetValue(node, out GameObject obj))
        {
            var renderer = obj.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.material.color = color;
            
            obj.transform.localScale = Vector3.one * scale;
        }
    }

    private void DrawNode(FileNode node, float minAngle, float maxAngle, int depth, Vector3 parentPos = default)
    {
        float angle = (minAngle + maxAngle) / 2f;
        float radius = depth * radiusStep;
        Vector3 currentPos = PolarToCartesian(angle, radius);

        GameObject prefab = node.IsDirectory ? folderPrefab : filePrefab;
        GameObject obj = Instantiate(prefab, currentPos, Quaternion.identity, transform);
        
        _nodeObjects[node] = obj;

        var text = obj.GetComponentInChildren<TMPro.TMP_Text>();
        if (text != null) 
        {
            text.text = node.Name;
        }

        if (node.Parent != null)
            DrawLine(parentPos, currentPos);

        if (node.Children.Count > 0)
        {
            float totalLeaves = node.GetLeafCount(node);
            float currentMinAngle = minAngle;

            for (int i = 0; i < node.Children.Count; i++)
            {
                FileNode child = node.Children[i];
                float childWeight = child.GetLeafCount(child);
                float childAngleRange = (childWeight / totalLeaves) * (maxAngle - minAngle);
                float currentMaxAngle = currentMinAngle + childAngleRange;
            
                DrawNode(child, currentMinAngle, currentMaxAngle, depth + 1, currentPos);
                currentMinAngle = currentMaxAngle;
            }
        }
    }

    private void HighlightNode(FileNode node) => SetNodeVisualState(node, selectedColor, 1.2f);

    private Vector3 PolarToCartesian(float angle, float radius)
    {
        float radians = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(radians) * radius, Mathf.Sin(radians) * radius, 0);
    }

    private void DrawLine(Vector3 start, Vector3 end)
    {
        GameObject lineObj = new GameObject("Line");
        lineObj.transform.SetParent(transform);
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
        lr.sortingOrder = -1;
    }
}