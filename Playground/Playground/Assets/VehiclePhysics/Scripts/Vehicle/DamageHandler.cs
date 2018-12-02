using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace NWH.VehiclePhysics
{
    /// <summary>
    /// Handles all damage related calculations and mesh deformations.
    /// Collision sounds are handled by CrashComponent class.
    /// </summary>
    [System.Serializable]
    public class DamageHandler
    {
        /// <summary>
        /// Contains data on the collision that has last happened.
        /// </summary>
        public class CollisionEvent
        {
            /// <summary>
            /// Queue of mesh filter components that are waiting for deformation. 
            /// Some of the meshes might be queued for checking even if not deformed.
            /// </summary>
            public Queue<MeshFilter> deformationQueue = new Queue<MeshFilter>();
            /// <summary>
            /// Collision data for the collision event.
            /// </summary>
            public Collision collision;
            /// <summary>
            /// Magnitude of the decekeration vector at the moment of impact.
            /// </summary>
            public float decelerationMagnitude;
        }

        /// <summary>
        /// Determines if damage, mesh deformation and performance degradation will be used.
        /// </summary>
        [Tooltip("Determines if damage, mesh deformation and performance degradation will be used.")]
        public bool enabled = false;

        /// <summary>
        /// Should damage affect vehicle performance (steering, power, etc.)?
        /// </summary>
        [Tooltip("Should damage affect vehicle performance (steering, power, etc.)?")]
        public bool performanceDegradation = false;

        /// <summary>
        /// Maximum allowed damage before the vehicle breaks down. Performance will decline as damage is nearing allowed damage.
        /// </summary>
        [Tooltip("Maximum allowed damage before the vehicle breaks down. Performance will decline as damage is nearing allowed damage.")]
        public float allowedDamage = 50000;

        /// <summary>
        /// Number of vertices that will be checked and eventually deformed per frame.
        /// </summary>
        [Tooltip("Number of vertices that will be checked and eventually deformed per frame. Setting it to lower values will reduce or remove frame drops but will" +
            " induce lag into mesh deformation as vehicle will be deformed over longer time span.")]
        public int deformationVerticesPerFrame = 8000;

        /// <summary>
        /// Radius is which vertices will be deformed.
        /// </summary>
        [Tooltip("Radius is which vertices will be deformed.")]
        [Range(0, 2)]
        public float deformationRadius = 0.6f;

        /// <summary>
        /// Determines how much vertices will be deformed for given collision strength.
        /// </summary>
        [Tooltip("Determines how much vertices will be deformed for given collision strength.")]
        [Range(0.1f, 5f)]
        public float deformationStrength = 1.6f;

        /// <summary>
        /// Adds noise to the mesh deformation. 0 will result in smooth mesh.
        /// </summary>
        [Tooltip("Adds noise to the mesh deformation. 0 will result in smooth mesh.")]
        [Range(0.001f, 0.5f)]
        public float deformationRandomness = 0.1f;

        /// <summary>
        /// Deceleration magnitude needed to trigger damage.
        /// </summary>
        [Tooltip("Deceleration magnitude needed to trigger damage.")]
        public float decelerationThreshold = 30f;

        /// <summary>
        /// Collisions with the objects that have a tag that is on this list will be ignored.
        /// Collision state will be changed but no processing will happen.
        /// </summary>
        [Tooltip("Collisions with the objects that have a tag that is on this list will be ignored.")]
        public List<string> ignoreTags = new List<string>();

        /// <summary>
        /// Hash of the previous queued collision. Prevents reacting to the same collision twice since collision is called during OnCollisionStay() so more data can be collected.
        /// </summary>        
        [HideInInspector]
        public int previousCollisionHash;

        private float damage;

        private List<MeshFilter> deformableMeshFilters = new List<MeshFilter>();
        private List<Mesh> originalMeshes = new List<Mesh>();
        private Queue<CollisionEvent> collisionEvents = new Queue<CollisionEvent>();
        private VehicleController vc;

        /// <summary>
        /// Current vehicle damage.
        /// </summary>
        public float Damage
        {
            get
            {
                if (enabled)
                    return damage;
                else
                    return 0;
            }
            set
            {
                damage = Mathf.Abs(value);
            }
        }

        /// <summary>
        /// Current vehicle damage. Percentage from allowed damage.
        /// </summary>
        public float DamagePercent
        {
            get
            {
                if (enabled)
                    return Mathf.Clamp01(damage / allowedDamage);
                else
                    return 0;
            }
        }

        public void Initialize(VehicleController vc)
        {
            this.vc = vc;
            
            // Find all mesh filters of the vehicle
            MeshFilter[] mfs = vc.transform.GetComponentsInChildren<MeshFilter>();
            foreach(MeshFilter mf in mfs)
            {
                if (!deformableMeshFilters.Contains(mf))
                {
                    deformableMeshFilters.Add(mf);
                    originalMeshes.Add(mf.sharedMesh);
                }
            }
        }

        public void Update()
        {
            if(collisionEvents.Count != 0)
            {
                CollisionEvent ce = collisionEvents.Peek();

                if (ce.deformationQueue.Count == 0)
                {
                    collisionEvents.Dequeue();
                    if (collisionEvents.Count != 0)
                        ce = collisionEvents.Peek();
                    else
                        return;
                }

                int vertexCount = 0;
                while(vertexCount < deformationVerticesPerFrame && ce.deformationQueue.Count > 0)
                {
                    MeshFilter mf = ce.deformationQueue.Dequeue();
                    vertexCount += mf.mesh.vertexCount;
                    MeshDeform(ce, mf);
                }

                if(DamagePercent >= 1)
                {
                    vc.engine.Stop();
                }
            }
        }

        /// <summary>
        /// Returns meshes to their original states.
        /// </summary>
        public void Repair()
        {
            for(int i = 0; i < deformableMeshFilters.Count; i++)
            {
                if(originalMeshes[i] != null) 
                    deformableMeshFilters[i].mesh = originalMeshes[i];
            }
            damage = 0;
        }

        /// <summary>
        /// Add collision to the queue of collisions waiting to be processed.
        /// </summary>
        public void Enqueue(Collision collision, float accelerationMagnitude)
        {
            foreach(string tag in ignoreTags)
            {
                if(collision.collider.gameObject.CompareTag(tag))
                {
                    return;
                }
            }

            CollisionEvent collisionEvent = new CollisionEvent();
            collisionEvent.collision = collision;
            collisionEvent.decelerationMagnitude = accelerationMagnitude;

            vc.damage.damage += accelerationMagnitude;

            Vector3 collisionPoint = AverageCollisionPoint(collision.contacts);
            //Vector3 direction = Vector3.Normalize(vc.transform.position - collisionPoint);

            foreach(MeshFilter deformableMeshFilter in deformableMeshFilters)
            {
                //Ray crashRay = new Ray(deformableMeshFilter.transform.InverseTransformPoint(collisionPoint), deformableMeshFilter.transform.InverseTransformDirection(direction));

                // Check if mesh is near collision point
                //float rayDistance = Mathf.Infinity;

                //if (deformableMeshFilter.mesh.bounds.Contains(deformableMeshFilter.transform.InverseTransformPoint(collisionPoint))
                //    || deformableMeshFilter.mesh.bounds.IntersectRay(crashRay, out rayDistance))
                //{
                    // Deform mesh only if not wheel
                    if (deformableMeshFilter.gameObject.tag != "Wheel")
                    {
                        //Debug.Log("Enqueue " + deformableMeshFilter.name);
                        collisionEvent.deformationQueue.Enqueue(deformableMeshFilter);
                    }
                    // If crash happened around wheel do not deform it but rather detoriate it's handling
                    else
                    {
                        foreach (Wheel wheel in vc.Wheels)
                        {
                            if (Vector3.Distance(collisionPoint, wheel.VisualTransform.position) < wheel.Radius * 1.2f)
                            {
                                wheel.Damage += accelerationMagnitude;
                            }
                        }
                    }
                //}
            }
            collisionEvents.Enqueue(collisionEvent);
        }

        /// <summary>
        /// Deforms a mesh using data from collision event.
        /// </summary>
        public void MeshDeform(CollisionEvent collisionEvent, MeshFilter deformableMeshFilter)
        {
            //Debug.Log("Deforming " + deformableMeshFilter.name);
            Vector3 collisionPoint = AverageCollisionPoint(collisionEvent.collision.contacts);
            //Vector3 direction = AverageCollisionNormal(collisionEvent.collision.contacts);
            Vector3 direction = Vector3.Normalize(deformableMeshFilter.transform.position - collisionPoint);

            float xDot = Mathf.Abs(Vector3.Dot(direction, Vector3.right));
            float yDot = Mathf.Abs(Vector3.Dot(direction, Vector3.up));
            float zDot = Mathf.Abs(Vector3.Dot(direction, Vector3.forward));

            float vertexDistanceThreshold = Mathf.Clamp((collisionEvent.decelerationMagnitude * deformationStrength) / (1000f), 0f, deformationRadius);

            Vector3[] vertices = deformableMeshFilter.mesh.vertices;

            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 globalVertex = deformableMeshFilter.transform.TransformPoint(vertices[i]);

                //float distance = Vector3.Distance(collisionPoint, globalVertex);
                float distance = Mathf.Sqrt(
                    (collisionPoint.x - globalVertex.x) * (collisionPoint.x - globalVertex.x) * xDot
                    + (collisionPoint.z - globalVertex.z) * (collisionPoint.z - globalVertex.z) * zDot
                    + (collisionPoint.y - globalVertex.y) * (collisionPoint.y - globalVertex.y) * yDot);               

                distance *= Random.Range(1f - deformationRandomness, 1f + deformationRandomness);

                if (distance < vertexDistanceThreshold)
                {
                    globalVertex = globalVertex + direction * (vertexDistanceThreshold - distance);
                    vertices[i] = deformableMeshFilter.transform.InverseTransformPoint(globalVertex);
                }
            }

            deformableMeshFilter.mesh.vertices = vertices;
            deformableMeshFilter.mesh.RecalculateNormals();
            deformableMeshFilter.mesh.RecalculateTangents();
        }

        /// <summary>
        /// Calculates average collision point from a list of contact points.
        /// </summary>
        private static Vector3 AverageCollisionPoint(ContactPoint[] contacts)
        {
            Vector3[] points = new Vector3[contacts.Length];
            for (int i = 0; i < contacts.Length; i++)
            {
                points[i] = contacts[i].point;
            }
            return AveragePoint(points);
        }

        /// <summary>
        /// Calculates average collision normal from a list of contact points.
        /// </summary>
        private static Vector3 AverageCollisionNormal(ContactPoint[] contacts)
        {
            Vector3[] points = new Vector3[contacts.Length];
            for (int i = 0; i < contacts.Length; i++)
            {
                points[i] = contacts[i].normal;
            }
            return AveragePoint(points);
        }

        /// <summary>
        /// Calculates average from multiple vectors.
        /// </summary>
        private static Vector3 AveragePoint(Vector3[] points)
        {
            Vector3 sum = Vector3.zero;
            for (int i = 0; i < points.Length; i++)
            {
                sum += points[i];
            }
            return sum / points.Length;
        }
    }
}

