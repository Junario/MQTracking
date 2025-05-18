using UnityEngine;
using Unity.WebRTC;
using UnityEngine.Networking;
using System.Collections;
using System.Text;

public class WebRTCStream : MonoBehaviour
{
    private RTCPeerConnection pc;
    private VideoStreamTrack videoTrack;
    private Material videoMaterial;

    [SerializeField] private GameObject ovrCameraRig; // OVRCameraRig 참조

    void Start()
    {
        // OVRCameraRig 및 CenterEyeAnchor 설정
        if (ovrCameraRig == null)
        {
            Debug.LogError("OVRCameraRig is not assigned!");
            return;
        }

        var centerEye = ovrCameraRig.transform.Find("TrackingSpace/CenterEyeAnchor");
        if (centerEye == null)
        {
            Debug.LogError("CenterEyeAnchor not found!");
            return;
        }

        // 2D Quad 생성
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        quad.transform.SetParent(centerEye, false);
        quad.transform.localPosition = Vector3.forward * 0.5f; // CenterEye 앞에 위치
        quad.transform.localRotation = Quaternion.identity;
        quad.transform.localScale = new Vector3(1, 0.5625f, 1); // 16:9 비율 (1920x1080 기준)
        videoMaterial = quad.GetComponent<Renderer>().material;
        videoMaterial.shader = Shader.Find("Unlit/Texture");

        // WebRTC 설정 (ICE 서버 명시적 설정)
        var config = new RTCConfiguration
        {
            iceServers = new[]
            {
                new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } }
            },
            iceTransportPolicy = RTCIceTransportPolicy.All // 모든 ICE 후보 허용
        };

        pc = new RTCPeerConnection(ref config);
        pc.OnIceConnectionChange = state => Debug.Log($"ICE Connection State: {state}");
        pc.OnTrack = OnTrack;

        StartCoroutine(CreateOfferAsync());
    }

    private IEnumerator CreateOfferAsync()
    {
        var offerOp = pc.CreateOffer();
        yield return offerOp;
        if (offerOp.IsError)
        {
            Debug.LogError($"CreateOffer failed: {offerOp.Error}");
            yield break;
        }

        var desc = offerOp.Desc;
        Debug.Log("Generated SDP Offer: " + desc.sdp); // SDP 확인용 로그
        if (!desc.sdp.Contains("ice-ufrag"))
        {
            Debug.LogError("SDP missing ice-ufrag, WebRTC configuration may be invalid.");
        }

        var setLocalOp = pc.SetLocalDescription(ref desc);
        yield return setLocalOp;
        if (setLocalOp.IsError)
        {
            Debug.LogError($"SetLocalDescription failed: {setLocalOp.Error}");
            yield break;
        }

        // 원시 문자열로 SDP 전송
        byte[] bodyRaw = Encoding.UTF8.GetBytes(desc.sdp);
        using (var www = new UnityWebRequest("http://172.30.1.58:8889/vr_stream/whep", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/sdp");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string answerSdp = www.downloadHandler.text;
                var answerDesc = new RTCSessionDescription { type = RTCSdpType.Answer, sdp = answerSdp };
                var setRemoteOp = pc.SetRemoteDescription(ref answerDesc);
                yield return setRemoteOp;
                if (setRemoteOp.IsError)
                {
                    Debug.LogError($"SetRemoteDescription failed: {setRemoteOp.Error}");
                    yield break;
                }
                Debug.Log("Remote Answer SDP: " + answerSdp);
            }
            else
            {
                Debug.LogError("Failed to connect: " + www.error);
            }
        }
    }

    private void OnTrack(RTCTrackEvent e)
    {
        if (e.Track is VideoStreamTrack track)
        {
            videoTrack = track;
            Debug.Log("Video track received");

            // 2D 영상 렌더링
            videoTrack.OnVideoReceived += (Texture texture) =>
            {
                videoMaterial.mainTexture = texture;
            };
        }
    }

    void OnDestroy()
    {
        if (videoTrack != null)
        {
            videoTrack.Dispose();
            videoTrack = null;
        }

        if (pc != null)
        {
            pc.Close();
            pc.Dispose();
            pc = null;
        }
    }
}