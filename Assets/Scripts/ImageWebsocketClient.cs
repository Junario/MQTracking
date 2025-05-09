using WebSocketSharp;
using UnityEngine;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;

public class ImageWebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    public string serverUrl = "ws://172.30.1.10:9091";

    private float reconnectInterval = 5f;
    private float reconnectTimer = 0f;

    public event Action<byte[]> OnImageReceived;

    private ConcurrentQueue<byte[]> imageDataQueue = new ConcurrentQueue<byte[]>();

    void Start()
    {
        if (ws == null)
        {
            ws = new WebSocket(serverUrl);
            Debug.Log($"[ImageWebSocketClient] WebSocket initialized with URL: {serverUrl}");
        }
        ws.Connect();

        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("[ImageWebSocketClient] Connection opened");
        };

        ws.OnMessage += (sender, e) =>
        {
            Debug.Log($"[ImageWebSocketClient] Raw message received: {e.Data}");
            ProcessMessage(e.Data);
        };

        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"[ImageWebSocketClient] Error: {e.Message}");
        };

        ws.OnClose += (sender, e) =>
        {
            Debug.Log($"[ImageWebSocketClient] Connection closed: {e.Reason}");
        };
    }

    void Update()
    {
        if (ws == null) return;

        if (!ws.IsAlive)
        {
            reconnectTimer += Time.deltaTime;
            if (reconnectTimer >= reconnectInterval)
            {
                Debug.Log("[ImageWebSocketClient] Attempting to reconnect...");
                ws.Connect();
                reconnectTimer = 0f;
            }
        }

        // 큐에서 데이터를 꺼내 메인 스레드에서 처리
        while (imageDataQueue.TryDequeue(out byte[] imageData))
        {
            Debug.Log("[ImageWebSocketClient] Dequeued JPEG image, invoking OnImageReceived...");
            OnImageReceived?.Invoke(imageData);
        }
    }

    private void ProcessMessage(string message)
    {
        try
        {
            try
            {
                var json = JsonConvert.DeserializeObject<ImageMessage>(message);
                if (json != null && json.type == "rgb_image" && !string.IsNullOrEmpty(json.data))
                {
                    Debug.Log($"[ImageWebSocketClient] Parsed JSON message: type={json.type}, timestamp={json.timestamp}, width={json.width}, height={json.height}");
                    Debug.Log($"[ImageWebSocketClient] Base64 data (first 50 chars): {json.data.Substring(0, Mathf.Min(50, json.data.Length))}...");

                    byte[] imageData = Convert.FromBase64String(json.data);
                    imageDataQueue.Enqueue(imageData);
                    Debug.Log($"[ImageWebSocketClient] JPEG image enqueued, size: {imageData.Length} bytes");
                    return;
                }
            }
            catch (JsonException)
            {
                if (!string.IsNullOrEmpty(message) && IsBase64String(message))
                {
                    Debug.Log("[ImageWebSocketClient] Received raw base64 string, processing as JPEG...");
                    Debug.Log($"[ImageWebSocketClient] Base64 data (first 50 chars): {message.Substring(0, Mathf.Min(50, message.Length))}...");

                    byte[] imageData = Convert.FromBase64String(message);
                    imageDataQueue.Enqueue(imageData);
                    Debug.Log($"[ImageWebSocketClient] JPEG image enqueued, size: {imageData.Length} bytes");
                    return;
                }
            }

            Debug.LogWarning($"[ImageWebSocketClient] Message does not match expected format: {message.Substring(0, Mathf.Min(50, message.Length))}...");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ImageWebSocketClient] Failed to process message: {e.Message}");
        }
    }

    private bool IsBase64String(string str)
    {
        if (string.IsNullOrEmpty(str) || str.Length % 4 != 0) return false;
        try
        {
            Convert.FromBase64String(str);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    void OnDestroy()
    {
        if (ws != null && ws.IsAlive)
        {
            ws.Close();
            Debug.Log("[ImageWebSocketClient] Connection closed on destroy");
        }
    }

    [Serializable]
    private class ImageMessage
    {
        public string type;
        public string data;
        public double timestamp;
        public int width;
        public int height;
        public string frame_id;
    }
}