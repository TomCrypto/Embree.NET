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

#if false

        /// <summary>
        /// Represents a size_t equivalent for interop.
        /// </summary>
        public struct SizeInt : IComparable, IComparable<SizeInt>, IEquatable<SizeInt>, IConvertible, ValueType
        {
            private readonly UIntPtr x;

            public SizeInt(UIntPtr x)
            {
                this.x = x;
            }

            public static implicit operator UIntPtr(int x)
            {
                return new SizeInt((UIntPtr)x);
            }

            public static implicit operator UIntPtr(uint x)
            {
                return new SizeInt((UIntPtr)x);
            }

            public static implicit operator int(UIntPtr x)
            {
                return x.;
            }

            public static SizeInt operator+(SizeInt a, SizeInt b)
            {
                return 
            }
        }

#endif

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
        public static extern uint NewTriangleMesh(IntPtr scene, MeshFlags flags, IntPtr numTriangles, IntPtr numVertices, IntPtr numTimeSteps);

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
            [ThreadStatic] private static RayPacket1*  rayPacket1;
            [ThreadStatic] private static RayPacket4*  rayPacket4;
            [ThreadStatic] private static RayPacket8*  rayPacket8;
            [ThreadStatic] private static RayPacket16* rayPacket16;

            [ThreadStatic] private static uint* activity;
            private const uint Active   = 0xFFFFFFFF;
            private const uint Inactive = 0x00000000;

            #region Ray Structure Conversions

            private static void PrepareActivityFlags()
            {
                if (activity == null)
                {
                    activity = (uint*)Marshal.AllocHGlobal(sizeof(uint) * 16 + 63);
                    while ((long)activity % 16 != 0) activity = (uint*)((byte*)activity + 1);
                }
            }

            private static void EncodeRayPacket1(Traversal ray)
            {
                if (rayPacket1 == null)
                {
                    rayPacket1 = (RayPacket1*)Marshal.AllocHGlobal(Marshal.SizeOf(typeof(RayPacket1)) + 15);
                    while ((long)rayPacket1 % 16 != 0) rayPacket1 = (RayPacket1*)((byte*)rayPacket1 + 1);
                }

                rayPacket1->orgX = ray.Ray.Origin.X;
                rayPacket1->orgY = ray.Ray.Origin.Y;
                rayPacket1->orgZ = ray.Ray.Origin.Z;

                rayPacket1->dirX = ray.Ray.Direction.X;
                rayPacket1->dirY = ray.Ray.Direction.Y;
                rayPacket1->dirZ = ray.Ray.Direction.Z;

                rayPacket1->geomID = InvalidGeometryID;
                rayPacket1->primID = InvalidGeometryID;
                rayPacket1->instID = InvalidGeometryID;
                rayPacket1->mask   = InvalidGeometryID;

                rayPacket1->time   = ray.Time;
                rayPacket1->tnear  = ray.Near;
                rayPacket1->tfar   = ray.Far;
            }

            private static void EncodeRayPacket4(Traversal[] rays)
            {
                if (rayPacket4 == null)
                {
                    rayPacket4 = (RayPacket4*)Marshal.AllocHGlobal(Marshal.SizeOf(typeof(RayPacket4)) + 15);
                    while ((long)rayPacket4 % 16 != 0) rayPacket4 = (RayPacket4*)((byte*)rayPacket4 + 1);
                }

                PrepareActivityFlags();

                for (int t = 0; t < 4; ++t)
                {
                    *(activity + t) = (rays[t].Active) ? Active : Inactive;

                    rayPacket4->geomID[t] = InvalidGeometryID;
                    rayPacket4->primID[t] = InvalidGeometryID;
                    rayPacket4->instID[t] = InvalidGeometryID;
                    rayPacket4->mask[t]   = InvalidGeometryID;

                    if (rays[t].Active)
                    {
                        rayPacket4->orgX[t] = rays[t].Ray.Origin.X;
                        rayPacket4->orgY[t] = rays[t].Ray.Origin.Y;
                        rayPacket4->orgZ[t] = rays[t].Ray.Origin.Z;

                        rayPacket4->dirX[t] = rays[t].Ray.Direction.X;
                        rayPacket4->dirY[t] = rays[t].Ray.Direction.Y;
                        rayPacket4->dirZ[t] = rays[t].Ray.Direction.Z;

                        rayPacket4->time[t]  = rays[t].Time;
                        rayPacket4->tnear[t] = rays[t].Near;
                        rayPacket4->tfar[t]  = rays[t].Far;
                    }
                }
            }

            private static void EncodeRayPacket8(Traversal[] rays)
            {
                if (rayPacket8 == null)
                {
                    rayPacket8 = (RayPacket8*)Marshal.AllocHGlobal(Marshal.SizeOf(typeof(RayPacket8)) + 31);
                    while ((long)rayPacket8 % 32 != 0) rayPacket8 = (RayPacket8*)((byte*)rayPacket8 + 1);
                }

                PrepareActivityFlags();

                for (int t = 0; t < 8; ++t)
                {
                    *(activity + t) = (rays[t].Active) ? Active : Inactive;

                    rayPacket8->geomID[t] = InvalidGeometryID;
                    rayPacket8->primID[t] = InvalidGeometryID;
                    rayPacket8->instID[t] = InvalidGeometryID;
                    rayPacket8->mask[t]   = InvalidGeometryID;

                    if (rays[t].Active)
                    {
                        rayPacket8->orgX[t] = rays[t].Ray.Origin.X;
                        rayPacket8->orgY[t] = rays[t].Ray.Origin.Y;
                        rayPacket8->orgZ[t] = rays[t].Ray.Origin.Z;

                        rayPacket8->dirX[t] = rays[t].Ray.Direction.X;
                        rayPacket8->dirY[t] = rays[t].Ray.Direction.Y;
                        rayPacket8->dirZ[t] = rays[t].Ray.Direction.Z;

                        rayPacket8->time[t]  = rays[t].Time;
                        rayPacket8->tnear[t] = rays[t].Near;
                        rayPacket8->tfar[t]  = rays[t].Far;
                    }
                }
            }

            private static void EncodeRayPacket16(Traversal[] rays)
            {
                if (rayPacket16 == null)
                {
                    rayPacket16 = (RayPacket16*)Marshal.AllocHGlobal(Marshal.SizeOf(typeof(RayPacket16)) + 31);
                    while ((long)rayPacket16 % 64 != 0) rayPacket16 = (RayPacket16*)((byte*)rayPacket16 + 1);
                }

                PrepareActivityFlags();

                for (int t = 0; t < 16; ++t)
                {
                    *(activity + t) = (rays[t].Active) ? Active : Inactive;

                    rayPacket16->geomID[t] = InvalidGeometryID;
                    rayPacket16->primID[t] = InvalidGeometryID;
                    rayPacket16->instID[t] = InvalidGeometryID;
                    rayPacket16->mask[t]   = InvalidGeometryID;

                    if (rays[t].Active)
                    {
                        rayPacket16->orgX[t] = rays[t].Ray.Origin.X;
                        rayPacket16->orgY[t] = rays[t].Ray.Origin.Y;
                        rayPacket16->orgZ[t] = rays[t].Ray.Origin.Z;

                        rayPacket16->dirX[t] = rays[t].Ray.Direction.X;
                        rayPacket16->dirY[t] = rays[t].Ray.Direction.Y;
                        rayPacket16->dirZ[t] = rays[t].Ray.Direction.Z;

                        rayPacket16->time[t]  = rays[t].Time;
                        rayPacket16->tnear[t] = rays[t].Near;
                        rayPacket16->tfar[t]  = rays[t].Far;
                    }
                }
            }

            #endregion

            public static Boolean OcclusionTest1(IntPtr scene, Traversal ray)
            {
                EncodeRayPacket1(ray);
                Occluded1(scene, rayPacket1);

                return rayPacket1->geomID == 0;
            }

            public static Boolean[] OcclusionTest4(IntPtr scene, Traversal[] rays)
            {
                EncodeRayPacket4(rays);
                Occluded4(activity, scene, rayPacket4);

                return new[]
                {
                    rayPacket4->geomID[0] == 0,
                    rayPacket4->geomID[1] == 0,
                    rayPacket4->geomID[2] == 0,
                    rayPacket4->geomID[3] == 0,
                };
            }

            public static Boolean[] OcclusionTest8(IntPtr scene, Traversal[] rays)
            {
                EncodeRayPacket8(rays);
                Occluded8(activity, scene, rayPacket8);

                return new[]
                {
                    rayPacket8->geomID[0] == 0,
                    rayPacket8->geomID[1] == 0,
                    rayPacket8->geomID[2] == 0,
                    rayPacket8->geomID[3] == 0,
                    rayPacket8->geomID[4] == 0,
                    rayPacket8->geomID[5] == 0,
                    rayPacket8->geomID[6] == 0,
                    rayPacket8->geomID[7] == 0,
                };
            }

            public static Boolean[] OcclusionTest16(IntPtr scene, Traversal[] rays)
            {
                EncodeRayPacket16(rays);
                Occluded16(activity, scene, rayPacket16);

                return new[]
                {
                    rayPacket16->geomID[ 0] == 0,
                    rayPacket16->geomID[ 1] == 0,
                    rayPacket16->geomID[ 2] == 0,
                    rayPacket16->geomID[ 3] == 0,
                    rayPacket16->geomID[ 4] == 0,
                    rayPacket16->geomID[ 5] == 0,
                    rayPacket16->geomID[ 6] == 0,
                    rayPacket16->geomID[ 7] == 0,
                    rayPacket16->geomID[ 8] == 0,
                    rayPacket16->geomID[ 9] == 0,
                    rayPacket16->geomID[10] == 0,
                    rayPacket16->geomID[11] == 0,
                    rayPacket16->geomID[12] == 0,
                    rayPacket16->geomID[13] == 0,
                    rayPacket16->geomID[14] == 0,
                    rayPacket16->geomID[15] == 0,
                };
            }

            public static RayPacket1 Intersection1(IntPtr scene, Traversal ray)
            {
                EncodeRayPacket1(ray);
                Intersect1(scene, rayPacket1);
                return *rayPacket1;
            }

            public static RayPacket4 Intersection4(IntPtr scene, Traversal[] rays)
            {
                EncodeRayPacket4(rays);
                Intersect4(activity, scene, rayPacket4);
                return *rayPacket4;
            }

            public static RayPacket8 Intersection8(IntPtr scene, Traversal[] rays)
            {
                EncodeRayPacket8(rays);
                Intersect8(activity, scene, rayPacket8);
                return *rayPacket8;
            }

            public static RayPacket16 Intersection16(IntPtr scene, Traversal[] rays)
            {
                EncodeRayPacket16(rays);
                Intersect16(activity, scene, rayPacket16);
                return *rayPacket16;
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

