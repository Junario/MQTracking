using WebSocketSharp;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

public class WebSocketClient : MonoBehaviour
{
    private WebSocket ws;
    public string serverUrl = "ws://172.30.1.10:9090";
    [SerializeField] private HandTracker handTracker;
    [SerializeField] private BodyTracking.BodyTracker bodyTracker;

    private float sendInterval = 0.5f; // 0.5초 간격 전송
    private float sendTimer = 0f;
    private float reconnectInterval = 5f; // 5초 간격 재연결 시도
    private float reconnectTimer = 0f;

    void Start()
    {
        // WebSocket 초기화
        if (ws == null)
        {
            ws = new WebSocket(serverUrl);
            Debug.Log($"[WebSocketClient] WebSocket initialized with URL: {serverUrl}");
        }
        ws.Connect();

        // WebSocket 이벤트 핸들러
        ws.OnOpen += (sender, e) =>
        {
            Debug.Log("[WebSocket] Connection opened");
        };
        ws.OnMessage += (sender, e) =>
        {
            Debug.Log($"[WebSocket] Message received: {e.Data}");
        };
        ws.OnError += (sender, e) =>
        {
            Debug.LogError($"[WebSocket] Error: {e.Message}");
        };
        ws.OnClose += (sender, e) =>
        {
            Debug.Log($"[WebSocket] Connection closed: {e.Reason}");
        };

        // handTracker와 bodyTracker 자동 할당 시도
        if (handTracker == null)
        {
            handTracker = FindAnyObjectByType<HandTracker>();
            if (handTracker == null)
            {
                Debug.LogError("[WebSocketClient] HandTracker component not found in scene. Please assign it in the Inspector.");
            }
            else
            {
                Debug.Log("[WebSocketClient] HandTracker component automatically assigned.");
            }
        }

        if (bodyTracker == null)
        {
            bodyTracker = FindAnyObjectByType<BodyTracking.BodyTracker>();
            if (bodyTracker == null)
            {
                Debug.LogError("[WebSocketClient] BodyTracker component not found in scene. Please assign it in the Inspector.");
            }
            else
            {
                Debug.Log("[WebSocketClient] BodyTracker component automatically assigned.");
            }
        }
    }

    void Update()
    {
        if (ws == null || (handTracker == null && bodyTracker == null))
        {
            Debug.LogWarning($"[WebSocketClient] Missing components - HandTracker: {(handTracker == null ? "null" : "assigned")}, BodyTracker: {(bodyTracker == null ? "null" : "assigned")}");
            return;
        }

        // WebSocket 연결 상태 점검 및 재연결
        if (!ws.IsAlive)
        {
            Debug.Log($"[WebSocket] Connection is not alive. IsAlive: {ws.IsAlive}, State: {ws.ReadyState}");
            reconnectTimer += Time.deltaTime;
            if (reconnectTimer >= reconnectInterval)
            {
                Debug.Log("[WebSocket] Attempting to reconnect...");
                ws.Connect();
                reconnectTimer = 0f;
            }
            return;
        }

        sendTimer += Time.deltaTime;
        if (sendTimer < sendInterval) return;

        // 데이터 준비
        object[] leftHandData = null;
        object[] rightHandData = null;
        object[] bodyData = null;

        if (handTracker != null && handTracker.gameObject.activeInHierarchy)
        {
            leftHandData = handTracker.LeftHandPositions.Select(j => new
            {
                name = j.name,
                x = j.position.x,
                y = j.position.y,
                z = j.position.z
            }).ToArray();

            rightHandData = handTracker.RightHandPositions.Select(j => new
            {
                name = j.name,
                x = j.position.x,
                y = j.position.y,
                z = j.position.z
            }).ToArray();

            Debug.Log($"[WebSocketClient] HandTracker active. LeftHandData count: {leftHandData.Length}, RightHandData count: {rightHandData.Length}");
        }

        if (bodyTracker != null && bodyTracker.gameObject.activeInHierarchy)
        {
            bodyData = bodyTracker.BodyPositions
                .Where(j => j.name == "HEAD" || j.name == "HIPS")
                .Select(j => new
                {
                    name = j.name,
                    x = j.position.x,
                    y = j.position.y,
                    z = j.position.z
                }).ToArray();

            Debug.Log($"[WebSocketClient] BodyTracker active. BodyData count: {bodyData.Length}");
        }

        // JSON 생성
        var jsonData = new
        {
            Left_Hand = leftHandData,
            Right_Hand = rightHandData,
            Body = bodyData
        };
        string jsonString = JsonConvert.SerializeObject(jsonData);
        Debug.Log($"[WebSocketClient] JSON to send: {jsonString}");

        // 데이터 전송
        try
        {
            ws.Send(jsonString);
            Debug.Log("[WebSocketClient] Data sent successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[WebSocketClient] Failed to send data: {e.Message}");
        }

        sendTimer = 0f;
    }

    void OnDestroy()
    {
        if (ws != null)
        {
            if (ws.IsAlive)
            {
                ws.Close();
                Debug.Log("[WebSocket] Connection closed on destroy");
            }
            ws = null;
        }
    }
}