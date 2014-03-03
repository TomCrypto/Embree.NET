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

namespace Sample
{
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
        public Model(Matrix transform)
        {
            Enabled = true;
            this.transform = transform;
            inverseTranspose = Matrix.InverseTranspose(transform);
            geometry = new Geometry(SceneFlags.Static | SceneFlags.HighQuality, TraversalFlags.Single | TraversalFlags.Packet4);
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
        public Renderer()
        {
            // Create an Embree.NET scene using our Model type

            scene = new Scene<Model>(SceneFlags.Static | SceneFlags.Coherent | SceneFlags.Incoherent | SceneFlags.Robust,
                                     TraversalFlags.Single | TraversalFlags.Packet4);

            // Load all required meshes here

            meshes.Add("buddha", ObjLoader.LoadMesh("Models/buddha.obj"));
            meshes.Add("lucy", ObjLoader.LoadMesh("Models/lucy.obj"));
            meshes.Add("ground", ObjLoader.LoadMesh("Models/ground.obj"));

            // Create a few Model instances with a given modelworld matrix which we will populate later

            var buddhaModel = new Model(Matrix.Combine(Matrix.Scaling(8),
                                                       Matrix.Rotation(-(float)Math.PI / 2, 0, 0.5f),
                                                       Matrix.Translation(new Vector(-2.5f, -1.8f, -4.5f))));

            var lucyModel = new Model(Matrix.Combine(Matrix.Scaling(1.0f / 175),
                                                     Matrix.Rotation(0, (float)Math.PI / 2 + 2.1f, 0),
                                                     Matrix.Translation(new Vector(-11, -1.56f, -5))));

            var lucyModel2 = new Model(Matrix.Combine(Matrix.Scaling(1.0f / 600),
                                                      Matrix.Rotation(0, (float)Math.PI / 2 - 1.8f, 0),
                                                      Matrix.Translation(new Vector(-2.5f, -3.98f, -8))));

            var groundModel = new Model(Matrix.Combine(Matrix.Scaling(100),
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
        public void Render(PixelBuffer pixbuf)
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

                var traversals = new[]
                {
                    new Traversal(camera.Trace(2 * (u - 0.25f * dx) - 1, 2 * (v - 0.25f * dy) - 1)),
                    new Traversal(camera.Trace(2 * (u + 0.25f * dx) - 1, 2 * (v - 0.25f * dy) - 1)),
                    new Traversal(camera.Trace(2 * (u - 0.25f * dx) - 1, 2 * (v + 0.25f * dy) - 1)),
                    new Traversal(camera.Trace(2 * (u + 0.25f * dx) - 1, 2 * (v + 0.25f * dy) - 1)),
                };

                // Trace a packet of 4 coherent (AA) rays
                var hits = scene.Intersects4(traversals);

                for (int t = 0; t < 4; ++t)
                {
                    if (hits[t].HasHit)
                    {
                        color += new Vector(0.1f, 0.1f, 0.1f);

                        var ray = (Ray)traversals[t].Ray;
                        var model = hits[t].Instance;

                        // Parse the surface normal returned and then process it manually
                        var rawNormal = new Vector(hits[t].NX, hits[t].NY, hits[t].NZ);
                        var normal = model.CorrectNormal(rawNormal); // Important!

                        // Calculate the new ray towards the light source
                        var hitPoint = ray.PointAt(hits[t].Distance);
                        var toLight = lightPosition - hitPoint; // from A to B = B - A
                        var lightRay = new Ray(hitPoint + normal * Constants.Epsilon, toLight);

                        // Is the light source occluded? If so, no point calculating any lighting
                        if (!scene.Occludes(new Traversal(lightRay, 0, toLight.Length())))
                        {
                            // Compute the Lambertian cosine term (rendering equation)
                            float cosLight = Vector.Dot(normal, toLight.Normalize());

                            // Calculate the total light attenuation (inverse square law + cosine law)
                            var attenuation = lightIntensity * cosLight / Vector.Dot(toLight, toLight);

                            color += model.Material(hits[t].Mesh).BRDF(toLight.Normalize(), ray.Direction, normal) * attenuation;
                        }
                    }
                }

                // Average the 4 per-pixel samples
                pixbuf.SetColor(pixel, color / 4);
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
        /// Entry point - pass "verbose" as a command-line
        /// argument to initialize Embree in verbose mode.
        /// </summary>
        public static int Main(String[] args)
        {
            try
            {
                var verbose = (args.Length == 1 && args[0].ToLower() == "verbose");

                if (verbose)
                {
                    Console.WriteLine("Embree.NET Sample [VERBOSE]");
                    Console.WriteLine("===========================");
                    RTC.Register("verbose=999"); // max verbosity?
                }
                else
                {
                    Console.WriteLine("Embree.NET Sample");
                    Console.WriteLine("=================");
                }

                Console.WriteLine(""); // this is for debugging
                Console.WriteLine("[+] " + Bits + "-bit mode.");
                Console.WriteLine("[+] Building a test scene.");

                using (var renderer = new Renderer())
                {
                    var pixBuf = new PixelBuffer(1920, 1080);
                    Console.WriteLine("[+] Now rendering.");
                    renderer.Render(pixBuf); // benchmark?

                    Console.WriteLine("[+] Saving image to 'render.png'.");
                    pixBuf.SaveToFile("render.png"); // save to png format
                }

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
