using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpObjParser
{
    public class Material
    {
        public string MaterialName;
        public string Texture;
        public string TextureSpecular;
        public string TextureAmbient;
        public string TextureEmissive;
        public string TextureBump;
        public string TextureNormal;
        public string TextureSpecularity;
        public string TextureOpacity;
        public string TextureDisp;
        public enum TextureType : int
        {
            DiffuseType = 0,
            SpecularType,
            AmbientType,
            EmmisiveType,
            BumpType,
            NormalType,
            SpecularityType,
            OpacityType,
            DispType,
            TypeCount
        }
        public bool[] Clamp;
        /// <summary>
        /// (float[3]) Ambient color
        /// </summary>
        public float[] Ambient;
        /// <summary>
        /// (float[3]) Diffuse color
        /// </summary>
        public float[] Diffuse;
        /// <summary>
        /// (float[3]) Specular color
        /// </summary>
        public float[] Specular;
        public float Alpha;
        public float Shineness;
        public int IlluminationModel;
        /// <summary>
        /// Index of refrection
        /// </summary>
        public float IOR;
        public Material()
        {
            Diffuse = new float[]{0.6f, 0.6f, 0.6f};
            Alpha = 1f;
            Shineness = 0f;
            IlluminationModel = 1;
            IOR = 1f;
            Clamp = new bool[(int)TextureType.TypeCount];
        }
    }
}
