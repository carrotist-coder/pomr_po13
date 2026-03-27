using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UdpService : MonoBehaviour, IUdpService
{
    [SerializeField] private int _listenPort = Constants.UDP.DefaultPort;
    
    private UdpClient _udpClient;
    private Thread _receiveThread;
    private bool _isRunning;
    
    private Vector2 _currentDirection = Vector2.zero;
    private bool _fistTriggered;
    
    private readonly object _lockObject = new();
    
    public Vector2 CurrentDirection
    {
        get
        {
            lock (_lockObject)
            {
                return _currentDirection;
            }
        }
    }
    
    private void Start()
    {
        InitializeUdpReceiver();
    }
    
    private void InitializeUdpReceiver()
    {
        try
        {
            _udpClient = new UdpClient(_listenPort);
            _isRunning = true;
            
            _receiveThread = new Thread(ReceiveLoop)
            {
                IsBackground = true
            };
            _receiveThread.Start();
            
            Debug.Log($"UDP service started on port {_listenPort}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start UDP service: {e.Message}");
        }
    }
    
    private void ReceiveLoop()
    {
        var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        
        while (_isRunning)
        {
            try
            {
                var data = _udpClient.Receive(ref remoteEndPoint);
                var message = Encoding.UTF8.GetString(data).Trim();
                
                if (string.IsNullOrEmpty(message))
                    continue;
                    
                ProcessMessage(message);
            }
            catch (Exception e)
            {
                if (_isRunning)
                {
                    Debug.LogError($"UDP receive error: {e.Message}");
                }
            }
        }
    }
    
    private void ProcessMessage(string message)
    {
        Debug.Log($"UDP RAW: {message}");
        
        // Fist detection
        if (message.Equals(Constants.UDP.FistCommand, StringComparison.OrdinalIgnoreCase))
        {
            lock (_lockObject)
            {
                _fistTriggered = true;
            }
            Debug.Log("Fist gesture detected");
            return;
        }
        
        // Direction vector
        var parts = message.Split(' ');
        
        if (parts.Length >= 2 &&
            float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
            float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
        {
            var direction = new Vector2(x, y);
            
            if (direction.sqrMagnitude > 0)
            {
                direction.Normalize();
            }
            
            lock (_lockObject)
            {
                _currentDirection = direction;
            }
            
            Debug.Log($"Direction received: {direction}");
        }
    }
    
    public bool ConsumeFist()
    {
        lock (_lockObject)
        {
            if (_fistTriggered)
            {
                _fistTriggered = false;
                return true;
            }
            return false;
        }
    }
    
    public void Dispose()
    {
        _isRunning = false;
        
        try
        {
            _udpClient?.Close();
            _receiveThread?.Abort();
        }
        catch { }
    }
    
    private void OnDestroy()
    {
        Dispose();
    }
}