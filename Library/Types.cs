/* Embree.NET
 * ==========
 *
 * A number of types and dumb containers that do
 * not perform any logic, used by the wrapper. A
 * class in this file should not depend on other
 * types outside this file if at all possible.
*/

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
        /// Gets the direction of the ray.
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

    #region Ray Structures

    public struct Intersection<T> where T : IInstance
    {
        /// <summary>
        /// Gets the intersection distance from the ray origin.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// Gets the index of the primitive intersected (e.g. nth triangle).
        /// </summary>
        public uint Primitive { get; set; }

        /// <summary>
        /// Gets the identifier of the instance intersected.
        /// </summary>
        public T Instance { get; set; }

        /// <summary>
        /// Gets the identifier of the mesh intersected.
        /// </summary>
        public IMesh Mesh { get; set; }

        /// <summary>
        /// Gets the barycentric u-coordinate of the intersection point.
        /// </summary>
        public float U { get; set; }

        /// <summary>
        /// Gets the barycentric v-coordinate of the intersection point.
        /// </summary>
        public float V { get; set; }

        /// <summary>
        /// Gets the x-coordinate of the raw surface normal.
        /// </summary>
        public float NX { get; set; }

        /// <summary>
        /// Gets the y-coordinate of the raw surface normal.
        /// </summary>
        public float NY { get; set; }

        /// <summary>
        /// Gets the z-coordinate of the raw surface normal.
        /// </summary>
        public float NZ { get; set; }

        /// <summary>
        /// Gets whether there exists a ray-scene intersection.
        /// </summary>
        /// <remarks>
        /// If this is false, all other fields are undefined.
        /// </remarks>
        public Boolean HasHit { get { return Mesh != null; } }

        /// <summary>
        /// Creates a new intersection.
        /// </summary>
        public Intersection(uint primID, IMesh geomID, T instID, float tfar, float u, float v, float nx, float ny, float nz) : this()
        {
            Distance  = tfar;
            Primitive = primID;
            Mesh      = geomID;
            Instance  = instID;
            U      = u;
            V      = v;
            NX     = nx;
            NY     = ny;
            NZ     = nz;
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
}

