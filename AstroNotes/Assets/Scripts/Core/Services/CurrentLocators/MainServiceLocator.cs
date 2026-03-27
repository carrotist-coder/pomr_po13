using UnityEngine;

public class MainServiceLocator : MonoBehaviour
{
    [SerializeField] private GraphViewConfig _graphViewConfig;
    
    private void Awake()
    {
        var graphView = FindObjectOfType<GraphView>();
        
        if (graphView != null && _graphViewConfig != null)
        {
            var graphViewConfigField = typeof(GraphView).GetField("_graphViewConfig", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (graphViewConfigField != null)
            {
                graphViewConfigField.SetValue(graphView, _graphViewConfig);
            }
        }
    }
    
    private void OnDestroy()
    {
        ServiceLocator.ResetCurrent();
    }
}