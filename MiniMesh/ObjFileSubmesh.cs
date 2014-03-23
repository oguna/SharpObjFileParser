using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;

namespace MiniMesh
{
    class ObjFileSubmesh
    {
        public int StartIndex;
        public int IndexCount;
        public Color3 Ambient;
        public Color3 Diffuse;
        public Color3 Specular;
        public float Alpha;
        public float Shineness;
        public Texture2D Texture;
        public ShaderResourceView TextureView;
    }
}
