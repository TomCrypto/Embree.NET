using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using Embree;

namespace Benchmark
{
    /// <summary>
    /// A set of benchmarking routines.
    /// </summary>
    static class Benchmarks
    {
        #region Coherent Intersection Benchmarks

        private static void CoherentIntersect1(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; ++y)
                for (var x = 0; x < w; ++x)
            {
                var traversal = new Ray(Vector.Zero, new Vector((float)x * invW, 1, (float)y * invH));

                scene.Intersects<Ray>(traversal);
            }
        }

        private static void CoherentIntersect4(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; y += 2)
                for (var x = 0; x < w; x += 2)
            {
                var rays = new[]
                {
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 1) * invH)),
                };

                scene.Intersects4(rays);
            }
        }

        private static void CoherentIntersect8(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; y += 4)
                for (var x = 0; x < w; x += 2)
            {
                var rays = new[]
                {
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 2) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 2) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 3) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 3) * invH)),
                };

                scene.Intersects8(rays);
            }
        }

        private static void CoherentIntersect16(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; y += 4)
                for (var x = 0; x < w; x += 4)
            {
                var rays = new[]
                {
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 2) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 2) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 2) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 2) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 3) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 3) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 3) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 3) * invH)),
                };

                scene.Intersects16(rays);
            }
        }

        #endregion

        #region Incoherent Intersection Benchmarks

        private static void IncoherentIntersect1(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray; ++t)
                scene.Intersects<Ray>(rays[t]);
        }

        private static void IncoherentIntersect4(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray / 4; ++t)
            {
                var rays2 = new[]
                {
                    rays[t * 4 + 0],
                    rays[t * 4 + 1],
                    rays[t * 4 + 2],
                    rays[t * 4 + 3],
                };

                scene.Intersects4(rays2);
            }
        }

        private static void IncoherentIntersect8(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray / 8; ++t)
            {
                var rays2 = new[]
                {
                    rays[t * 8 + 0],
                    rays[t * 8 + 1],
                    rays[t * 8 + 2],
                    rays[t * 8 + 3],
                    rays[t * 8 + 4],
                    rays[t * 8 + 5],
                    rays[t * 8 + 6],
                    rays[t * 8 + 7],
                };

                scene.Intersects8(rays2);
            }
        }

        private static void IncoherentIntersect16(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray / 16; ++t)
            {
                var rays2 = new[]
                {
                    rays[t * 16 +  0],
                    rays[t * 16 +  1],
                    rays[t * 16 +  2],
                    rays[t * 16 +  3],
                    rays[t * 16 +  4],
                    rays[t * 16 +  5],
                    rays[t * 16 +  6],
                    rays[t * 16 +  7],
                    rays[t * 16 +  8],
                    rays[t * 16 +  9],
                    rays[t * 16 + 10],
                    rays[t * 16 + 11],
                    rays[t * 16 + 12],
                    rays[t * 16 + 13],
                    rays[t * 16 + 14],
                    rays[t * 16 + 15],
                };

                scene.Intersects16(rays2);
            }
        }

        #endregion

        #region Coherent Occlusion Benchmarks

        private static void CoherentOcclusion1(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; ++y)
                for (var x = 0; x < w; ++x)
                    scene.Occludes<Ray>(new Ray(Vector.Zero, new Vector((float)x * invW, 1, (float)y * invH)));
        }

        private static void CoherentOcclusion4(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; y += 2)
                for (var x = 0; x < w; x += 2)
            {
                var rays = new[]
                {
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 1) * invH)),
                };

                scene.Occludes4(rays);
            }
        }

        private static void CoherentOcclusion8(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; y += 4)
                for (var x = 0; x < w; x += 2)
            {
                var rays = new[]
                {
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 2) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 2) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 3) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 3) * invH)),
                };

                scene.Occludes8(rays);
            }
        }

        private static void CoherentOcclusion16(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; y += 4)
                for (var x = 0; x < w; x += 4)
            {
                var rays = new[]
                {
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 0) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 1) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 2) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 2) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 2) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 2) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 3) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 3) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 3) * invH)),
                    new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 3) * invH)),
                };

                scene.Occludes16(rays);
            }
        }

        #endregion

        #region Incoherent Occlusion Benchmarks

        private static void IncoherentOcclusion1(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray; ++t)
                scene.Occludes<Ray>(rays[t]);
        }

        private static void IncoherentOcclusion4(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray / 4; ++t)
            {
                var rays2 = new[]
                {
                    rays[t * 4 + 0],
                    rays[t * 4 + 1],
                    rays[t * 4 + 2],
                    rays[t * 4 + 3],
                };

                scene.Occludes4(rays2);
            }
        }

        private static void IncoherentOcclusion8(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray / 8; ++t)
            {
                var rays2 = new[]
                {
                    rays[t * 8 + 0],
                    rays[t * 8 + 1],
                    rays[t * 8 + 2],
                    rays[t * 8 + 3],
                    rays[t * 8 + 4],
                    rays[t * 8 + 5],
                    rays[t * 8 + 6],
                    rays[t * 8 + 7],
                };

                scene.Occludes8(rays2);
            }
        }

        private static void IncoherentOcclusion16(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray / 16; ++t)
            {
                var rays2 = new[]
                {
                    rays[t * 16 +  0],
                    rays[t * 16 +  1],
                    rays[t * 16 +  2],
                    rays[t * 16 +  3],
                    rays[t * 16 +  4],
                    rays[t * 16 +  5],
                    rays[t * 16 +  6],
                    rays[t * 16 +  7],
                    rays[t * 16 +  8],
                    rays[t * 16 +  9],
                    rays[t * 16 + 10],
                    rays[t * 16 + 11],
                    rays[t * 16 + 12],
                    rays[t * 16 + 13],
                    rays[t * 16 + 14],
                    rays[t * 16 + 15],
                };

                scene.Occludes16(rays2);
            }
        }

        #endregion

        private static void CreateGeometry(SceneFlags sceneFlags, TraversalFlags traversalFlags, MeshFlags meshFlags, TriangleMesh sphere, int numMeshes)
        {
            using (var scene = new Scene<Sphere>(sceneFlags, traversalFlags))
            {
                for (var t = 0; t < numMeshes; ++t)
                {
                    scene.Add(new Sphere(sceneFlags, traversalFlags, sphere, meshFlags));

                    for (int v = 0; v < sphere.Vertices.Count; ++v)
                        sphere.Vertices[v] = (Vector)sphere.Vertices[v] + new Vector(1, 1, 1);
                }

                scene.Commit();
            }
        }

        #region Benchmark Providers

        public static IEnumerable<Tuple<String, Action, Func<Double, String>>> Intersections(SceneFlags sceneFlags, TraversalFlags traversalFlags, int numPhi, int w, int h)
        {
            Func<Double, String> timer = (t) => String.Format("{0:N3} Mrays/s", 1e-6 * w * h / t);

            using (var scene = new Scene<Sphere>(sceneFlags, traversalFlags))
            {
                scene.Add(new Sphere(sceneFlags, traversalFlags, numPhi));
                scene.Commit();

                if (traversalFlags.HasFlag(TraversalFlags.Single))
                    yield return new Tuple<String, Action, Func<Double, String>>("coherent_intersect1",
                                                                                 () => CoherentIntersect1(scene, w, h),
                                                                                 timer);
                if (traversalFlags.HasFlag(TraversalFlags.Packet4))
                    yield return new Tuple<String, Action, Func<Double, String>>("coherent_intersect4",
                                                                                 () => CoherentIntersect4(scene, w, h),
                                                                                 timer);
                if (traversalFlags.HasFlag(TraversalFlags.Packet8))
                    yield return new Tuple<String, Action, Func<Double, String>>("coherent_intersect8",
                                                                                 () => CoherentIntersect8(scene, w, h),
                                                                                 timer);
                if (traversalFlags.HasFlag(TraversalFlags.Packet16))
                    yield return new Tuple<String, Action, Func<Double, String>>("coherent_intersect16",
                                                                                 () => CoherentIntersect16(scene, w, h),
                                                                                 timer);

                var random = new Random();
                var rays = new Ray[w * h];

                for (var t = 0; t < w * h; ++t)
                    rays[t] = new Ray(Vector.Zero, new Vector(2 * (float)random.NextDouble() - 1,
                                                              2 * (float)random.NextDouble() - 1,
                                                              2 * (float)random.NextDouble() - 1));

                if (traversalFlags.HasFlag(TraversalFlags.Single))
                    yield return new Tuple<String, Action, Func<Double, String>>("incoherent_intersect1",
                                                                                 () => IncoherentIntersect1(scene, w * h, rays),
                                                                                 timer);
                if (traversalFlags.HasFlag(TraversalFlags.Packet4))
                    yield return new Tuple<String, Action, Func<Double, String>>("incoherent_intersect4",
                                                                                 () => IncoherentIntersect4(scene, w * h, rays),
                                                                                 timer);
                if (traversalFlags.HasFlag(TraversalFlags.Packet8))
                    yield return new Tuple<String, Action, Func<Double, String>>("incoherent_intersect8",
                                                                                 () => IncoherentIntersect8(scene, w * h, rays),
                                                                                 timer);
                if (traversalFlags.HasFlag(TraversalFlags.Packet16))
                    yield return new Tuple<String, Action, Func<Double, String>>("incoherent_intersect16",
                                                                                 () => IncoherentIntersect16(scene, w * h, rays),
                                                                                 timer);
            }
        }

        public static IEnumerable<Tuple<String, Action, Func<Double, String>>> Occlusions(SceneFlags sceneFlags, TraversalFlags traversalFlags, int numPhi, int w, int h)
        {
            Func<Double, String> timer = (t) => String.Format("{0:N3} Mrays/s", 1e-6 * w * h / t);

            using (var scene = new Scene<Sphere>(sceneFlags, traversalFlags))
            {
                scene.Add(new Sphere(sceneFlags, traversalFlags, numPhi));
                scene.Commit();

                if (traversalFlags.HasFlag(TraversalFlags.Single))
                    yield return new Tuple<String, Action, Func<Double, String>>("coherent_occlusion1",
                                                                                 () => CoherentOcclusion1(scene, w, h),
                                                                                 timer);
                if (traversalFlags.HasFlag(TraversalFlags.Packet4))
                    yield return new Tuple<String, Action, Func<Double, String>>("coherent_occlusion4",
                                                                                 () => CoherentOcclusion4(scene, w, h),
                                                                                 timer);
                if (traversalFlags.HasFlag(TraversalFlags.Packet8))
                    yield return new Tuple<String, Action, Func<Double, String>>("coherent_occlusion8",
                                                                                 () => CoherentOcclusion8(scene, w, h),
                                                                                 timer);
                if (traversalFlags.HasFlag(TraversalFlags.Packet16))
                    yield return new Tuple<String, Action, Func<Double, String>>("coherent_occlusion16",
                                                                                 () => CoherentOcclusion16(scene, w, h),
                                                                                 timer);

                var random = new Random();
                var rays = new Ray[w * h];

                for (var t = 0; t < w * h; ++t)
                    rays[t] = new Ray(Vector.Zero, new Vector(2 * (float)random.NextDouble() - 1,
                                                              2 * (float)random.NextDouble() - 1,
                                                              2 * (float)random.NextDouble() - 1));

                if (traversalFlags.HasFlag(TraversalFlags.Single))
                    yield return new Tuple<String, Action, Func<Double, String>>("incoherent_occlusion1",
                                                                                 () => IncoherentOcclusion1(scene, w * h, rays),
                                                                                 timer);
                if (traversalFlags.HasFlag(TraversalFlags.Packet4))
                    yield return new Tuple<String, Action, Func<Double, String>>("incoherent_occlusion4",
                                                                                 () => IncoherentOcclusion4(scene, w * h, rays),
                                                                                 timer);
                if (traversalFlags.HasFlag(TraversalFlags.Packet8))
                    yield return new Tuple<String, Action, Func<Double, String>>("incoherent_occlusion8",
                                                                                 () => IncoherentOcclusion8(scene, w * h, rays),
                                                                                 timer);
                if (traversalFlags.HasFlag(TraversalFlags.Packet16))
                    yield return new Tuple<String, Action, Func<Double, String>>("incoherent_occlusion16",
                                                                                 () => IncoherentOcclusion16(scene, w * h, rays),
                                                                                 timer);
            }
        }

        public static IEnumerable<Tuple<String, Action, Func<Double, String>>> Geometries(SceneFlags sceneFlags, TraversalFlags traversalFlags)
        {
            var items = new Dictionary<String, Tuple<int, int>>
            {
                { "120", new Tuple<int, int>(6, 1) },
                { "1k", new Tuple<int, int>(17, 1) },
                { "10k", new Tuple<int, int>(51, 1) },
                { "100k", new Tuple<int, int>(159, 1) },
                { "1000k_1", new Tuple<int, int>(501, 1) },
                { "100k_10", new Tuple<int, int>(159, 10) },
                { "10k_100", new Tuple<int, int>(51, 100) },
                { "1k_1000", new Tuple<int, int>(17, 1000) },
                //{ "120_10000", new Tuple<int, int>(6, 8334) },
            };

            foreach (var item in items)
            {
                var sphere = (TriangleMesh)Sphere.GenerateSphere(item.Value.Item1);
                yield return new Tuple<String, Action, Func<Double, String>>("create_static_geometry_" + item.Key,
                                                                             () => CreateGeometry(sceneFlags, traversalFlags, MeshFlags.Static, sphere, item.Value.Item2),
                                                                             (t) => String.Format("{0:N3} Mtris/s", 1e-6 * item.Value.Item2 * sphere.Indices.Count / 3));
            }

            foreach (var item in items)
            {
                var sphere = (TriangleMesh)Sphere.GenerateSphere(item.Value.Item1);
                yield return new Tuple<String, Action, Func<Double, String>>("create_dynamic_geometry_" + item.Key,
                                                                             () => CreateGeometry(sceneFlags, traversalFlags, MeshFlags.Dynamic, sphere, item.Value.Item2),
                                                                             (t) => String.Format("{0:N3} Mtris/s", 1e-6 * item.Value.Item2 * sphere.Indices.Count / 3));
            }
        }

        #endregion
    }
}

