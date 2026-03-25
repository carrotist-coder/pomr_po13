using System.Collections.Generic;
using UnityEngine;

public class GraphView : MonoBehaviour
{
    private IFileService _fileService;
    private UdpReciever _udpReciever;

    private FileNode _tree;
    private FileNode _currentNode;

    private Dictionary<FileNode, GameObject> _nodeObjects = new();

    [Header("Prefabs")]
    public GameObject folderPrefab;
    public GameObject filePrefab;
    public Material lineMaterial;

    [Header("Layout")]
    public float radiusStep = 3f;

    [Header("Camera")]
    public Camera mainCamera;

    [Header("Selection")]
    public float selectionThreshold = 0.8f;
    public int requiredStableFrames = 2;

    [Header("Colors")]
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;
    public Color candidateColor = Color.cyan;
    
    [Header("Debug")]
    public bool logDirections = true;

    private FileNode _candidate;
    private FileNode _lastCandidate;
    private int _candidateCounter = 0;

    void Start()
    {
        _fileService = ServiceLocator.Global.Get<IFileService>();
        _udpReciever = FindObjectOfType<UdpReciever>();

        _tree = _fileService.GetFileStructure();
        if (_tree == null) return;

        DrawNode(_tree, 0f, 360f, 0);

        _currentNode = _tree;
        CenterCameraInstant();

        HighlightNode(_currentNode);
        
        LogNodeDirections();
    }

    void Update()
    {
        if (_udpReciever == null || _currentNode == null) return;

        Vector2 input = _udpReciever.CurrentDirection;
        
        if (input == Vector2.zero)
            return;
        
        //Debug.Log($"INPUT vector: {input}");

        if (input.magnitude > 0.2f)
        {
            ProcessDirection(input.normalized);
        }

        if (_udpReciever.ConsumeFist())
        {
            if (_candidate != null)
            {
                Debug.Log($"GO TO: {_candidate.Name}");
                ChangeSelection(_candidate);
                ResetCandidate();
            }
        }
    }
    
    private void LogNodeDirections()
    {
        if (!logDirections || _currentNode == null) return;

        Debug.Log($"===== CURRENT NODE: {_currentNode.Name} =====");

        Vector3 center = mainCamera.transform.position;

        // CHILDRENS
        foreach (var child in _currentNode.Children)
        {
            if (!_nodeObjects.ContainsKey(child)) continue;

            Vector2 dir = GetDirectionFromCamera(child);

            Debug.Log($"CHILD: {child.Name} → {dir}");
        }

        // PARENT
        if (_currentNode.Parent != null && _nodeObjects.ContainsKey(_currentNode.Parent))
        {
            Vector2 dir = GetDirectionFromCamera(_currentNode.Parent);

            Debug.Log($"PARENT: {_currentNode.Parent.Name} → {dir}");
        }

        Debug.Log("=========================================");
    }
    
    private void ProcessDirection(Vector2 fingerDir)
    {
        Debug.Log("PROCESS DIRECTION");
        List<FileNode> candidates = new List<FileNode>(_currentNode.Children);
        if (_currentNode.Parent != null)
            candidates.Add(_currentNode.Parent);

        FileNode best = null;
        float bestDot = -1f;

        foreach (var node in candidates)
        {
            if (!_nodeObjects.ContainsKey(node)) continue;

            Vector2 nodeDir = GetDirectionFromCamera(node);
            float dot = Vector2.Dot(fingerDir, nodeDir);

            //Debug.Log($"NODE: {node.Name} | dir: {nodeDir} | dot: {dot}");

            if (dot > bestDot)
            {
                bestDot = dot;
                best = node;
            }
        }

        if (best != null && bestDot > selectionThreshold)
        {
            if (best == _lastCandidate)
            {
                _candidateCounter++;
            }
            else
            {
                _candidateCounter = 1;
                _lastCandidate = best;
            }

            if (_candidateCounter >= requiredStableFrames)
            {
                if (_candidate != best)
                {
                    SetCandidate(best);
                }
            }
        }
        else
        {
            ResetCandidate();
        }
    }
    private Vector2 GetDirectionFromCamera(FileNode node)
    {
        Vector3 camPos = mainCamera.transform.position;
        Vector3 nodePos = _nodeObjects[node].transform.position;

        Vector3 dir3 = (nodePos - camPos).normalized;

        return new Vector2(dir3.x, dir3.y).normalized;
    }
    
    private void SetCandidate(FileNode node)
    {
        ClearCandidate();

        _candidate = node;
        SetNodeColor(node, candidateColor);

        Debug.Log($"CANDIDATE LOCKED: {node.Name}");
    }

    private void ResetCandidate()
    {
        ClearCandidate();
        _lastCandidate = null;
        _candidateCounter = 0;
    }

    private void ClearCandidate()
    {
        if (_candidate != null && _candidate != _currentNode)
            SetNodeColor(_candidate, normalColor);

        _candidate = null;
    }

    private void ChangeSelection(FileNode node)
    {
        SetNodeVisual(_currentNode, normalColor, 1f);

        _currentNode = node;
        Debug.Log($"{node.Name}");

        SetNodeVisual(_currentNode, selectedColor, 1.3f);

        CenterCameraInstant();
    }

    private void CenterCameraInstant()
    {
        Vector3 pos = _nodeObjects[_currentNode].transform.position;
        mainCamera.transform.position = new Vector3(pos.x, pos.y, -10f);
    }

    private void SetNodeVisual(FileNode node, Color color, float scale)
    {
        if (_nodeObjects.TryGetValue(node, out GameObject obj))
        {
            var r = obj.GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = color;

            obj.transform.localScale = Vector3.one * scale;
        }
    }

    private void SetNodeColor(FileNode node, Color color)
    {
        if (_nodeObjects.TryGetValue(node, out GameObject obj))
        {
            var r = obj.GetComponentInChildren<Renderer>();
            if (r != null) r.material.color = color;
        }
    }

    private void HighlightNode(FileNode node)
    {
        SetNodeVisual(node, selectedColor, 1.3f);
    }

    private void DrawNode(FileNode node, float minAngle, float maxAngle, int depth, Vector3 parentPos = default)
    {
        float angle = (minAngle + maxAngle) / 2f;
        float radius = depth * radiusStep;

        Vector3 pos = PolarToCartesian(angle, radius);

        GameObject prefab = node.IsDirectory ? folderPrefab : filePrefab;
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity, transform);

        _nodeObjects[node] = obj;

        var text = obj.GetComponentInChildren<TMPro.TMP_Text>();
        if (text != null)
            text.text = node.Name;

        if (node.Parent != null)
            DrawLine(parentPos, pos);

        if (node.Children.Count > 0)
        {
            float total = node.GetLeafCount(node);
            float current = minAngle;

            foreach (var child in node.Children)
            {
                float weight = child.GetLeafCount(child);
                float range = (weight / total) * (maxAngle - minAngle);

                DrawNode(child, current, current + range, depth + 1, pos);
                current += range;
            }
        }
    }

    private Vector3 PolarToCartesian(float angle, float radius)
    {
        float rad = angle * Mathf.Deg2Rad;

        return new Vector3(
            Mathf.Cos(rad) * radius,
            Mathf.Sin(rad) * radius,
            0
        );
    }

    private void DrawLine(Vector3 start, Vector3 end)
    {
        GameObject line = new GameObject("Line");
        line.transform.SetParent(transform);

        var lr = line.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.positionCount = 2;
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
}