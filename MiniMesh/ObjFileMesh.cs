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

        [StructLayout(LayoutKind.Sequential)]
        struct CBuffer
        {
            public Matrix WorldViewProj;
            public Vector4 LocalLightDirection;
            public Vector4 EyePos;
            public Vector4 Ambient;
            public Vector4 Diffuse;
            public Vector4 Specular;
            public float Shineness;
            public Vector3 Dummy;
        }

        private Buffer vertexBuffer;
        private Buffer indexBuffer;
        private Buffer constantBuffer;
        private VertexShader vertexShader;
        private PixelShader pixelShader;
        private PixelShader texPixelShader;
        private ObjFileSubmesh[] submeshes;
        private int primitiveCount;
        private InputLayout layout;
        private SamplerState sampler;

        private Device device;

        public Matrix View;
        public Matrix Projection;
        public Matrix World;

        public ObjFileMesh(Device device)
        {
            this.device = device;
        }

        public void Load(string filename)
        {
            // load .obj file
            SharpObjParser.Model model;
            using (var stream = new FileStream(filename, FileMode.Open))
            {
                var parser = new SharpObjParser.ObjFileParser(stream, "cup", "Resources/");
                model = parser.GetModel();
            }

            // per mesh
            List<VertexPositionNormalTexture> vertexBufferSource = new List<VertexPositionNormalTexture>();
            int[] triangleIndex = new int[] { 0, 1, 2 };
            int[] wuadrilateralIndex = new int[] { 0, 1, 2, 0, 2, 3 };
            int startIndex = 0;
            submeshes = new ObjFileSubmesh[model.Meshes.Count];
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
                        indexToAdd = wuadrilateralIndex;
                        indexCount += 6;
                    }
                    else
                    {
                        indexToAdd = new int[0];
                    }
                    foreach (var k in indexToAdd)
                    {
                        VertexPositionNormalTexture vert = new VertexPositionNormalTexture()
                        {
                            Position = new Vector3(model.Vertices[(int)j.Vertices[k] - 1]),
                            Normal = new Vector3(model.Normals[(int)j.Normals[k] - 1]),
                            TextureCoordinate = new Vector2(model.TextureCoord[(int)j.TextureCoords[k] - 1])
                        };
                        vertexBufferSource.Add(vert);
                    }
                }

                // submesh
                SharpObjParser.Material materialSource = model.MaterialMap[model.MaterialLib[(int)mesh.MaterialIndex]];
                var submesh = new ObjFileSubmesh();
                submesh.Diffuse = new Vector3(materialSource.Diffuse);
                submesh.Ambient = new Color3(materialSource.Ambient);
                submesh.Shineness = materialSource.Shineness;
                submesh.Specular = new Vector3(materialSource.Specular);
                submesh.Alpha = materialSource.Alpha;
                if (!string.IsNullOrWhiteSpace(materialSource.Texture))
                {
                    var tex = Texture2D.FromFile<Texture2D>(device, "Resources/" + materialSource.Texture);
                    submesh.Texture = tex;
                    submesh.TextureView = new ShaderResourceView(device, tex);
                }
                submesh.IndexCount = indexCount;
                submesh.StartIndex = startIndex;
                submeshes[i] = submesh;

                startIndex += indexCount;
            }
            vertexBuffer = Buffer.Create(device, BindFlags.VertexBuffer, vertexBufferSource.ToArray());

            // Constant Buffer
            var size = Utilities.SizeOf<CBuffer>();
            constantBuffer = new Buffer(device, size, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            // Compile Vertex and Pixel shaders
            var vertexShaderByteCode = ShaderBytecode.CompileFromFile("Phong.fx", "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None, new[] { new ShaderMacro("TEXTURED", 0) });
            vertexShader = new VertexShader(device, vertexShaderByteCode);

            var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Phong.fx", "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None, new[] { new ShaderMacro("TEXTURED", 0) });
            pixelShader = new PixelShader(device, pixelShaderByteCode);

            var texPixelShaderByteCode = ShaderBytecode.CompileFromFile("Phong.fx", "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None, new[] { new ShaderMacro("TEXTURED", 1) });
            texPixelShader = new PixelShader(device, texPixelShaderByteCode);

            var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
            // Layout from VertexShader input signature
            layout = new InputLayout(device, signature, new[]
                    {
                        new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                        new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                        new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0)
                    });

            // Create Sampler
            sampler = new SamplerState(device, new SamplerStateDescription()
            {
                Filter = Filter.MinMagMipLinear,
                AddressU = TextureAddressMode.Wrap,
                AddressV = TextureAddressMode.Wrap,
                AddressW = TextureAddressMode.Wrap,
                BorderColor = Color.Black,
                ComparisonFunction = Comparison.Never,
                MaximumAnisotropy = 16,
                MipLodBias = 0,
                MinimumLod = 0,
                MaximumLod = 16,
            });
        }

        public void Draw(DeviceContext context)
        {
            // Prepare All the stages
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(vertexBuffer, Utilities.SizeOf<VertexPositionNormalTexture>(), 0));
            context.InputAssembler.SetIndexBuffer(indexBuffer, Format.R32_UInt, 0);
            context.VertexShader.Set(vertexShader);
            context.VertexShader.SetConstantBuffer(0, constantBuffer);
            //context.PixelShader.Set(pixelShader);
            context.PixelShader.SetConstantBuffer(0, constantBuffer);

            // Update WorldViewProj Matrix
            var worldViewProj = World * View * Projection;
            worldViewProj.Transpose();

            var lightDir = new Vector4(Vector3.Normalize(-new Vector3(1, 2, 3)), 0);
            lightDir = Vector4.Transform(lightDir, Matrix.Transpose(World));
            var cbuffer = new CBuffer()
            {
                WorldViewProj = worldViewProj,
                Ambient = new Color(0.2f, 0.2f, 0.2f, 1f).ToVector4(),
                LocalLightDirection = lightDir,
                EyePos = new Vector4(0, 0, -5, 0)
            };


            foreach(var submesh in submeshes)
            {
                // change pixel shader
                if (submesh.TextureView == null)
                {
                    context.PixelShader.Set(pixelShader);
                }
                else
                {
                    context.PixelShader.Set(texPixelShader);
                    context.PixelShader.SetSampler(0, sampler);
                    context.PixelShader.SetShaderResource(0, submesh.TextureView);
                }
                // set material and update constant buffer
                cbuffer.Diffuse = new Vector4(submesh.Diffuse.ToVector3(), 1);
                cbuffer.Specular = new Vector4(submesh.Specular.ToVector3(), 1);
                cbuffer.Shineness = submesh.Shineness;
                context.UpdateSubresource(ref cbuffer, constantBuffer);

                // draw
                context.Draw(submesh.IndexCount, submesh.StartIndex);


                //effect.ConstantBufferData.MaterialAmbient = submesh.Ambient;
                //effect.ConstantBufferData.MaterialDiffuse = submesh.Diffuse;
                //effect.ConstantBufferData.MaterialSpecular = submesh.Specular;
                //effect.ConstantBufferData.MaterialAlpha = submesh.Alpha;
                //effect.ConstantBufferData.MaterialShininess = (int)submesh.Shineness;
                //effect.ConstantBufferData.LightColor = new Vector3(0.2f, 0.2f, 0.2f);
                //effect.ConstantBufferData.LightPosition = new Vector3(1, 1, 1);
                //effect.ConstantBufferData.CameraPosition = new Vector3(0, 0, -5);
                //effect.ConstantBufferData.World = World;
                //effect.ConstantBufferData.WorldViewProjection = World * View * Projection;
                

                //if (submesh.TextureView != null)
                //{
                //    effect.TextureView = submesh.TextureView;
                //    effect.Apply(context, true, true);
                //}else
                //{
                //    effect.Apply(context, true, false);
                //}
                context.Draw(submesh.IndexCount, submesh.StartIndex);
            }
        }
    }
}
