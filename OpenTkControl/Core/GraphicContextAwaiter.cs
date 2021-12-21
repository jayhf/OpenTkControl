using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace OpenTkWPFHost.Core
{
    public class GraphicContextAwaiter : INotifyCompletion
    {
        private readonly GLContextBinding _binding;
        private TaskAwaiter _taskAwaiter;

        public GraphicContextAwaiter(Task task, GLContextBinding binding)
        {
            this._binding = binding;
            _taskAwaiter = task.GetAwaiter();
        }

        public bool IsCompleted => _taskAwaiter.IsCompleted;

        public void GetResult()
        {
            _binding.BindCurrentThread();
            _taskAwaiter.GetResult();
        }

        public void OnCompleted(Action continuation)
        {
            _binding.BindNull();
            _taskAwaiter.OnCompleted(continuation);
        }

        public GraphicContextAwaiter GetAwaiter()
        {
            return this;
        }
    }
}