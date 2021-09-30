using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using OpenTK.Graphics.OpenGL;

namespace OpenTkWPFHost
{
    internal class RenderProcedureTask
    {
        public TaskCompletionSource<BitmapSource> BitmapCompletionSource { get; set; }
            = new TaskCompletionSource<BitmapSource>();

        private readonly Action<IRenderProcedure> _beforeAction;
        private readonly Action<IRenderProcedure> _afterAction;

        public RenderProcedureTask(Action<IRenderProcedure> beforeAction, Action<IRenderProcedure> afterAction)
        {
            this._beforeAction = beforeAction;
            this._afterAction = afterAction;
        }

        public void Enter(IRenderProcedure procedure)
        {
            _beforeAction.Invoke(procedure);
        }

        public void Exit(IRenderProcedure procedure)
        {
            _afterAction.Invoke(procedure);
        }
    }
}