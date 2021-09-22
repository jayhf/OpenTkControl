using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;
using OpenTK.Platform.Windows;

namespace OpenTkWPFHost
{
    public class DoubleBuffer<T>
    {
        private readonly Func<T> _createFunc;
        private T[] drawingVisual = new T[2];

        public T FrontVisual { get; private set; }
        public T BackVisual { get; private set; }

        public DoubleBuffer(Func<T> createFunc)
        {
            _createFunc = createFunc;
        }

        public void Create()
        {
            drawingVisual[0] = _createFunc();
            drawingVisual[1] = _createFunc();
            FrontVisual = drawingVisual[0];
            BackVisual = drawingVisual[1];
        }

        public void Swap()
        {
            var visual = FrontVisual;
            FrontVisual = BackVisual;
            BackVisual = visual;
        }
    }

    ///Renderer that uses DX_Interop for a fast-path.
    public class GLDXProcedure : IRenderProcedure
    {
        private DxGlContext _context;

        private DxGLFramebuffer _framebuffers;

        private IRenderer _renderer;

        /// The OpenGL framebuffer handle.
        public int FrameBufferHandle => _framebuffers?.GLFramebufferHandle ?? 0;

        public IntPtr DxRenderTargetHandle => _framebuffers?.DxRenderTargetHandle ?? IntPtr.Zero;

        /// The OpenGL Framebuffer width
        public int Width => _framebuffers?.FramebufferWidth ?? 0;

        /// The OpenGL Framebuffer height
        public int Height => _framebuffers?.FramebufferHeight ?? 0;

        private volatile bool _rendererInitialized = false;

        private DxCanvas dxCanvas = new DxCanvas();

        public void FlushFrame(DrawingContext drawingContext)
        {
            if (!dxCanvas.IsAvailable)
            {
                return;
            }
            var transformGroup = this._framebuffers.TransformGroup;
            drawingContext.PushTransform(transformGroup);
            var dxCanvasImage = this.dxCanvas.Image;
            drawingContext.DrawImage(dxCanvasImage, new Rect(new Size(dxCanvasImage.Width, dxCanvasImage.Height)));
            drawingContext.Pop();
        }

        public bool IsInitialized { get; private set; }

        public bool ReadyToRender => Width != 0 && Height != 0;

        public IRenderer Renderer
        {
            get => _renderer;
            set
            {
                _renderer = value;
                _rendererInitialized = false;
            }
        }

        public GLSettings GlSettings { get; }

        public GLDXProcedure(GLSettings glSettings)
        {
            this.GlSettings = glSettings;
        }

        public void SwapBuffer()
        {
        }


        public bool CanAsync { get; set; } = false;

        public void SizeCanvas(CanvasInfo info)
        {
            dxCanvas.Create(info);
        }

        public void Begin()
        {
            var image = dxCanvas.Image;
            image.Lock();
            image.SetBackBuffer(D3DResourceType.IDirect3DSurface9,
                this.DxRenderTargetHandle);
        }

        public void End()
        {
            var image = dxCanvas.Image;
            image.AddDirtyRect(new Int32Rect(0, 0, this.Width,
                this.Height));
            image.Unlock();
        }

        /// Sets up the framebuffer, directx stuff for rendering. 
        private void PreRender()
        {
            Wgl.DXLockObjectsNV(_context.GlDeviceHandle, 1, new[] { _framebuffers.DxInteropRegisteredHandle });
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _framebuffers.GLFramebufferHandle);
        }

        /// Sets up the framebuffer and prepares stuff for usage in directx.
        private void PostRender()
        {
            Wgl.DXUnlockObjectsNV(_context.GlDeviceHandle, 1, new[] { _framebuffers.DxInteropRegisteredHandle });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>indicate that if renderer initialize successfully or already initialized</returns>
        private bool CheckRenderer()
        {
            if (_rendererInitialized)
            {
                return true;
            }

            if (Renderer != null)
            {
                Renderer.Initialize(_context.GraphicsContext);
                _rendererInitialized = true;
                return true;
            }

            return false;
        }

        public void Initialize(IWindowInfo window)
        {
            _context = new DxGlContext(GlSettings, window);
            CheckRenderer();
            IsInitialized = true;
        }

        public void SizeFrame(CanvasInfo size)
        {
            var width = size.ActualWidth;
            var height = size.ActualHeight;
            if (_framebuffers == null || _framebuffers.Width != width || _framebuffers.Height != height)
            {
                _framebuffers?.Dispose();
                if (width > 0 && height > 0)
                {
                    _framebuffers = new DxGLFramebuffer(_context, width, height, size.DpiScaleX, size.DpiScaleY);
                    if (CheckRenderer())
                    {
                        Renderer.Resize(_framebuffers.PixelSize);
                    }

                    // GL.Viewport(0, 0, _framebuffers.FramebufferWidth, _framebuffers.FramebufferHeight);
                }
            }
        }

        public bool Render()
        {
            PreRender();
            Renderer?.Render(new GlRenderEventArgs(Width, Height, false));
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Finish();
            PostRender();
            return true;
        }

        public void Dispose()
        {
            _framebuffers?.Dispose();
            _context?.Dispose();
        }
    }
}