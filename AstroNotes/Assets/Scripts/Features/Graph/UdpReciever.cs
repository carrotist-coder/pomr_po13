using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UdpReciever : MonoBehaviour
{
    private UdpClient _udpClient;
    private Thread _receiveThread;
    private Vector2 _lastDirection;
    private readonly object _lock = new object();

    public Vector2 CurrentDirection 
    {
        get { lock(_lock) return _lastDirection; }
    }

    void Start()
    {
        _udpClient = new UdpClient(5005);
        _receiveThread = new Thread(ReceiveData);
        _receiveThread.IsBackground = true;
        _receiveThread.Start();
    }

    private void ReceiveData()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            try
            {
                byte[] data = _udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);
                
                string[] parts = message.Split(new char[] { ',', ';' });
                if (parts.Length >= 2)
                {
                    float x = float.Parse(parts[0]);
                    float y = float.Parse(parts[1]);
                    lock(_lock)
                    {
                        _lastDirection = new Vector2(x, y).normalized;
                    }
                }
            }
            catch (Exception e) { Debug.LogError(e.Message); }
        }
    }

    private void OnDisable()
    {
        _udpClient?.Close();
        _receiveThread?.Abort();
    }
}