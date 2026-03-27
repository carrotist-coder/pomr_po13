using UnityEngine;

public interface IUdpService : IService
{
    Vector2 CurrentDirection { get; }
    bool ConsumeFist();
}
