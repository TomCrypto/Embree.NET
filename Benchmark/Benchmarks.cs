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
                var traversal = new Traversal(new Ray(Vector.Zero, new Vector((float)x * invW, 1, (float)y * invH)));

                scene.Intersects(traversal);
            }
        }

        private static void CoherentIntersect4(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; y += 2)
                for (var x = 0; x < w; x += 2)
            {
                var traversals = new[]
                {
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 1) * invH))),
                };

                scene.Intersects4(traversals);
            }
        }

        private static void CoherentIntersect8(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; y += 4)
                for (var x = 0; x < w; x += 2)
            {
                var traversals = new[]
                {
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 2) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 2) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 3) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 3) * invH))),
                };

                scene.Intersects8(traversals);
            }
        }

        private static void CoherentIntersect16(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; y += 4)
                for (var x = 0; x < w; x += 4)
            {
                var traversals = new[]
                {
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 2) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 2) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 2) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 2) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 3) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 3) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 3) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 3) * invH))),
                };

                scene.Intersects16(traversals);
            }
        }

        #endregion

        #region Incoherent Intersection Benchmarks

        private static void IncoherentIntersect1(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray; ++t)
                scene.Intersects(new Traversal(rays[t]));
        }

        private static void IncoherentIntersect4(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray / 4; ++t)
            {
                var traversals = new[]
                {
                    new Traversal(rays[t * 4 + 0]),
                    new Traversal(rays[t * 4 + 1]),
                    new Traversal(rays[t * 4 + 2]),
                    new Traversal(rays[t * 4 + 3]),
                };

                scene.Intersects4(traversals);
            }
        }

        private static void IncoherentIntersect8(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray / 8; ++t)
            {
                var traversals = new[]
                {
                    new Traversal(rays[t * 8 + 0]),
                    new Traversal(rays[t * 8 + 1]),
                    new Traversal(rays[t * 8 + 2]),
                    new Traversal(rays[t * 8 + 3]),
                    new Traversal(rays[t * 8 + 4]),
                    new Traversal(rays[t * 8 + 5]),
                    new Traversal(rays[t * 8 + 6]),
                    new Traversal(rays[t * 8 + 7]),
                };

                scene.Intersects8(traversals);
            }
        }

        private static void IncoherentIntersect16(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray / 16; ++t)
            {
                var traversals = new[]
                {
                    new Traversal(rays[t * 16 +  0]),
                    new Traversal(rays[t * 16 +  1]),
                    new Traversal(rays[t * 16 +  2]),
                    new Traversal(rays[t * 16 +  3]),
                    new Traversal(rays[t * 16 +  4]),
                    new Traversal(rays[t * 16 +  5]),
                    new Traversal(rays[t * 16 +  6]),
                    new Traversal(rays[t * 16 +  7]),
                    new Traversal(rays[t * 16 +  8]),
                    new Traversal(rays[t * 16 +  9]),
                    new Traversal(rays[t * 16 + 10]),
                    new Traversal(rays[t * 16 + 11]),
                    new Traversal(rays[t * 16 + 12]),
                    new Traversal(rays[t * 16 + 13]),
                    new Traversal(rays[t * 16 + 14]),
                    new Traversal(rays[t * 16 + 15]),
                };

                scene.Intersects16(traversals);
            }
        }

        #endregion

        #region Coherent Occlusion Benchmarks

        private static void CoherentOcclusion1(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; ++y)
                for (var x = 0; x < w; ++x)
                    scene.Occludes(new Traversal(new Ray(Vector.Zero, new Vector((float)x * invW, 1, (float)y * invH))));
        }

        private static void CoherentOcclusion4(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; y += 2)
                for (var x = 0; x < w; x += 2)
            {
                var traversals = new[]
                {
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 1) * invH))),
                };

                scene.Occludes4(traversals);
            }
        }

        private static void CoherentOcclusion8(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; y += 4)
                for (var x = 0; x < w; x += 2)
            {
                var traversals = new[]
                {
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 2) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 2) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 3) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 3) * invH))),
                };

                scene.Occludes8(traversals);
            }
        }

        private static void CoherentOcclusion16(Scene<Sphere> scene, int w, int h)
        {
            float invW = 1.0f / w, invH = 1.0f / h;

            for (var y = 0; y < h; y += 4)
                for (var x = 0; x < w; x += 4)
            {
                var traversals = new[]
                {
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 0) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 1) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 2) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 2) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 2) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 2) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 0) * invW, 1, (float)(y + 3) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 1) * invW, 1, (float)(y + 3) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 2) * invW, 1, (float)(y + 3) * invH))),
                    new Traversal(new Ray(Vector.Zero, new Vector((float)(x + 3) * invW, 1, (float)(y + 3) * invH))),
                };

                scene.Occludes16(traversals);
            }
        }

        #endregion

        #region Incoherent Occlusion Benchmarks

        private static void IncoherentOcclusion1(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray; ++t)
                scene.Occludes(new Traversal(rays[t]));
        }

        private static void IncoherentOcclusion4(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray / 4; ++t)
            {
                var traversals = new[]
                {
                    new Traversal(rays[t * 4 + 0]),
                    new Traversal(rays[t * 4 + 1]),
                    new Traversal(rays[t * 4 + 2]),
                    new Traversal(rays[t * 4 + 3]),
                };

                scene.Occludes4(traversals);
            }
        }

        private static void IncoherentOcclusion8(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray / 8; ++t)
            {
                var traversals = new[]
                {
                    new Traversal(rays[t * 8 + 0]),
                    new Traversal(rays[t * 8 + 1]),
                    new Traversal(rays[t * 8 + 2]),
                    new Traversal(rays[t * 8 + 3]),
                    new Traversal(rays[t * 8 + 4]),
                    new Traversal(rays[t * 8 + 5]),
                    new Traversal(rays[t * 8 + 6]),
                    new Traversal(rays[t * 8 + 7]),
                };

                scene.Occludes8(traversals);
            }
        }

        private static void IncoherentOcclusion16(Scene<Sphere> scene, int nray, Ray[] rays)
        {
            for (var t = 0; t < nray / 16; ++t)
            {
                var traversals = new[]
                {
                    new Traversal(rays[t * 16 +  0]),
                    new Traversal(rays[t * 16 +  1]),
                    new Traversal(rays[t * 16 +  2]),
                    new Traversal(rays[t * 16 +  3]),
                    new Traversal(rays[t * 16 +  4]),
                    new Traversal(rays[t * 16 +  5]),
                    new Traversal(rays[t * 16 +  6]),
                    new Traversal(rays[t * 16 +  7]),
                    new Traversal(rays[t * 16 +  8]),
                    new Traversal(rays[t * 16 +  9]),
                    new Traversal(rays[t * 16 + 10]),
                    new Traversal(rays[t * 16 + 11]),
                    new Traversal(rays[t * 16 + 12]),
                    new Traversal(rays[t * 16 + 13]),
                    new Traversal(rays[t * 16 + 14]),
                    new Traversal(rays[t * 16 + 15]),
                };

                scene.Occludes16(traversals);
            }
        }

        #endregion

        #region Benchmark Providers

        public static IEnumerable<Tuple<String, Action>> Intersections(SceneFlags sceneFlags, TraversalFlags traversalFlags, int numPhi, int w, int h)
        {
            var scene = new Scene<Sphere>(sceneFlags, traversalFlags);
            scene.Add(new Sphere(sceneFlags, traversalFlags, numPhi));
            scene.Commit();

            if (traversalFlags.HasFlag(TraversalFlags.Single))
                yield return new Tuple<String, Action>("coherent_intersect1", () => CoherentIntersect1(scene, w, h));
            if (traversalFlags.HasFlag(TraversalFlags.Packet4))
                yield return new Tuple<String, Action>("coherent_intersect4", () => CoherentIntersect4(scene, w, h));
            if (traversalFlags.HasFlag(TraversalFlags.Packet8))
                yield return new Tuple<String, Action>("coherent_intersect8", () => CoherentIntersect8(scene, w, h));
            if (traversalFlags.HasFlag(TraversalFlags.Packet16))
                yield return new Tuple<String, Action>("coherent_intersect16", () => CoherentIntersect16(scene, w, h));

            var random = new Random();
            var rays = new Ray[w * h];

            for (var t = 0; t < w * h; ++t)
                rays[t] = new Ray(Vector.Zero, new Vector(2 * (float)random.NextDouble() - 1,
                                                          2 * (float)random.NextDouble() - 1,
                                                          2 * (float)random.NextDouble() - 1));

            if (traversalFlags.HasFlag(TraversalFlags.Single))
                yield return new Tuple<String, Action>("incoherent_intersect1", () => IncoherentIntersect1(scene, w * h, rays));
            if (traversalFlags.HasFlag(TraversalFlags.Packet4))
                yield return new Tuple<String, Action>("incoherent_intersect4", () => IncoherentIntersect4(scene, w * h, rays));
            if (traversalFlags.HasFlag(TraversalFlags.Packet8))
                yield return new Tuple<String, Action>("incoherent_intersect8", () => IncoherentIntersect8(scene, w * h, rays));
            if (traversalFlags.HasFlag(TraversalFlags.Packet16))
                yield return new Tuple<String, Action>("incoherent_intersect16", () => IncoherentIntersect16(scene, w * h, rays));
        }

        public static IEnumerable<Tuple<String, Action>> Occlusions(SceneFlags sceneFlags, TraversalFlags traversalFlags, int numPhi, int w, int h)
        {
            var scene = new Scene<Sphere>(sceneFlags, traversalFlags);
            scene.Add(new Sphere(sceneFlags, traversalFlags, numPhi));
            scene.Commit();

            if (traversalFlags.HasFlag(TraversalFlags.Single))
                yield return new Tuple<String, Action>("coherent_occlusion1", () => CoherentOcclusion1(scene, w, h));
            if (traversalFlags.HasFlag(TraversalFlags.Packet4))
                yield return new Tuple<String, Action>("coherent_occlusion4", () => CoherentOcclusion4(scene, w, h));
            if (traversalFlags.HasFlag(TraversalFlags.Packet8))
                yield return new Tuple<String, Action>("coherent_occlusion8", () => CoherentOcclusion8(scene, w, h));
            if (traversalFlags.HasFlag(TraversalFlags.Packet16))
                yield return new Tuple<String, Action>("coherent_occlusion16", () => CoherentOcclusion16(scene, w, h));

            var random = new Random();
            var rays = new Ray[w * h];

            for (var t = 0; t < w * h; ++t)
                rays[t] = new Ray(Vector.Zero, new Vector(2 * (float)random.NextDouble() - 1,
                                                          2 * (float)random.NextDouble() - 1,
                                                          2 * (float)random.NextDouble() - 1));

            if (traversalFlags.HasFlag(TraversalFlags.Single))
                yield return new Tuple<String, Action>("incoherent_occlusion1", () => IncoherentOcclusion1(scene, w * h, rays));
            if (traversalFlags.HasFlag(TraversalFlags.Packet4))
                yield return new Tuple<String, Action>("incoherent_occlusion4", () => IncoherentOcclusion4(scene, w * h, rays));
            if (traversalFlags.HasFlag(TraversalFlags.Packet8))
                yield return new Tuple<String, Action>("incoherent_occlusion8", () => IncoherentOcclusion8(scene, w * h, rays));
            if (traversalFlags.HasFlag(TraversalFlags.Packet16))
                yield return new Tuple<String, Action>("incoherent_occlusion16", () => IncoherentOcclusion16(scene, w * h, rays));
        }

        #endregion
    }
}

