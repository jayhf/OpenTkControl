using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace OpenTkWPFHost.Core
{
    public class GLContextTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly Thread _thread;

        private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();

        public GLContextTaskScheduler(GLContextBinding binding, DebugProc debugProc)
        {
            _thread = new Thread(() =>
            {
                binding.BindCurrentThread();
                GL.Enable(EnableCap.DebugOutputSynchronous);
                GL.Enable(EnableCap.DebugOutput);
                GL.DebugMessageCallback(debugProc, IntPtr.Zero);
                foreach (var task in _tasks.GetConsumingEnumerable())
                {
                    TryExecuteTask(task);
                }
            });
            _thread.Start();
        }


        protected override void QueueTask(Task task)
        {
            if (_thread.IsAlive)
            {
                _tasks.Add(task);
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false;
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks.ToArray();
        }

        public void Dispose()
        {
            _tasks.CompleteAdding();
        }
    }
}