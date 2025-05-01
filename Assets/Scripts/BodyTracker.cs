using UnityEngine;
using Oculus.Interaction; // OVRSkeleton 네임스페이스
using System.Linq; // LINQ 네임스페이스 추가
using System.Collections.Generic; // List 및 Dictionary 사용

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

        private OVRSkeleton skeleton;
        private GameObject bodyJoints;
        private GameObject bodyLines;
        private Material sphereMaterial;
        private Material lineMaterial;
        private bool isBodyTrackingEnabled = false;

        void Start()
        {
            skeleton = GetComponent<OVRSkeleton>();
            if (skeleton == null)
            {
                Debug.LogError("OVRSkeleton component not found! Body Tracking will not work.");
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

            // SDK 버전 확인
            Debug.Log($"Meta XR SDK Version: {OVRPlugin.version}");

            isBodyTrackingEnabled = true;
        }

        // LineRenderer 생성 및 설정 메서드
        private void DrawLineBetween(string fromName, string toName, Vector3 fromPosition, Vector3 toPosition)
        {
            string lineName = $"Body_{fromName}To{toName}_Line";
            GameObject lineObject = bodyLines.transform.Find(lineName)?.gameObject;
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
            line.SetPosition(0, fromPosition);
            line.SetPosition(1, toPosition);
        }

        void Update()
        {
            if (!isBodyTrackingEnabled || skeleton == null) return;

            // 데이터 유효성 확인
            if (!skeleton.IsDataValid || skeleton.Bones == null || skeleton.Bones.Count == 0)
            {
                Debug.Log("Body is not tracked or data is invalid.");
                bodyJoints.SetActive(false);
                bodyLines.SetActive(false);
                return;
            }

            bodyJoints.SetActive(true);
            bodyLines.SetActive(true);

            Debug.Log("Body is tracked.");

            // 대상 관절: 골반과 머리 (BoneId와 이름 쌍으로 저장)
            var targetJoints = new List<(OVRSkeleton.BoneId boneId, string name)>
            {
                (OVRSkeleton.BoneId.Body_Hips, "HIPS"),
                (OVRSkeleton.BoneId.Body_SpineLower, "SPINE_LOWER"),
                (OVRSkeleton.BoneId.Body_Head, "HEAD")
            };

            // 위치값 저장용 딕셔너리
            var jointPositions = new Dictionary<string, Vector3>();

            // 관절 처리
            foreach (var joint in targetJoints)
            {
                // OVRSkeleton.Bones에서 해당 관절 찾기
                var bone = skeleton.Bones.FirstOrDefault(b => b.Id == joint.boneId);
                if (bone != null && bone.Transform != null)
                {
                    Vector3 position = bone.Transform.position;

                    // 위치 저장
                    jointPositions[joint.name] = position;

                    // 위치 출력 (리스트에서 이름 가져오기)
                    Debug.Log($"[BODY_JOINT_{joint.name}] POSITION = ({position.x:F2}, {position.y:F2}, {position.z:F2})");

                    // 시각화 (스피어)
                    if (drawMeshes)
                    {
                        string sphereName = $"Body_{joint.name}_Sphere"; // 이름 동기화
                        GameObject jointSphere = bodyJoints.transform.Find(sphereName)?.gameObject;

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
                }
                else
                {
                    Debug.LogWarning($"Bone not found for {joint.name}");
                }
            }

            // 선 연결 (targetJoints 순서대로 연결)
            if (drawLines)
            {
                for (int i = 0; i < targetJoints.Count - 1; i++)
                {
                    string fromName = targetJoints[i].name;
                    string toName = targetJoints[i + 1].name;

                    if (jointPositions.ContainsKey(fromName) && jointPositions.ContainsKey(toName))
                    {
                        DrawLineBetween(fromName, toName, jointPositions[fromName], jointPositions[toName]);
                    }
                }
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