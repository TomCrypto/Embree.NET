/* A few utility classes for the Embree.NET sample, these don't
 * use the library - they are things like OBJ loaders and image
 * buffers, which are required but don't contribute much to the
 * understanding of how to use the Embree.NET library.
*/

using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Collections;
using System.Globalization;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using Embree;

namespace Sample
{
    /// <summary>
    /// Manages a 2D pixel array.
    /// </summary>
    public class PixelBuffer : IEnumerable<System.Drawing.Point>
    {
        private readonly byte[] pixelColor;
        private readonly Rectangle bufSize;

        /// <summary>
        /// Gets the width of the buffer.
        /// </summary>
        public int Width { get { return bufSize.Width; } }

        /// <summary>
        /// Gets the height of the buffer.
        /// </summary>
        public int Height { get { return bufSize.Height; } }

        /// <summary>
        /// Creates a new pixel buffer of the specified dimensions.
        /// </summary>
        public PixelBuffer(int width, int height)
        {
            if ((width <= 0) || (height <= 0))
                throw new ArgumentOutOfRangeException("Invalid dimensions");

            bufSize = new Rectangle(0, 0, width, height);
            pixelColor = new byte[width * height * 3];
        }

        /// <summary>
        /// Sets the color of a pixel, in the range [0..1].
        /// </summary>
        public void SetColor(System.Drawing.Point p, Vector color)
        {
            if (!bufSize.Contains(p))
                throw new ArgumentOutOfRangeException("Pixel out of bounds");

            pixelColor[3 * (p.Y * Width + p.X) + 2] = (byte)(Math.Min(Math.Max(color.X, 0), 1) * 255);
            pixelColor[3 * (p.Y * Width + p.X) + 1] = (byte)(Math.Min(Math.Max(color.Y, 0), 1) * 255);
            pixelColor[3 * (p.Y * Width + p.X) + 0] = (byte)(Math.Min(Math.Max(color.Z, 0), 1) * 255);
        }

        /// <summary>
        /// Saves the pixel buffer to a PNG file.
        /// </summary>
        public void SaveToFile(String path)
        {
            using (var bmp = new Bitmap(Width, Height, PixelFormat.Format24bppRgb))
            {
                var data = bmp.LockBits(bufSize, ImageLockMode.WriteOnly, bmp.PixelFormat);

                for (var y = 0; y < Height; ++y)
                    Marshal.Copy(pixelColor, y * Width * 3, data.Scan0 + y * data.Stride, data.Stride);

                bmp.UnlockBits(data);
                bmp.Save(path, ImageFormat.Png);
            }
        }

        #region IEnumerable

        /// <summary>
        /// Enumerates the pixels in the buffer.
        /// </summary>
        public IEnumerator<System.Drawing.Point> GetEnumerator()
        {
            for (var y = 0; y < Height; ++y)
                for (var x = 0; x < Width; ++x)
                    yield return new System.Drawing.Point(x, y);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion
    }

    /// <summary>
    /// A simple camera with FoV and aspect ratio.
    /// </summary>
    public class Camera
    {
        private bool ready;
        private Point position;
        private Vector viewDir;
        private Matrix transform;
        private float roll, fov, aspectRatio;

        /// <summary>
        /// Recomputes the transformation matrix.
        /// </summary>
        private void RebuildTransform()
        {
            if (ready)
            {
                var rotation = Matrix.Rotation(Vector.Inclination(viewDir),
                                               Vector.Azimuth(viewDir) - (float)Math.PI / 2,
                                               roll);
                var translation = Matrix.Translation(position);
                transform = Matrix.Combine(rotation, translation);
            }
        }

        /// <summary>
        /// Gets the camera's field of view in radians.
        /// </summary>
        public float FieldOfView
        {
            get { return fov; }

            set
            {
                if (value <= 0 || value > Math.PI)
                    throw new ArgumentOutOfRangeException("Invalid field of view");
                else
                {
                    fov = value;
                    RebuildTransform();
                }
            }
        }

        /// <summary>
        /// Gets the camera's aspect ratio.
        /// </summary>
        public float AspectRatio
        {
            get { return aspectRatio; }

            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("Invalid aspect ratio");
                else
                {
                    aspectRatio = value;
                    RebuildTransform();
                }
            }
        }

        /// <summary>
        /// Gets the camera's position.
        /// </summary>
        public Point Position
        {
            get { return position; }

            set
            {
                position = value;
                RebuildTransform();
            }
        }

        /// <summary>
        /// Gets the camera's view direction.
        /// </summary>
        public Vector Direction
        {
            get { return viewDir; }

            set
            {
                viewDir = value.Normalize();
                RebuildTransform();
            }
        }

        /// <summary>
        /// Gets the roll of the camera in radians.
        /// </summary>
        public float Roll
        {
            get { return roll; }
            set { roll = value; }
        }

        /// <summary>
        /// Creates a new camera with default settings.
        /// </summary>
        public Camera(float fov, float aspectRatio, Point position, Vector viewDir, float roll)
        {
            FieldOfView = fov;
            AspectRatio = aspectRatio;

            Position = position;
            Direction = viewDir;
            Roll = roll;

            ready = true;
            RebuildTransform();
        }

        /// <summary>
        /// Traces a ray through the camera plane.
        /// </summary>
        public Ray Trace(float u, float v)
        {
            return transform * new Ray(position, new Vector(u * aspectRatio, -v, 1 / (float)Math.Tan(fov / 2)));
        }
    }

    /// <summary>
    /// A simple material interface.
    /// </summary>
    public interface IMaterial
    {
        /// <summary>
        /// Computes the reflectance at specific angles.
        /// </summary>
        Vector BRDF(Vector l, Vector v, Vector n);
    }

    /// <summary>
    /// A diffuse BRDF.
    /// </summary>
    public class Diffuse : IMaterial
    {
        private readonly Vector albedo;

        public Diffuse(Vector albedo)
        {
            this.albedo = albedo;
        }

        public Vector BRDF(Vector v, Vector l, Vector n)
        {
            return albedo;
        }
    }

    /// <summary>
    /// A Phong BRDF.
    /// </summary>
    public class Phong : IMaterial
    {
        private readonly float specularCoeff, shininess;
        private readonly Vector albedo;

        public Phong(Vector albedo, float specularCoeff, float shininess)
        {
            this.albedo = albedo;
            this.specularCoeff = specularCoeff;
            this.shininess = shininess;
        }

        public Vector BRDF(Vector v, Vector l, Vector n)
        {
            var cosR = -Vector.Dot(v, l.Reflect(n));

            return albedo * (1 - specularCoeff) + new Vector(1, 1, 1) * specularCoeff * (float)Math.Pow(cosR, shininess);
        }
    }

    /// <summary>
    /// A simple OBJ mesh loader.
    /// </summary>
    public static class ObjLoader
    {
        /// <summary>
        /// Loads an Embree.NET mesh from an OBJ file.
        /// </summary>
        public static TriangleMesh LoadMesh(String path)
        {
            var indices  = new List<int>();
            var vertices = new List<IEmbreePoint>();

            foreach (var tokens in (from line in File.ReadLines(path) select line.Split()))
            {
                switch (tokens.Length == 4 ? tokens[0] : null)
                {
                    case "v":
                        vertices.Add(new Point(float.Parse(tokens[1], CultureInfo.InvariantCulture),
                                               float.Parse(tokens[2], CultureInfo.InvariantCulture),
                                               float.Parse(tokens[3], CultureInfo.InvariantCulture)));
                        break;
                    case "f":
                        indices.Add(int.Parse(tokens[1]) - 1);
                        indices.Add(int.Parse(tokens[3]) - 1);
                        indices.Add(int.Parse(tokens[2]) - 1);
                        break;
                }
            }

            return new TriangleMesh(indices, vertices);
        }
    }
}

