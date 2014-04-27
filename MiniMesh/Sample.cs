using System;
using System.Diagnostics;
using System.Windows.Forms;

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
    class Sample : IDisposable
    {
        protected RenderForm form;

        protected Device device;

        protected SwapChain swapChain;

        protected DepthStencilView depthView;

        protected RenderTargetView renderView;

        public void Run()
        {
            form = new RenderForm("SharpDX - Direct3D11 Sample");

            // SwapChain description
            var desc = new SwapChainDescription()
            {
                BufferCount = 1,
                ModeDescription =
                    new ModeDescription(form.ClientSize.Width, form.ClientSize.Height,
                                        new Rational(60, 1), Format.R8G8B8A8_UNorm),
                IsWindowed = true,
                OutputHandle = form.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            // Create Device and SwapChain
            Device.CreateWithSwapChain(DriverType.Hardware, DeviceCreationFlags.Debug, desc, out device, out swapChain);
            var context = device.ImmediateContext;

            // Ignore all windows events
            var factory = swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(form.Handle, WindowAssociationFlags.IgnoreAll);

            // New RenderTargetView from the backbuffer
            var backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);
            renderView = new RenderTargetView(device, backBuffer);

            // Create Depth Buffer & View
            var depthBuffer = new Texture2D(device, new Texture2DDescription()
            {
                Format = Format.D32_Float_S8X24_UInt,
                ArraySize = 1,
                MipLevels = 1,
                Width = form.ClientSize.Width,
                Height = form.ClientSize.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            });

            depthView = new DepthStencilView(device, depthBuffer);

            // Load
            Load(device);

            // Prepare All the stages
            context.OutputMerger.SetTargets(depthView, renderView); 
            context.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));

            var userResized = true;
            // Setup handler on resize form
            form.UserResized += (sender, args) => userResized = true;

            // Setup full screen mode change F5 (Full) F4 (Window)
            form.KeyUp += (sender, args) =>
            {
                if (args.KeyCode == Keys.F5)
                    swapChain.SetFullscreenState(true, null);
                else if (args.KeyCode == Keys.F4)
                    swapChain.SetFullscreenState(false, null);
                else if (args.KeyCode == Keys.Escape)
                    form.Close();
            };

            // Use clock
            var clock = new Stopwatch();
            float totalTime = 0f;
            clock.Start();

            // Main loop
            RenderLoop.Run(form, () =>
            {
                 // If Form resized
                 if (userResized)
                 {
                     // Dispose all previous allocated resources
                     Utilities.Dispose(ref backBuffer);
                     Utilities.Dispose(ref renderView);
                     Utilities.Dispose(ref depthBuffer);
                     Utilities.Dispose(ref depthView);

                     // Resize the backbuffer
                     swapChain.ResizeBuffers(desc.BufferCount, form.ClientSize.Width, form.ClientSize.Height, Format.Unknown, SwapChainFlags.None);

                     // Get the backbuffer from the swapchain
                     backBuffer = Texture2D.FromSwapChain<Texture2D>(swapChain, 0);

                     // Renderview on the backbuffer
                     renderView = new RenderTargetView(device, backBuffer);

                     // Create the depth buffer
                     depthBuffer = new Texture2D(device, new Texture2DDescription()
                     {
                         Format = Format.D32_Float_S8X24_UInt,
                         ArraySize = 1,
                         MipLevels = 1,
                         Width = form.ClientSize.Width,
                         Height = form.ClientSize.Height,
                         SampleDescription = new SampleDescription(1, 0),
                         Usage = ResourceUsage.Default,
                         BindFlags = BindFlags.DepthStencil,
                         CpuAccessFlags = CpuAccessFlags.None,
                         OptionFlags = ResourceOptionFlags.None
                     });

                     // Create the depth buffer view
                     depthView = new DepthStencilView(device, depthBuffer);

                     // Setup targets and viewport for rendering
                     context.Rasterizer.SetViewport(new Viewport(0, 0, form.ClientSize.Width, form.ClientSize.Height, 0.0f, 1.0f));
                     context.OutputMerger.SetTargets(depthView, renderView);

                     // Setup new projection matrix with correct aspect ratio
                     //proj = Matrix.PerspectiveFovLH((float)Math.PI / 4.0f, form.ClientSize.Width / (float)form.ClientSize.Height, 0.1f, 100.0f);

                     // We are done resizing
                     userResized = false;
                 }

                var elapsedTime = clock.ElapsedMilliseconds / 1000.0f;
                clock.Restart();
                totalTime += elapsedTime;

                Update(elapsedTime, totalTime);

                // Clear views
                context.ClearDepthStencilView(depthView, DepthStencilClearFlags.Depth, 1.0f, 0);
                context.ClearRenderTargetView(renderView, Color.CornflowerBlue);

                // Draw
                Draw(device);

                // Present!
                swapChain.Present(1, PresentFlags.None);
            });
            Unload(device);

            // Release all resources
            renderView.Dispose();
            backBuffer.Dispose();
            context.ClearState();
            context.Flush();
            device.Dispose();
            context.Dispose();
            swapChain.Dispose();
            factory.Dispose();
        }

        protected virtual void Load(Device device)
        {

        }

        protected virtual void Update(float elapsedTime, float totalTime)
        {

        }

        protected virtual void Draw(Device device)
        {

        }

        protected virtual void Unload(Device device)
        {

        }

        public virtual void Dispose()
        { }
    }
}
