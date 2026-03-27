using UnityEngine;

public interface IGraphLayoutStrategy
{
    public Vector3 CalculatePosition(float startAngle, float endAngle, int depth);
}
