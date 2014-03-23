using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpObjParser
{
    public class Object
    {
        public enum ObjectType
        {
            ObjType,
            GroupType
        }

        public string ObjName;
        /// <summary>
        /// float[4,4] Transformation matrix, stored in OpenGL format
        /// </summary>
        public float[,] Transformation;
        public List<Object> SubObjects;
        public List<uint> Meshes;
        public Object()
        {
            this.ObjName = "";
            SubObjects = new List<Object>();
            Meshes = new List<uint>();
        }
    }
}
