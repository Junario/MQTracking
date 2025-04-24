using UnityEngine;
using Oculus.Interaction;

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
        // IsDataValid를 추가하여 추적 데이터 유효성 확인
        if (!hand.IsTracked || !skeleton.IsDataValid)
        {
            jointGroup.SetActive(false);
            lineGroup.SetActive(false);
            return;
        }

        // 시각화 오브젝트 활성화
        jointGroup.SetActive(true);
        lineGroup.SetActive(true);

        Debug.Log($"[{handSide.ToUpper()} HAND] Tracking Status: TRACKED");

        // 위치 출력용 관절 목록 (6개: Tip 제외)
        OVRSkeleton.BoneId[] positionJoints = new OVRSkeleton.BoneId[]
        {
            OVRSkeleton.BoneId.Hand_Thumb0,  // 엄지 CMC
            OVRSkeleton.BoneId.Hand_Thumb1,  // 엄지 MCP
            OVRSkeleton.BoneId.Hand_Index1,  // 검지 MCP
            OVRSkeleton.BoneId.Hand_Middle1, // 중지 MCP
            OVRSkeleton.BoneId.Hand_Ring1,   // 약지 MCP
            OVRSkeleton.BoneId.Hand_Pinky1   // 새끼손가락 MCP
        };

        // 시각화용 관절 목록 (11개: Tip 포함, Hand_Thumb3을 Hand_ThumbTip으로 교체)
        OVRSkeleton.BoneId[] visualizationJoints = new OVRSkeleton.BoneId[]
        {
            OVRSkeleton.BoneId.Hand_Thumb0,     // 엄지 CMC
            OVRSkeleton.BoneId.Hand_Thumb1,     // 엄지 MCP
            OVRSkeleton.BoneId.Hand_ThumbTip,   // 엄지 Tip (Hand_Thumb3 교체)
            OVRSkeleton.BoneId.Hand_Index1,     // 검지 MCP
            OVRSkeleton.BoneId.Hand_IndexTip,   // 검지 Tip
            OVRSkeleton.BoneId.Hand_Middle1,    // 중지 MCP
            OVRSkeleton.BoneId.Hand_MiddleTip,  // 중지 Tip
            OVRSkeleton.BoneId.Hand_Ring1,      // 약지 MCP
            OVRSkeleton.BoneId.Hand_RingTip,    // 약지 Tip
            OVRSkeleton.BoneId.Hand_Pinky1,     // 새끼손가락 MCP
            OVRSkeleton.BoneId.Hand_PinkyTip    // 새끼손가락 Tip
        };

        // GetBoneTransform 대신 skeleton.Bones 순회로 복구
        foreach (var bone in skeleton.Bones)
        {
            if (!System.Array.Exists(visualizationJoints, joint => joint == bone.Id)) continue;

            string boneName = bone.Id.ToString();
            Vector3 bonePosition = bone.Transform.position;

            // 위치 출력: 6개 관절만 출력 (Tip 제외)
            if (System.Array.Exists(positionJoints, joint => joint == bone.Id))
            {
                Debug.Log($"[{handSide.ToUpper()}_HAND_JOINT_{boneName}] POSITION = ({bonePosition.x:F2}, {bonePosition.y:F2}, {bonePosition.z:F2})");
            }

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

    // GetParentJoint 메서드 최적화 (ParentBoneIndex 활용)
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