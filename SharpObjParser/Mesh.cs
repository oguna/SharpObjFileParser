using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpObjParser
{
    public class Mesh
    {
        public const uint NoMaterial = ~0u;
        public const uint AI_MAX_NUMBER_OF_TEXTURECOORDS = 4;

        public List<Face> Faces;
        public Material Material;
        public uint NumIndices;
        public uint[] UVCoordinates;
        public uint MaterialIndex;
        public bool HasNormals;
        public Mesh()
        {
            this.Material = null;
            this.NumIndices = 0;
            this.MaterialIndex = NoMaterial;
            this.HasNormals = false;
            this.UVCoordinates = new uint[AI_MAX_NUMBER_OF_TEXTURECOORDS];

            Faces = new List<Face>();
        }
    }
}
