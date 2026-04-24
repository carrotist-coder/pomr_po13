using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using Unity.VisualScripting;

public class GraphView : MonoBehaviour, IGraphView
{
    [SerializeField] private GraphViewConfig _graphViewConfig;
    [SerializeField] private MarkdownEditorView _editorView;
    [SerializeField] private Camera _mainCamera; 

    private IFileService _fileService;
    private IUdpService _udpService;
    private INavigationStrategy _navigationStrategy;
    private IGraphLayoutStrategy _graphLayoutStrategy;

    private FileNode _rootNode;
    private FileNode _currentNode;
    private FileNode _candidateNode;

    private readonly Dictionary<FileNode, GameObject> _nodeObjects = new();
    private readonly List<LineRenderer> _lines = new();

    private int _stableFramesCount;
    private FileNode _lastCandidate;
    
    #region Initialization

        private void Awake()
        {
            InitializeServices();
            InitializeStrategies();
        }

        private void InitializeServices()
        {
            _fileService = ServiceLocator.Global.Get<IFileService>();
            _udpService = ServiceLocator.Global.Get<IUdpService>();
            
            /*if (_udpService == null)
            {
                _udpService = FindObjectOfType<UdpService>();
                if (_udpService != null)
                    ServiceLocator.Global.Register(_udpService);
            }*/
        }

        private void InitializeStrategies()
        {
            _navigationStrategy = new AngularNavigationStrategy(this, _graphViewConfig.SelectionThreshold);
            //_navigationStrategy = new AngularNavigationStrategy(this, Constants.Input.SelectionDotThreshold);
            _graphLayoutStrategy = new RadialLayoutStrategy(_graphViewConfig.RadiusStep);
            //_graphLayoutStrategy = new RadialLayoutStrategy(Constants.Graph.DefaultRadiusStep);
        }
    
    #endregion

    #region Graph Building
    
        private void Start()
        {
            BuildGraph();

            if (_rootNode != null )
            {
                _currentNode = _rootNode;
                CenterCameraOnNode(_currentNode);
                HighlightNode(_currentNode);
            }

            if (_graphViewConfig.LogDirections)
            {
                LogAllNodeDirections();
            }
        }

        private void BuildGraph()
        {
            _rootNode = _fileService.GetFileStructure();

            if (_rootNode == null)
            {
                Debug.LogError($"Failed to load file structure");
                return;
            }

            DrawNodeRecursive(_rootNode, 0, 360, 0);
        }

        private void DrawNodeRecursive(FileNode node, float startAngle, float endAngle, int depth, Vector3? parentPosition = null)
        {
            Vector2 position = _graphLayoutStrategy.CalculatePosition(startAngle, endAngle, depth);
            GameObject nodeObject = CreateNodeObject(node, position);

            _nodeObjects[node] = nodeObject;

            if (parentPosition.HasValue)
            {
                DrawConnection(parentPosition.Value, position);
            }

            if (node.Children.Count > 0)
            {
                int totalWeight = node.Children.Sum(child => child.GetLeafCount());
                float currentAngle = startAngle;
                
                foreach (var child in node.Children)
                {
                    int weight = child.GetLeafCount();
                    float angleRange = (weight / (float)totalWeight) * (endAngle - startAngle);
                    
                    DrawNodeRecursive(child, currentAngle, currentAngle + angleRange, depth + 1, position);
                    currentAngle += angleRange;
                }
            }
        }

        private GameObject CreateNodeObject(FileNode node, Vector3 position)
        {
            GameObject prefab = node.IsDirectory ? _graphViewConfig.FolderPrefab : _graphViewConfig.FilePrefab;
            GameObject obj = Instantiate(prefab, position, Quaternion.identity, transform);
            
            TMP_Text text = obj.GetComponentInChildren<TMP_Text>();
            if (text != null)
            {
                text.text = node.Name;
            }
            
            return obj;
        }
        
        private void DrawConnection(Vector3 start, Vector3 end)
        {
            var lineObject = new GameObject("Connection");
            lineObject.transform.SetParent(transform);

            var lineRenderer = lineObject.AddComponent<LineRenderer>();
            lineRenderer.material = _graphViewConfig.LineMaterial;
            lineRenderer.startWidth = _graphViewConfig.LineWidth;
            lineRenderer.endWidth = _graphViewConfig.LineWidth;

            lineRenderer.positionCount = 2;

            float zOffset = 1f;
            Vector3 offset = new Vector3(0f, 0f, zOffset);

            lineRenderer.SetPosition(0, start + offset);
            lineRenderer.SetPosition(1, end + offset);

            _lines.Add(lineRenderer);
        }
    
    #endregion
    
    private void Update()
    {
        if (_udpService == null || _currentNode == null) 
            return;
            
        var direction = _udpService.CurrentDirection;

        if (direction == Vector2.zero)
            return;
        
        Debug.Log($"{direction}");
        
        if (direction.magnitude > Constants.Input.DirectionThreshold && direction != Vector2.zero)
        {
            ProcessNavigation(direction.normalized);
        }
        
        if (_udpService.ConsumeFist() && _candidateNode != null)
        {
            NavigateToNode(_candidateNode);
            ResetCandidate();
        }
    }
    
    private void ProcessNavigation(Vector2 direction)
    {
        var bestMatch = _navigationStrategy.FindBestMatch(_currentNode, direction);
        //Debug.Log($"Candidates: {_currentNode.Children.Count + (_currentNode.Parent != null ? 1 : 0)}");
        
        if (bestMatch != null)
        {
            if (bestMatch == _lastCandidate)
            {
                _stableFramesCount++;
            }
            else
            {
                _stableFramesCount = 1;
                _lastCandidate = bestMatch;
            }
            
            if (_stableFramesCount >= _graphViewConfig.RequiredStableFrames && _candidateNode != bestMatch)
            {
                SetCandidate(bestMatch);
            }
        }
        else
        {
            ResetCandidate();
        }
    }
    
    private void SetCandidate(FileNode node)
    {
        ClearCandidate();
        
        _candidateNode = node;
        SetNodeColor(node, _graphViewConfig.CandidateColor);
        
        Debug.Log($"Candidate selected: {node.Name}");
    }
    
    private void ResetCandidate()
    {
        ClearCandidate();
        _lastCandidate = null;
        _stableFramesCount = 0;
    }
    
    private void ClearCandidate()
    {
        if (_candidateNode != null && _candidateNode != _currentNode)
        {
            SetNodeColor(_candidateNode, _graphViewConfig.NormalColor);
        }
        
        _candidateNode = null;
    }
    
    private void NavigateToNode(FileNode node)
    {
        if (_currentNode != null && !_currentNode.IsDirectory)
        {
            _editorView.CloseAndSave();
        }
        
        SetNodeVisual(_currentNode, _graphViewConfig.NormalColor, Constants.Graph.NormalScale);
        
        _currentNode = node;
        
        SetNodeVisual(_currentNode, _graphViewConfig.SelectedColor, Constants.Graph.SelectedScale);
        CenterCameraOnNode(_currentNode);
        
        Debug.Log($"Navigated to: {node.Name}");
        
        if (_graphViewConfig.LogDirections)
        {
            LogNodeDirections(node);
        }
        
        if (!_currentNode.IsDirectory)
        {
            _editorView.OpenFile(_currentNode);
        }
    }
    
    private void CenterCameraOnNode(FileNode node)
    {
        if (!_nodeObjects.TryGetValue(node, out var obj)) 
            return;

        Vector3 position = obj.transform.position;
        
        _mainCamera.GetComponent<CameraMovement>().SetTarget(position);
        //_mainCamera.transform.position = new Vector3(position.x, position.y, Constants.Camera.DefaultZPosition);
    }
    
    private void SetNodeVisual(FileNode node, Color color, float scale)
    {
        if (!_nodeObjects.TryGetValue(node, out var obj)) 
            return;
            
        var renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
        
        obj.transform.localScale = Vector3.one * scale;
    }
    
    public void SetNodeColor(FileNode node, Color color)
    {
        if (!_nodeObjects.TryGetValue(node, out var obj)) 
            return;
            
        var renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }
    }
    
    private void HighlightNode(FileNode node)
    {
        SetNodeVisual(node, _graphViewConfig.SelectedColor, Constants.Graph.SelectedScale);
    }
    
    public Vector2 GetDirectionToNode(FileNode node)
    {
        if (!_nodeObjects.TryGetValue(node, out var obj))
            return Vector2.zero;

        var cameraPos = _mainCamera.transform.position;
        var nodePos = obj.transform.position;
        Vector2 delta = new Vector2(nodePos.x - cameraPos.x, nodePos.y - cameraPos.y);
        return delta.normalized;
    }
    
    public Vector3 GetNodePosition(FileNode node)
    {
        return _nodeObjects.TryGetValue(node, out var obj) ? obj.transform.position : Vector3.zero;
    }
    
    private void LogNodeDirections(FileNode node)
    {
        if (!_graphViewConfig.LogDirections) return;
        
        Debug.Log($"===== CURRENT NODE: {node.Name} =====");
        
        foreach (var child in node.Children)
        {
            if (_nodeObjects.ContainsKey(child))
            {
                var direction = GetDirectionToNode(child);
                Debug.Log($"CHILD: {child.Name} → {direction}");
            }
        }
        
        if (node.Parent != null && _nodeObjects.ContainsKey(node.Parent))
        {
            var direction = GetDirectionToNode(node.Parent);
            Debug.Log($"PARENT: {node.Parent.Name} → {direction}");
        }
        
        Debug.Log("=========================================");
    }
    
    private void LogAllNodeDirections()
    {
        if (_currentNode != null)
        {
            LogNodeDirections(_currentNode);
        }
    }
    
    private void OnDestroy()
    {
        foreach (var line in _lines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }
        
        _lines.Clear();
        _nodeObjects.Clear();
    }
}