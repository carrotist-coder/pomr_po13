using UnityEngine;

public class RadialLayoutStrategy : IGraphLayoutStrategy
{
    private readonly float _radiusStep;
    
    public RadialLayoutStrategy(float radiusStep)
    {
        _radiusStep = radiusStep;
    }
    
    public Vector3 CalculatePosition(float startAngle, float endAngle, int depth)
    {
        var angle = (startAngle + endAngle) / 2f;
        var radius = depth * _radiusStep;
        
        var rad = angle * Mathf.Deg2Rad;
        
        return new Vector3(
            Mathf.Cos(rad) * radius,
            Mathf.Sin(rad) * radius,
            0
        );
    }
}