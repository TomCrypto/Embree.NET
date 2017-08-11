/* Embree.NET
 * ==========
 *
 * Some mesh types to use, for instance triangle
 * meshes, and so on - specific geometric shapes
 * probably do not belong here, but should exist
 * in a static helper class outside the library.
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
    /// Represents a simple triangle mesh.
    /// </summary>
    public class TriangleMesh : IMesh
    {
        private readonly IList<Int32> indices;
        private readonly IList<IEmbreePoint> vertices;
        private readonly int triangleCount, vertexCount;
        private readonly Device device;
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
        public TriangleMesh(Device device, IList<Int32> indices, IList<IEmbreePoint> vertices)
        {
            this.device = device;
            this.indices = indices;
            this.vertices = vertices;
            this.vertexCount = vertices.Count;
            this.triangleCount = indices.Count / 3;
            device.CheckLastError();

            if (vertexCount == 0)
                throw new ArgumentOutOfRangeException("No vertices in mesh");

            if (triangleCount == 0)
                throw new ArgumentOutOfRangeException("No triangles in mesh");
        }

        public uint Add(IntPtr scenePtr, MeshFlags flags)
        {
            var meshID = RTC.NewTriangleMesh(scenePtr, flags, triangleCount, vertexCount, 1);
            device.CheckLastError();
            return meshID;
        }

        public void Update(uint geomID, IntPtr scenePtr)
        {
            if (indices.Count / 3 == triangleCount)
            {
                var indexBuffer = RTC.MapBuffer(scenePtr, geomID, RTC.BufferType.IndexBuffer);
                device.CheckLastError();

                Marshal.Copy(indices.ToArray(), 0, indexBuffer, indices.Count);

                RTC.UnmapBuffer(scenePtr, geomID, RTC.BufferType.IndexBuffer);
            }
            else
                throw new InvalidOperationException("Index buffer length was changed.");

            if (vertices.Count == vertexCount)
            {
                var vertexBuffer = RTC.MapBuffer(scenePtr, geomID, RTC.BufferType.VertexBuffer);
                device.CheckLastError();

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
        private readonly int triangleCount, vertexCount;
        private readonly Device device;

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
        public TriangleMeshMotion(Device device, IList<Int32> indices, IList<IEmbreePoint> vertices0, IList<IEmbreePoint> vertices1)
        {
            if (vertices0.Count != vertices1.Count)
                throw new ArgumentException("Both vertex buffers must have the same length");
            this.device = device;
            this.indices = indices;
            this.vertices0 = vertices0;
            this.vertices1 = vertices1;
            this.vertexCount = vertices0.Count;
            this.triangleCount = indices.Count / 3;

            if (vertexCount == 0)
                throw new ArgumentOutOfRangeException("No vertices in mesh");

            if (triangleCount == 0)
                throw new ArgumentOutOfRangeException("No triangles in mesh");
        }

        public uint Add(IntPtr scenePtr, MeshFlags flags)
        {
            var meshID = RTC.NewTriangleMesh(scenePtr, flags, triangleCount, vertexCount, 2);
            device.CheckLastError();
            return meshID;
        }

        public void Update(uint geomID, IntPtr scenePtr)
        {
            if (indices.Count / 3 == triangleCount)
            {
                var indexBuffer = RTC.MapBuffer(scenePtr, geomID, RTC.BufferType.IndexBuffer);
                device.CheckLastError();

                Marshal.Copy(indices.ToArray(), 0, indexBuffer, indices.Count);

                RTC.UnmapBuffer(scenePtr, geomID, RTC.BufferType.IndexBuffer);
            }
            else
                throw new InvalidOperationException("Index buffer length was changed.");

            if (vertices0.Count == vertexCount)
            {
                var vertexBuffer = RTC.MapBuffer(scenePtr, geomID, RTC.BufferType.VertexBuffer0);
                device.CheckLastError();

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

            if (vertices1.Count == vertexCount)
            {
                var vertexBuffer = RTC.MapBuffer(scenePtr, geomID, RTC.BufferType.VertexBuffer1);
                device.CheckLastError();

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
}

