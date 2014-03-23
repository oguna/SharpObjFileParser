using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SharpObjParser
{
    public class ObjFileParser
    {
        public ObjFileParser(Stream stream, string modelName, string dir)
        {
            this.Reader = new StreamReader(stream);
            this.FilePass = dir;

            // Create the model instance to store all the data
            Model = new Model();
            Model.ModelName = modelName;
            Model.DefaultMaterial = new Material();
            Model.DefaultMaterial.MaterialName = DEFAULT_MATERIAL;
            Model.MaterialLib.Add(DEFAULT_MATERIAL);
            Model.MaterialMap[DEFAULT_MATERIAL] = Model.DefaultMaterial;

            // Start parsing the file
            parseFile();
        }

        public Model GetModel()
        {
            return Model;
        }

        private void parseFile()
        {
            string line;
            while(null!=(line = Reader.ReadLine()))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                Tokens = line.Split(' ', '\t');
                switch(Tokens[0])
                {
                    case "v":
                        getVector3(ref Model.Vertices);
                        break;
                    case "vt":
                        getVector2(ref Model.TextureCoord);
                        break;
                    case "vn":
                        getVector3(ref Model.Normals);
                        break;
                    case "p":
                        getFace(PrimitiveType.Point);
                        break;
                    case "l":
                        getFace(PrimitiveType.Line);
                        break;
                    case "f":
                        getFace(PrimitiveType.Polygon);
                        break;
                    case "#":
                            getComment();
                        break;
                    case "usemtl":
                        {
                            getMaterialDesc();
                        }
                        break;
                    case "mtllib":
                        {
                            getMaterialLib();
                        }
                        break;
                    case "g":
                        getGroupName();
                        break;
                    case "mg":
                        getGroupNumberAndResolution();
                        break;
                    case "s":
                        {
                           getGroupNumber();
                        }
                        break;
                    case "o":
                        getObjectName();
                        break;
                    default:
                        {
                        }
                        break;
                }
            }
        }

        private void getVector3(ref List<float[]> point3d_array)
        {
            float[] vec3 = new float[3];
            for(int i=0; i<3; i++)
            {
                vec3[i] = float.Parse(Tokens[i + 1]);
            }
            point3d_array.Add(vec3);
        }

        private void getVector2(ref List<float[]> point2d_array)
        {
            float[] vec2 = new float[2];
            for (int i = 0; i < 2; i++)
            {
                vec2[i] = float.Parse(Tokens[i + 1]);
            }
            point2d_array.Add(vec2);
        }

        private void getFace(PrimitiveType type)
        {
            List<uint> indices = new List<uint>();
            List<uint> texID = new List<uint>();
            List<uint> normalID = new List<uint>();
            bool hasNormal = false;

            bool vt = !(Model.TextureCoord == null);
            bool vn = !(Model.Normals == null);
            int step = 0, pos = 0;
            for(int i=0; i<Tokens.Length-1; i++)
            {
                string[] ids = Tokens[i + 1].Split('/');
                for(int j=0; j<ids.Length; j++)
                {
                    if(string.IsNullOrEmpty(ids[j]))
                    {
                        continue;
                    }
                    uint id = uint.Parse(ids[j]);
                    switch(j)
                    {
                        case 0:
                            indices.Add(id);
                            break;
                        case 1:
                            texID.Add(id);
                            break;
                        case 2:
                            normalID.Add(id);
                            break;
                        default:
                            throw(new Exception());
                    }
                }
            }
            if(indices.Count==0)
            {
                Console.Error.WriteLine("Obj: Ignoring empty face");
                return;
            }

            Face face = new Face(indices, normalID, texID, type);

            // Set active material, if one set
            if (null != Model.CurrentMaterial)
            {
                face.Material = Model.CurrentMaterial;
            }
            else
            {
                face.Material = Model.DefaultMaterial;
            }

            // Create a default object, if nothing is there
            if(Model.Current==null)
            {
                createObject("defaultobject");
            }
            // Assign face to mesh
            if(Model.CurrentMesh==null)
            {
                createMesh();
            }
            // Store the face
            Model.CurrentMesh.Faces.Add(face);
            Model.CurrentMesh.NumIndices += (uint)face.Vertices.Count;
            //Model.CurrentMesh.UVCoordinates[0] += face.TextureCoords[0];

        }

        private void getMaterialDesc()
        {
            // get name
            string matName = Tokens[1];
            // search for material
            Material mat;
            if(Model.MaterialMap.TryGetValue(matName,out mat))
            {
                // Found, using detected material
                Model.CurrentMaterial = mat;
                if (needsNewMesh(matName))
                {
                    createMesh();
                }
                Model.CurrentMesh.MaterialIndex = (uint)getMaterialIndex(matName);
            }
            else
            {
                // Not found, use default material
                Model.CurrentMaterial = Model.DefaultMaterial;
                Console.Error.WriteLine("OBJ: failed to locate material " + matName + ", skipping");
            }
        }

        /// <summary>
        /// Get values for a new material description
        /// </summary>
        private void getComment()
        {
        }

        /// <summary>
        /// Get material library from file.
        /// </summary>
        private void getMaterialLib()
        {
            // Translate tuple
            string matName = Tokens[1];
            string fileName = FilePass + "//" + matName;
            // Check for existence
            if(!File.Exists(fileName))
            {
                Console.Error.WriteLine("OBJ: Unable to locate material file " + matName);
                return;
            }
            // Import material library data from file
            using(StreamReader fs = new StreamReader(fileName))
            {
                // Importing the material library 
                new ObjFileMtlImporter(fs, fileName, Model);
            }
        }

        private void getNewMaterial()
        {
            string strMat = Tokens[1];
            Material mat;
            if(Model.MaterialMap.TryGetValue(strMat,out mat))
            {
                // Set new material
                if(needsNewMesh(strMat))
                {
                    createMesh();
                }
                Model.CurrentMesh.MaterialIndex = (uint)getMaterialIndex(strMat);
            }
            else
            {
                // Show a warning, if material was not found
                Console.Error.WriteLine("OBJ: Unsupported material requested: " + strMat);
                Model.CurrentMaterial = Model.DefaultMaterial;
            }
        }

        private void getGroupName()
        {
            string strGroupName = Tokens[1];

            // Change active group, if necessary
            if(Model.ActiveGroup!=strGroupName)
            {
                List<uint> groups;
                if(Model.Groups.TryGetValue(strGroupName, out groups))
                {
                    Model.GroupFaceIDs = groups;
                }
                else
                {
                    List<uint> faceIDArray = new List<uint>();
                    Model.Groups[strGroupName] = faceIDArray;
                    Model.GroupFaceIDs = faceIDArray;
                }
                createObject(strGroupName);
                Model.ActiveGroup = strGroupName;
            }
        }

        private void getGroupNumber()
        {
            // Not used
        }

        private void getGroupNumberAndResolution()
        {
            // Not used
        }

        private int getMaterialIndex(string materialName)
        {
            int mat_index = -1;
            if(string.IsNullOrEmpty(materialName))
            {
                return mat_index;
            }
            for(int index=0;index<Model.MaterialLib.Count; index++)
            {
                if(materialName == Model.MaterialLib[index])
                {
                    mat_index = index;
                    break;
                }
            }
            return mat_index;
        }

        private void getObjectName()
        {
            string strObjectName = Tokens[1];
            if(!string.IsNullOrEmpty(strObjectName))
            {
                // Reset current object
                Model.Current = null;
                // Search for actual object
                foreach(Object obj in Model.Objects)
                {
                    if(obj.ObjName == strObjectName)
                    {
                        Model.Current = obj;
                        break;
                    }
                }
                // Allocate a new object, if current one was not found before
                if(null == Model.Current)
                {
                    createObject(strObjectName);
                }
            }
        }

        private void createObject(string objectName)
        {
            Model.Current = new Object();
            Model.Current.ObjName = objectName;
            Model.Objects.Add(Model.Current);

            createMesh();

            if(Model.CurrentMaterial!=null)
            {
                Model.CurrentMesh.MaterialIndex =
                    (uint)getMaterialIndex(Model.CurrentMaterial.MaterialName);
                Model.CurrentMesh.Material = Model.CurrentMaterial;
            }
        }

        private void createMesh()
        {
            Model.CurrentMesh = new Mesh();
            Model.Meshes.Add(Model.CurrentMesh);
            uint meshId = (uint)Model.Meshes.Count-1;
            if(null!=Model.Current)
            {
                Model.Current.Meshes.Add(meshId);
            }
            else
            {
                Console.Error.WriteLine("OBJ: No object detected to attach a new mesh instance.");
            }
        }

        private bool needsNewMesh(string materialName)
        {
            if(null==Model.CurrentMesh)
            {
                return true;
            }
            bool newMat = false;
            int matIdx = getMaterialIndex(materialName);
            uint curMatIdx = Model.CurrentMesh.MaterialIndex;
            if(curMatIdx != Mesh.NoMaterial || curMatIdx != matIdx)
            {
                newMat = true;
            }
            return newMat;
        }

        private void reportErrorTokenInFace()
        {
            ObjTools.SkipLine(Reader, ref Line);
            Console.Error.WriteLine("OBJ: Not supported token in face description detected");
        }


        private static string DEFAULT_MATERIAL = "default";

        private string[] Tokens;
        Model Model;
        private uint Line;
        private StreamReader Reader;
        private string FilePass;
    }
}
