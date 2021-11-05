using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Platform;

namespace OpenTkWPFHost
{
    public static class ContextWaiterExtension
    {
        public static GraphicContextAwaiter GetAwaiter(this ContextWaiter contextWaiter)
        {
            contextWaiter.ResetTask();
            return new GraphicContextAwaiter(contextWaiter.CompletionSource.Task, contextWaiter.Context,
                contextWaiter.WindowInfo);
        }

        public static GraphicContextAwaiter Delay(this IGraphicsContext graphicsContext, int millisecondsDelay,
            IWindowInfo windowInfo)
        {
            return new GraphicContextAwaiter(Task.Delay(millisecondsDelay), graphicsContext, windowInfo);
        }
    }

    /// <summary>
    /// can make use of context switch
    /// </summary>
    public class ContextWaiter
    {
        public bool IsWaiting
        {
            get { return CompletionSource.Task.Status == TaskStatus.WaitingForActivation; }
        }

        internal TaskCompletionSource<bool> CompletionSource => _taskCompletionSource;

        public IGraphicsContext Context { get; set; }

        public IWindowInfo WindowInfo { get; set; }

        private TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();

        public ContextWaiter()
        {
        }

        public ContextWaiter(IGraphicsContext context, IWindowInfo windowInfo)
        {
            this.Context = context;
            this.WindowInfo = windowInfo;
        }

        public void ResetTask()
        {
            if (_taskCompletionSource.Task.IsCompleted)
            {
                _taskCompletionSource = new TaskCompletionSource<bool>();
            }
        }

        public async Task WaitInfinityAsync()
        {
            await CompletionSource.Task;
        }

        public void TrySet()
        {
            if (IsWaiting)
            {
                CompletionSource.TrySetResult(true);
            }
        }

        public void ForceSet()
        {
            CompletionSource.TrySetResult(true);
        }
    }
}