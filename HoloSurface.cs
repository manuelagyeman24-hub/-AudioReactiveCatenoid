using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using CatenoidCore;

namespace CatenoidDemo
{
    /// <summary>
    /// Owns the live surface mesh. Triangle indices and texture coordinates are built once per
    /// resolution change; each frame only rewrites the existing <see cref="Point3DCollection"/>
    /// in place, so no per-frame mesh allocation happens.
    /// </summary>
    internal sealed class HoloSurface
    {
        private SurfaceTessellation _tessellation;
        private Vec3[] _positions;
        private Point3DCollection _points;

        public HoloSurface(int uSteps, int vSteps)
        {
            _tessellation = new SurfaceTessellation(uSteps, vSteps);
            _positions = _tessellation.CreatePositionBuffer();
            _points = new Point3DCollection(_tessellation.VertexCount);
            Mesh = new MeshGeometry3D();
            Rebuild();
        }

        /// <summary>The mesh instance handed to WPF; its identity never changes.</summary>
        public MeshGeometry3D Mesh { get; }

        public int VertexCount => _tessellation.VertexCount;

        public int TriangleCount => _tessellation.TriangleCount;

        public void SetResolution(int uSteps, int vSteps)
        {
            if (uSteps == _tessellation.USteps && vSteps == _tessellation.VSteps) return;

            _tessellation = new SurfaceTessellation(uSteps, vSteps);
            _positions = _tessellation.CreatePositionBuffer();
            _points = new Point3DCollection(_tessellation.VertexCount);
            Rebuild();
        }

        /// <summary>Recomputes the surface for <paramref name="parameters"/> without reallocating.</summary>
        public void Update(SurfaceParameters parameters)
        {
            _tessellation.FillPositions(_positions, parameters);

            for (int i = 0; i < _positions.Length; i++)
            {
                Vec3 p = _positions[i];
                _points[i] = new Point3D(p.X, p.Y, p.Z);
            }
        }

        private void Rebuild()
        {
            _tessellation.FillPositions(_positions, SurfaceParameters.Default);

            _points.Clear();
            foreach (Vec3 p in _positions) _points.Add(new Point3D(p.X, p.Y, p.Z));

            PointCollection coordinates = new(_tessellation.TextureCoordinates.Length);
            foreach (Vec2 c in _tessellation.TextureCoordinates) coordinates.Add(new Point(c.U, c.V));

            Int32Collection indices = new(_tessellation.TriangleIndices.Length);
            foreach (int i in _tessellation.TriangleIndices) indices.Add(i);

            coordinates.Freeze();
            indices.Freeze();

            Mesh.Positions = _points;
            Mesh.TextureCoordinates = coordinates;
            Mesh.TriangleIndices = indices;
        }
    }
}
