using System;
using System.Threading;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Input;
using OpenTkWPFHost;
using TestRenderer;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            using (Game game = new Game(1280, 720, "LearnOpenTK"))
            {
                game.VSync = OpenTK.VSyncMode.Off;
                game.Run();
            }
        }
    }

    public class Game : GameWindow
    {
        private readonly Timer _timer;

        private readonly TestRendererCase _testRendererCase = new TestRendererCase();

        private IRenderer Renderer => _testRendererCase.Renderer;

        public Game(int width, int height, string title) : base(width, height, GraphicsMode.Default, title)
        {
            _timer = new Timer((state => { this.Title = this.RenderFrequency.ToString(); }), null, TimeSpan.Zero,
                period: TimeSpan.FromSeconds(1));
           
        }
        protected override void OnLoad(EventArgs e)
        {
            Renderer.Initialize(this.Context);
            base.OnLoad(e);
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            base.OnRenderFrame(e);
            Renderer.Render(new GlRenderEventArgs(this.Width, this.Height, false));
            Context.SwapBuffers();
        }

        protected override void OnResize(EventArgs e)
        {
            var clientSize = this.ClientSize;
            Renderer.Resize(new PixelSize(clientSize.Width, clientSize.Height));
            base.OnResize(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            KeyboardState input = Keyboard.GetState();

            if (input.IsKeyDown(Key.Escape))
            {
                Exit();
            }

            base.OnUpdateFrame(e);
        }

        protected override void OnUnload(EventArgs e)
        {
            base.OnUnload(e);
            Renderer.Uninitialize();
            
        }
        

        protected override void Dispose(bool manual)
        {
            _timer.Dispose();
            base.Dispose(manual);
        }
    }
}