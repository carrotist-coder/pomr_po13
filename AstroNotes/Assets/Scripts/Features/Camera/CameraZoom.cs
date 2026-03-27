using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    [SerializeField] private float _zoomSpeed = Constants.Camera.DefaultZoomSpeed;
    [SerializeField] private float _minSize = Constants.Camera.DefaultMinSize;
    [SerializeField] private float _maxSize = Constants.Camera.DefaultMaxSize;
    
    private Camera _camera;
    
    private void Start()
    {
        _camera = GetComponent<Camera>();
        
        if (!_camera.orthographic)
        {
            Debug.LogWarning("CameraZoom works best with orthographic camera");
        }
    }
    
    private void Update()
    {
        var scrollDelta = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Approximately(scrollDelta, 0))
            return;
            
        var targetSize = _camera.orthographicSize - scrollDelta * _zoomSpeed;
        _camera.orthographicSize = Mathf.Clamp(targetSize, _minSize, _maxSize);
    }
}