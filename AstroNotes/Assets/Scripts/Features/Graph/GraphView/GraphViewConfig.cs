using UnityEngine;

[CreateAssetMenu(fileName = "GraphViewConfig", menuName = "AstroNotes/GraphViewConfig")]
public class GraphViewConfig : ScriptableObject
{
    [Header("Prefabs")]
    public GameObject FolderPrefab;
    public GameObject FilePrefab;
    public Material LineMaterial;
    
    [Header("Layout")]
    public float RadiusStep = Constants.Graph.DefaultRadiusStep;
    public float LineWidth = Constants.Graph.DefaultLineWidth;
    
    [Header("Camera")]
    public float CameraSmoothSpeed = 5f;
    public float CameraOffsetStrength = 2f;
    public float CameraReturnSpeed = 3f;
    
    [Header("Navigation")]
    public float SelectionThreshold = Constants.Input.SelectionDotThreshold;
    public int RequiredStableFrames = Constants.Input.RequiredStableFrames;
    
    [Header("Colors")]
    public Color NormalColor = Constants.Colors.Normal;
    public Color SelectedColor = Constants.Colors.Selected;
    public Color CandidateColor = Constants.Colors.Candidate;
    
    [Header("Debug")]
    public bool LogDirections = true;
}