using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using System.Runtime.InteropServices;
using System.IO;

namespace MiniMesh
{
    class ObjFileMesh
    {
        public Buffer VertexBuffer;
        public ObjFileSubmesh[] Submeshes;

        public ObjFileMesh(Device device, string fileName)
        {
            // load obj file
            SharpObjParser.Model model;
            var parser = new SharpObjParser.ObjFileParser(fileName, System.IO.Path.GetFileNameWithoutExtension(fileName));
            model = parser.GetModel();

            // per mesh
            List<VertexPositionNormalTexture> vertexBufferSource = new List<VertexPositionNormalTexture>();
            int[] triangleIndex = new int[] { 0, 1, 2 };
            int[] quadrilateralIndex = new int[] { 0, 1, 2, 0, 2, 3 };
            int startIndex = 0;
            Submeshes = new ObjFileSubmesh[model.Meshes.Count];
            for (int i = 0; i < model.Meshes.Count; i++ )
            {
                int indexCount = 0;
                var mesh = model.Meshes[i];
                
                // vertex
                foreach (var j in mesh.Faces)
                {
                    int[] indexToAdd;
                    if (j.Vertices.Count == 3)
                    {
                        indexToAdd = triangleIndex;
                        indexCount += 3;
                    }
                    else if (j.Vertices.Count == 4)
                    {
                        indexToAdd = quadrilateralIndex;
                        indexCount += 6;
                    }
                    else
                    {
                        indexToAdd = new int[0];
                    }
                    // 頂点位置から生成した法線
                    var v1 = new Vector3(model.Vertices[(int)j.Vertices[0] - 1]);
                    var v2 = new Vector3( model.Vertices[(int)j.Vertices[1] - 1]);
                    var v3 = new Vector3( model.Vertices[(int)j.Vertices[2] - 1]);
                    var normal = Vector3.Cross(v1 - v2, v1 - v3);
                    normal.Normalize();
                    foreach (var k in indexToAdd)
                    {
                        int posIndex = (int)j.Vertices[k] - 1;
                        int texIndex = (int)j.TextureCoords[k] - 1;
                        VertexPositionNormalTexture vert = new VertexPositionNormalTexture()
                        {
                            Position = new Vector3(model.Vertices[posIndex]),
                            Normal = j.Normals.Count > 0 ? new Vector3(model.Normals[(int)j.Normals[k] - 1]) : normal,
                            TextureCoordinate = new Vector2(model.TextureCoord[texIndex])
                        };
                        vertexBufferSource.Add(vert);
                    }
                }

                // submesh
                SharpObjParser.Material materialSource = model.MaterialMap[model.MaterialLib[(int)mesh.MaterialIndex]];
                var submesh = new ObjFileSubmesh();
                submesh.Diffuse = new Vector3(materialSource.Diffuse);
                
                submesh.Ambient = (materialSource.Ambient == null) ? new Color3(0.5f) : new Color3(materialSource.Ambient);
                submesh.Shineness = materialSource.Shineness;
                submesh.Specular = (materialSource.Specular == null) ? new Vector3(0.5f) : new Vector3(materialSource.Specular);
                submesh.Alpha = materialSource.Alpha;
                if (!string.IsNullOrWhiteSpace(materialSource.Texture))
                {
                    var tex = Texture2D.FromFile<Texture2D>(device, materialSource.Texture);
                    submesh.Texture = tex;
                    submesh.TextureView = new ShaderResourceView(device, tex);
                }
                submesh.IndexCount = indexCount;
                submesh.StartIndex = startIndex;
                Submeshes[i] = submesh;

                startIndex += indexCount;
            }
            VertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertexBufferSource.ToArray());
        }
    }
}
