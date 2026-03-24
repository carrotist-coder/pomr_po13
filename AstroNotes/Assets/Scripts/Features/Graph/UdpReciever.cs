using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UdpReciever : MonoBehaviour
{
    private UdpClient _udpClient;
    private Thread _receiveThread;

    private Vector2 _lastDirection = Vector2.zero;
    private bool _fistTriggered = false;

    private readonly object _lock = new object();

    public Vector2 CurrentDirection
    {
        get { lock (_lock) return _lastDirection; }
    }

    public bool ConsumeFist()
    {
        lock (_lock)
        {
            if (_fistTriggered)
            {
                _fistTriggered = false;
                return true;
            }
            return false;
        }
    }

    [Header("UDP Settings")]
    public int listenPort = 5005;

    void Start()
    {
        try
        {
            _udpClient = new UdpClient(listenPort);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start UDP client on port {listenPort}: {e.Message}");
            return;
        }

        _receiveThread = new Thread(ReceiveLoop)
        {
            IsBackground = true
        };
        _receiveThread.Start();

        Debug.Log($"UDP Receiver started on port {listenPort}");
    }

    private void ReceiveLoop()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (true)
        {
            try
            {
                byte[] data = _udpClient.Receive(ref remoteEndPoint);

                if (data == null || data.Length == 0)
                    continue;

                string message = Encoding.UTF8.GetString(data).Trim();
                if (string.IsNullOrEmpty(message))
                    continue;

                Debug.Log($"UDP RAW: {message}");

                // FIST
                if (message.Equals("ok", StringComparison.OrdinalIgnoreCase))
                {
                    lock (_lock)
                    {
                        _fistTriggered = true;
                    }
                    Debug.Log("FIST DETECTED");
                    continue;
                }

                // VECTOR
                string[] parts = message.Split(' ');

                if (parts.Length >= 2 &&
                    float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) &&
                    float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                {
                    Vector2 dir = new Vector2(x, y);
                    if (dir.sqrMagnitude > 0f)
                        dir.Normalize();

                    lock (_lock)
                    {
                        _lastDirection = dir;
                    }

                    Debug.Log($"VECTOR RECEIVED: {dir}");
                }
                else
                {
                    Debug.LogWarning($"UDP: Cannot parse message '{message}'");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"UDP Receive Error: {e.Message}");
            }
        }
    }

    private void OnDisable()
    {
        try
        {
            _udpClient?.Close();
            _receiveThread?.Abort();
        }
        catch { }
    }
}