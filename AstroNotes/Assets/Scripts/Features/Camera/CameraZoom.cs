using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    private Camera _cam;
    
    public float zoomSpeed = 5f;
    public float minSize = 2f;
    public float maxSize = 50f;

    void Start()
    {
        _cam = GetComponent<Camera>();
        
        if (!_cam.orthographic)
            Debug.LogWarning("Set camera to Orthographic");
    }

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (scroll != 0)
        {
            float targetSize = _cam.orthographicSize - scroll * zoomSpeed;

            _cam.orthographicSize = Mathf.Clamp(targetSize, minSize, maxSize);
        }
    }
}
