using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Platform;

namespace OpenTkWPFHost
{
    public class GraphicContextAwaiter : INotifyCompletion
    {
        private readonly IGraphicsContext _graphicsContext;
        private readonly IWindowInfo _windowInfo;
        private TaskAwaiter _taskAwaiter;

        public GraphicContextAwaiter(Task task, IGraphicsContext graphicsContext, IWindowInfo windowInfo)
        {
            _graphicsContext = graphicsContext;
            _windowInfo = windowInfo;
            _taskAwaiter = task.GetAwaiter();
        }

        public bool IsCompleted => _taskAwaiter.IsCompleted;

        public void GetResult()
        {
            if (!_graphicsContext.IsCurrent)
            {
                _graphicsContext.MakeCurrent(_windowInfo);
            }

            _taskAwaiter.GetResult();
        }

        public void OnCompleted(Action continuation)
        {
            _graphicsContext.MakeCurrent(new EmptyWindowInfo());
            _taskAwaiter.OnCompleted(continuation);
        }

        public GraphicContextAwaiter GetAwaiter()
        {
            return this;
        }
    }
}