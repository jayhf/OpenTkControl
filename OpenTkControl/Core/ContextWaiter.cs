using System.Threading.Tasks;

namespace OpenTkWPFHost.Core
{
    /// <summary>
    /// can make use of context switch
    /// </summary>
    public class TaskCompletionEvent
    {
        public bool IsWaiting
        {
            get { return CompletionSource.Task.Status == TaskStatus.WaitingForActivation; }
        }

        internal TaskCompletionSource<bool> CompletionSource => _taskCompletionSource;

        private TaskCompletionSource<bool> _taskCompletionSource = new TaskCompletionSource<bool>();

        public TaskCompletionEvent()
        {
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