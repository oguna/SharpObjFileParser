using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using SharpDX;

namespace MiniMesh
{
    [StructLayout(LayoutKind.Sequential)]
    struct VertexPositionNormalTexture : IEquatable<VertexPositionNormalTexture>
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 TextureCoordinate;

        public bool Equals(VertexPositionNormalTexture other)
        {
            return (this.Position == other.Position) && (this.Normal == other.Normal) && (this.TextureCoordinate == other.TextureCoordinate);
        }
    }
}
