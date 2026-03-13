using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphView : MonoBehaviour
{
    private IFileService _fileService;

    private FileNode _tree;
    
    public GameObject folderPrefab;
    public GameObject filePrefab;
    
    public Material lineMaterial;
    
    public float radiusStep = 3.0f;
    
    void Start()
    {
        _fileService = ServiceLocator.Global.Get<IFileService>();

        if (_fileService == null) 
        {
            Debug.LogError("FileManager not found in ServiceLocator");
            return;
        }

        _tree = _fileService.GetFileStructure();

        if (_tree != null)
        {
            DebugLogStructure(_tree);
            DrawNode(_tree, 0f, 360f, 0);
        }
        else
            Debug.LogWarning("Empty tree.");
    }
    
    private void DrawNode(FileNode node, float minAngle, float maxAngle, int depth, Vector3 parentPos = default)
    {
        float angle = (minAngle + maxAngle) / 2f;
        
        float radius = depth * radiusStep;

        /*if (depth > 0)
        {
            float offset = (node.Name.GetHashCode() % 2 == 0) ? 0.7f : -0.7f;
            radius += offset;
        }*/

        Vector3 currentPos = PolarToCartesian(angle, radius);

        GameObject prefab = node.IsDirectory ? folderPrefab : filePrefab;
        GameObject obj = Instantiate(prefab, currentPos, Quaternion.identity, transform);
        
        var text = obj.GetComponentInChildren<TMPro.TMP_Text>();
        if (text != null) 
        {
            text.text = node.Name;
            text.transform.rotation = Quaternion.identity; 
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
    
    public void DebugLogStructure(FileNode node, int indent = 0)
    {
        string prefix = new string(' ', indent * 4) + (node.IsDirectory ? "Dir: " : "File: ");
    
        Debug.Log($"{prefix}{node.Name}");

        foreach (var child in node.Children)
        {
            DebugLogStructure(child, indent + 2);
        }
    }
}
