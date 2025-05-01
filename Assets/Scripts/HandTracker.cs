using UnityEngine;
using Oculus.Interaction;
using System.Linq; // LINQ 네임스페이스 추가
using System.Collections.Generic; // List 사용

public class HandTracker : MonoBehaviour
{
    public bool drawMeshes = true;
    public float jointSphereSize = 0.02f;
    public Color sphereColor = Color.red;
    public bool drawLines = true;
    public float lineWidth = 0.005f;
    public Color lineColor = Color.blue;

    [SerializeField] private Transform leftHandAnchor;
    [SerializeField] private Transform rightHandAnchor;

    private OVRHand leftHand;
    private OVRSkeleton leftSkeleton;
    private OVRHand rightHand;
    private OVRSkeleton rightSkeleton;

    private GameObject leftHandJoints;
    private GameObject rightHandJoints;
    private GameObject leftHandLines;
    private GameObject rightHandLines;

    private Material sphereMaterial;
    private Material lineMaterial;

    private bool isHandTrackingEnabled = false;

    void Start()
    {
        if (leftHandAnchor == null || rightHandAnchor == null)
        {
            Debug.LogError("Hand anchors not assigned in inspector! Please assign them.");
            return;
        }

        // 컴포넌트 초기화
        leftHand = leftHandAnchor.GetComponent<OVRHand>();
        leftSkeleton = leftHandAnchor.GetComponent<OVRSkeleton>();
        rightHand = rightHandAnchor.GetComponent<OVRHand>();
        rightSkeleton = rightHandAnchor.GetComponent<OVRSkeleton>();

        if (leftHand == null || leftSkeleton == null || rightHand == null || rightSkeleton == null)
        {
            Debug.LogError("OVRHand or OVRSkeleton component not found! Hand Tracking will not work.");
            return;
        }

        // Material 초기화 (URP 호환 셰이더로 변경)
        sphereMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        sphereMaterial.color = sphereColor;

        lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        lineMaterial.color = lineColor;

        // GameObject 초기화
        leftHandJoints = new GameObject("LeftHandJoints");
        rightHandJoints = new GameObject("RightHandJoints");
        leftHandLines = new GameObject("LeftHandLines");
        rightHandLines = new GameObject("RightHandLines");

        leftHandJoints.transform.SetParent(transform);
        rightHandJoints.transform.SetParent(transform);
        leftHandLines.transform.SetParent(transform);
        rightHandLines.transform.SetParent(transform);

        isHandTrackingEnabled = true;
    }

    void Update()
    {
        if (!isHandTrackingEnabled || leftHand == null || rightHand == null || leftSkeleton == null || rightSkeleton == null) return;

        Debug.Log($"Left Hand Tracked: {leftHand.IsTracked}, Right Hand Tracked: {rightHand.IsTracked}");

        ProcessHandJoints("left", leftHand, leftSkeleton, leftHandJoints, leftHandLines);
        ProcessHandJoints("right", rightHand, rightSkeleton, rightHandJoints, rightHandLines);
    }

    void ProcessHandJoints(string handSide, OVRHand hand, OVRSkeleton skeleton, GameObject jointGroup, GameObject lineGroup)
    {
        // 추적 데이터 유효성 확인
        if (!hand.IsTracked || !skeleton.IsDataValid || skeleton.Bones == null || skeleton.Bones.Count == 0)
        {
            jointGroup.SetActive(false);
            lineGroup.SetActive(false);
            Debug.Log($"[{handSide.ToUpper()} HAND] Not tracked or data is invalid.");
            return;
        }

        // 시각화 오브젝트 활성화
        jointGroup.SetActive(true);
        lineGroup.SetActive(true);

        Debug.Log($"[{handSide.ToUpper()} HAND] Tracking Status: TRACKED");

        // 위치 출력용 관절 목록 (6개: Tip 제외, 이름 포함)
        var positionJoints = new List<(OVRSkeleton.BoneId boneId, string name)>
        {
            (OVRSkeleton.BoneId.Hand_Thumb0, "Thumb0"),   // 엄지 CMC
            (OVRSkeleton.BoneId.Hand_Thumb1, "Thumb1"),   // 엄지 MCP
            (OVRSkeleton.BoneId.Hand_Index1, "Index1"),   // 검지 MCP
            (OVRSkeleton.BoneId.Hand_Middle1, "Middle1"), // 중지 MCP
            (OVRSkeleton.BoneId.Hand_Ring1, "Ring1"),     // 약지 MCP
            (OVRSkeleton.BoneId.Hand_Pinky1, "Pinky1")    // 새끼손가락 MCP
        };

        // 시각화용 관절 목록 (11개: Tip 포함)
        OVRSkeleton.BoneId[] visualizationJoints = new OVRSkeleton.BoneId[]
        {
            OVRSkeleton.BoneId.Hand_Thumb0,     // 엄지 CMC
            OVRSkeleton.BoneId.Hand_Thumb1,     // 엄지 MCP
            OVRSkeleton.BoneId.Hand_ThumbTip,   // 엄지 Tip
            OVRSkeleton.BoneId.Hand_Index1,     // 검지 MCP
            OVRSkeleton.BoneId.Hand_IndexTip,   // 검지 Tip
            OVRSkeleton.BoneId.Hand_Middle1,    // 중지 MCP
            OVRSkeleton.BoneId.Hand_MiddleTip,  // 중지 Tip
            OVRSkeleton.BoneId.Hand_Ring1,      // 약지 MCP
            OVRSkeleton.BoneId.Hand_RingTip,    // 약지 Tip
            OVRSkeleton.BoneId.Hand_Pinky1,     // 새끼손가락 MCP
            OVRSkeleton.BoneId.Hand_PinkyTip    // 새끼손가락 Tip
        };

        // 위치 출력
        foreach (var joint in positionJoints)
        {
            var bone = skeleton.Bones.FirstOrDefault(b => b.Id == joint.boneId);
            if (bone != null && bone.Transform != null)
            {
                Vector3 position = bone.Transform.position;
                Debug.Log($"[{handSide.ToUpper()}_HAND_JOINT_{joint.name}] POSITION = ({position.x:F2}, {position.y:F2}, {position.z:F2})");
            }
            else
            {
                Debug.LogWarning($"[{handSide.ToUpper()}] Bone not found for {joint.name}");
            }
        }

        // 시각화 (기존 로직 유지)
        foreach (var bone in skeleton.Bones)
        {
            if (!System.Array.Exists(visualizationJoints, joint => joint == bone.Id)) continue;

            string boneName = bone.Id.ToString();
            Vector3 bonePosition = bone.Transform.position;

            // 시각화: 11개 관절만 시각화 (Tip 포함)
            if (drawMeshes)
            {
                string sphereName = $"{handSide}_{boneName}_Sphere";
                GameObject jointSphere = transform.Find($"{jointGroup.name}/{sphereName}")?.gameObject;

                if (jointSphere == null)
                {
                    jointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    jointSphere.name = sphereName;
                    jointSphere.transform.localScale = Vector3.one * jointSphereSize;
                    jointSphere.GetComponent<Renderer>().material = sphereMaterial;
                    jointSphere.transform.SetParent(jointGroup.transform);
                    Destroy(jointSphere.GetComponent<SphereCollider>());
                }
                jointSphere.transform.position = bonePosition;
            }

            if (drawLines && bone.Id != OVRSkeleton.BoneId.Hand_Start)
            {
                Transform parentBone = GetParentJoint(skeleton, bone.Id);
                if (parentBone != null)
                {
                    string lineName = $"{handSide}_{boneName}_Line";
                    GameObject lineObject = transform.Find($"{lineGroup.name}/{lineName}")?.gameObject;

                    if (lineObject == null)
                    {
                        lineObject = new GameObject(lineName);
                        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
                        lineRenderer.positionCount = 2;
                        lineRenderer.startWidth = lineWidth;
                        lineRenderer.endWidth = lineWidth;
                        lineRenderer.material = lineMaterial;
                        lineObject.transform.SetParent(lineGroup.transform);
                    }

                    LineRenderer line = lineObject.GetComponent<LineRenderer>();
                    line.SetPosition(0, parentBone.position);
                    line.SetPosition(1, bonePosition);
                }
            }
        }
    }

    // GetParentJoint 메서드 (기존 로직 유지)
    Transform GetParentJoint(OVRSkeleton skeleton, OVRSkeleton.BoneId boneId)
    {
        for (int i = 0; i < skeleton.Bones.Count; i++)
        {
            if (skeleton.Bones[i].Id == boneId)
            {
                int parentIndex = skeleton.Bones[i].ParentBoneIndex;
                if (parentIndex >= 0 && parentIndex < skeleton.Bones.Count)
                {
                    return skeleton.Bones[parentIndex].Transform;
                }
                return null;
            }
        }
        return null;
    }

    void OnDestroy()
    {
        // Material 정리
        if (sphereMaterial != null)
            Destroy(sphereMaterial);
        if (lineMaterial != null)
            Destroy(lineMaterial);
    }
}