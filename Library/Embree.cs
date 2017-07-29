/* Embree.NET
 * ==========
 *
 * The wrapper's API. Note some checking is only
 * available in debug mode, for efficiency, this
 * means release binaries may segfault on error.
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
        uint Add(IntPtr scene, MeshFlags flags);

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
        private readonly Device device;

        /// <summary>
        /// The list of meshes currently in this collection.
        /// </summary>
        private Dictionary<uint, IMesh> meshes = new Dictionary<uint, IMesh>();

        /// <summary>
        /// Creates a new empty geometry.
        /// </summary>
        public Geometry(Device device, SceneFlags sceneFlags, TraversalFlags traversalFlags = TraversalFlags.Single)
        {
            this.device = device;
            this.sceneFlags = sceneFlags;
            this.traversalFlags = traversalFlags;
            scenePtr = RTC.NewScene(device.DevicePtr, sceneFlags, traversalFlags);
        }

        #region Collection Methods

        /// <summary>
        /// Adds a mesh to the geometry.
        /// </summary>
        public void Add(IMesh mesh, MeshFlags flags = MeshFlags.Static)
        {
            var meshID = mesh.Add(scenePtr, flags);
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
                device.CheckLastError();

                if (sceneFlags.HasFlag(SceneFlags.Dynamic))
                {
                    RTC.UpdateGeometry(scenePtr, entry.Key);
                    device.CheckLastError();
                }
            }

            RTC.Commit(scenePtr);
            device.CheckLastError();
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
    /// Class represents an embree context
    /// Scenes and geometry objects must be associated with a device context
    /// </summary>
    public class Device : IDisposable
    {
        private readonly IntPtr devicePtr;
        public IntPtr DevicePtr { get { return devicePtr; } }

        /// <summary>
        /// Creates a new raytracing device context
        /// </summary>
        /// <param name="verbose">if true embree will run inverbose mode</param>
        public Device(bool verbose = false)
        {
            string cfg = verbose ? "verbose=999" : null;
            this.devicePtr = RTC.InitEmbree(cfg);
        }

        /// <summary>
        /// Checks the last embree error for this devices and throws exceptions as needed
        /// </summary>
        public void CheckLastError()
        {
            RTC.CheckLastError(this.devicePtr);
        }

        #region IDisposable

        private bool disposed;

        ~Device()
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
                RTC.Unregister(devicePtr);
                disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents a collection of instanced geometries.
    /// </summary>
    public unsafe class Scene<Instance> : IDisposable, IEnumerable<Instance> where Instance : class, IInstance
    {
        private TraversalFlags traversalFlags;
        private SceneFlags sceneFlags;
        private IntPtr scenePtr;
        private readonly Device device;

        public Device Device { get { return device; } }
        /// <summary>
        /// The list of instances currently in this collection.
        /// </summary>
        private Dictionary<uint, Instance> instances = new Dictionary<uint, Instance>();

        /// <summary>
        /// Creates a new empty scene.
        /// </summary>
        public Scene(Device device, SceneFlags sceneFlags, TraversalFlags traversalFlags = TraversalFlags.Single)
        {
            this.device = device;
            this.sceneFlags = sceneFlags;
            this.traversalFlags = traversalFlags;
            scenePtr = RTC.NewScene(device.DevicePtr, sceneFlags, traversalFlags);
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
                Device.CheckLastError();

                if (instance.Enabled)
                    RTC.EnableGeometry(scenePtr, entry.Key);
                else
                    RTC.DisableGeometry(scenePtr, entry.Key);

                Device.CheckLastError();

                if (sceneFlags.HasFlag(SceneFlags.Dynamic))
                {
                    RTC.UpdateGeometry(scenePtr, entry.Key);
                    Device.CheckLastError(); // static mesh?
                }
            }

            RTC.Commit(scenePtr);
            Device.CheckLastError();
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
        public unsafe Boolean Occludes<Ray>(Ray ray, float near = 0, float far = float.PositiveInfinity, float time = 0) where Ray : IEmbreeRay
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Single))
                throw new InvalidOperationException("Traversal flags forbid single-ray traversal");
            #endif

            var o = ray.Origin;
            var d = ray.Direction;
            var p = RTC.RayInterop.Packet1;

            p->orgX = o.X; p->orgY = o.Y; p->orgZ = o.Z;
            p->dirX = d.X; p->dirY = d.Y; p->dirZ = d.Z;

            p->geomID = RTC.InvalidGeometryID;
            p->primID = RTC.InvalidGeometryID;
            p->instID = RTC.InvalidGeometryID;

            p->time   = time;
            p->tnear  = near;
            p->tfar   = far;

            RTC.Occluded1(scenePtr, p);

            return p->geomID == 0;
        }

        /// <summary>
        /// Performs an occlusion test against a packet of 4 rays.
        /// </summary>
        public unsafe Boolean[] Occludes4<Ray>(Ray[] rays, float near = 0, float far = float.PositiveInfinity, float time = 0) where Ray : IEmbreeRay
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Packet4))
                throw new InvalidOperationException("Traversal flags forbid 4-ray packet traversal");
            #endif

            var p = RTC.RayInterop.Packet4;
            var a = RTC.RayInterop.Activity;

            for (var t = 0; t < 4; ++t)
            {
                if (rays[t] != null)
                    a[t] = RTC.RayInterop.Active;
                else
                {
                    a[t] = RTC.RayInterop.Inactive;
                    continue;
                }

                var o = rays[t].Origin;
                var d = rays[t].Direction;

                p->orgX[t] = o.X; p->orgY[t] = o.Y; p->orgZ[t] = o.Z;
                p->dirX[t] = d.X; p->dirY[t] = d.Y; p->dirZ[t] = d.Z;

                p->geomID[t] = RTC.InvalidGeometryID;
                p->primID[t] = RTC.InvalidGeometryID;
                p->instID[t] = RTC.InvalidGeometryID;

                p->time[t]  = time;
                p->tnear[t] = near;
                p->tfar[t]  = far;
            }

            RTC.Occluded4(a, scenePtr, p);

            return new[]
            {
                p->geomID[0] == 0, p->geomID[1] == 0,
                p->geomID[2] == 0, p->geomID[3] == 0,
            };
        }

        /// <summary>
        /// Performs an occlusion test against a packet of 8 rays.
        /// </summary>
		public unsafe Boolean[] Occludes8<Ray>(Ray[] rays, float near = 0, float far = float.PositiveInfinity, float time = 0) where Ray : IEmbreeRay
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Packet8))
                throw new InvalidOperationException("Traversal flags forbid 8-ray packet traversal");
            #endif

			var p = RTC.RayInterop.Packet8;
			var a = RTC.RayInterop.Activity;

			for (var t = 0; t < 8; ++t)
			{
				if (rays[t] != null)
					a[t] = RTC.RayInterop.Active;
				else
				{
					a[t] = RTC.RayInterop.Inactive;
					continue;
				}

				var o = rays[t].Origin;
				var d = rays[t].Direction;

				p->orgX[t] = o.X; p->orgY[t] = o.Y; p->orgZ[t] = o.Z;
				p->dirX[t] = d.X; p->dirY[t] = d.Y; p->dirZ[t] = d.Z;

				p->geomID[t] = RTC.InvalidGeometryID;
				p->primID[t] = RTC.InvalidGeometryID;
				p->instID[t] = RTC.InvalidGeometryID;

				p->time[t]  = time;
				p->tnear[t] = near;
				p->tfar[t]  = far;
			}

			RTC.Occluded8(a, scenePtr, p);

			return new[]
			{
				p->geomID[0] == 0, p->geomID[1] == 0,
				p->geomID[2] == 0, p->geomID[3] == 0,
				p->geomID[4] == 0, p->geomID[5] == 0,
				p->geomID[6] == 0, p->geomID[7] == 0,
			};
        }

        /// <summary>
        /// Performs an occlusion test against a packet of 16 rays.
        /// </summary>
		public unsafe Boolean[] Occludes16<Ray>(Ray[] rays, float near = 0, float far = float.PositiveInfinity, float time = 0) where Ray : IEmbreeRay
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Packet16))
                throw new InvalidOperationException("Traversal flags forbid 16-ray packet traversal");
            #endif

			var p = RTC.RayInterop.Packet16;
			var a = RTC.RayInterop.Activity;

			for (var t = 0; t < 16; ++t)
			{
				if (rays[t] != null)
					a[t] = RTC.RayInterop.Active;
				else
				{
					a[t] = RTC.RayInterop.Inactive;
					continue;
				}

				var o = rays[t].Origin;
				var d = rays[t].Direction;

				p->orgX[t] = o.X; p->orgY[t] = o.Y; p->orgZ[t] = o.Z;
				p->dirX[t] = d.X; p->dirY[t] = d.Y; p->dirZ[t] = d.Z;

				p->geomID[t] = RTC.InvalidGeometryID;
				p->primID[t] = RTC.InvalidGeometryID;
				p->instID[t] = RTC.InvalidGeometryID;

				p->time[t]  = time;
				p->tnear[t] = near;
				p->tfar[t]  = far;
			}

			RTC.Occluded16(a, scenePtr, p);

            return new[]
            {
				p->geomID[ 0] == 0, p->geomID[ 1] == 0,
				p->geomID[ 2] == 0, p->geomID[ 3] == 0,
				p->geomID[ 4] == 0, p->geomID[ 5] == 0,
				p->geomID[ 6] == 0, p->geomID[ 7] == 0,
				p->geomID[ 8] == 0, p->geomID[ 9] == 0,
				p->geomID[10] == 0, p->geomID[11] == 0,
				p->geomID[12] == 0, p->geomID[13] == 0,
				p->geomID[14] == 0, p->geomID[15] == 0,
            };
        }

        /// <summary>
        /// Performs an intersection test against the specified ray.
        /// </summary>
        public unsafe RTC.RayPacket1 Intersects<Ray>(Ray ray, float near = 0, float far = float.PositiveInfinity, float time = 0) where Ray : IEmbreeRay
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Single))
                throw new InvalidOperationException("Traversal flags forbid single-ray traversal");
            #endif

            var o = ray.Origin;
            var d = ray.Direction;
            var p = RTC.RayInterop.Packet1;

            p->orgX = o.X; p->orgY = o.Y; p->orgZ = o.Z;
            p->dirX = d.X; p->dirY = d.Y; p->dirZ = d.Z;

            p->geomID = RTC.InvalidGeometryID;
            p->primID = RTC.InvalidGeometryID;
            p->instID = RTC.InvalidGeometryID;

            p->time   = time;
            p->tnear  = near;
            p->tfar   = far;

            RTC.Intersect1(scenePtr, p);

            return *p;
        }

        /// <summary>
        /// Performs an intersection test against a packet of 4 rays.
        /// </summary>
        public unsafe RTC.RayPacket4 Intersects4<Ray>(Ray[] rays, float near = 0, float far = float.PositiveInfinity, float time = 0) where Ray : IEmbreeRay
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Packet4))
                throw new InvalidOperationException("Traversal flags forbid 4-ray packet traversal");
            #endif

            var p = RTC.RayInterop.Packet4;
            var a = RTC.RayInterop.Activity;

            for (var t = 0; t < 4; ++t)
            {
                if (rays[t] != null)
                    a[t] = RTC.RayInterop.Active;
                else
                {
                    a[t] = RTC.RayInterop.Inactive;
                    continue;
                }

                var o = rays[t].Origin;
                var d = rays[t].Direction;

                p->orgX[t] = o.X; p->orgY[t] = o.Y; p->orgZ[t] = o.Z;
                p->dirX[t] = d.X; p->dirY[t] = d.Y; p->dirZ[t] = d.Z;

                p->geomID[t] = RTC.InvalidGeometryID;
                p->primID[t] = RTC.InvalidGeometryID;
                p->instID[t] = RTC.InvalidGeometryID;

                p->time[t]  = time;
                p->tnear[t] = near;
                p->tfar[t]  = far;
            }

            RTC.Intersect4(a, scenePtr, p);

            return *p;
        }

        /// <summary>
        /// Performs an intersection test against a packet of 8 rays.
        /// </summary>
		public unsafe RTC.RayPacket8 Intersects8<Ray>(Ray[] rays, float near = 0, float far = float.PositiveInfinity, float time = 0) where Ray : IEmbreeRay
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Packet8))
                throw new InvalidOperationException("Traversal flags forbid 8-ray packet traversal");
            #endif

			var p = RTC.RayInterop.Packet8;
			var a = RTC.RayInterop.Activity;

			for (var t = 0; t < 8; ++t)
			{
				if (rays[t] != null)
					a[t] = RTC.RayInterop.Active;
				else
				{
					a[t] = RTC.RayInterop.Inactive;
					continue;
				}

				var o = rays[t].Origin;
				var d = rays[t].Direction;

				p->orgX[t] = o.X; p->orgY[t] = o.Y; p->orgZ[t] = o.Z;
				p->dirX[t] = d.X; p->dirY[t] = d.Y; p->dirZ[t] = d.Z;

				p->geomID[t] = RTC.InvalidGeometryID;
				p->primID[t] = RTC.InvalidGeometryID;
				p->instID[t] = RTC.InvalidGeometryID;

				p->time[t]  = time;
				p->tnear[t] = near;
				p->tfar[t]  = far;
			}

			RTC.Intersect8(a, scenePtr, p);

			return *p;
        }

        /// <summary>
        /// Performs an intersection test against a packet of 16 rays.
        /// </summary>
		public unsafe RTC.RayPacket16 Intersects16<Ray>(Ray[] rays, float near = 0, float far = float.PositiveInfinity, float time = 0) where Ray : IEmbreeRay
        {
            #if DEBUG
            if (!traversalFlags.HasFlag(TraversalFlags.Packet16))
                throw new InvalidOperationException("Traversal flags forbid 16-ray packet traversal");
            #endif

			var p = RTC.RayInterop.Packet16;
			var a = RTC.RayInterop.Activity;

			for (var t = 0; t < 16; ++t)
			{
				if (rays[t] != null)
					a[t] = RTC.RayInterop.Active;
				else
				{
					a[t] = RTC.RayInterop.Inactive;
					continue;
				}

				var o = rays[t].Origin;
				var d = rays[t].Direction;

				p->orgX[t] = o.X; p->orgY[t] = o.Y; p->orgZ[t] = o.Z;
				p->dirX[t] = d.X; p->dirY[t] = d.Y; p->dirZ[t] = d.Z;

				p->geomID[t] = RTC.InvalidGeometryID;
				p->primID[t] = RTC.InvalidGeometryID;
				p->instID[t] = RTC.InvalidGeometryID;

				p->time[t]  = time;
				p->tnear[t] = near;
				p->tfar[t]  = far;
			}

			RTC.Intersect16(a, scenePtr, p);

			return *p;
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
                disposed = true;
            }
        }

        #endregion
    }
}
