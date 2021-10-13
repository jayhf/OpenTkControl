using System;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace OpenTkWPFHost
{
    internal class RenderProcedureTask
    {
        public bool ImageWriteBack { get; }

        public TaskCompletionSource<BitmapSource> BitmapCompletionSource { get; }

        private readonly Action<IRenderProcedure> _beforeAction;
        private readonly Action<IRenderProcedure> _afterAction;

        public RenderProcedureTask(Action<IRenderProcedure> beforeAction, Action<IRenderProcedure> afterAction,
            bool imageWriteBack = true)
        {
            this._beforeAction = beforeAction;
            this._afterAction = afterAction;
            ImageWriteBack = imageWriteBack;
            if (imageWriteBack)
            {
                BitmapCompletionSource = new TaskCompletionSource<BitmapSource>();
            }
        }

        public void Enter(IRenderProcedure procedure)
        {
            _beforeAction?.Invoke(procedure);
        }

        public void Exit(IRenderProcedure procedure)
        {
            _afterAction?.Invoke(procedure);
        }
    }
}