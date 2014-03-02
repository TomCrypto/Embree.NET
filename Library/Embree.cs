/* Embree.NET
 * ==========
 * 
 * The wrapper's API. Note some checking is only
 * available in debug mode, for efficiency, this
 * means release binaries may segfault on error->
*/

using System;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Embree
{
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
            meshes.Add(meshID, mesh);
        }

        /// <summary>
        /// Removes a mesh from the geometry.
        /// </summary>
        public void Remove(IMesh mesh)
        {
            if (!meshes.ContainsValue(mesh))
                throw new ArgumentException("No such mesh present");
            else
            {
                var meshID = meshes.First(x => x.Value == mesh).Key;
                RTC.DeleteGeometry(scenePtr, meshID);
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
        /// Gets the Embree scene pointer->
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
                pinned.Free(); // Release before checking for error
                RTC.CheckLastError();

                if (instance.Enabled)
                    RTC.EnableGeometry(scenePtr, entry.Key);
                else
                    RTC.DisableGeometry(scenePtr, entry.Key);

                RTC.CheckLastError();

                if (sceneFlags.HasFlag(SceneFlags.Dynamic))
                {
                    RTC.UpdateGeometry(scenePtr, entry.Key);
                    RTC.CheckLastError(); // static mesh?
                }
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
                throw new ArgumentException("Inconsistent traversal flags");

            var instanceID = RTC.NewInstance(scenePtr, instance.Geometry.EmbreePointer);
            instances.Add(instanceID, instance);
        }

        /// <summary>
        /// Deletes an instance from this scene.
        /// </summary>
        public void Delete(Instance instance)
        {
            if (!instances.ContainsValue(instance))
                throw new ArgumentException("No such instance present");
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
        public unsafe Boolean Occludes(Traversal traversal)
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Single))
                throw new InvalidOperationException("Traversal flags forbid single-ray traversal");
            #endif

            var r = RTC.RayInterop.OcclusionTest1(scenePtr, traversal);

            return r->geomID == 0;
        }

        /// <summary>
        /// Performs an occlusion test against a packet of 4 rays.
        /// </summary>
        public unsafe Boolean[] Occludes4(Traversal[] traversals)
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Packet4))
                throw new InvalidOperationException("Traversal flags forbid 4-ray packet traversal");
            #endif

            var r = RTC.RayInterop.OcclusionTest4(scenePtr, traversals);

            return new[]
            {
                r->geomID[0] == 0, r->geomID[1] == 0,
                r->geomID[2] == 0, r->geomID[3] == 0,
            };
        }

        /// <summary>
        /// Performs an occlusion test against a packet of 8 rays.
        /// </summary>
        public unsafe Boolean[] Occludes8<T>(Traversal[] traversals)
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Packet8))
                throw new InvalidOperationException("Traversal flags forbid 8-ray packet traversal");
            #endif

            var r = RTC.RayInterop.OcclusionTest8(scenePtr, traversals);

            return new[]
            {
                r->geomID[0] == 0, r->geomID[1] == 0,
                r->geomID[2] == 0, r->geomID[3] == 0,
                r->geomID[4] == 0, r->geomID[5] == 0,
                r->geomID[6] == 0, r->geomID[7] == 0,
            };
        }

        /// <summary>
        /// Performs an occlusion test against a packet of 16 rays.
        /// </summary>
        public unsafe Boolean[] Occludes16(Traversal[] traversals)
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Packet16))
                throw new InvalidOperationException("Traversal flags forbid 16-ray packet traversal");
            #endif

            var r = RTC.RayInterop.OcclusionTest16(scenePtr, traversals);

            return new[]
            {
                r->geomID[ 0] == 0, r->geomID[ 1] == 0,
                r->geomID[ 2] == 0, r->geomID[ 3] == 0,
                r->geomID[ 4] == 0, r->geomID[ 5] == 0,
                r->geomID[ 6] == 0, r->geomID[ 7] == 0,
                r->geomID[ 8] == 0, r->geomID[ 9] == 0,
                r->geomID[10] == 0, r->geomID[11] == 0,
                r->geomID[12] == 0, r->geomID[13] == 0,
                r->geomID[14] == 0, r->geomID[15] == 0,
            };
        }

        /// <summary>
        /// Performs an intersection test against the specified ray.
        /// </summary>
        public unsafe Intersection<Instance> Intersects(Traversal traversals)
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Single))
                throw new InvalidOperationException("Traversal flags forbid single-ray traversal");
            #endif

            var r = RTC.RayInterop.Intersection1(scenePtr, traversals);

            if (r->geomID == RTC.InvalidGeometryID)
                return Intersection<Instance>.None;
            else
                return new Intersection<Instance>(r->primID, this[r->instID].Geometry[r->geomID], this[r->instID],
                                                  r->tfar, r->u, r->v, r->NgX, r->NgY, r->NgZ);
        }

        /// <summary>
        /// Performs an intersection test against a packet of 4 rays.
        /// </summary>
        public unsafe Intersection<Instance>[] Intersects4(Traversal[] traversals)
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Packet4))
                throw new InvalidOperationException("Traversal flags forbid 4-ray packet traversal");
            #endif

            var r = RTC.RayInterop.Intersection4(scenePtr, traversals);
            var ret = new Intersection<Instance>[4]; // fixed length

            for (var t = 0; t < 4; ++t)
            {
                if (r->geomID[t] == RTC.InvalidGeometryID)
                    ret[t] = Intersection<Instance>.None;
                else
                    ret[t] = new Intersection<Instance>(r->primID[t], this[r->instID[t]].Geometry[r->geomID[t]], this[r->instID[t]],
                                                        r->tfar[t], r->u[t], r->v[t], r->NgX[t], r->NgY[t], r->NgZ[t]);
            }

            return ret;
        }

        /// <summary>
        /// Performs an intersection test against a packet of 8 rays.
        /// </summary>
        public unsafe Intersection<Instance>[] Intersects8(Traversal[] traversals)
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Packet8))
                throw new InvalidOperationException("Traversal flags forbid 8-ray packet traversal");
            #endif

            var r = RTC.RayInterop.Intersection8(scenePtr, traversals);
            var ret = new Intersection<Instance>[8]; // fixed length

            for (var t = 0; t < 8; ++t)
            {
                if (r->geomID[t] == RTC.InvalidGeometryID)
                    ret[t] = Intersection<Instance>.None;
                else
                    ret[t] = new Intersection<Instance>(r->primID[t], this[r->instID[t]].Geometry[r->geomID[t]], this[r->instID[t]],
                                                        r->tfar[t], r->u[t], r->v[t], r->NgX[t], r->NgY[t], r->NgZ[t]);
            }

            return ret;
        }

        /// <summary>
        /// Performs an intersection test against a packet of 16 rays.
        /// </summary>
        public unsafe Intersection<Instance>[] Intersects16(Traversal[] traversals)
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Packet16))
                throw new InvalidOperationException("Traversal flags forbid 16-ray packet traversal");
            #endif

            var r = RTC.RayInterop.Intersection16(scenePtr, traversals);
            var ret = new Intersection<Instance>[16]; // fixed length

            for (var t = 0; t < 16; ++t)
            {
                if (r->geomID[t] == RTC.InvalidGeometryID)
                    ret[t] = Intersection<Instance>.None;
                else
                    ret[t] = new Intersection<Instance>(r->primID[t], this[r->instID[t]].Geometry[r->geomID[t]], this[r->instID[t]],
                                                        r->tfar[t], r->u[t], r->v[t], r->NgX[t], r->NgY[t], r->NgZ[t]);
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
}
