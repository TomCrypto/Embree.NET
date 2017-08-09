/* Sample program for Embree.NET.
 *
 * Here the Model class holds the instance identifier returned
 * by Embree.NET, and the Renderer keeps track of models. This
 * is possibly the simplest renderer architecture, but this is
 * just an example - because the identifier is simply a 32-bit
 * integer, you can manage it in any way you find suitable.
 *
 * The program itself raytraces a couple models, under a point
 * light source, using direct lighting and a basic Phong BRDF.
 *
 * Note this sample isn't particularly optimized and is geared
 * towards readability rather than performance. The Embree.NET
 * library itself has rather low overhead (considering...), so
 * it's up to the host application to use it efficiently.
 *
 * You need a processor with SSE2 support, and SSE2 support to
 * be enabled in the Embree library (probably not a problem).
*/

using System;
using System.Threading.Tasks;
using System.Collections.Generic;

using Embree;
using System.Linq;

namespace Sample
{

    /// <summary>
    /// Consolidate flag definitions
    /// Flags are used when build the scene and geometry objects
    /// </summary>
    public class Flags
    {
        public const SceneFlags SCENE = SceneFlags.Static | SceneFlags.Coherent | SceneFlags.HighQuality | SceneFlags.Robust;
        public const TraversalFlags TRAVERSAL = TraversalFlags.Single | TraversalFlags.Packet4 | TraversalFlags.Packet8;
    }

    /// <summary>
    /// The Model implements the Embree.NET IInstance interface,
    /// and as such manages a model by wrapping it with material
    /// and transform matrix. Advanced renderers might add other
    /// things like texture coordinates and so on.
    /// </summary>
    public class Model : IInstance, IDisposable
    {
        private readonly Dictionary<IMesh, IMaterial> materials = new Dictionary<IMesh, IMaterial>();
        private Matrix transform, inverseTranspose;
        private readonly Geometry geometry;

        /// <summary>
        /// Gets the wrapped Geometry collection.
        /// </summary>
        public Geometry Geometry { get { return geometry; } }

        /// <summary>
        /// Gets or sets whether this model is enabled.
        /// </summary>
        public Boolean Enabled { get; set; }

        /// <summary>
        /// Gets the material associated with a mesh.
        /// </summary>
        public IMaterial Material(IMesh mesh)
        {
            return materials[mesh];
        }

        /// <summary>
        /// Gets the transform associated with this model.
        /// </summary>
        public IEmbreeMatrix Transform { get { return transform; } }

        /// <summary>
        /// Creates a new empty model.
        /// </summary>
        public Model(Device device, Matrix transform)
        {
            Enabled = true;
            this.transform = transform;
            inverseTranspose = Matrix.InverseTranspose(transform);
            geometry = new Geometry(device, Flags.SCENE, Flags.TRAVERSAL);
        }

        /// <summary>
        /// Adds a mesh to this model with a given material.
        /// </summary>
        public void AddMesh(IMesh mesh, IMaterial material)
        {
            geometry.Add(mesh);
            materials.Add(mesh, material);
        }

        /// <summary>
        /// Corrects an Embree.NET normal, which is unnormalized
        /// and in object space, to a world space normal vector.
        /// </summary>
        public Vector CorrectNormal(Vector normal)
        {
            return (inverseTranspose * normal).Normalize();
        }

        #region IDisposable

        ~Model()
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
            if (disposing)
            {
                geometry.Dispose();
            }
        }

        #endregion
    }

    /// <summary>
    /// The renderer keeps a collection of Embree.NET meshes instanced as Models.
    /// </summary>
    public class Renderer : IDisposable
    {
        private readonly Dictionary<String, IMesh> meshes = new Dictionary<String, IMesh>();

        private readonly float lightIntensity;
        private readonly Point lightPosition;
        private readonly Camera camera;
        private readonly Scene<Model> scene;
    

        /// <summary>
        /// Creates a new renderer.
        /// </summary>
        public Renderer(Device device)
        {
            // Create an Embree.NET scene using our Model type
            scene = new Scene<Model>(device, Flags.SCENE, Flags.TRAVERSAL);

            // Load all required meshes here

            meshes.Add("buddha", ObjLoader.LoadMesh(device, "Models/buddha.obj"));
            meshes.Add("lucy", ObjLoader.LoadMesh(scene.Device, "Models/lucy.obj"));
            meshes.Add("ground", ObjLoader.LoadMesh(scene.Device, "Models/ground.obj"));

            // Create a few Model instances with a given modelworld matrix which we will populate later

            var buddhaModel = new Model(scene.Device, Matrix.Combine(Matrix.Scaling(8),
                                                       Matrix.Rotation(-(float)Math.PI / 2, 0, 0.5f),
                                                       Matrix.Translation(new Vector(-2.5f, -1.8f, -4.5f))));

            var lucyModel = new Model(scene.Device, Matrix.Combine(Matrix.Scaling(1.0f / 175),
                                                     Matrix.Rotation(0, (float)Math.PI / 2 + 2.1f, 0),
                                                     Matrix.Translation(new Vector(-11, -1.56f, -5))));

            var lucyModel2 = new Model(scene.Device, Matrix.Combine(Matrix.Scaling(1.0f / 600),
                                                      Matrix.Rotation(0, (float)Math.PI / 2 - 1.8f, 0),
                                                      Matrix.Translation(new Vector(-2.5f, -3.98f, -8))));

            var groundModel = new Model(scene.Device, Matrix.Combine(Matrix.Scaling(100),
                                                       Matrix.Translation(new Vector(0, -5, 0))));

            // Now place these meshes into the world with a given material

            buddhaModel.AddMesh(meshes["buddha"], new Phong(new Vector(0.55f, 0.25f, 0.40f), 0.65f, 48));
            lucyModel.AddMesh(meshes["lucy"], new Phong(new Vector(0.35f, 0.65f, 0.15f), 0.85f, 256));
            groundModel.AddMesh(meshes["ground"], new Phong(new Vector(0.25f, 0.25f, 0.95f), 0.45f, 1024));
            lucyModel2.AddMesh(meshes["lucy"], new Diffuse(new Vector(0.95f, 0.85f, 0.05f) * 0.318f)); // instancing example

            // And finally add them to the scene (into the world)

            scene.Add(buddhaModel);
            scene.Add(lucyModel);
            scene.Add(lucyModel2);
            scene.Add(groundModel);

            // Don't forget to commit when we're done messing with the geometry

            scene.Commit();

            // Place a light source somewhere

            lightPosition = new Point(-11.85f, 11, -13);
            lightIntensity = 900;

            // Get a good shot of the world

            camera = new Camera((float)Math.PI / 5, 1,    // unknown aspect ratio for now
                                new Point(-2.5f, -0.45f, -12), // good position for the camera
                                new Vector(0, 0, 1), 0);  // view direction + no roll (upright)
        }

        /// <summary>
        /// Renders the scene into a pixel buffer.
        /// </summary>
        public void Render(PixelBuffer pixbuf, TraversalFlags mode = TraversalFlags.Single)
        {
            float dx = 1.0f / pixbuf.Width, dy = 1.0f / pixbuf.Height;
            camera.AspectRatio = (float)pixbuf.Width / pixbuf.Height;

            // Free parallelism, why not! Note a Parallel.For loop
            // over each row is slightly faster but less readable.
            Parallel.ForEach(pixbuf, (pixel) =>
            {
                var color = Vector.Zero;
                float u = pixel.X * dx;
                float v = pixel.Y * dy;

                Ray[] rays = null;
                Intersection<Model>[] hits = null;
                if (mode == TraversalFlags.Single)
                {
                    rays = new[] { camera.Trace(2 * (u - 0.25f * dx) - 1, 2 * (v - 0.25f * dy) - 1) };
                    var packet = scene.Intersects(rays[0]);
                    hits = new Intersection<Model>[] { packet.ToIntersection<Model>(scene) };
                }
                else if (mode == TraversalFlags.Packet4)
                {
                    rays = new[]
                    {
                        camera.Trace(2 * (u - 0.25f * dx) - 1, 2 * (v - 0.25f * dy) - 1),
                        camera.Trace(2 * (u + 0.25f * dx) - 1, 2 * (v - 0.25f * dy) - 1),
                        camera.Trace(2 * (u - 0.25f * dx) - 1, 2 * (v + 0.25f * dy) - 1),
                        camera.Trace(2 * (u + 0.25f * dx) - 1, 2 * (v + 0.25f * dy) - 1)
                    };
                    // Trace a packet of coherent AA rays
                    var packet = scene.Intersects4(rays);
                    // Convert the packet to a set of usable ray-geometry intersections
                    hits = packet.ToIntersection<Model>(scene);
                }
                else if (mode == TraversalFlags.Packet8)
                {
                    // Sampling pattern
                    // ------------
                    // | X      X | 
                    // |   X  X   |
                    // |   X  X   |
                    // | X      X |
                    // ------------
                    rays = new[]
                    {
                        camera.Trace(2 * (u - 0.16f * dx) - 1, 2 * (v - 0.16f * dy) - 1),
                        camera.Trace(2 * (u + 0.16f * dx) - 1, 2 * (v - 0.16f * dy) - 1),
                        camera.Trace(2 * (u - 0.16f * dx) - 1, 2 * (v + 0.16f * dy) - 1),
                        camera.Trace(2 * (u + 0.16f * dx) - 1, 2 * (v + 0.16f * dy) - 1),
                        camera.Trace(2 * (u - 0.33f * dx) - 1, 2 * (v - 0.33f * dy) - 1),
                        camera.Trace(2 * (u + 0.33f * dx) - 1, 2 * (v - 0.33f * dy) - 1),
                        camera.Trace(2 * (u - 0.33f * dx) - 1, 2 * (v + 0.33f * dy) - 1),
                        camera.Trace(2 * (u + 0.33f * dx) - 1, 2 * (v + 0.33f * dy) - 1)
                    };
                    // Trace a packet of coherent AA rays
                    var packet = scene.Intersects8(rays);
                    // Convert the packet to a set of usable ray-geometry intersections
                    hits = packet.ToIntersection<Model>(scene);
                }
                else
                {
                    throw new Exception("Invalid mode");
                }

                for (int t = 0; t < hits.Length; ++t)
                {
                    if (hits[t].HasHit)
                    {
                        color += new Vector(0.1f, 0.1f, 0.1f);

						var ray = rays[t];
                        var model = hits[t].Instance;

                        // Parse the surface normal returned and then process it manually
                        var rawNormal = new Vector(hits[t].NX, hits[t].NY, hits[t].NZ);
                        var normal = model.CorrectNormal(rawNormal); // Important!

                        // Calculate the new ray towards the light source
                        var hitPoint = ray.PointAt(hits[t].Distance);
                        var toLight = lightPosition - hitPoint; // from A to B = B - A
                        var lightRay = new Ray(hitPoint + normal * Constants.Epsilon, toLight);

                        // Is the light source occluded? If so, no point calculating any lighting
                        if (!scene.Occludes(lightRay, 0, toLight.Length()))
                        {
                            // Compute the Lambertian cosine term (rendering equation)
                            float cosLight = Vector.Dot(normal, toLight.Normalize());

                            // Calculate the total light attenuation (inverse square law + cosine law)
                            var attenuation = lightIntensity * cosLight / Vector.Dot(toLight, toLight);

                            color += model.Material(hits[t].Mesh).BRDF(toLight.Normalize(), ray.Direction, normal) * attenuation;
                        }
                    }
                }
                // Average the per-pixel samples
                pixbuf.SetColor(pixel, color / rays.Length);
            });
        }

        #region IDisposable

        ~Renderer()
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
            if (disposing)
            {
                foreach (var model in scene)
                    model.Dispose();

                scene.Dispose();
            }
        }

        #endregion
    }

    #region User Interface

    static class Program
    {
        /// <summary>
        /// Word size in bits of the current runtime.
        /// </summary>
        private static int Bits = IntPtr.Size * 8;

        private const int EXIT_SUCCESS = 0;
        private const int EXIT_FAILURE = 1;


        /// <summary>
        /// Parses command-line arguments for traversal flags.
        /// </summary>
        private static TraversalFlags ParseCommandLineArguments(String[] args)
        {
            TraversalFlags flags = 0;
            foreach (var arg in args)
            {
                int v;
                if (int.TryParse(arg, out v))
                {
                    switch (v)
                    {
                        case 1:
                            flags |= TraversalFlags.Single;
                            break;
                        case 4:
                            flags |= TraversalFlags.Packet4;
                            break;
                        case 8:
                            flags |= TraversalFlags.Packet8;
                            break;
                        case 16:
                            flags |= TraversalFlags.Packet16;
                            break;
                        default:
                            throw new ArgumentException("Unknown ray packet size argument");
                    }
                }
                else if (!arg.Equals("verbose"))
                {
                    throw new ArgumentException("Failed to parse ray packet size argument");
                }
            }
            if (flags == 0) // If no arguments, fall back to 1/8
                flags = TraversalFlags.Single | TraversalFlags.Packet4 | TraversalFlags.Packet8;
            return flags;
        }

        /// <summary>
        /// Entry point - pass "verbose" as a command-line
        /// argument to initialize Embree in verbose mode.
        /// </summary>
        public static int Main(String[] args)
        {
            try
            {
                var verbose = (args.Select(s => s.ToLower()).Contains("verbose"));
                var flags =  ParseCommandLineArguments(args);

                if (verbose)
                {
                    Console.WriteLine("Embree.NET Sample [VERBOSE]");
                    Console.WriteLine("===========================");

                }
                else
                {
                    Console.WriteLine("Embree.NET Sample");
                    Console.WriteLine("=================");

                }
                Console.WriteLine(""); // this is for debugging
                Console.WriteLine("[+] " + Bits + "-bit mode.");
                Console.WriteLine("[+] Building a test scene.");
                using (Device device = new Device(verbose))
                {
                    using (var renderer = new Renderer(device))
                    {
                        if (flags.HasFlag(TraversalFlags.Single))
                        {
                            var pixBuf = new PixelBuffer(1920, 1080);
                            Console.WriteLine("[+] Now rendering single.");
                            renderer.Render(pixBuf, TraversalFlags.Single); // benchmark?
                            Console.WriteLine("[+] Saving image to 'render_single.png'.");
                            pixBuf.SaveToFile("render_single.png"); // save to png format
                        }
                        if (flags.HasFlag(TraversalFlags.Packet4))
                        {
                            var pixBuf = new PixelBuffer(1920, 1080);
                            Console.WriteLine("[+] Now rendering packet 4.");
                            renderer.Render(pixBuf, TraversalFlags.Packet4); // benchmark?
                            Console.WriteLine("[+] Saving image to 'render_packet4.png'.");
                            pixBuf.SaveToFile("render_packet4.png"); // save to png format
                        }
                        if (flags.HasFlag(TraversalFlags.Packet8))
                        {
                            var pixBuf = new PixelBuffer(1920, 1080);
                            Console.WriteLine("[+] Now rendering packet 8.");
                            renderer.Render(pixBuf, TraversalFlags.Packet8); // benchmark?
                            Console.WriteLine("[+] Saving image to 'render_packet8.png'.");
                            pixBuf.SaveToFile("render_packet8.png"); // save to png format
                        }
                    }
                }

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
