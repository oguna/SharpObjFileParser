using System;
using System.Text;
using System.Collections.Generic;
using SharpDX;
using System.IO;

namespace Demo
{
    // Use these namespaces here to override SharpDX.Direct3D11
    using SharpDX.Toolkit;
    using SharpDX.Toolkit.Graphics;
    using SharpDX.Toolkit.Input;

    /// <summary>
    /// Simple Demo game using SharpDX.Toolkit.
    /// </summary>
    public class Demo : Game
    {
        private GraphicsDeviceManager graphicsDeviceManager;

        private Buffer<VertexPositionNormalTexture> vertexBuffer;
        private Buffer<uint> indexBuffer;
        private BasicEffect[] effects;
        private uint[] startIndexies;
        private uint[] indexNums;

        /// <summary>
        /// Initializes a new instance of the <see cref="Demo" /> class.
        /// </summary>
        public Demo()
        {
            // Creates a graphics manager. This is mandatory.
            graphicsDeviceManager = new GraphicsDeviceManager(this);

            // Setup the relative directory to the executable directory
            // for loading contents with the ContentManager
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            // Modify the title of the window
            Window.Title = "Demo";

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // ファイルの読み込み
            SharpObjParser.Model model;
            string filename = "Resources/cup.obj";
            using (var stream = new FileStream(filename, FileMode.Open))
            {
                var parser = new SharpObjParser.ObjFileParser(stream, "cup", "Resources/");
                model = parser.GetModel();
            }

            // メッシュ毎に...
            List<VertexPositionNormalTexture> vertexBufferSource = new List<VertexPositionNormalTexture>();
            effects = new BasicEffect[model.Meshes.Count];
            startIndexies = new uint[model.Meshes.Count];
            indexNums = new uint[model.Meshes.Count];
            int index = 0;
            foreach(var i in model.Meshes)
            {
                // 開始インデックス
                startIndexies[index] = (uint)vertexBufferSource.Count;
                // 頂点リスト
                foreach(var j in i.Faces)
                {
                    int[] indexToAdd;
                    if (j.Vertices.Count == 3)
                    {
                        indexToAdd = new int[] {0,1,2 };
                    }
                    else if (j.Vertices.Count == 4)
                    {
                        indexToAdd = new int[] { 0, 1, 2, 0, 2, 3 };
                    }
                    else
                    {
                        indexToAdd = new int[0];
                    }
                    foreach(var k in indexToAdd)
                    {
                        VertexPositionNormalTexture vert = new VertexPositionNormalTexture()
                        {
                            Position = new Vector3(model.Vertices[(int)j.Vertices[k]-1]),
                            Normal = new Vector3(model.Normals[(int)j.Normals[k]-1]),
                            TextureCoordinate = new Vector2(model.TextureCoord[(int)j.TextureCoords[k]-1])
                        };
                        vertexBufferSource.Add(vert);
                    }
                }
                // マテリアル
                SharpObjParser.Material materialSource = model.MaterialMap[model.MaterialLib[(int)i.MaterialIndex]];
                i.Material = materialSource;
                BasicEffect be = new BasicEffect(GraphicsDevice)
                {
                    DiffuseColor = new Vector4(i.Material.Diffuse[0], i.Material.Diffuse[1], i.Material.Diffuse[2],1),
                    AmbientLightColor = new Vector3(i.Material.Ambient),
                    SpecularColor = new Vector3(i.Material.Specular),
                    SpecularPower = i.Material.IOR / 100f
                };
                if (!string.IsNullOrWhiteSpace(i.Material.Texture))
                {
                    be.Texture = Texture2D.Load(GraphicsDevice, "Resources\\" + i.Material.Texture);
                    be.TextureEnabled = true;
                }
                effects[index] = be;
                // 照明モード
                if (i.Material.IlluminationModel == 1)
                {
                    effects[index].SpecularColor = Color.Black.ToVector3();
                }
                // インデックス数
                indexNums[index] = (uint)vertexBufferSource.Count - startIndexies[index];

                index++;
            }
            vertexBuffer = Buffer.New(GraphicsDevice, vertexBufferSource.ToArray(), BufferFlags.VertexBuffer);

            // すべての頂点を含むAABBを求める
            Vector3[] positions = new Vector3[model.Vertices.Count];
            for (int i = 0; i < model.Vertices.Count; i++ )
            {
                positions[i] = new Vector3(model.Vertices[i]);
            }
            BoundingBox bb = BoundingBox.FromPoints(positions);
            BoundingSphere bs = BoundingSphere.FromPoints(positions);

                base.LoadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

        }

        protected override void Draw(GameTime gameTime)
        {
            // Use time in seconds directly
            var time = (float)gameTime.TotalGameTime.TotalSeconds;

            // Clears the screen with the Color.CornflowerBlue
            GraphicsDevice.Clear(Color.CornflowerBlue);

            GraphicsDevice.SetVertexBuffer(vertexBuffer);
            GraphicsDevice.SetVertexInputLayout(VertexInputLayout.FromBuffer(0, vertexBuffer));

            var view = Matrix.LookAtLH(new Vector3(0, 0, -5), new Vector3(0, 0, 0), Vector3.UnitY);
            var proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, this.GraphicsDevice.BackBuffer.Width / (float)this.GraphicsDevice.BackBuffer.Height, 0.1f, 100.0f);
            var world = Matrix.Scaling(1f) * Matrix.RotationX(time) * Matrix.RotationY(time * 2) * Matrix.RotationZ(time * .7f);

            //GraphicsDevice.Draw(PrimitiveType.TriangleList, vertexBuffer.ElementCount);

            for (int i = 0; i < startIndexies.Length; i++ )
            {
                effects[i].PreferPerPixelLighting = true;

                effects[i].View = view;
                effects[i].Projection = proj;
                effects[i].World = world;
                effects[i].EnableDefaultLighting();
                effects[i].CurrentTechnique.Passes[0].Apply();

                GraphicsDevice.Draw(PrimitiveType.TriangleList, (int)indexNums[i], (int)startIndexies[i]);
            }

            base.Draw(gameTime);
        }
    }
}
