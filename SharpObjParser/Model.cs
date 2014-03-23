using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpObjParser
{
    /// <summary>
    /// Data structure to store all obj-specific model datas
    /// </summary>
    public class Model
    {
        public string ModelName;
        public List<Object> Objects;
        public Object Current;
        public Material CurrentMaterial;
        public Material DefaultMaterial;
        public List<string> MaterialLib;
        public List<string> GroupLib;
        public List<float[]> Vertices;
        public List<float[]> Normals;
        public Dictionary<string, List<uint>> Groups;
        public List<uint> GroupFaceIDs;
        public string ActiveGroup;
        public List<float[]> TextureCoord;
        public Mesh CurrentMesh;
        public List<Mesh> Meshes;
        public Dictionary<string, Material> MaterialMap;

        public Model()
        {
            ModelName = "";
            Current = null;
            CurrentMaterial = null;
            DefaultMaterial = null;
            GroupFaceIDs = null;
            ActiveGroup = "";
            CurrentMesh = null;

            Objects = new List<Object>();
            MaterialLib = new List<string>();
            GroupLib = new List<string>();
            Vertices = new List<float[]>();
            Normals = new List<float[]>();
            Groups = new Dictionary<string, List<uint>>();
            GroupFaceIDs = new List<uint>();
            TextureCoord = new List<float[]>();
            Meshes = new List<Mesh>();
            MaterialMap = new Dictionary<string, Material>();
        }
    }
}
