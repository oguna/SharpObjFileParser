using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.D3DCompiler;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Windows;
using Buffer = SharpDX.Direct3D11.Buffer;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;

namespace MiniMesh
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

    class MiniMeshSample : Sample
    {
        /// <summary>
        /// 頂点シェーダ
        /// </summary>
        VertexShader vertexShader;

        /// <summary>
        /// テクスチャなしピクセルシェーダ
        /// </summary>
        PixelShader pixelShader;

        /// <summary>
        /// テクスチャありピクセルシェーダ
        /// </summary>
        PixelShader texPixelShader;

        /// <summary>
        /// 入力レイアウト
        /// </summary>
        InputLayout layout;

        /// <summary>
        /// 定数バッファ
        /// </summary>
        Buffer constantBuffer;

        /// <summary>
        /// サンプラ
        /// </summary>
        SamplerState sampler;

        /// <summary>
        /// メッシュ
        /// </summary>
        ObjFileMesh objFileMesh;

        /// <summary>
        /// ゲーム開始からの時間
        /// </summary>
        float time;

        protected override void Load(Device device)
        {
            // 頂点シェーダ
            var vertexShaderByteCode = ShaderBytecode.CompileFromFile("Phong.fx", "VS", "vs_4_0", ShaderFlags.None, EffectFlags.None, new[] { new ShaderMacro("TEXTURED", 0) });
            vertexShader = new VertexShader(device, vertexShaderByteCode);
            
            // テクスチャなしピクセルシェーダ
            var pixelShaderByteCode = ShaderBytecode.CompileFromFile("Phong.fx", "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None, new[] { new ShaderMacro("TEXTURED", 0) });
            pixelShader = new PixelShader(device, pixelShaderByteCode);

            // テクスチャありピクセルシェーダ
            var texPixelShaderByteCode = ShaderBytecode.CompileFromFile("Phong.fx", "PS", "ps_4_0", ShaderFlags.None, EffectFlags.None, new[] { new ShaderMacro("TEXTURED", 1) });
            texPixelShader = new PixelShader(device, texPixelShaderByteCode);

            // 入力レイアウト
            var signature = ShaderSignature.GetInputSignature(vertexShaderByteCode);
            layout = new InputLayout(device, signature, new[]
                    {
                        new InputElement("POSITION", 0, Format.R32G32B32_Float, 0, 0),
                        new InputElement("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
                        new InputElement("TEXCOORD", 0, Format.R32G32_Float, 24, 0)
                    });

            // 定数バッファ
            var size = Utilities.SizeOf<CBuffer>();
            constantBuffer = new Buffer(device, size, ResourceUsage.Default, BindFlags.ConstantBuffer, CpuAccessFlags.None, ResourceOptionFlags.None, 0);

            // サンプラ
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

            // メッシュ
            objFileMesh = new ObjFileMesh(device,"cup.obj");

            base.Load(device);
        }

        protected override void Unload(Device device)
        {
            Utilities.Dispose(ref vertexShader);
            Utilities.Dispose(ref pixelShader);
            Utilities.Dispose(ref texPixelShader);
            Utilities.Dispose(ref layout);
            Utilities.Dispose(ref sampler);
            base.Unload(device);
        }

        protected override void Update(float elapsedTime, float totalTime)
        {
            time = totalTime;
            base.Update(elapsedTime, totalTime);
        }

        protected override void Draw(Device device)
        {
            var view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, form.ClientSize.Width / (float)form.ClientSize.Height, 0.1f, 100.0f);
            var viewProj = Matrix.Multiply(view, proj);
            var world = Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f);

            // メッシュの描画
            DrawMesh(device.ImmediateContext, objFileMesh);

            base.Draw(device);
        }

        protected void DrawMesh(DeviceContext context, ObjFileMesh mesh)
        {
            // 定数バッファの構造体を作成
            var view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, form.ClientSize.Width / (float)form.ClientSize.Height, 0.1f, 100.0f);
            var viewProj = Matrix.Multiply(view, proj);

            var world = Matrix.Scaling(2f) * Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f);
            var worldViewProj = world * view * proj;
            worldViewProj.Transpose();
            var cbuffer = new CBuffer()
            {
                WorldViewProj = worldViewProj,
                Ambient = new Color(0.2f, 0.2f, 0.2f, 1f).ToVector4(),
                EyePos = new Vector4(0, 0, -5, 0),
                LocalLightDirection = Vector4.Transform(new Vector4(Vector3.Normalize(-new Vector3(1, 2, 3)), 0), Matrix.Transpose(world))
            };

            // ステージにセット
            context.InputAssembler.InputLayout = layout;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            context.InputAssembler.SetVertexBuffers(0, new VertexBufferBinding(objFileMesh.VertexBuffer, Utilities.SizeOf<VertexPositionNormalTexture>(), 0));
            context.VertexShader.Set(vertexShader);
            context.VertexShader.SetConstantBuffer(0, constantBuffer);
            context.PixelShader.SetConstantBuffer(0, constantBuffer);

            foreach(var submesh in objFileMesh.Submeshes)
            {
                // テクスチャの有無によってピクセルシェーダを変更
                if (submesh.TextureView == null)
                {
                    context.PixelShader.Set(pixelShader);
                    context.PixelShader.SetSampler(0, null);
                    context.PixelShader.SetShaderResource(0, null);
                }
                else
                {
                    context.PixelShader.Set(texPixelShader);
                    context.PixelShader.SetSampler(0, sampler);
                    context.PixelShader.SetShaderResource(0, submesh.TextureView);
                }

                // マテリアルの値を定数バッファに設定
                cbuffer.Diffuse = new Vector4(submesh.Diffuse.ToVector3(), 1);
                cbuffer.Specular = new Vector4(submesh.Specular.ToVector3(), 1);
                cbuffer.Shineness = submesh.Shineness;

                // 定数バッファの更新
                context.UpdateSubresource(ref cbuffer, constantBuffer);

                // 描画命令の発行
                context.Draw(submesh.IndexCount, submesh.StartIndex);
            }
        }
    }
}
