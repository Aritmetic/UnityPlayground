using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Generates skidmark meshes.
    /// </summary>
    [System.Serializable]
    public class SkidmarkGenerator
    {
        // Current number of skid meshes (snapshots).
        private int snapshotCount = 0;

        // Max number of rectangles (two tris) per one snapshot.
        public int maxMarks = 512;

        public int maxTris;

        // Width of the skidmark.
        public float markWidth = 0.35f;

        // Height above ground at which skidmarks will be drawn
        public float groundOffset = 0.014f;

        // Distance traveled needed to generate one rectangle
        public float minSqrDistance;

        // Index of the surface the wheel is on.
        private int groundIndex;
        private int prevGroundIndex;

        // Is the wheel grounded?
        private bool isGrounded = false;
        private bool wasGrounded = false;

        private float intensity;
        private float prevIntensity;

        private VehicleController vc;
        public Wheel wheel;
        private Ray ray;

        /// <summary>
        /// One section (rectangle) of the skidmark.
        /// </summary>
        private class SkidmarkSection
        {
            public SkidmarkSection() { }

            public SkidmarkSection(SkidmarkSection ss)
            {
                position = ss.position;
                normal = ss.normal;
                tangent = ss.tangent;
                positionLeft = ss.positionLeft;
                positionRight = ss.positionRight;
                intensity = ss.intensity;
            }

            public Vector3 position = Vector3.zero;
            public Vector3 normal = Vector3.zero;
            public Vector4 tangent = Vector4.zero;
            public Vector3 positionLeft = Vector3.zero;
            public Vector3 positionRight = Vector3.zero;
            public byte intensity;
        }

        private SkidmarkSection currentSkidmarkSection = null;
        private SkidmarkSection previousSkidmarkSection = null;

        private class Index
        {
            public int head;
            public int tail;
        }

        private Index[] indices;
        private int[] lastUpdated;
        private int[] triangleCounts;
        private int commonIndex;

        private GameObject skidObject;
        private GameObject skidContainer;
        private Mesh skidmarkMesh;
        private MeshRenderer meshRenderer;
        private MeshFilter meshFilter;

        private Vector3[] vertices;
        private Vector3[] normals;
        private Vector4[] tangents;
        private Color32[] colors;
        private Vector2[] uvs;
        private int[][] triangles;

        private List<Material> materials = new List<Material>();

        private int groundEntityCount;

        // GC Avoidal
        Bounds bounds = new Bounds(Vector3.zero, Vector3.one * 100000);
        private Vector2 vector00 = new Vector2(0, 0);
        private Vector2 vector01 = new Vector2(0, 1);
        private Vector2 vector10 = new Vector2(1, 0);
        private Vector2 vector11 = new Vector2(1, 1);
        private Color32 color0000 = new Color32(0, 0, 0, 0);
        private int[] result;
        private Vector3 direction, xDirection;
        //private RaycastHit hit;
        private List<int> A;
        private List<int> idx;

        public void CreateNewSnapshot(bool generateNew = false)
        {
            indices = new Index[groundEntityCount];
            lastUpdated = new int[groundEntityCount];
            triangleCounts = new int[groundEntityCount];

            for (int i = 0; i < groundEntityCount; i++)
            {
                indices[i] = new Index();
                lastUpdated[i] = -1;
                triangleCounts[i] = 0;
            }

            // Add skid container
            if(snapshotCount == 0 || generateNew)
            {
                skidContainer = GameObject.Find("SkidContainer");
                if(skidContainer == null)
                {
                    skidContainer = new GameObject("SkidContainer");
                }
            }
            else
            {
                skidContainer.isStatic = true;
                skidObject.isStatic = true;
            }

            // Add skid object
            skidObject = new GameObject("SkidMesh_" + vc.gameObject.name + "_" + snapshotCount);
            skidObject.transform.parent = skidContainer.transform;
            skidObject.transform.position = vc.transform.position;

            SkidmarkDestroy skidmarkDestroy = skidObject.AddComponent<SkidmarkDestroy>();
            skidmarkDestroy.parentVehicleController = vc;
            skidmarkDestroy.distanceThreshold = vc.effects.skidmarks.persistentSkidmarkDistance;

            // Setup mesh renderer
            if(!skidObject.GetComponent<MeshRenderer>())
                meshRenderer = skidObject.AddComponent<MeshRenderer>();

            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;

            // Add mesh filter
            meshFilter = skidObject.AddComponent<MeshFilter>();

            if (snapshotCount == 0)
            {
                // Add ground materials (only once)
                foreach (GroundDetection.GroundEntity ge in vc.groundDetection.groundEntities)
                {
                    materials.Add(ge.skidmarkMaterial);
                }
            }

            meshRenderer.materials = materials.ToArray();

            foreach(Material mat in meshRenderer.materials)
            {
                if (mat == null) continue;

                if (vc.effects.skidmarks.threadAlbedo != null)
                {
                    mat.SetTexture("_MainTex", vc.effects.skidmarks.threadAlbedo);
                }
                try
                {
                    if (vc.effects.skidmarks.threadBump != null)
                    {
                        mat.SetTexture("_ParallaxMap", vc.effects.skidmarks.threadBump);
                    }

                    if (vc.effects.skidmarks.threadNormal != null)
                    {
                        mat.SetTexture("_BumpMap", vc.effects.skidmarks.threadNormal);
                    }
                }
                catch
                { }
            }

            // Add mesh arrays
            vertices = new Vector3[maxMarks * 4 * groundEntityCount];
            normals = new Vector3[maxMarks * 4 * groundEntityCount];
            tangents = new Vector4[maxMarks * 4 * groundEntityCount];
            colors = new Color32[maxMarks * 4 * groundEntityCount];
            uvs = new Vector2[maxMarks * 4 * groundEntityCount];

            triangles = new int[groundEntityCount][];
            for (int i = 0; i < groundEntityCount; i++)
                triangles[i] = new int[maxMarks * 9];

            // Create new mesh
            skidmarkMesh = new Mesh();
            skidmarkMesh.bounds = bounds;
            skidmarkMesh.MarkDynamic();
            skidmarkMesh.name = "SkidmarkMesh";
            skidmarkMesh.subMeshCount = groundEntityCount;
            meshFilter.mesh = skidmarkMesh;

            previousSkidmarkSection = null;
            currentSkidmarkSection = null;

            snapshotCount++;
        }

        public void Initialize(VehicleController vc, Wheel wheel)
        {
            maxTris = maxMarks * 6;

            this.vc = vc;
            this.wheel = wheel;

            // Calculate common variables
            markWidth = wheel.Width;
            minSqrDistance = vc.effects.skidmarks.minDistance * vc.effects.skidmarks.minDistance;
            groundEntityCount = vc.groundDetection.groundEntities.Count;

            CreateNewSnapshot();
        }


        public void Update()
        {
            if(skidObject == null)
            {
                CreateNewSnapshot(true);
                return;
            }

            prevGroundIndex = groundIndex;
            groundIndex = GetGroundIndex();

            wasGrounded = isGrounded;
            isGrounded = wheel.IsGrounded;

            // Calculate skidmark intensity on hard surfaces (asphalt, concrete, etc.)
            if (isGrounded && groundIndex >= 0 && skidObject != null && vc.groundDetection.groundEntities[groundIndex].skidmarkMaterial != null)
            {
                prevIntensity = intensity;
                intensity = 0f;
                if (wheel.HasForwardSlip || wheel.HasSideSlip)
                {
                    intensity = Mathf.Clamp01(Mathf.Abs(wheel.ForwardSlip) - vc.forwardSlipThreshold)
                        + Mathf.Clamp01(Mathf.Abs(wheel.SideSlip) - vc.sideSlipThreshold) * 2.5f;
                    intensity *= vc.effects.skidmarks.skidmarkStrength;
                    intensity = Mathf.Clamp(intensity, 0f, vc.effects.skidmarks.maxSkidmarkAlpha);
                }

                if (wheel.WheelController.isGrounded)
                {
                    Vector3 skidmarkPosition = skidObject.transform.InverseTransformPoint(wheel.WheelController.wheelHit.groundPoint);
                    skidmarkPosition += wheel.WheelController.wheelHit.normal * groundOffset;
                    skidmarkPosition += vc.vehicleRigidbody.GetPointVelocity(wheel.ControllerTransform.position) * Time.deltaTime * 1.8f;

                    // Add first skidmark in series
                    if (currentSkidmarkSection == null)
                    {
                        currentSkidmarkSection = new SkidmarkSection();
                        currentSkidmarkSection.position = skidmarkPosition;
                        currentSkidmarkSection.normal = wheel.WheelController.wheelHit.normal;
                        currentSkidmarkSection.intensity = 0;
                        currentSkidmarkSection.positionLeft = skidmarkPosition - wheel.ControllerTransform.right * markWidth * 0.5f;
                        currentSkidmarkSection.positionRight = skidmarkPosition + wheel.ControllerTransform.right * markWidth * 0.5f;

                        direction = wheel.ControllerTransform.forward;
                        xDirection = -wheel.ControllerTransform.right;
                        currentSkidmarkSection.tangent = new Vector4(xDirection.x, xDirection.y, xDirection.y, 1f);

                        previousSkidmarkSection = new SkidmarkSection(currentSkidmarkSection);

                        UpdateCircularIndex();
                    }
                    // First skidmark in series is already set, continue building mesh
                    else
                    {
                        currentSkidmarkSection.position = skidmarkPosition;
                        currentSkidmarkSection.normal = wheel.WheelController.wheelHit.normal;
                        currentSkidmarkSection.intensity = (byte)(intensity * 255f);

                        direction = (currentSkidmarkSection.position - previousSkidmarkSection.position);
                        xDirection = Vector3.Cross(direction, wheel.WheelController.wheelHit.normal).normalized;

                        currentSkidmarkSection.positionLeft = skidmarkPosition + xDirection * markWidth * 0.5f;
                        currentSkidmarkSection.positionRight = skidmarkPosition - xDirection * markWidth * 0.5f;
                        currentSkidmarkSection.tangent = new Vector4(xDirection.x, xDirection.y, xDirection.z, 1f);

                        // Calculate distance between last and current skidmark section
                        float sqrDistance = (currentSkidmarkSection.position - previousSkidmarkSection.position).sqrMagnitude;

                        // Ground has changed, start new section
                        if(prevGroundIndex != groundIndex 
                            || (isGrounded == true && wasGrounded == false)
                            || (prevIntensity == 0 && intensity > 0))
                        {
                            previousSkidmarkSection = new SkidmarkSection(currentSkidmarkSection);
                        }

                        if (sqrDistance > minSqrDistance)
                        {
                            int groundIndex = vc.groundDetection.GetCurrentGroundEntityIndex(wheel.WheelController);

                            if (groundIndex >= 0)
                            {
                                int indexOffset = commonIndex * 4;

                                // Generate geometry.

                                vertices[indexOffset + 0] = previousSkidmarkSection.positionLeft;
                                vertices[indexOffset + 1] = previousSkidmarkSection.positionRight;
                                vertices[indexOffset + 2] = currentSkidmarkSection.positionLeft;
                                vertices[indexOffset + 3] = currentSkidmarkSection.positionRight;

                                normals[indexOffset + 0] = previousSkidmarkSection.normal;
                                normals[indexOffset + 1] = previousSkidmarkSection.normal;
                                normals[indexOffset + 2] = currentSkidmarkSection.normal;
                                normals[indexOffset + 3] = currentSkidmarkSection.normal;

                                tangents[indexOffset + 0] = previousSkidmarkSection.tangent;
                                tangents[indexOffset + 1] = previousSkidmarkSection.tangent;
                                tangents[indexOffset + 2] = currentSkidmarkSection.tangent;
                                tangents[indexOffset + 3] = currentSkidmarkSection.tangent;

                                color0000.a = previousSkidmarkSection.intensity;
                                colors[indexOffset + 0] = color0000;
                                colors[indexOffset + 1] = color0000;

                                color0000.a = currentSkidmarkSection.intensity;
                                colors[indexOffset + 2] = color0000;
                                colors[indexOffset + 3] = color0000;

                                uvs[indexOffset + 0] = vector00;
                                uvs[indexOffset + 1] = vector10;
                                uvs[indexOffset + 2] = vector01;
                                uvs[indexOffset + 3] = vector11;

                                int triCount = indices[groundIndex].head;
                                triangles[groundIndex][triCount + 0] = commonIndex * 4 + 0;
                                triangles[groundIndex][triCount + 2] = commonIndex * 4 + 1;
                                triangles[groundIndex][triCount + 1] = commonIndex * 4 + 2;

                                triangles[groundIndex][triCount + 3] = commonIndex * 4 + 2;
                                triangles[groundIndex][triCount + 5] = commonIndex * 4 + 1;
                                triangles[groundIndex][triCount + 4] = commonIndex * 4 + 3;

                                // Reassign the mesh if it's changed this frame
                                skidmarkMesh.vertices = vertices;
                                skidmarkMesh.normals = normals;
                                skidmarkMesh.tangents = tangents;

                                for (int i = 0; i < triangles.Length; i++)
                                {
                                    // Tail is chasing head
                                    if (indices[i].head > indices[i].tail)
                                    {
                                        int length = indices[i].head - indices[i].tail;
                                        SubArray(ref triangles[i], out result, indices[i].head - length, length);
                                        skidmarkMesh.SetTriangles(result, i);
                                    }
                                    // Head is chasing tail
                                    else if (indices[i].head < indices[i].tail)
                                    {
                                        int index1 = indices[i].tail;
                                        int length1 = maxMarks * 9 - indices[i].tail;

                                        int index2 = 0;
                                        int length2 = indices[i].head;

                                        DoubleSubArray(ref triangles[i], out result, index1, index2, length1, length2);
                                        skidmarkMesh.SetTriangles(result, i);
                                    }
                                }

                                // Assign to mesh
                                skidmarkMesh.colors32 = colors;
                                skidmarkMesh.uv = uvs;
                                skidmarkMesh.bounds = bounds;

                                meshFilter.mesh = skidmarkMesh;
                                previousSkidmarkSection = new SkidmarkSection(currentSkidmarkSection);
                                UpdateCircularIndex();
                            }
                        }
                    }
                }
            }
        }

        void UpdateCircularIndex()
        {
            // Update head and tail indices
            indices[groundIndex].head += 6;
            lastUpdated[groundIndex] = Time.frameCount;

            if (indices[groundIndex].head >= maxMarks * 9)
                indices[groundIndex].head = indices[groundIndex].head - maxMarks * 9;

            // Too many triangles, move oldest tail
            // Sort index array by age
            A = lastUpdated.ToList();

            var sorted = A
                .Select((x, i) => new KeyValuePair<int, int>(x, i))
                .OrderBy(x => x.Key)
                .ToList();

            idx = sorted.Select(x => x.Value).ToList();

            bool endSnapshot = false;
            for(int i = 0; i < idx.Count; i++)
            {
                // Move the tail of the oldest skid section
                if (indices[idx[i]].head != indices[idx[i]].tail && (GetTotalTriangleCount() >= maxTris || GetTriangleCount(idx[i]) >= maxTris))
                {
                    if (vc.effects.skidmarks.persistentSkidmarks)
                    {
                        CreateNewSnapshot();
                        endSnapshot = true;
                        break;
                    }

                    int prevTail = indices[idx[i]].tail;
                    indices[idx[i]].tail += 6;

                    if (indices[idx[i]].tail >= maxMarks * 9)
                        indices[idx[i]].tail = indices[idx[i]].tail - maxMarks * 9;

                    if (prevTail < indices[idx[i]].head && indices[idx[i]].tail > indices[idx[i]].head)
                        indices[idx[i]].tail = indices[idx[i]].head;
                }
            }

            // Don't update index this frame if new snapshot created
            if (endSnapshot)
                return;

            // Update common index (surface independent)
            commonIndex++;
            if (commonIndex >= maxMarks * groundEntityCount)
            {
                commonIndex = 0;
            }
        }


        int GetTotalTriangleCount()
        {   
                int sum = 0;
                for(int i = 0; i < triangleCounts.Length; i++)
                {
                    sum += GetTriangleCount(i);
                }
                return sum;
        }


        int GetTriangleCount(int i)
        {
            // Update triangle counts
            // Tail is chasing head
            if (indices[i].head > indices[i].tail)
            {
                triangleCounts[i] = indices[i].head - indices[i].tail;
            }
            // Overflow
            else if (indices[i].head < indices[i].tail)
            {
                triangleCounts[i] = (maxMarks * 9 - indices[i].tail) + indices[i].head;
            }
            // Head is equal to tail, no triangles
            else
            {
                triangleCounts[i] = 0;
            }
            return triangleCounts[i];
        }

        int GetGroundIndex()
        {
            int groundIndex = vc.groundDetection.GetCurrentGroundEntityIndex(wheel.WheelController);
            return groundIndex;
        }

        public void SubArray(ref int[] data, out int[] result, int index, int length)
        {
            result = new int[length];
            Array.Copy(data, index, result, 0, length);
        }

        public void DoubleSubArray(ref int[] data, out int[] result, int index1, int index2, int length1, int length2)
        {
            result = new int[length1 + length2];
            Array.Copy(data, index1, result, 0, length1);
            Array.Copy(data, index2, result, length1, length2);
        }
    }
}

