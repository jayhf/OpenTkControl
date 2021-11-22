using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Platform;

namespace OpenTkWPFHost
{
    public static class ContextWaiterExtension
    {
        public static GraphicContextAwaiter Wait(this TaskCompletionEvent contextWaiter, GLContextBinding glBinding)
        {
            contextWaiter.ResetTask();
            return new GraphicContextAwaiter(contextWaiter.CompletionSource.Task, glBinding.Context,
                glBinding.Info);
        }

        public static GraphicContextAwaiter Delay(this IGraphicsContext graphicsContext, int millisecondsDelay,
            IWindowInfo windowInfo)
        {
            return new GraphicContextAwaiter(Task.Delay(millisecondsDelay), graphicsContext, windowInfo);
        }
    }
}