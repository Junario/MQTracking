using UnityEngine;
using Oculus.Interaction;
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

            var targetJoints = new List<(OVRSkeleton.BoneId boneId, string name)>
            {
                (OVRSkeleton.BoneId.Body_Hips, "HIPS"),
                (OVRSkeleton.BoneId.Body_Head, "HEAD")
            };

            BodyPositions.Clear();
            foreach (var joint in targetJoints)
            {
                var bone = skeleton.Bones.FirstOrDefault(b => b.Id == joint.boneId);
                if (bone != null && bone.Transform != null)
                {
                    Vector3 position = bone.Transform.position;
                    BodyPositions.Add((joint.name, position));
                    Debug.Log($"[BODY_JOINT_{joint.name}] POSITION = ({position.x:F2}, {position.y:F2}, {position.z:F2})");
                }
                else
                {
                    Debug.LogWarning($"Bone not found for {joint.name}");
                }
            }

            var jointPositions = new Dictionary<string, Vector3>();
            foreach (var joint in targetJoints)
            {
                var bone = skeleton.Bones.FirstOrDefault(b => b.Id == joint.boneId);
                if (bone != null && bone.Transform != null)
                {
                    Vector3 position = bone.Transform.position;
                    jointPositions[joint.name] = position;

                    if (drawMeshes)
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
                        jointSphere.transform.position = position;
                    }
                }
            }

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

            if (bodyJoints != null)
                Destroy(bodyJoints);
            if (bodyLines != null)
                Destroy(bodyLines);
        }
    }
}