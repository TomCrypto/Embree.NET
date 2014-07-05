/* Embree.NET
 * ==========
 * 
 * This file defines the RTC static class, which
 * contains the Embree native API, some helpers,
 * and the RayInterop class which stores the per
 * thread (memory-aligned) packet structures.
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
    /// Embree native API.
    /// </summary>
    public static class RTC
    {
        #region Interop

        /* Searches embree.dll, libembree.so. */
        private const String DLLName = "embree";

        #endregion

        #region Error Handling

        /// <summary>
        /// Error codes returned by the rtcGetError function.
        /// </summary>
        public enum Error
        {
            /// <summary>
            /// No error has been recorded.
            /// </summary>
            NoError          = 0,

            /// <summary>
            /// An unknown error has occurred.
            /// </summary>
            UnknownError     = 1,

            /// <summary>
            /// An invalid argument is specified.
            /// </summary>
            InvalidArgument  = 2,

            /// <summary>
            /// The operation is not allowed for the specified object.
            /// </summary>
            InvalidOperation = 3,

            /// <summary>
            /// There is not enough memory left to execute the command.
            /// </summary>
            OutOfMemory      = 4,

            /// <summary>
            /// The CPU is not supported as it does not support SSE2 (?).
            /// </summary>
            UnsupportedCPU = 5
        }

        /// <summary>
        /// Returns the value of the per-thread error flag. 
        /// </summary>
        [DllImport(DLLName, EntryPoint="rtcGetError")]
        public static extern Error GetError();

        /// <summary>
        /// Checks the last Embree error and throws as needed.
        /// </summary>
        /// <remarks>
        /// This method operates on a per-thread basis.
        /// </remarks>
        public static void CheckLastError()
        {
            switch (GetError())
            {
                case Error.UnknownError:
                    throw new InvalidOperationException("An unknown error occurred in the Embree library");
                case Error.InvalidArgument:
                    throw new ArgumentException("An argument to an Embree function was invalid");
                case Error.InvalidOperation:
                    throw new InvalidOperationException("An invalid operation was attempted on an Embree object");
                case Error.OutOfMemory:
                    throw new OutOfMemoryException("The Embree library has run out of memory");
                case Error.UnsupportedCPU:
                    throw new InvalidOperationException("This operation is not valid, unsupported processor");
            }
        }

        #endregion

        #region Scene Functions

        /// <summary>
        /// Creates a new scene.
        /// </summary>
        [DllImport(DLLName, EntryPoint="rtcNewScene")]
        public static extern IntPtr NewScene(SceneFlags flags, TraversalFlags aFlags);

        /// <summary>
        /// Commits the geometry of the scene.
        /// </summary>
        /// <remarks>
        /// After initializing or modifying geometries, this
        /// function has to get called before tracing rays.
        /// </remarks>
        [DllImport(DLLName, EntryPoint="rtcCommit")]
        public static extern void Commit(IntPtr scene);

        /// <summary>
        /// Deletes the scene.
        /// </summary>
        /// <remarks>
        /// All contained geometry get also destroyed.
        /// </remarks>
        [DllImport(DLLName, EntryPoint="rtcDeleteScene")]
        public static extern void DeleteScene(IntPtr scene);

        #endregion

        #region Geometry Functions

        /// <summary>
        /// Specifies the type of buffers when mapping buffers.
        /// </summary>
        public enum BufferType
        {
            /// <summary>
            /// The index buffer.
            /// </summary>
            IndexBuffer = 0x01000000,

            /// <summary>
            /// The vertex buffer.
            /// </summary>
            VertexBuffer = 0x02000000,

            /// <summary>
            /// The t = 0 vertex buffer.
            /// </summary>
            VertexBuffer0 = 0x02000000,

            /// <summary>
            /// The t = 1 vertex buffer.
            /// </summary>
            VertexBuffer1 = 0x02000001
        }

        /// <summary>
        /// Creates a new triangle mesh. 
        /// </summary>
        [DllImport(DLLName, EntryPoint="rtcNewTriangleMesh")]
        #if X86
        public static extern uint NewTriangleMesh(IntPtr scene, MeshFlags flags, int numTriangles, int numVertices, int numTimeSteps);
        #else
        public static extern uint NewTriangleMesh(IntPtr scene, MeshFlags flags, long numTriangles, long numVertices, long numTimeSteps);
        #endif

        /// <summary>
        /// Maps specified buffer.
        /// </summary>
        /// <remarks>
        /// This function can be used to set index and vertex buffers of geometries.
        /// </remarks>
        [DllImport(DLLName, EntryPoint="rtcMapBuffer")]
        public static extern IntPtr MapBuffer(IntPtr scene, uint geomID, BufferType type);

        /// <summary>
        /// Unmaps specified buffer.
        /// </summary>
        /// <remarks>
        /// A buffer has to be unmapped before the rtcEnable, rtcDisable,
        /// rtcUpdate, or rtcDeleteGeometry calls are executed.
        /// </remarks>
        [DllImport(DLLName, EntryPoint="rtcUnmapBuffer")]
        public static extern void UnmapBuffer(IntPtr scene, uint geomID, BufferType type);

        /// <summary>
        /// Enable geometry. Enabled geometry can be hit by a ray.
        /// </summary>
        [DllImport(DLLName, EntryPoint="rtcEnable")]
        public static extern void EnableGeometry(IntPtr scene, uint geomID);

        /// <summary>
        /// Disable geometry.
        /// </summary>
        /// <remarks>
        /// Disabled geometry is not hit by any ray. Disabling and enabling
        /// geometry gives higher performance than deleting and recreating
        /// geometry.
        /// </remarks>
        [DllImport(DLLName, EntryPoint="rtcDisable")]
        public static extern void DisableGeometry(IntPtr scene, uint geomID);

        /// <summary>
        /// Update geometry.
        /// </summary>
        /// <remarks>
        /// This function has to get called, each time the user modifies some
        /// geometry for dynamic scenes. The function does not have to get
        /// called after initializing some geometry for the first time.
        /// </remarks>
        [DllImport(DLLName, EntryPoint="rtcUpdate")]
        public static extern void UpdateGeometry(IntPtr scene, uint geomID);

        /// <summary>
        /// Deletes the geometry.
        /// </summary>
        [DllImport(DLLName, EntryPoint="rtcDeleteGeometry")]
        public static extern void DeleteGeometry(IntPtr scene, uint geomID);

        #endregion

        #region Ray Packet Structures

        /// <summary>
        /// Ray structure for an individual ray.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 84)]
        public unsafe struct RayPacket1
        {
            [FieldOffset( 0)] public float orgX;
            [FieldOffset( 4)] public float orgY;
            [FieldOffset( 8)] public float orgZ;
            [FieldOffset(16)] public float dirX;
            [FieldOffset(20)] public float dirY;
            [FieldOffset(24)] public float dirZ;
            [FieldOffset(32)] public float tnear;
            [FieldOffset(36)] public float tfar;
            [FieldOffset(40)] public float time;
            [FieldOffset(44)] public uint mask;

            [FieldOffset(48)] public float NgX;
            [FieldOffset(52)] public float NgY;
            [FieldOffset(56)] public float NgZ;

            [FieldOffset(64)] public float u;
            [FieldOffset(68)] public float v;

            [FieldOffset(72)] public uint geomID;
            [FieldOffset(76)] public uint primID;
            [FieldOffset(80)] public uint instID;

			/// <summary>
			/// Converts the raw packet into a user-friendly intersection structure.
			/// </summary>
			public Intersection<T> ToIntersection<T>(Scene<T> scene) where T : class, IInstance
			{
				if (geomID == RTC.InvalidGeometryID)
					return Intersection<T>.None;
				else
				{
					T instance = scene[instID];

					return new Intersection<T>(primID, instance.Geometry[geomID], instance,
					                           tfar, u, v, NgX, NgY, NgZ);
				}
			}
        }

        /// <summary>
        /// Ray structure for a packet of 4 rays.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 288)]
        public unsafe struct RayPacket4
        {
            [FieldOffset(  0)] public fixed float orgX[4];
            [FieldOffset( 16)] public fixed float orgY[4];
            [FieldOffset( 32)] public fixed float orgZ[4];
            [FieldOffset( 48)] public fixed float dirX[4];
            [FieldOffset( 64)] public fixed float dirY[4];
            [FieldOffset( 80)] public fixed float dirZ[4];
            [FieldOffset( 96)] public fixed float tnear[4];
            [FieldOffset(112)] public fixed float tfar[4];
            [FieldOffset(128)] public fixed float time[4];
            [FieldOffset(144)] public fixed uint mask[4];

            [FieldOffset(160)] public fixed float NgX[4];
            [FieldOffset(176)] public fixed float NgY[4];
            [FieldOffset(192)] public fixed float NgZ[4];

            [FieldOffset(208)] public fixed float u[4];
            [FieldOffset(224)] public fixed float v[4];

            [FieldOffset(240)] public fixed uint geomID[4];
            [FieldOffset(256)] public fixed uint primID[4];
            [FieldOffset(272)] public fixed uint instID[4];

			/// <summary>
			/// Converts the raw packet into an array of user-friendly intersection structures.
			/// </summary>
			public unsafe Intersection<T>[] ToIntersection<T>(Scene<T> scene) where T : class, IInstance
			{
				fixed (RayPacket4 *v = &this)
				{
					Intersection<T>[] hits = new Intersection<T>[4];

					for (int t = 0; t < 4; ++t)
					{
						if (v->geomID[t] == RTC.InvalidGeometryID)
							hits[t] = Intersection<T>.None;
						else
						{
							T instance = scene[v->instID[t]];

							hits [t] = new Intersection<T>(v->primID [t], instance.Geometry[v->geomID[t]], instance,
							                             v->tfar [t], v->u [t], v->v [t], v->NgX [t], v->NgY [t], v->NgZ [t]);
						}
					}

					return hits;
				}
			}
        }

        /// <summary>
        /// Ray structure for a packet of 8 rays.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 576)]
        public unsafe struct RayPacket8
        {
            [FieldOffset(  0)] public fixed float orgX[8];
            [FieldOffset( 32)] public fixed float orgY[8];
            [FieldOffset( 64)] public fixed float orgZ[8];
            [FieldOffset( 96)] public fixed float dirX[8];
            [FieldOffset(128)] public fixed float dirY[8];
            [FieldOffset(160)] public fixed float dirZ[8];
            [FieldOffset(196)] public fixed float tnear[8];
            [FieldOffset(224)] public fixed float tfar[8];
            [FieldOffset(256)] public fixed float time[8];
            [FieldOffset(288)] public fixed uint mask[8];

            [FieldOffset(320)] public fixed float NgX[8];
            [FieldOffset(352)] public fixed float NgY[8];
            [FieldOffset(384)] public fixed float NgZ[8];

            [FieldOffset(416)] public fixed float u[8];
            [FieldOffset(448)] public fixed float v[8];

            [FieldOffset(480)] public fixed uint geomID[8];
            [FieldOffset(512)] public fixed uint primID[8];
            [FieldOffset(544)] public fixed uint instID[8];

			/// <summary>
			/// Converts the raw packet into an array of user-friendly intersection structures.
			/// </summary>
			public unsafe Intersection<T>[] ToIntersection<T>(Scene<T> scene) where T : class, IInstance
			{
				fixed (RayPacket8 *v = &this)
				{
					Intersection<T>[] hits = new Intersection<T>[8];

					for (int t = 0; t < 8; ++t)
					{
						if (v->geomID[t] == RTC.InvalidGeometryID)
							hits[t] = Intersection<T>.None;
						else
						{
							T instance = scene[v->instID[t]];

							hits [t] = new Intersection<T>(v->primID [t], instance.Geometry[v->geomID[t]], instance,
							                               v->tfar [t], v->u [t], v->v [t], v->NgX [t], v->NgY [t], v->NgZ [t]);
						}
					}

					return hits;
				}
			}
        }

        /// <summary>
        /// Ray structure for a packet of 16 rays.
        /// </summary>
        [StructLayout(LayoutKind.Explicit, Size = 1152)]
        public unsafe struct RayPacket16
        {
            [FieldOffset(   0)] public fixed float orgX[8];
            [FieldOffset(  64)] public fixed float orgY[8];
            [FieldOffset( 128)] public fixed float orgZ[8];
            [FieldOffset( 196)] public fixed float dirX[8];
            [FieldOffset( 256)] public fixed float dirY[8];
            [FieldOffset( 320)] public fixed float dirZ[8];
            [FieldOffset( 384)] public fixed float tnear[8];
            [FieldOffset( 448)] public fixed float tfar[8];
            [FieldOffset( 512)] public fixed float time[8];
            [FieldOffset( 576)] public fixed uint mask[8];

            [FieldOffset( 640)] public fixed float NgX[8];
            [FieldOffset( 704)] public fixed float NgY[8];
            [FieldOffset( 768)] public fixed float NgZ[8];

            [FieldOffset( 832)] public fixed float u[8];
            [FieldOffset( 896)] public fixed float v[8];

            [FieldOffset( 960)] public fixed uint geomID[8];
            [FieldOffset(1024)] public fixed uint primID[8];
            [FieldOffset(1088)] public fixed uint instID[8];

			/// <summary>
			/// Converts the raw packet into an array of user-friendly intersection structures.
			/// </summary>
			public unsafe Intersection<T>[] ToIntersection<T>(Scene<T> scene) where T : class, IInstance
			{
				fixed (RayPacket16 *v = &this)
				{
					Intersection<T>[] hits = new Intersection<T>[16];

					for (int t = 0; t < 16; ++t)
					{
						if (v->geomID[t] == RTC.InvalidGeometryID)
							hits[t] = Intersection<T>.None;
						else
						{
							T instance = scene[v->instID[t]];

							hits [t] = new Intersection<T>(v->primID [t], instance.Geometry[v->geomID[t]], instance,
							                               v->tfar [t], v->u [t], v->v [t], v->NgX [t], v->NgY [t], v->NgZ [t]);
						}
					}

					return hits;
				}
			}
        }

        #endregion

        #region Embree Intersection/Occlusion Functions

        [DllImport(DLLName, EntryPoint="rtcOccluded")]
        public static extern unsafe void Occluded1(IntPtr scene, RayPacket1* ray);

        [DllImport(DLLName, EntryPoint="rtcOccluded4")]
        public static extern unsafe void Occluded4(uint* valid, IntPtr scene, RayPacket4* ray);

        [DllImport(DLLName, EntryPoint="rtcOccluded8")]
        public static extern unsafe void Occluded8(uint* valid, IntPtr scene, RayPacket8* ray);

        [DllImport(DLLName, EntryPoint="rtcOccluded16")]
        public static extern unsafe void Occluded16(uint* valid, IntPtr scene, RayPacket16* ray);

        [DllImport(DLLName, EntryPoint="rtcIntersect")]
        public static extern unsafe void Intersect1(IntPtr scene, RayPacket1* ray);

        [DllImport(DLLName, EntryPoint="rtcIntersect4")]
        public static extern unsafe void Intersect4(uint* valid, IntPtr scene, RayPacket4* ray);

        [DllImport(DLLName, EntryPoint="rtcIntersect8")]
        public static extern unsafe void Intersect8(uint* valid, IntPtr scene, RayPacket8* ray);

        [DllImport(DLLName, EntryPoint="rtcIntersect16")]
        public static extern unsafe void Intersect16(uint* valid, IntPtr scene, RayPacket16* ray);

        #endregion

        #region Ray Tracing Functions

        /// <summary>
        /// Sentinel value indicating no intersection.
        /// </summary>
        public const uint InvalidGeometryID = 0xFFFFFFFF;

        /// <summary>
        /// Manages per-thread aligned ray structures.
        /// </summary>
        public static unsafe class RayInterop
        {
            [ThreadStatic] private static RayPacket1*  packet1;
            [ThreadStatic] private static RayPacket4*  packet4;
            [ThreadStatic] private static RayPacket8*  packet8;
            [ThreadStatic] private static RayPacket16* packet16;

            [ThreadStatic] private static uint* activity;
            public const uint Active   = 0xFFFFFFFF;
            public const uint Inactive = 0x00000000;

            public static uint* Activity
            {
                get
                {
                    if (activity == null)
                        activity = (uint*)Align(typeof(uint), 64);

                    return activity;
                }
            }

            public static RayPacket1* Packet1
            {
                get
                {
                    if (packet1 == null)
                    {
                        packet1 = (RayPacket1*)Align(typeof(RayPacket1), 16);
                        packet1->mask = RTC.InvalidGeometryID; // not used
                    }

                    return packet1;
                }
            }

            public static RayPacket4* Packet4
            {
                get
                {
                    if (packet4 == null)
                    {
                        packet4 = (RayPacket4*)Align(typeof(RayPacket4), 16);
						for (int t = 0; t < 4; ++t) packet4->mask[t] = RTC.InvalidGeometryID;
                    }

                    return packet4;
                }
            }

			public static RayPacket8* Packet8
			{
				get
				{
					if (packet8 == null)
					{
						packet8 = (RayPacket8*)Align(typeof(RayPacket8), 32);
						for (int t = 0; t < 8; ++t) packet8->mask[t] = RTC.InvalidGeometryID;
					}

					return packet8;
				}
			}

			public static RayPacket16* Packet16
			{
				get
				{
					if (packet16 == null)
					{
						packet16 = (RayPacket16*)Align(typeof(RayPacket16), 64);
						for (int t = 0; t < 16; ++t) packet16->mask[t] = RTC.InvalidGeometryID;
					}

					return packet16;
				}
			}

            /// <summary>
            /// Allocates memory aligned to a specific boundary.
            /// </summary>
            public static void* Align(Type type, int alignment)
            {
                byte* ptr = (byte*)Marshal.AllocHGlobal(Marshal.SizeOf(type) + alignment - 1);
                while ((long)ptr % alignment != 0) ptr++;
                return (void*)ptr;
            }
        }

        #endregion

        #region Instancing Functions

        /// <summary>
        /// Supported types of matrix layout for functions involving matrices.
        /// </summary>
        public enum MatrixLayout
        {
            /// <summary>
            /// Row-major affine (3x4) matrix.
            /// </summary>
            RowMajor = 0,

            /// <summary>
            /// Column-major affine (3x4) matrix.
            /// </summary>
            ColumnMajor = 1,

            /// <summary>
            /// Column-major homogenous (4x4) matrix.
            /// </summary>
            ColumnMajorAligned16 = 2,
        }

        /// <summary>
        /// Creates a new scene instance.
        /// </summary>
        [DllImport(DLLName, EntryPoint="rtcNewInstance")]
        public static extern uint NewInstance(IntPtr scene, IntPtr source);

        /// <summary>
        /// Sets transformation of the instance.
        /// </summary>
        [DllImport(DLLName, EntryPoint="rtcSetTransform")]
        public static extern void SetTransform(IntPtr scene, uint geomID, MatrixLayout layout, IntPtr transform);

        #endregion

        #region Lifetime Management

        /// <summary>
        /// Initializes the Embree ray tracing core.
        /// </summary>
        [DllImport(DLLName, EntryPoint="rtcInit", CharSet=CharSet.Ansi)]
        public static extern void InitEmbree(String cfg = null);

        /// <summary>
        /// Shuts down Embree.
        /// </summary>
        [DllImport(DLLName, EntryPoint="rtcExit")]
        public static extern void FreeEmbree();

        /// <summary>
        /// Registers usage of the Embree library.
        /// </summary>
        public static void Register(String cfg = null)
        {
            if (refCount++ == 0)
            {
                InitEmbree(cfg);
                CheckLastError();
            }
        }

        /// <summary>
        /// Unregisters usage of the Embree library.
        /// </summary>
        public static void Unregister()
        {
            if (--refCount == 0)
                FreeEmbree();
        }

        private static int refCount = 0;

        #endregion
    }
}

