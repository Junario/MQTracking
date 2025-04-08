using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using System.Collections.Generic;

public class HandTracker : MonoBehaviour
{
    private XRHandSubsystem handSubsystem;

    // 위치 출력용 관절 (6개)
    private readonly XRHandJointID[] trackedJoints = new XRHandJointID[]
    {
        XRHandJointID.ThumbMetacarpal,  // 엄지 기저부
        XRHandJointID.ThumbProximal,    // 엄지 중간 관절
        XRHandJointID.IndexProximal,    // 검지 근위지골
        XRHandJointID.MiddleProximal,   // 중지 근위지골
        XRHandJointID.RingProximal,     // 약지 근위지골
        XRHandJointID.LittleProximal    // 소지 근위지골
    };

    // 시각화용 관절 (Wrist, Proximal, ThumbMetacarpal, Tips)
    private readonly XRHandJointID[] visualizedJoints = new XRHandJointID[]
    {
        XRHandJointID.Wrist,            // 손목
        XRHandJointID.ThumbMetacarpal,  // 엄지 기저부
        XRHandJointID.ThumbProximal,    // 엄지 중간 관절
        XRHandJointID.ThumbTip,         // 엄지 끝
        XRHandJointID.IndexProximal,    // 검지 근위지골
        XRHandJointID.IndexTip,         // 검지 끝
        XRHandJointID.MiddleProximal,   // 중지 근위지골
        XRHandJointID.MiddleTip,        // 중지 끝
        XRHandJointID.RingProximal,     // 약지 근위지골
        XRHandJointID.RingTip,          // 약지 끝
        XRHandJointID.LittleProximal,   // 소지 근위지골
        XRHandJointID.LittleTip         // 소지 끝
    };

    // 관절 시각화를 위한 오브젝트 저장
    private Dictionary<XRHandJointID, GameObject> leftHandJoints = new Dictionary<XRHandJointID, GameObject>();
    private Dictionary<XRHandJointID, GameObject> rightHandJoints = new Dictionary<XRHandJointID, GameObject>();
    private Dictionary<XRHandJointID, LineRenderer> leftHandLines = new Dictionary<XRHandJointID, LineRenderer>();
    private Dictionary<XRHandJointID, LineRenderer> rightHandLines = new Dictionary<XRHandJointID, LineRenderer>();

    // 시각화 설정 (Inspector에서 조정 가능)
    [SerializeField] private bool drawMeshes = true; // 스피어 표시 여부
    [SerializeField] private float jointSphereSize = 0.02f;
    [SerializeField] private Color sphereColor = Color.red; // 스피어 색상
    [SerializeField] private bool drawLines = true; // 선 표시 여부
    [SerializeField] private float lineWidth = 0.005f;
    [SerializeField] private Color lineColor = Color.blue; // 선 색상

    void Start()
    {
        // XR Hands 서브시스템 초기화
        var subsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetInstances(subsystems);
        Debug.Log($"Found {subsystems.Count} XRHandSubsystem instances.");

        if (subsystems.Count > 0)
        {
            handSubsystem = subsystems[0];
            Debug.Log("Hand Subsystem Initialized: " + (handSubsystem != null));
        }
        else
        {
            Debug.LogWarning("No XR Hand Subsystem found! Check OpenXR settings and XR Hands package.");
        }

        // 시각화 오브젝트 초기화
        InitializeHandVisuals();
    }

    void InitializeHandVisuals()
    {
        // 왼손과 오른손 그룹 생성
        GameObject leftHandGroup = new GameObject("LeftHand");
        leftHandGroup.transform.SetParent(transform, false);
        GameObject rightHandGroup = new GameObject("RightHand");
        rightHandGroup.transform.SetParent(transform, false);

        foreach (XRHandJointID jointID in visualizedJoints)
        {
            if (jointID == XRHandJointID.Invalid) continue;

            // 왼손 관절 스피어
            GameObject leftSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            leftSphere.transform.localScale = Vector3.one * jointSphereSize;
            leftSphere.name = $"Left_{jointID}";
            leftSphere.SetActive(drawMeshes);
            Renderer leftRenderer = leftSphere.GetComponent<Renderer>();
            if (leftRenderer != null)
            {
                leftRenderer.material.color = sphereColor; // 스피어 색상 설정
            }
            leftSphere.transform.SetParent(leftHandGroup.transform, false);
            leftHandJoints[jointID] = leftSphere;

            // 왼손 관절 선
            LineRenderer leftLine = leftSphere.AddComponent<LineRenderer>();
            leftLine.positionCount = 2;
            leftLine.startWidth = lineWidth;
            leftLine.endWidth = lineWidth;
            leftLine.enabled = drawLines;
            leftLine.material = new Material(Shader.Find("Sprites/Default")); // 기본 셰이더 사용
            leftLine.startColor = lineColor; // 선 색상 설정
            leftLine.endColor = lineColor;
            leftHandLines[jointID] = leftLine;

            // 오른손 관절 스피어
            GameObject rightSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            rightSphere.transform.localScale = Vector3.one * jointSphereSize;
            rightSphere.name = $"Right_{jointID}";
            rightSphere.SetActive(drawMeshes);
            Renderer rightRenderer = rightSphere.GetComponent<Renderer>();
            if (rightRenderer != null)
            {
                rightRenderer.material.color = sphereColor; // 스피어 색상 설정
            }
            rightSphere.transform.SetParent(rightHandGroup.transform, false);
            rightHandJoints[jointID] = rightSphere;

            // 오른손 관절 선
            LineRenderer rightLine = rightSphere.AddComponent<LineRenderer>();
            rightLine.positionCount = 2;
            rightLine.startWidth = lineWidth;
            rightLine.endWidth = lineWidth;
            rightLine.enabled = drawLines;
            rightLine.material = new Material(Shader.Find("Sprites/Default")); // 기본 셰이더 사용
            rightLine.startColor = lineColor; // 선 색상 설정
            rightLine.endColor = lineColor;
            rightHandLines[jointID] = rightLine;
        }
    }

    void Update()
    {
        if (handSubsystem == null) return;

        // 손 데이터 업데이트
        handSubsystem.TryUpdateHands(XRHandSubsystem.UpdateType.Dynamic);

        // 왼손과 오른손 데이터 가져오기
        XRHand leftHand = handSubsystem.leftHand;
        XRHand rightHand = handSubsystem.rightHand;

        ProcessHandJoints("left", leftHand, leftHandJoints, leftHandLines);
        ProcessHandJoints("right", rightHand, rightHandJoints, rightHandLines);
    }

    private void ProcessHandJoints(string handSide, XRHand hand, Dictionary<XRHandJointID, GameObject> jointObjects, Dictionary<XRHandJointID, LineRenderer> lines)
    {
        if (hand.isTracked)
        {
            try
            {
                // 시각화용 관절 업데이트
                foreach (XRHandJointID jointID in visualizedJoints)
                {
                    if (jointID == XRHandJointID.Invalid) continue;

                    XRHandJoint joint = hand.GetJoint(jointID);
                    if (joint.TryGetPose(out Pose pose))
                    {
                        // 관절 위치 업데이트
                        if (drawMeshes && jointObjects.ContainsKey(jointID))
                        {
                            jointObjects[jointID].transform.position = pose.position;
                            jointObjects[jointID].transform.rotation = pose.rotation;
                        }

                        // 관절 사이 선 연결
                        if (drawLines && lines.ContainsKey(jointID))
                        {
                            XRHandJointID parentJointID = GetParentJoint(jointID);
                            if (parentJointID != XRHandJointID.Invalid && jointObjects.ContainsKey(parentJointID))
                            {
                                lines[jointID].SetPosition(0, pose.position);
                                lines[jointID].SetPosition(1, jointObjects[parentJointID].transform.position);
                                lines[jointID].enabled = true;
                            }
                            else
                            {
                                lines[jointID].enabled = false;
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[{handSide}] {jointID}: Failed to get pose.");
                    }
                }

                // 위치값 출력용 관절
                foreach (XRHandJointID jointID in trackedJoints)
                {
                    XRHandJoint joint = hand.GetJoint(jointID);
                    if (joint.TryGetPose(out Pose pose))
                    {
                        Debug.Log($"[{handSide}] {jointID}: {pose.position}");
                    }
                    else
                    {
                        Debug.LogWarning($"[{handSide}] {jointID}: Failed to get pose.");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Failed to process {handSide} hand joint data: {ex.Message}");
            }
        }
        else
        {
            Debug.Log($"{handSide} hand not tracked");
        }
    }

    // 부모 관절 찾기
    private XRHandJointID GetParentJoint(XRHandJointID jointID)
    {
        switch (jointID)
        {
            // 손목(Wrist)에서 각 Proximal 관절로 연결
            case XRHandJointID.ThumbMetacarpal:
            case XRHandJointID.IndexProximal:
            case XRHandJointID.MiddleProximal:
            case XRHandJointID.RingProximal:
            case XRHandJointID.LittleProximal:
                return XRHandJointID.Wrist;

            // 엄지: ThumbMetacarpal → ThumbProximal → ThumbTip
            case XRHandJointID.ThumbProximal:
                return XRHandJointID.ThumbMetacarpal;
            case XRHandJointID.ThumbTip:
                return XRHandJointID.ThumbProximal;

            // 나머지 손가락: Proximal → Tip
            case XRHandJointID.IndexTip:
                return XRHandJointID.IndexProximal;
            case XRHandJointID.MiddleTip:
                return XRHandJointID.MiddleProximal;
            case XRHandJointID.RingTip:
                return XRHandJointID.RingProximal;
            case XRHandJointID.LittleTip:
                return XRHandJointID.LittleProximal;

            default:
                return XRHandJointID.Invalid; // Wrist는 부모가 없음
        }
    }

    void OnDestroy()
    {
        // 시각화 오브젝트 정리
        foreach (var joint in leftHandJoints.Values)
        {
            Destroy(joint);
        }
        foreach (var joint in rightHandJoints.Values)
        {
            Destroy(joint);
        }
    }
}