using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpObjParser
{
    public class Face
    {
        public PrimitiveType PrimitiveType;
        public List<uint> Vertices;
        public List<uint> Normals;
        public List<uint> TextureCoords;
        public Material Material;
        public Face(List<uint> vertices, List<uint> normals, List<uint> texCoords, PrimitiveType pt = PrimitiveType.Polygon)
        {
            this.PrimitiveType = pt;
            this.Vertices = vertices;
            this.Normals = normals;
            this.TextureCoords = texCoords;
            this.Material = null;
        }
    }
}
