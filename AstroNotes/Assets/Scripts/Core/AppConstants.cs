public class Constants
{
    public static class Paths
    {
        public const string NoteFolderName = "AstroNotes";
        //public const string GraphFileName = "Graph.json";
        //public const string PythonPathConfigFile = "python_path.txt";
    }
    
    public static class FileExtensions
    {
        public const string Markdown = "*.md";
        public const string Text = "*.txt";
    }
    
    public static class Camera
    {
        public const float DefaultZoomSpeed = 5f;
        public const float DefaultMinSize = 2f;
        public const float DefaultMaxSize = 50f;
        public const float DefaultZPosition = -10f;
    }
    
    public static class Graph
    {
        public const float DefaultRadiusStep = 3f;
        public const float DefaultLineWidth = 0.05f;
        public const float SelectedScale = 1.3f;
        public const float NormalScale = 1f;
    }
    
    public static class Input
    {
        public const float DirectionThreshold = 0.2f;
        public const float SelectionDotThreshold = 0.5f;
        public const int RequiredStableFrames = 1;
    }
    
    public static class UDP
    {
        public const int DefaultPort = 5005;
        public const string FistCommand = "ok";
    }
    
    public static class Colors
    {
        public static readonly UnityEngine.Color Normal = UnityEngine.Color.white;
        public static readonly UnityEngine.Color Selected = UnityEngine.Color.yellow;
        public static readonly UnityEngine.Color Candidate = UnityEngine.Color.cyan;
    }
}
