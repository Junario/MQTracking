using UnityEngine;
using Oculus.Interaction;
using System.Collections.Generic;

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

        // 관절 활성화 플래그
        private bool[] jointEnabled = new bool[(int)OVRPlugin.BoneId.Body_End];
        // 캐싱된 필터링 결과
        private int[] positionJoints; // 인덱스를 직접 사용
        private int[] visualizationJoints;

        // GameObject 캐싱용 Dictionary
        private Dictionary<string, GameObject> jointSpheres = new Dictionary<string, GameObject>();
        private Dictionary<string, LineRenderer> jointLines = new Dictionary<string, LineRenderer>();

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

            // 관절 활성화 플래그 및 필터링 초기화
            InitializeJointFlags();
            CacheFilteredJoints();

            // 디버깅: OVRPlugin.BoneId 값 확인
            Debug.Log($"Body_Root: {(int)OVRSkeleton.BoneId.Body_Root}");
            Debug.Log($"Body_Hips: {(int)OVRSkeleton.BoneId.Body_Hips}");
            Debug.Log($"Body_Head: {(int)OVRSkeleton.BoneId.Body_Head}");

            isBodyTrackingEnabled = true;
        }

        void InitializeJointFlags()
        {
            // 모든 관절 비활성화
            for (int i = 0; i < (int)OVRPlugin.BoneId.Body_End; i++)
            {
                jointEnabled[i] = false;
            }

            // 상체 관절 활성화 (인덱스 직접 사용)
            jointEnabled[0] = true; // Body_Root
            jointEnabled[1] = true; // Body_Hips
            jointEnabled[7] = true; // Body_Head (인덱스 7로 보정)

            // 디버깅: 활성화된 관절 확인
            List<string> enabledJoints = new List<string>();
            for (int i = 0; i < (int)OVRPlugin.BoneId.Body_End; i++)
            {
                if (jointEnabled[i])
                {
                    enabledJoints.Add(((OVRPlugin.BoneId)i).ToString());
                }
            }
            Debug.Log("Enabled Joints: " + string.Join(", ", enabledJoints));
        }

        void CacheFilteredJoints()
        {
            // 상체 관절 목록 (인덱스 직접 사용)
            int[] allPositionJoints = new int[]
            {
                0,  // Body_Root
                1,  // Body_Hips
                7   // Body_Head (인덱스 7로 보정)
            };

            // 상체 관절 목록 (시각화용)
            int[] allVisualizationJoints = allPositionJoints; // 동일한 관절 사용

            // 직접 배열 할당 (필터링 없이)
            positionJoints = allPositionJoints;
            visualizationJoints = allVisualizationJoints;

            // 디버깅: positionJoints와 visualizationJoints 출력
            List<string> positionJointNames = new List<string>();
            foreach (var joint in positionJoints)
            {
                positionJointNames.Add(((OVRPlugin.BoneId)joint).ToString());
            }
            Debug.Log("Position Joints: " + string.Join(", ", positionJointNames));

            // 디버깅: positionJoints의 숫자 값 출력
            Debug.Log("Position Joints Values: " + string.Join(", ", positionJoints));

            List<string> visualizationJointNames = new List<string>();
            foreach (var joint in visualizationJoints)
            {
                visualizationJointNames.Add(((OVRPlugin.BoneId)joint).ToString());
            }
            Debug.Log("Visualization Joints: " + string.Join(", ", visualizationJointNames));
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
                return;
            }

            bodyJoints.SetActive(true);
            bodyLines.SetActive(true);

            Debug.Log("Body is tracked.");

            // OVRBody에서 직접 관절 위치 가져오기
            var jointLocations = bodyState.Value.JointLocations;

            // 디버깅: 사용 가능한 관절 출력
            for (int i = 0; i < jointLocations.Length; i++)
            {
                var jointLocation = jointLocations[i];
                if (jointLocation.OrientationValid && jointLocation.PositionValid)
                {
                    Debug.Log($"Available Joint: {(OVRPlugin.BoneId)i} (index: {i})");
                }
            }

            // 필요한 관절 위치 출력 및 시각화
            foreach (var jointIndex in positionJoints)
            {
                // 관절 위치 가져오기
                if (jointIndex < 0 || jointIndex >= jointLocations.Length)
                {
                    Debug.LogWarning($"Joint index {jointIndex} out of range for {(OVRPlugin.BoneId)jointIndex}");
                    continue;
                }

                var jointLocation = jointLocations[jointIndex];
                // 데이터 유효성 확인
                Debug.Log($"Joint {(OVRPlugin.BoneId)jointIndex} - OrientationValid: {jointLocation.OrientationValid}, PositionValid: {jointLocation.PositionValid}");
                if (!jointLocation.OrientationValid || !jointLocation.PositionValid)
                {
                    Debug.Log($"Joint {(OVRPlugin.BoneId)jointIndex} has invalid orientation or position.");
                    continue;
                }

                // 위치 데이터 가져오기
                Vector3 bonePosition = new Vector3(jointLocation.Pose.Position.x, jointLocation.Pose.Position.y, jointLocation.Pose.Position.z);
                string boneName = ((OVRSkeleton.BoneId)jointIndex).ToString();

                // 위치 출력
                Debug.Log($"[BODY_JOINT_{boneName}] POSITION = ({bonePosition.x:F2}, {bonePosition.y:F2}, {bonePosition.z:F2})");

                // 시각화 (스피어)
                if (drawMeshes)
                {
                    string sphereName = $"Body_{boneName}_Sphere";
                    if (!jointSpheres.TryGetValue(sphereName, out GameObject jointSphere))
                    {
                        jointSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        jointSphere.name = sphereName;
                        jointSphere.transform.localScale = Vector3.one * jointSphereSize;
                        jointSphere.GetComponent<Renderer>().material = sphereMaterial;
                        jointSphere.transform.SetParent(bodyJoints.transform);
                        Destroy(jointSphere.GetComponent<SphereCollider>());
                        jointSpheres[sphereName] = jointSphere;
                    }
                    jointSphere.transform.position = bonePosition;
                }

                // 시각화 (라인)
                if (drawLines && jointIndex != 0) // Body_Root (인덱스 0) 제외
                {
                    Transform parentBone = GetParentJoint(skeleton, jointIndex);
                    if (parentBone != null)
                    {
                        string lineName = $"Body_{boneName}_Line";
                        if (!jointLines.TryGetValue(lineName, out LineRenderer line))
                        {
                            GameObject lineObject = new GameObject(lineName);
                            line = lineObject.AddComponent<LineRenderer>();
                            line.positionCount = 2;
                            line.startWidth = lineWidth;
                            line.endWidth = lineWidth;
                            line.material = lineMaterial;
                            lineObject.transform.SetParent(bodyLines.transform);
                            jointLines[lineName] = line;
                        }
                        line.SetPosition(0, parentBone.position);
                        line.SetPosition(1, bonePosition);
                    }
                }
            }
        }

        Transform GetParentJoint(OVRSkeleton skeleton, int jointIndex)
        {
            // 인덱스를 OVRSkeleton.BoneId로 매핑
            OVRSkeleton.BoneId skeletonBoneId;
            switch (jointIndex)
            {
                case 0: // Body_Root
                    skeletonBoneId = OVRSkeleton.BoneId.Body_Root;
                    break;
                case 1: // Body_Hips
                    skeletonBoneId = OVRSkeleton.BoneId.Body_Hips;
                    break;
                case 7: // Body_Head
                    skeletonBoneId = OVRSkeleton.BoneId.Body_Head;
                    break;
                default:
                    Debug.LogWarning($"No mapping for index {jointIndex} in GetParentJoint.");
                    return null;
            }

            for (int i = 0; i < skeleton.Bones.Count; i++)
            {
                if (skeleton.Bones[i].Id == skeletonBoneId)
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
        }
    }
}