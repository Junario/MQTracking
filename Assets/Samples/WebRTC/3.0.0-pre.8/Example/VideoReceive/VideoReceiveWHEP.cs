using UnityEngine;
using Unity.WebRTC;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using System.Text;

namespace Unity.WebRTC.Samples
{
    public class VideoReceiveWHEP : MonoBehaviour
    {
        [SerializeField] private Button callButton;
        [SerializeField] private Button hangUpButton;
        [SerializeField] private RawImage receiveImage;

        private RTCPeerConnection pc;
        private VideoStreamTrack videoTrack;

        void Start()
        {
            callButton.onClick.AddListener(Call);
            hangUpButton.onClick.AddListener(HangUp);
            callButton.interactable = true;
            hangUpButton.interactable = false;

            StartCoroutine(WebRTC.Update());
        }

        private void Call()
        {
            callButton.interactable = false;
            hangUpButton.interactable = true;

            var config = new RTCConfiguration
            {
                iceServers = new[] { new RTCIceServer { urls = new[] { "stun:stun.l.google.com:19302" } } },
                iceTransportPolicy = RTCIceTransportPolicy.All
            };

            pc = new RTCPeerConnection(ref config);
            Debug.Log("Created peer connection");
            pc.OnIceConnectionChange = state => Debug.Log($"ICE Connection State: {state}");
            pc.OnIceCandidate = candidate => Debug.Log($"ICE Candidate: {candidate.Candidate} (SDP: {candidate.Sdp})");
            pc.OnTrack = OnTrack;

            pc.AddTransceiver(TrackKind.Video, new RTCRtpTransceiverInit { direction = RTCRtpTransceiverDirection.RecvOnly });

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
            Debug.Log($"Generated SDP Offer:\n{desc.sdp}");
            if (!desc.sdp.Contains("a=ice-ufrag"))
            {
                Debug.LogError("SDP missing ice-ufrag. Update WebRTC package.");
                yield break;
            }

            var setLocalOp = pc.SetLocalDescription(ref desc);
            yield return setLocalOp;
            if (setLocalOp.IsError)
            {
                Debug.LogError($"SetLocalDescription failed: {setLocalOp.Error}");
                yield break;
            }

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
                    Debug.Log($"Received SDP Answer:\n{answerSdp}");
                    var answerDesc = new RTCSessionDescription { type = RTCSdpType.Answer, sdp = answerSdp };
                    var setRemoteOp = pc.SetRemoteDescription(ref answerDesc);
                    yield return setRemoteOp;
                    if (setRemoteOp.IsError)
                    {
                        Debug.LogError($"SetRemoteDescription failed: {setRemoteOp.Error}");
                        yield break;
                    }
                }
                else
                {
                    Debug.LogError($"Failed to connect: {www.error}");
                }
            }
        }

        private void OnTrack(RTCTrackEvent e)
        {
            if (e.Track is VideoStreamTrack track)
            {
                videoTrack = track;
                Debug.Log("Video track received");
                videoTrack.OnVideoReceived += (Texture texture) =>
                {
                    receiveImage.texture = texture;
                    Debug.Log("Video texture applied to RawImage");
                };
            }
        }

        private void HangUp()
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

            receiveImage.texture = null;
            callButton.interactable = true;
            hangUpButton.interactable = false;
        }

        void OnDestroy()
        {
            HangUp();
        }
    }
}