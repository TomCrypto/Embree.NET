// disable checks: optimal performance but
// may lead to segmentation fault on error
#define EMBREE_CHECKING

using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Embree
{
    #region Geometry Interop Layer

    /// <summary>
    /// Your vector class should implement this interface.
    /// </summary>
    public interface IEmbreeVector
    {
        float X { get; }
        float Y { get; }
        float Z { get; }
    }

    /// <summary>
    /// Your point class should implement this interface.
    /// </summary>
    public interface IEmbreePoint
    {
        float X { get; }
        float Y { get; }
        float Z { get; }
    }

    /// <summary>
    /// Your ray class should implement this interface.
    /// </summary>
    public interface IEmbreeRay
    {
        /// <summary>
        /// Gets the origin of the ray.
        /// </summary>
        IEmbreePoint Origin { get; }

        /// <summary>
        /// Gets the (normalized) direction of the ray.
        /// </summary>
        IEmbreeVector Direction { get; }
    }

    /// <summary>
    /// Your matrix class should implement this interface.
    /// </summary>
    public interface IEmbreeMatrix
    {
        /// <summary>
        /// Gets the x-axis basis vector.
        /// </summary>
        IEmbreeVector U { get; }

        /// <summary>
        /// Gets the y-axis basis vector.
        /// </summary>
        IEmbreeVector V { get; }

        /// <summary>
        /// Gets the z-axis basis vector.
        /// </summary>
        IEmbreeVector W { get; }

        /// <summary>
        /// Gets the translation vector.
        /// </summary>
        IEmbreeVector T { get; }
    }

    #endregion

    #region Enumerations

    /// <summary>
    /// Scene utilization flags.
    /// </summary>
    [Flags]
    public enum SceneFlags
    {
        /// <summary>
        /// The scene will be static.
        /// </summary>
        Static = 0 << 0,

        /// <summary>
        /// The scene will be dynamic.
        /// </summary>
        Dynamic = 1 << 0,

        /// <summary>
        /// Optimize for memory usage.
        /// </summary>
        Compact = 1 << 8,

        /// <summary>
        /// Optimize for coherent rays.
        /// </summary>
        Coherent = 1 << 9,

        /// <summary>
        /// Optimize for incoherent rays.
        /// </summary>
        Incoherent = 1 << 10,

        /// <summary>
        /// Optimize for quality.
        /// </summary>
        HighQuality = 1 << 11,

        /// <summary>
        /// Optimize for robustness.
        /// </summary>
        Robust = 1 << 16
    }

    /// <summary>
    /// Traversal flags.
    /// </summary>
    [Flags]
    public enum TraversalFlags
    {
        /// <summary>
        /// Enable single-ray traversal.
        /// </summary>
        Single = 1 << 0,

        /// <summary>
        /// Enable 4-ray packet traversal.
        /// </summary>
        Packet4 = 1 << 1,

        /// <summary>
        /// Enable 8-ray packet traversal.
        /// </summary>
        Packet8 = 1 << 2,

        /// <summary>
        /// Enable 16-ray packet traversal.
        /// </summary>
        Packet16 = 1 << 3,
    }

    /// <summary>
    /// Mesh handling flags.
    /// </summary>
    [Flags]
    public enum MeshFlags
    {
        /// <summary>
        /// The mesh is static.
        /// </summary>
        Static = 0,

        /// <summary>
        /// The mesh is deformable.
        /// </summary>
        Deformable = 1,

        /// <summary>
        /// The mesh is dynamic.
        /// </summary>
        Dynamic = 2
    }

    #endregion

    #region Ray Tracing Structures

    /// <summary>
    /// Defines a ray-scene traversal.
    /// </summary>
    public struct Traversal
    {
        private readonly float time, near, far;
        private readonly IEmbreeRay ray;
        private readonly bool active;

        /// <summary>
        /// Gets the traversal time (between 0 and 1).
        /// </summary>
        public float Time { get { return time; } }

        /// <summary>
        /// Gets the near traversal bound.
        /// </summary>
        public float Near { get { return near; } }

        /// <summary>
        /// Gets the far traversal bound.
        /// </summary>
        public float Far { get { return far; } }

        /// <summary>
        /// Gets the ray to traverse the scene with.
        /// </summary>
        public IEmbreeRay Ray { get { return ray; } }

        /// <summary>
        /// Gets whether this traversal is active.
        /// </summary>
        public bool Active { get { return active; } }

        /// <summary>
        /// Creates a new traversal from a ray, near/far planes, and a traversal time.
        /// </summary>
        public Traversal(IEmbreeRay ray, float near = 0, float far = float.PositiveInfinity, float time = 0, bool active = true)
        {
            if (near < 0)
                throw new ArgumentOutOfRangeException("Near bound must be nonnegative");

            if (time < 0 || time > 1)
                throw new ArgumentOutOfRangeException("Time must be between zero and one");

            this.active = active;
            this.time   = time;
            this.near   = near;
            this.far    = far;
            this.ray    = ray;
        }

        /// <summary>
        /// Creates a new inactive traversal (will be ignored).
        /// </summary>
        public static Traversal Inactive
        {
            get { return new Traversal(default(IEmbreeRay), 0, float.PositiveInfinity, 0, false); }
        }
    }

    public struct Intersection<T> where T : IInstance
    {
        private readonly float tfar, u, v, nx, ny, nz;
        private readonly IMesh geomID;
        private readonly uint primID;
        private readonly T instID;

        /// <summary>
        /// Gets the intersection distance from the ray origin.
        /// </summary>
        public float Distance { get { return tfar; } }

        /// <summary>
        /// Gets the index of the primitive intersected (e.g. nth triangle).
        /// </summary>
        public uint Primitive { get { return primID; } }

        /// <summary>
        /// Gets the identifier of the instance intersected.
        /// </summary>
        public T Instance { get { return instID; } }

        /// <summary>
        /// Gets the identifier of the mesh intersected.
        /// </summary>
        public IMesh Mesh { get { return geomID; } }

        /// <summary>
        /// Gets the barycentric u-coordinate of the intersection point.
        /// </summary>
        public float U { get { return u; } }

        /// <summary>
        /// Gets the barycentric v-coordinate of the intersection point.
        /// </summary>
        public float V { get { return v; } }

        /// <summary>
        /// Gets the x-coordinate of the raw surface normal.
        /// </summary>
        public float NX { get { return nx; } }

        /// <summary>
        /// Gets the y-coordinate of the raw surface normal.
        /// </summary>
        public float NY { get { return ny; } }

        /// <summary>
        /// Gets the z-coordinate of the raw surface normal.
        /// </summary>
        public float NZ { get { return nz; } }

        /// <summary>
        /// Gets whether there exists a ray-scene intersection.
        /// </summary>
        /// <remarks>
        /// If this is false, all other fields are undefined.
        /// </remarks>
        public Boolean HasHit { get { return geomID != null; } }

        /// <summary>
        /// Creates a new intersection.
        /// </summary>
        public Intersection(uint primID, IMesh geomID, T instID, float tfar, float u, float v, float nx, float ny, float nz)
        {
            this.primID = primID;
            this.geomID = geomID;
            this.instID = instID;
            this.tfar   = tfar;
            this.u      = u;
            this.v      = v;
            this.nx     = nx;
            this.ny     = ny;
            this.nz     = nz;
        }

        /// <summary>
        /// Represents no intersection.
        /// </summary>
        public static Intersection<T> None
        {
            get { return default(Intersection<T>); }
        }
    }

    #endregion

    #region Scene API

    /// <summary>
    /// All mesh types implement this interface.
    /// </summary>
    public interface IMesh
    {
        /// <summary>
        /// Adds this mesh to an existing scene.
        /// </summary>
        /// <remarks>
        /// Do not call this method directly.
        /// </remarks>
        uint Add(IntPtr scene);

        /// <summary>
        /// Updates the mesh data for a specific scene.
        /// </summary>
        /// <remarks>
        /// Do not call this method directly.
        /// </remarks>
        void Update(uint id, IntPtr scene);
    }

    /// <summary>
    /// Represents a collection of non-instanced meshes.
    /// </summary>
    public class Geometry : IDisposable, IEnumerable<IMesh>
    {
        private TraversalFlags traversalFlags;
        private SceneFlags sceneFlags;
        private IntPtr scenePtr;

        /// <summary>
        /// The list of meshes currently in this collection.
        /// </summary>
        private Dictionary<uint, IMesh> meshes = new Dictionary<uint, IMesh>();

        /// <summary>
        /// Creates a new empty geometry.
        /// </summary>
        public Geometry(SceneFlags sceneFlags, TraversalFlags traversalFlags = TraversalFlags.Single)
        {
            RTC.Register();
            this.sceneFlags = sceneFlags;
            this.traversalFlags = traversalFlags;
            scenePtr = RTC.NewScene(sceneFlags, traversalFlags);
        }

        #region Collection Methods

        /// <summary>
        /// Adds a mesh to the geometry.
        /// </summary>
        public void Add(IMesh mesh)
        {
            var meshID = mesh.Add(scenePtr);
            RTC.CheckLastError();

            meshes.Add(meshID, mesh);
        }

        /// <summary>
        /// Removes a mesh from the geometry.
        /// </summary>
        public void Remove(IMesh mesh)
        {
            if (!meshes.ContainsValue(mesh))
                throw new ArgumentException("No such mesh exists");
            else
            {
                var meshID = meshes.First(x => x.Value == mesh).Key;
                RTC.DeleteGeometry(scenePtr, meshID);
                RTC.CheckLastError();
                meshes.Remove(meshID);
            }
        }

        /// <summary>
        /// Gets the number of meshes.
        /// </summary>
        public int Count { get { return meshes.Count; } }

        #endregion

        #region Internal Members

        /// <summary>
        /// Commits all meshes in this geometry.
        /// </summary>
        internal void Commit()
        {
            foreach (var entry in meshes)
            {
                entry.Value.Update(entry.Key, scenePtr);
                RTC.CheckLastError();

                if (sceneFlags.HasFlag(SceneFlags.Dynamic))
                {
                    RTC.UpdateGeometry(scenePtr, entry.Key);
                    RTC.CheckLastError();
                }
            }

            RTC.Commit(scenePtr);
            RTC.CheckLastError();
        }

        /// <summary>
        /// Gets the Embree scene pointer.
        /// </summary>
        internal IntPtr EmbreePointer { get { return scenePtr; } }

        /// <summary>
        /// Gets the traversal flags selected.
        /// </summary>
        /// <remarks>
        /// Used for ensuring these flags are consistent.
        /// </remarks>
        internal TraversalFlags TraversalFlags { get { return traversalFlags; } }

        /// <summary>
        /// Gets the mesh associated with a mesh ID.
        /// </summary>
        internal IMesh this[uint id] { get { return meshes[id]; } }

        #endregion

        #region IEnumerable

        public IEnumerator<IMesh> GetEnumerator()
        {
            return meshes.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IDisposable

        private bool disposed;

        ~Geometry()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                RTC.DeleteScene(scenePtr);
                RTC.Unregister();
                disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// User types which represent instances should implement this interface.
    /// </summary>
    public interface IInstance
    {
        /// <summary>
        /// Gets the Embree geometry.
        /// </summary>
        Geometry Geometry { get; }

        /// <summary>
        /// Gets whether this instance is enabled.
        /// </summary>
        Boolean Enabled { get; }

        /// <summary>
        /// Gets the transform for this instance.
        /// </summary>
        IEmbreeMatrix Transform { get; }
    }

    /// <summary>
    /// Represents a collection of instanced geometries.
    /// </summary>
    public class Scene<Instance> : IDisposable, IEnumerable<Instance> where Instance : class, IInstance
    {
        private TraversalFlags traversalFlags;
        private SceneFlags sceneFlags;
        private IntPtr scenePtr;

        /// <summary>
        /// The list of instances currently in this collection.
        /// </summary>
        private Dictionary<uint, Instance> instances = new Dictionary<uint, Instance>();

        /// <summary>
        /// Creates a new empty scene.
        /// </summary>
        public Scene(SceneFlags sceneFlags, TraversalFlags traversalFlags = TraversalFlags.Single)
        {
            RTC.Register();
            this.sceneFlags = sceneFlags;
            this.traversalFlags = traversalFlags;
            scenePtr = RTC.NewScene(sceneFlags, traversalFlags);
        }

        /// <summary>
        /// Commits all current instances to the scene.
        /// </summary>
        public void Commit()
        {
            //foreach (var instance in reverse.Keys)
            foreach (var entry in instances)
            {
                var instance = entry.Value;
                instance.Geometry.Commit();

                var xtf = new float[12] // Column-major order
                {
                    instance.Transform.U.X, instance.Transform.U.Y, instance.Transform.U.Z,
                    instance.Transform.V.X, instance.Transform.V.Y, instance.Transform.V.Z,
                    instance.Transform.W.X, instance.Transform.W.Y, instance.Transform.W.Z,
                    instance.Transform.T.X, instance.Transform.T.Y, instance.Transform.T.Z
                };

                var pinned = GCHandle.Alloc(xtf, GCHandleType.Pinned); // Pin transform matrix to raw float* array
                RTC.SetTransform(scenePtr, entry.Key, RTC.MatrixLayout.ColumnMajor, pinned.AddrOfPinnedObject());
                pinned.Free();

                RTC.CheckLastError();

                if (instance.Enabled)
                    RTC.EnableGeometry(scenePtr, entry.Key);
                else
                    RTC.DisableGeometry(scenePtr, entry.Key);

                if (sceneFlags.HasFlag(SceneFlags.Dynamic))
                {
                    RTC.UpdateGeometry(scenePtr, entry.Key);
                    RTC.CheckLastError();
                }

                RTC.CheckLastError();
            }

            RTC.Commit(scenePtr);
            RTC.CheckLastError();
        }

        #region Collection Methods

        /// <summary>
        /// Adds an instance to this scene.
        /// </summary>
        public void Add(Instance instance)
        {
            if (instance.Geometry.TraversalFlags != traversalFlags)
                throw new ArgumentException("Incompatible traversal flags");

            var instanceID = RTC.NewInstance(scenePtr, instance.Geometry.EmbreePointer);
            RTC.CheckLastError();

            instances.Add(instanceID, instance);
        }

        /// <summary>
        /// Deletes an instance from this scene.
        /// </summary>
        public void Delete(Instance instance)
        {
            if (!instances.ContainsValue(instance))
                throw new ArgumentException("No such instance");
            else
            {
                var instanceID = instances.First(x => x.Value == instance).Key;
                RTC.DeleteGeometry(scenePtr, instanceID);
                instances.Remove(instanceID);
            }
        }

        /// <summary>
        /// Gets the number of instances.
        /// </summary>
        public int Count { get { return instances.Count; } }

        #endregion

        #region Ray Tracing Functions

        /// <summary>
        /// Performs an occlusion test against the specified ray.
        /// </summary>
        public Boolean Occludes(Traversal traversal)
        {
            #if !EMBREE_CHECKING
            if (!traversalFlags.HasFlag(TraversalFlags.Single))
                throw new InvalidOperationException("Traversal flags forbid single-ray traversal");
            #endif

            return RTC.RayInterop.OcclusionTest1(scenePtr, traversal);
        }

        /// <summary>
        /// Performs an occlusion test against a packet of 4 rays.
        /// </summary>
        public Boolean[] Occludes4(Traversal[] traversals)
        {
            #if !EMBREE_CHECKING
            if (!traversalFlags.HasFlag(TraversalFlags.Packet4))
                throw new InvalidOperationException("Traversal flags forbid 4-ray packet traversal");
            #endif

            return RTC.RayInterop.OcclusionTest4(scenePtr, traversals);
        }

        /// <summary>
        /// Performs an occlusion test against a packet of 8 rays.
        /// </summary>
        public Boolean[] Occludes8<T>(Traversal[] traversals)
        {
            #if !EMBREE_CHECKING
            if (!traversalFlags.HasFlag(TraversalFlags.Packet8))
                throw new InvalidOperationException("Traversal flags forbid 8-ray packet traversal");
            #endif

            return RTC.RayInterop.OcclusionTest8(scenePtr, traversals);
        }

        /// <summary>
        /// Performs an occlusion test against a packet of 16 rays.
        /// </summary>
        public Boolean[] Occludes16(Traversal[] traversals)
        {
            #if !EMBREE_CHECKING
            if (!traversalFlags.HasFlag(TraversalFlags.Packet16))
                throw new InvalidOperationException("Traversal flags forbid 16-ray packet traversal");
            #endif

            return RTC.RayInterop.OcclusionTest16(scenePtr, traversals);
        }

        /// <summary>
        /// Performs an intersection test against the specified ray.
        /// </summary>
        public Intersection<Instance> Intersects(Traversal traversals)
        {
            #if !EMBREE_CHECKING
            if (!traversalFlags.HasFlag(TraversalFlags.Single))
                throw new InvalidOperationException("Traversal flags forbid single-ray traversal");
            #endif

            var r = RTC.RayInterop.Intersection1(scenePtr, traversals);

            if (r.geomID == RTC.InvalidGeometryID)
                return Intersection<Instance>.None;
            else
                return new Intersection<Instance>(r.primID, this[r.instID].Geometry[r.geomID], this[r.instID],
                                                  r.tfar, r.u, r.v, r.NgX, r.NgY, r.NgZ);
        }

        /// <summary>
        /// Performs an intersection test against a packet of 4 rays.
        /// </summary>
        public unsafe Intersection<Instance>[] Intersects4(Traversal[] traversals)
        {
            #if !EMBREE_CHECKING
            if (!traversalFlags.HasFlag(TraversalFlags.Packet4))
                throw new InvalidOperationException("Traversal flags forbid 4-ray packet traversal");
            #endif

            var r = RTC.RayInterop.Intersection4(scenePtr, traversals);
            var ret = new Intersection<Instance>[4]; // fixed length

            for (var t = 0; t < 4; ++t)
            {
                if (r.geomID[t] == RTC.InvalidGeometryID)
                    ret[t] = Intersection<Instance>.None;
                else
                    ret[t] = new Intersection<Instance>(r.primID[t], this[r.instID[t]].Geometry[r.geomID[t]], this[r.instID[t]],
                                                        r.tfar[t], r.u[t], r.v[t], r.NgX[t], r.NgY[t], r.NgZ[t]);
            }

            return ret;
        }

        /// <summary>
        /// Performs an intersection test against a packet of 8 rays.
        /// </summary>
        public unsafe Intersection<Instance>[] Intersects8(Traversal[] traversals)
        {
            #if !EMBREE_CHECKING
            if (!traversalFlags.HasFlag(TraversalFlags.Packet8))
                throw new InvalidOperationException("Traversal flags forbid 8-ray packet traversal");
            #endif

            var r = RTC.RayInterop.Intersection8(scenePtr, traversals);
            var ret = new Intersection<Instance>[8]; // fixed length

            for (var t = 0; t < 8; ++t)
            {
                if (r.geomID[t] == RTC.InvalidGeometryID)
                    ret[t] = Intersection<Instance>.None;
                else
                    ret[t] = new Intersection<Instance>(r.primID[t], this[r.instID[t]].Geometry[r.geomID[t]], this[r.instID[t]],
                                                        r.tfar[t], r.u[t], r.v[t], r.NgX[t], r.NgY[t], r.NgZ[t]);
            }

            return ret;
        }

        /// <summary>
        /// Performs an intersection test against a packet of 16 rays.
        /// </summary>
        public unsafe Intersection<Instance>[] Intersects16(Traversal[] traversals)
        {
            #if !EMBREE_CHECKING
            if (!traversalFlags.HasFlag(TraversalFlags.Packet16))
                throw new InvalidOperationException("Traversal flags forbid 16-ray packet traversal");
            #endif

            var r = RTC.RayInterop.Intersection16(scenePtr, traversals);
            var ret = new Intersection<Instance>[16]; // fixed length

            for (var t = 0; t < 16; ++t)
            {
                if (r.geomID[t] == RTC.InvalidGeometryID)
                    ret[t] = Intersection<Instance>.None;
                else
                    ret[t] = new Intersection<Instance>(r.primID[t], this[r.instID[t]].Geometry[r.geomID[t]], this[r.instID[t]],
                                                        r.tfar[t], r.u[t], r.v[t], r.NgX[t], r.NgY[t], r.NgZ[t]);
            }

            return ret;
        }

        #endregion

        #region Internal Members

        /// <summary>
        /// Gets the instance associated with an instance ID.
        /// </summary>
        internal Instance this[uint id] { get { return instances[id]; } }

        #endregion

        #region IEnumerable

        public IEnumerator<Instance> GetEnumerator()
        {
            return instances.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region IDisposable

        private bool disposed;

        ~Scene()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                RTC.DeleteScene(scenePtr);
                RTC.Unregister();
                disposed = true;
            }
        }

        #endregion
    }

    #endregion

    #region Built-in Mesh Types

    /// <summary>
    /// Represents a simple triangle mesh.
    /// </summary>
    public class TriangleMesh : IMesh
    {
        private readonly IList<Int32> indices;
        private readonly IList<IEmbreePoint> vertices;
        private readonly long triangleCount, vertexCount;

        /// <summary>
        /// Gets the index buffer.
        /// </summary>
        public IList<Int32> Indices { get { return indices; } }

        /// <summary>
        /// Gets the vertex buffer.
        /// </summary>
        public IList<IEmbreePoint> Vertices { get { return vertices; } }

        /// <summary>
        /// Creates a new mesh from index and vertex buffers.
        /// </summary>
        public TriangleMesh(IList<Int32> indices, IList<IEmbreePoint> vertices)
        {
            this.indices = indices;
            this.vertices = vertices;
            this.vertexCount = vertices.Count();
            this.triangleCount = indices.Count() / 3;
            RTC.CheckLastError();

            if (vertexCount == 0)
                throw new ArgumentOutOfRangeException("No vertices in mesh");

            if (triangleCount == 0)
                throw new ArgumentOutOfRangeException("No triangles in mesh");
        }

        public uint Add(IntPtr scenePtr)
        {
            var meshID = RTC.NewTriangleMesh(scenePtr, MeshFlags.Static, new IntPtr(triangleCount), new IntPtr(vertexCount), new IntPtr(1));
            RTC.CheckLastError();
            return meshID;
        }

        public void Update(uint geomID, IntPtr scenePtr)
        {
            if (indices.Count() / 3 == triangleCount)
            {
                var indexBuffer = RTC.MapBuffer(scenePtr, geomID, RTC.BufferType.IndexBuffer);
                RTC.CheckLastError();

                Marshal.Copy(indices.ToArray(), 0, indexBuffer, indices.Count());

                RTC.UnmapBuffer(scenePtr, geomID, RTC.BufferType.IndexBuffer);
            }
            else
                throw new InvalidOperationException("Index buffer length was changed.");

            if (vertices.Count() == vertexCount)
            {
                var vertexBuffer = RTC.MapBuffer(scenePtr, geomID, RTC.BufferType.VertexBuffer);
                RTC.CheckLastError();

                unsafe
                {
                    float* ptr = (float*)vertexBuffer;
                    foreach (var vertex in vertices)
                    {
                        *(ptr++) = vertex.X;
                        *(ptr++) = vertex.Y;
                        *(ptr++) = vertex.Z;
                        *(ptr++) = 1.0f;
                    }
                }

                RTC.UnmapBuffer(scenePtr, geomID, RTC.BufferType.VertexBuffer);
            }
            else
                throw new InvalidOperationException("Vertex buffer length was changed.");
        }
    }

    /// <summary>
    /// Represents a triangle mesh with linear motion blur.
    /// </summary>
    /// <remarks>
    /// Work in progress.
    /// </remarks>
    public class TriangleMeshMotion : IMesh
    {
        private readonly IList<Int32> indices;
        private readonly IList<IEmbreePoint> vertices0;
        private readonly IList<IEmbreePoint> vertices1;
        private readonly long triangleCount, vertexCount;

        /// <summary>
        /// Gets the index buffer.
        /// </summary>
        public IList<Int32> Indices { get { return indices; } }

        /// <summary>
        /// Gets the vertex buffer at time t = 0.
        /// </summary>
        public IList<IEmbreePoint> Vertices0 { get { return vertices0; } }

        /// <summary>
        /// Gets the vertex buffer at time t = 1.
        /// </summary>
        public IList<IEmbreePoint> Vertices1 { get { return vertices1; } }

        /// <summary>
        /// Creates a new mesh from index and vertex buffers.
        /// </summary>
        public TriangleMeshMotion(IList<Int32> indices, IList<IEmbreePoint> vertices0, IList<IEmbreePoint> vertices1)
        {
            if (vertices0.Count != vertices1.Count)
                throw new ArgumentException("Both vertex buffers must have the same length");

            RTC.Register();

            this.indices = indices;
            this.vertices0 = vertices0;
            this.vertices1 = vertices1;
            this.vertexCount = vertices0.Count();
            this.triangleCount = indices.Count() / 3;

            if (vertexCount == 0)
                throw new ArgumentOutOfRangeException("No vertices in mesh");

            if (triangleCount == 0)
                throw new ArgumentOutOfRangeException("No triangles in mesh");
        }

        public uint Add(IntPtr scenePtr)
        {
            var meshID = RTC.NewTriangleMesh(scenePtr, MeshFlags.Static, new IntPtr(triangleCount), new IntPtr(vertexCount), new IntPtr(2));
            RTC.CheckLastError();
            return meshID;
        }

        public void Update(uint geomID, IntPtr scenePtr)
        {
            if (indices.Count() / 3 == triangleCount)
            {
                var indexBuffer = RTC.MapBuffer(scenePtr, geomID, RTC.BufferType.IndexBuffer);
                RTC.CheckLastError();

                Marshal.Copy(indices.ToArray(), 0, indexBuffer, indices.Count());

                RTC.UnmapBuffer(scenePtr, geomID, RTC.BufferType.IndexBuffer);
            }
            else
                throw new InvalidOperationException("Index buffer length was changed.");

            if (vertices0.Count() == vertexCount)
            {
                var vertexBuffer = RTC.MapBuffer(scenePtr, geomID, RTC.BufferType.VertexBuffer0);
                RTC.CheckLastError();

                unsafe
                {
                    float* ptr = (float*)vertexBuffer;
                    foreach (var vertex in vertices0)
                    {
                        *(ptr++) = vertex.X;
                        *(ptr++) = vertex.Y;
                        *(ptr++) = vertex.Z;
                        *(ptr++) = 1.0f;
                    }
                }

                RTC.UnmapBuffer(scenePtr, geomID, RTC.BufferType.VertexBuffer0);
            }
            else
                throw new InvalidOperationException("Vertex buffer 0 length was changed.");

            if (vertices1.Count() == vertexCount)
            {
                var vertexBuffer = RTC.MapBuffer(scenePtr, geomID, RTC.BufferType.VertexBuffer1);
                RTC.CheckLastError();

                unsafe
                {
                    float* ptr = (float*)vertexBuffer;
                    foreach (var vertex in vertices1)
                    {
                        *(ptr++) = vertex.X;
                        *(ptr++) = vertex.Y;
                        *(ptr++) = vertex.Z;
                        *(ptr++) = 1.0f;
                    }
                }

                RTC.UnmapBuffer(scenePtr, geomID, RTC.BufferType.VertexBuffer1);
            }
            else
                throw new InvalidOperationException("Vertex buffer 1 length was changed.");
        }
    }

    #endregion
}
