using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GraphData : MonoBehaviour
{
    public Dictionary<string, NodePositionData> NodePositions { get; set; } = new();
    public Dictionary<string, List<string>> Connections { get; set; } = new();
}
