using UnityEngine;
using Oculus.Interaction;

namespace BodyTracking
{
    public class BodyTracker : MonoBehaviour
    {
        public bool drawMeshes = true;
        public float jointSphereSize = 0.02f;
        public Color sphereColor = Color.green;
        public bool drawLines = true;
        public float lineWidth = 0.005f;
        public Color lineColor = Color.blue;

        private OVRBody body;
        private OVRSkeleton skeleton;

        private GameObject bodyJoints;
        private GameObject bodyLines;

        private Material sphereMaterial;
        private Material lineMaterial;

        private bool isBodyTrackingEnabled = false;

        // 하체 관절 위치 (IK 추정용)
        private Vector3 leftLegUpper, rightLegUpper;
        private Vector3 leftKnee, rightKnee;
        private Vector3 leftFoot, rightFoot;
        private bool isLowerBodyEstimated = false;

        void Start()
        {
            body = GetComponent<OVRBody>();
            skeleton = GetComponent<OVRSkeleton>();

            if (body == null || skeleton == null)
            {
                Debug.LogError("OVRBody or OVRSkeleton component not found! Body Tracking will not work.");
                return;
            }

            // Material 초기화 (URP 환경 기준)
            sphereMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sphereMaterial.color = sphereColor;
            
            lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            lineMaterial.color = lineColor;

            // GameObject 초기화
            bodyJoints = new GameObject("BodyJoints");
            bodyLines = new GameObject("BodyLines");

            bodyJoints.transform.SetParent(transform);
            bodyLines.transform.SetParent(transform);

            isBodyTrackingEnabled = true;
        }

        void Update()
        {
            if (!isBodyTrackingEnabled || body == null || skeleton == null) return;

            // 데이터 유효성 확인
            var bodyState = body.BodyState;
            if (!bodyState.HasValue || bodyState.Value.JointLocations == null || 
                bodyState.Value.JointLocations.Length == 0 || !skeleton.IsDataValid)
            {
                Debug.Log("Body is not tracked or data is invalid.");
                bodyJoints.SetActive(false);
                bodyLines.SetActive(false);
                isLowerBodyEstimated = false;
                return;
            }

            bodyJoints.SetActive(true);
            bodyLines.SetActive(true);

            Debug.Log("Body is tracked.");

            // 위치 출력 및 시각화용 관절 목록 (상체, 손목만 포함)
            OVRSkeleton.BoneId[] positionJoints = new OVRSkeleton.BoneId[]
            {
                OVRSkeleton.BoneId.Body_Root,
                OVRSkeleton.BoneId.Body_Hips,
                OVRSkeleton.BoneId.Body_SpineLower,
                OVRSkeleton.BoneId.Body_SpineMiddle,
                OVRSkeleton.BoneId.Body_SpineUpper,
                OVRSkeleton.BoneId.Body_Chest,
                OVRSkeleton.BoneId.Body_Neck,
                OVRSkeleton.BoneId.Body_Head,
                OVRSkeleton.BoneId.Body_LeftShoulder,
                OVRSkeleton.BoneId.Body_LeftArmUpper,
                OVRSkeleton.BoneId.Body_LeftArmLower,
                OVRSkeleton.BoneId.Body_LeftHandWrist, // 손목 추가
                OVRSkeleton.BoneId.Body_RightShoulder,
                OVRSkeleton.BoneId.Body_RightArmUpper,
                OVRSkeleton.BoneId.Body_RightArmLower,
                OVRSkeleton.BoneId.Body_RightHandWrist, // 손목 추가
            };

            // 시각화용 관절 목록 (상체, 손목만 포함)
            OVRSkeleton.BoneId[] visualizationJoints = new OVRSkeleton.BoneId[]
            {
                OVRSkeleton.BoneId.Body_Root,
                OVRSkeleton.BoneId.Body_Hips,
                OVRSkeleton.BoneId.Body_SpineLower,
                OVRSkeleton.BoneId.Body_SpineMiddle,
                OVRSkeleton.BoneId.Body_SpineUpper,
                OVRSkeleton.BoneId.Body_Chest,
                OVRSkeleton.BoneId.Body_Neck,
                OVRSkeleton.BoneId.Body_Head,
                OVRSkeleton.BoneId.Body_LeftShoulder,
                OVRSkeleton.BoneId.Body_LeftArmUpper,
                OVRSkeleton.BoneId.Body_LeftArmLower,
                OVRSkeleton.BoneId.Body_LeftHandWrist, // 손목 추가
                OVRSkeleton.BoneId.Body_RightShoulder,
                OVRSkeleton.BoneId.Body_RightArmUpper,
                OVRSkeleton.BoneId.Body_RightArmLower,
                OVRSkeleton.BoneId.Body_RightHandWrist, // 손목 추가
            };

            // IK를 통해 하체 관절 위치 추정
            Vector3 hipsPosition = Vector3.zero;
            foreach (var bone in skeleton.Bones)
            {
                if (bone.Id == OVRSkeleton.BoneId.Body_Hips)
                {
                    hipsPosition = bone.Transform.position;
                    break;
                }
            }

            // 간단한 오프셋으로 하체 위치 추정 (인간 신체 비율 고려)
            leftLegUpper = hipsPosition + new Vector3(-0.1f, -0.3f, 0); // 왼쪽 상부 다리
            rightLegUpper = hipsPosition + new Vector3(0.1f, -0.3f, 0); // 오른쪽 상부 다리
            leftKnee = leftLegUpper + new Vector3(0, -0.4f, 0); // 왼쪽 무릎
            rightKnee = rightLegUpper + new Vector3(0, -0.4f, 0); // 오른쪽 무릎
            leftFoot = leftKnee + new Vector3(0, -0.4f, 0.1f); // 왼쪽 발
            rightFoot = rightKnee + new Vector3(0, -0.4f, 0.1f); // 오른쪽 발
            isLowerBodyEstimated = true;

            // 상체 관절 처리 (손목 포함)
            foreach (var bone in skeleton.Bones)
            {
                if (!System.Array.Exists(visualizationJoints, joint => joint == bone.Id)) continue;

                string boneName = bone.Id.ToString();
                Vector3 bonePosition = bone.Transform.position;

                // 위치 출력
                if (System.Array.Exists(positionJoints, joint => joint == bone.Id))
                {
                    Debug.Log($"[BODY_JOINT_{boneName}] POSITION = ({bonePosition.x:F2}, {bonePosition.y:F2}, {bonePosition.z:F2})");
                }

                // 시각화 (스피어)
                if (drawMeshes)
                {
                    string sphereName = $"Body_{boneName}_Sphere";
                    GameObject jointSphere = transform.Find($"{bodyJoints.name}/{sphereName}")?.gameObject;
                    
                    if (jointSphere == null)
                    {
                        jointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        jointSphere.name = sphereName;
                        jointSphere.transform.localScale = Vector3.one * jointSphereSize;
                        jointSphere.GetComponent<Renderer>().material = sphereMaterial;
                        jointSphere.transform.SetParent(bodyJoints.transform);
                        Destroy(jointSphere.GetComponent<SphereCollider>());
                    }
                    jointSphere.transform.position = bonePosition;
                }

                // 시각화 (라인)
                if (drawLines && bone.Id != OVRSkeleton.BoneId.Body_Root)
                {
                    Transform parentBone = GetParentJoint(skeleton, bone.Id);
                    if (parentBone != null)
                    {
                        string lineName = $"Body_{boneName}_Line";
                        GameObject lineObject = transform.Find($"{bodyLines.name}/{lineName}")?.gameObject;
                        
                        if (lineObject == null)
                        {
                            lineObject = new GameObject(lineName);
                            LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
                            lineRenderer.positionCount = 2;
                            lineRenderer.startWidth = lineWidth;
                            lineRenderer.endWidth = lineWidth;
                            lineRenderer.material = lineMaterial;
                            lineObject.transform.SetParent(bodyLines.transform);
                        }

                        LineRenderer line = lineObject.GetComponent<LineRenderer>();
                        line.SetPosition(0, parentBone.position);
                        line.SetPosition(1, bonePosition);
                    }
                }
            }

            // 하체 관절 시각화 (IK)
            if (isLowerBodyEstimated)
            {
                VisualizeLowerBodyJoint("LeftLegUpper", leftLegUpper, hipsPosition);
                VisualizeLowerBodyJoint("RightLegUpper", rightLegUpper, hipsPosition);
                VisualizeLowerBodyJoint("LeftKnee", leftKnee, leftLegUpper);
                VisualizeLowerBodyJoint("RightKnee", rightKnee, rightLegUpper);
                VisualizeLowerBodyJoint("LeftFoot", leftFoot, leftKnee);
                VisualizeLowerBodyJoint("RightFoot", rightFoot, rightKnee);
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

        void VisualizeLowerBodyJoint(string jointName, Vector3 position, Vector3 parentPosition)
        {
            // 위치 출력
            Debug.Log($"[BODY_JOINT_{jointName}] POSITION = ({position.x:F2}, {position.y:F2}, {position.z:F2})");

            // 시각화 (스피어)
            if (drawMeshes)
            {
                string sphereName = $"Body_{jointName}_Sphere";
                GameObject jointSphere = transform.Find($"{bodyJoints.name}/{sphereName}")?.gameObject;
                
                if (jointSphere == null)
                {
                    jointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    jointSphere.name = sphereName;
                    jointSphere.transform.localScale = Vector3.one * jointSphereSize;
                    jointSphere.GetComponent<Renderer>().material = sphereMaterial;
                    jointSphere.transform.SetParent(bodyJoints.transform);
                    Destroy(jointSphere.GetComponent<SphereCollider>());
                }
                jointSphere.transform.position = position;
            }

            // 시각화 (라인)
            if (drawLines)
            {
                string lineName = $"Body_{jointName}_Line";
                GameObject lineObject = transform.Find($"{bodyLines.name}/{lineName}")?.gameObject;
                
                if (lineObject == null)
                {
                    lineObject = new GameObject(lineName);
                    LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
                    lineRenderer.positionCount = 2;
                    lineRenderer.startWidth = lineWidth;
                    lineRenderer.endWidth = lineWidth;
                    lineRenderer.material = lineMaterial;
                    lineObject.transform.SetParent(bodyLines.transform);
                }

                LineRenderer line = lineObject.GetComponent<LineRenderer>();
                line.SetPosition(0, parentPosition);
                line.SetPosition(1, position);
            }
        }

        void OnDestroy()
        {
            if (sphereMaterial != null)
                Destroy(sphereMaterial);
            if (lineMaterial != null)
                Destroy(lineMaterial);
        }
    }
}