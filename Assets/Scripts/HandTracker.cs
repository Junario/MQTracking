using UnityEngine;
using Oculus.Interaction;
using System.Linq;
using System.Collections.Generic;

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

    public List<(string name, Vector3 position, Quaternion orientation)> LeftHandPositions { get; private set; } = new List<(string, Vector3, Quaternion)>();
    public List<(string name, Vector3 position, Quaternion orientation)> RightHandPositions { get; private set; } = new List<(string, Vector3, Quaternion)>();

    private readonly Dictionary<string, GameObject> jointSpheres = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, LineRenderer> jointLines = new Dictionary<string, LineRenderer>();

    void Start()
    {
        if (leftHandAnchor == null || rightHandAnchor == null)
        {
            Debug.LogError("Hand anchors not assigned in inspector! Please assign them.");
            return;
        }

        leftHand = leftHandAnchor.GetComponent<OVRHand>();
        leftSkeleton = leftHandAnchor.GetComponent<OVRSkeleton>();
        rightHand = rightHandAnchor.GetComponent<OVRHand>();
        rightSkeleton = rightHandAnchor.GetComponent<OVRSkeleton>();

        if (leftHand == null || leftSkeleton == null || rightHand == null || rightSkeleton == null)
        {
            Debug.LogError("OVRHand or OVRSkeleton component not found! Hand Tracking will not work.");
            return;
        }

        sphereMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        sphereMaterial.color = sphereColor;

        lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        lineMaterial.color = lineColor;

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

        ProcessHandJoints("left", leftHand, leftSkeleton, leftHandJoints, leftHandLines, LeftHandPositions);
        ProcessHandJoints("right", rightHand, rightSkeleton, rightHandJoints, rightHandLines, RightHandPositions);
    }

    void ProcessHandJoints(string handSide, OVRHand hand, OVRSkeleton skeleton, GameObject jointGroup, GameObject lineGroup, List<(string name, Vector3 position, Quaternion orientation)> handPositions)
    {
        if (!hand.IsTracked || !skeleton.IsDataValid || skeleton.Bones == null || skeleton.Bones.Count == 0)
        {
            jointGroup.SetActive(false);
            lineGroup.SetActive(false);
            Debug.Log($"[{handSide.ToUpper()} HAND] Not tracked or data is invalid.");
            handPositions.Clear();
            return;
        }

        jointGroup.SetActive(true);
        lineGroup.SetActive(true);

        Debug.Log($"[{handSide.ToUpper()} HAND] Tracking Status: TRACKED");

        var positionJoints = new List<(OVRSkeleton.BoneId boneId, string name)>
        {
            (OVRSkeleton.BoneId.Hand_Thumb0, "Thumb0"),
            (OVRSkeleton.BoneId.Hand_Thumb1, "Thumb1"),
            (OVRSkeleton.BoneId.Hand_Index1, "Index1"),
            (OVRSkeleton.BoneId.Hand_Middle1, "Middle1"),
            (OVRSkeleton.BoneId.Hand_Ring1, "Ring1"),
            (OVRSkeleton.BoneId.Hand_Pinky1, "Pinky1")
        };

        OVRSkeleton.BoneId[] visualizationJoints = new OVRSkeleton.BoneId[]
        {
            OVRSkeleton.BoneId.Hand_Thumb0,
            OVRSkeleton.BoneId.Hand_Thumb1,
            OVRSkeleton.BoneId.Hand_ThumbTip,
            OVRSkeleton.BoneId.Hand_Index1,
            OVRSkeleton.BoneId.Hand_IndexTip,
            OVRSkeleton.BoneId.Hand_Middle1,
            OVRSkeleton.BoneId.Hand_MiddleTip,
            OVRSkeleton.BoneId.Hand_Ring1,
            OVRSkeleton.BoneId.Hand_RingTip,
            OVRSkeleton.BoneId.Hand_Pinky1,
            OVRSkeleton.BoneId.Hand_PinkyTip
        };

        handPositions.Clear();
        foreach (var joint in positionJoints)
        {
            var bone = skeleton.Bones.FirstOrDefault(b => b.Id == joint.boneId);
            if (bone != null && bone.Transform != null)
            {
                Vector3 position = bone.Transform.position;
                Quaternion orientation = bone.Transform.rotation;
                handPositions.Add((joint.name, position, orientation));
                Debug.Log($"[{handSide.ToUpper()}_HAND_JOINT_{joint.name}] POSITION = ({position.x:F2}, {position.y:F2}, {position.z:F2}), ORIENTATION = ({orientation.x:F2}, {orientation.y:F2}, {orientation.z:F2}, {orientation.w:F2})");
            }
            else
            {
                Debug.LogWarning($"[{handSide.ToUpper()}] Bone not found for {joint.name}");
            }
        }

        foreach (var bone in skeleton.Bones)
        {
            if (!System.Array.Exists(visualizationJoints, joint => joint == bone.Id)) continue;

            string boneName = bone.Id.ToString();
            Vector3 bonePosition = bone.Transform.position;

            if (drawMeshes)
            {
                string sphereName = $"{handSide}_{boneName}_Sphere";
                if (!jointSpheres.TryGetValue(sphereName, out GameObject jointSphere))
                {
                    jointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    jointSphere.name = sphereName;
                    jointSphere.transform.localScale = Vector3.one * jointSphereSize;
                    jointSphere.GetComponent<Renderer>().material = sphereMaterial;
                    jointSphere.transform.SetParent(jointGroup.transform);
                    Destroy(jointSphere.GetComponent<SphereCollider>());
                    jointSpheres[sphereName] = jointSphere;
                }
                jointSphere.transform.position = bonePosition;
            }

            if (drawLines && bone.Id != OVRSkeleton.BoneId.Hand_Start)
            {
                Transform parentBone = GetParentJoint(skeleton, bone.Id);
                if (parentBone != null)
                {
                    string lineName = $"{handSide}_{boneName}_Line";
                    if (!jointLines.TryGetValue(lineName, out LineRenderer line))
                    {
                        GameObject lineObject = new GameObject(lineName);
                        line = lineObject.AddComponent<LineRenderer>();
                        line.positionCount = 2;
                        line.startWidth = lineWidth;
                        line.endWidth = lineWidth;
                        line.material = lineMaterial;
                        lineObject.transform.SetParent(lineGroup.transform);
                        jointLines[lineName] = line;
                    }
                    line.SetPosition(0, parentBone.position);
                    line.SetPosition(1, bonePosition);
                }
            }
        }
    }

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
        if (sphereMaterial != null)
            Destroy(sphereMaterial);
        if (lineMaterial != null)
            Destroy(lineMaterial);

        if (leftHandJoints != null)
            Destroy(leftHandJoints);
        if (rightHandJoints != null)
            Destroy(rightHandJoints);
        if (leftHandLines != null)
            Destroy(leftHandLines);
        if (rightHandLines != null)
            Destroy(rightHandLines);
    }
}