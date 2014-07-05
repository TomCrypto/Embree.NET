/* A benchmark, approximately equivalent to the one distributed
 * with the Embree library, for measuring the interop overhead.
 * 
 * By default, handles only single-ray and 4-ray configurations
 * conservatively - pass e.g. "1 4 8" as command-line arguments
 * to enable more (we don't have static compilation information
 * like Embree does so cannot decide what is available or not).
*/

// TODO implement geometry creation/refitting benchmarks

using System;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;

using Embree;

namespace Benchmark
{
    #region Embree.NET Interop

    // We're not benchmarking a math library, so make the
    // geometry types as efficient as possible, as in the
    // original benchmark (e.g. no normalization, etc..).

    struct Vector : IEmbreeVector, IEmbreePoint
    {
        private readonly float x, y, z;

        public float X { get { return x; } }
        public float Y { get { return y; } }
        public float Z { get { return z; } }

        public Vector(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector operator+(Vector a, Vector b)
        {
            return new Vector(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector Zero { get { return default(Vector); } }
    }

    struct Ray : IEmbreeRay
    {
        private Vector origin, direction;

        public IEmbreePoint Origin { get { return origin; } }
        public IEmbreeVector Direction { get { return direction; } }

        public Ray(Vector origin, Vector direction)
        {
            this.origin = origin;
            this.direction = direction;
        }
    }

    struct Matrix : IEmbreeMatrix
    {
        private Vector u, v, w, t;

        public IEmbreeVector U { get { return u; } }
        public IEmbreeVector V { get { return v; } }
        public IEmbreeVector W { get { return w; } }
        public IEmbreeVector T { get { return t; } }

        public Matrix(Vector u, Vector v, Vector w, Vector t)
        {
            this.u = u;
            this.v = v;
            this.w = w;
            this.t = t;
        }

        public static Matrix Identity
        {
            get
            {
                return new Matrix(new Vector(1, 0, 0),
                                  new Vector(0, 1, 0),
                                  new Vector(0, 0, 1),
                                  new Vector(0, 0, 0));
            }
        }
    }

    #endregion

    #region Geometry Setup

    /// <summary>
    /// This IInstance implementation stores a sphere.
    /// </summary>
    internal class Sphere : IInstance
    {
        private readonly Geometry geometry;

        public IEmbreeMatrix Transform { get { return Matrix.Identity; } }
        public Geometry Geometry { get { return geometry; } }
        public bool Enabled { get { return true; } }

        /// <summary>
        /// Generates the unit sphere to arbitrary resolution.
        /// </summary>
        /// <remarks>
        /// Copied verbatim from the Embree benchmark code.
        /// </remarks>
        public static IMesh GenerateSphere(int numPhi)
        {
            var numTheta = 2 * numPhi; // we tessellate the unit sphere
            var vertices = new IEmbreePoint[numTheta * (numPhi + 1)];
            var indices = new int[3 * 2 * numTheta * (numPhi - 1)];

            int tri = 0;
            float rcpNumTheta = 1.0f / (float)numTheta;
            float rcpNumPhi   = 1.0f / (float)numPhi;

            for (var phi = 0; phi <= numPhi; ++phi)
            {
                for (var theta = 0; theta < numTheta; ++theta)
                {
                    float phif   = phi * (float)Math.PI * rcpNumPhi;
                    float thetaf = theta * 2 * (float)Math.PI * rcpNumTheta;
                    float x = (float)(Math.Sin(phif) * Math.Sin(thetaf));
                    float y = (float)(Math.Cos(phif));
                    float z = (float)(Math.Sin(phif) * Math.Cos(thetaf));
                    vertices[phi * numTheta + theta] = new Vector(x, y, z);
                }

                if (phi == 0)
                    continue;

                for (var theta = 1; theta <= numTheta; ++theta)
                {
                    int p00 = (phi - 1) * numTheta + theta - 1;
                    int p01 = (phi - 1) * numTheta + theta % numTheta;
                    int p10 = phi * numTheta + theta - 1;
                    int p11 = phi * numTheta + theta % numTheta;

                    if (phi > 1)
                    {
                        indices[3 * tri + 0] = p10;
                        indices[3 * tri + 1] = p00;
                        indices[3 * tri + 2] = p01;
                        ++tri;
                    }

                    if (phi < numPhi)
                    {
                        indices[3 * tri + 0] = p11;
                        indices[3 * tri + 1] = p10;
                        indices[3 * tri + 2] = p01;
                        ++tri;
                    }
                }
            }

            return new TriangleMesh(indices, vertices);
        }

        public Sphere(SceneFlags sceneFlags, TraversalFlags traversalFlags, int numPhi, MeshFlags meshFlags = MeshFlags.Static)
        {
            geometry = new Geometry(sceneFlags, traversalFlags);
            geometry.Add(GenerateSphere(numPhi), meshFlags);
        }

        public Sphere(SceneFlags sceneFlags, TraversalFlags traversalFlags, IMesh mesh, MeshFlags meshFlags = MeshFlags.Static)
        {
            geometry = new Geometry(sceneFlags, traversalFlags);
            geometry.Add(mesh, meshFlags);
        }
    }

    #endregion

    #region User Interface

    static class Program
    {
        /// <summary>
        /// Word size in bits of the current runtime.
        /// </summary>
        private static int Bits = IntPtr.Size * 8;

        private const int EXIT_SUCCESS = 0;
        private const int EXIT_FAILURE = 1;

        #region Benchmark Utilities

        /// <summary>
        /// Flags to use for benchmarking.
        /// </summary>
        public static TraversalFlags Flags;

        /// <summary>
        /// Measures the time taken by an action.
        /// </summary>
        private static void Measure(Action action, Func<Double, String> timer, Action<String> output)
        {
            var watch = new Stopwatch();
            watch.Start();
            action();
            watch.Stop();
            output(timer(watch.Elapsed.TotalSeconds));
        }

        /// <summary>
        /// Parses command-line arguments for traversal flags.
        /// </summary>
        private static void ParseCommandLineArguments(String[] args)
        {
            if (args.Length == 0) // If no arguments, fall back to 1/4
                Flags = TraversalFlags.Single | TraversalFlags.Packet4;
            else
            {
                foreach (var arg in args)
                {
                    try
                    {
                        switch (int.Parse(arg))
                        {
                            case 1:
                                Flags |= TraversalFlags.Single;
                                break;
                            case 4:
                                Flags |= TraversalFlags.Packet4;
                                break;
                            case 8:
                                Flags |= TraversalFlags.Packet8;
                                break;
                            case 16:
                                Flags |= TraversalFlags.Packet16;
                                break;
                            default:
                                throw new ArgumentException("Unknown ray packet size argument");
                        }
                    }
                    catch (FormatException)
                    {
                        throw new ArgumentException("Failed to parse ray packet size argument");
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Entry point - pass "verbose" as a command-line
        /// argument to initialize Embree in verbose mode.
        /// </summary>
        public static int Main(String[] args)
        {
            try
            {
                var verbose = (args.Select(s => s.ToLower()).Contains("verbose"));
                if (verbose) args.Select(s => s != "verbose"); // Clean up arglist
                ParseCommandLineArguments(args); // For selecting ray packet sizes

                if (verbose)
                {
                    Console.WriteLine("Embree.NET Benchmark [VERBOSE]");
                    Console.WriteLine("==============================");
                    RTC.Register("verbose=999"); // max verbosity?
                }
                else
                {
                    Console.WriteLine("Embree.NET Benchmark");
                    Console.WriteLine("====================");
                }

                Console.WriteLine(""); // this is for debugging
                Console.WriteLine("[+] " + Bits + "-bit mode.");

                // Note this is similar to the original Embree benchmark program
                Console.WriteLine("[+] Performance indicators are per-thread.");

                {
                    // Benchmark parameters
                    int w = 1024, h = 1024;

                    Console.WriteLine("[+] Benchmarking intersection queries.");

                    foreach (var item in Benchmarks.Intersections(SceneFlags.Static, Flags, 501, w, h))
                        Measure(item.Item2, item.Item3, (s) => Console.WriteLine("    {0} = {1}", item.Item1.PadRight(35), s));

                    Console.WriteLine("[+] Benchmarking occlusion queries.");

                    foreach (var item in Benchmarks.Occlusions(SceneFlags.Static, Flags, 501, w, h))
                        Measure(item.Item2, item.Item3, (s) => Console.WriteLine("    {0} = {1}", item.Item1.PadRight(35), s));
                }

                /*{
                    Console.WriteLine("[+] Benchmarking geometry manipulations.");

                    foreach (var item in Benchmarks.Geometries(SceneFlags.Static, Flags))
                        Measure(item.Item2, item.Item3, (s) => Console.WriteLine("    {0} = {1}", item.Item1.PadRight(35), s));
                }*/

                if (verbose)
                    RTC.Unregister();

                return EXIT_SUCCESS;
            }
            catch (Exception e)
            {
                var msg = e is AggregateException ? e.InnerException.Message : e.Message;
                Console.WriteLine(String.Format("[!] Error: {0}.", msg));
                Console.WriteLine("\n========= STACK TRACE =========\n");
                Console.WriteLine(e.StackTrace);
                return EXIT_FAILURE;
            }
        }
    }

    #endregion
}
