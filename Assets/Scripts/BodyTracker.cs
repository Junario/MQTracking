using UnityEngine;
using System.Linq;
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

        private OVRSkeleton skeleton;
        private GameObject bodyJoints;
        private GameObject bodyLines;
        private Material sphereMaterial;
        private Material lineMaterial;
        private bool isBodyTrackingEnabled = false;

        public List<(string name, Vector3 position)> BodyPositions { get; private set; } = new List<(string, Vector3)>();

        private readonly Dictionary<string, GameObject> jointSpheres = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, LineRenderer> jointLines = new Dictionary<string, LineRenderer>();

        void Start()
        {
            skeleton = GetComponent<OVRSkeleton>();
            if (skeleton == null)
            {
                Debug.LogError("OVRSkeleton component not found! Body Tracking will not work.");
                return;
            }

            sphereMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            sphereMaterial.color = sphereColor;

            lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            lineMaterial.color = lineColor;

            bodyJoints = new GameObject("BodyJoints");
            bodyLines = new GameObject("BodyLines");

            bodyJoints.transform.SetParent(transform);
            bodyLines.transform.SetParent(transform);

            Debug.Log($"Meta XR SDK Version: {OVRPlugin.version}");

            isBodyTrackingEnabled = true;
        }

        private void DrawLineBetween(string fromName, string toName, Vector3 fromPosition, Vector3 toPosition)
        {
            string lineName = $"Body_{fromName}To{toName}_Line";
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
            line.SetPosition(0, fromPosition);
            line.SetPosition(1, toPosition);
        }

        void Update()
        {
            if (!isBodyTrackingEnabled || skeleton == null) return;

            if (!skeleton.IsDataValid || skeleton.Bones == null || skeleton.Bones.Count == 0)
            {
                bodyJoints.SetActive(false);
                bodyLines.SetActive(false);
                Debug.Log("Body is not tracked or data is invalid.");
                BodyPositions.Clear();
                return;
            }

            bodyJoints.SetActive(true);
            bodyLines.SetActive(true);

            Debug.Log("Body is tracked.");

            // Debug all available BoneIds to check for Arm support (1초마다 출력)
            if (Time.frameCount % 60 == 0)
            {
                foreach (var bone in skeleton.Bones)
                {
                    Debug.Log($"Available Bone: {bone.Id}, Position: {bone.Transform.position}");
                }
            }

            var targetJoints = new List<(OVRSkeleton.BoneId boneId, string name)>
            {
                (OVRSkeleton.BoneId.Body_Hips, "Hip"),
                (OVRSkeleton.BoneId.Body_Head, "Head"),
                (OVRSkeleton.BoneId.Body_LeftShoulder, "LeftShoulder"),
                (OVRSkeleton.BoneId.Body_LeftArmUpper, "LeftArmUpper"),
                (OVRSkeleton.BoneId.Body_LeftArmLower, "LeftArmLower"),
                (OVRSkeleton.BoneId.Body_LeftHandWrist, "LeftHandWrist"),
                (OVRSkeleton.BoneId.Body_RightShoulder, "RightShoulder"),
                (OVRSkeleton.BoneId.Body_RightArmUpper, "RightArmUpper"),
                (OVRSkeleton.BoneId.Body_RightArmLower, "RightArmLower"),
                (OVRSkeleton.BoneId.Body_RightHandWrist, "RightHandWrist")
            };

            BodyPositions.Clear();
            var jointPositions = new Dictionary<string, Vector3>();
            foreach (var joint in targetJoints)
            {
                var bone = skeleton.Bones.FirstOrDefault(b => b.Id == joint.boneId);
                if (bone != null && bone.Transform != null)
                {
                    Vector3 position = bone.Transform.position;
                    BodyPositions.Add((joint.name, position));
                    jointPositions[joint.name] = position;
                    Debug.Log($"[BODY_JOINT_{joint.name}] POSITION = ({position.x:F2}, {position.y:F2}, {position.z:F2})");
                }
                else
                {
                    Debug.LogWarning($"Bone not found for {joint.name}");
                }
            }

            if (drawMeshes)
            {
                foreach (var joint in targetJoints)
                {
                    if (jointPositions.ContainsKey(joint.name))
                    {
                        string sphereName = $"Body_{joint.name}_Sphere";
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
                        jointSphere.transform.position = jointPositions[joint.name];
                    }
                }
            }

            if (drawLines)
            {
                var connections = new List<(string fromName, string toName)>
                {
                    ("Hip", "Head"),
                    ("Hip", "LeftShoulder"),
                    ("Hip", "RightShoulder"),
                    ("LeftShoulder", "LeftArmUpper"),
                    ("LeftArmUpper", "LeftArmLower"),
                    ("LeftArmLower", "LeftHandWrist"),
                    ("RightShoulder", "RightArmUpper"),
                    ("RightArmUpper", "RightArmLower"),
                    ("RightArmLower", "RightHandWrist")
                };

                foreach (var conn in connections)
                {
                    if (jointPositions.ContainsKey(conn.fromName) && jointPositions.ContainsKey(conn.toName))
                    {
                        DrawLineBetween(conn.fromName, conn.toName, jointPositions[conn.fromName], jointPositions[conn.toName]);
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

            if (bodyJoints != null)
                Destroy(bodyJoints);
            if (bodyLines != null)
                Destroy(bodyLines);
        }
    }
}