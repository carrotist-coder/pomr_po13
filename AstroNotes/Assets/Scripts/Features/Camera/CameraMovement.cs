using System;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [SerializeField] private Vector3 _target = new Vector3(0,0,-10);

    private float _t;
    private Vector3 _startPosition;
    private bool _isMoving;

    void Update()
    {
        if (!_isMoving) return;

        _t += Time.deltaTime * 2f;

        float easedT = Mathf.SmoothStep(0, 1, _t);

        transform.position = Vector3.Lerp(_startPosition, _target, easedT);

        if (_t >= 1f)
        {
            _isMoving = false;
        }
    }

    public void SetTarget(Vector3 target)
    {
        _startPosition = transform.position;
        _target = new Vector3(target.x, target.y, -10);
        _t = 0;
        _isMoving = true;
    }
}
