using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Platform;

namespace OpenTkWPFHost
{
    public class GLContextTaskScheduler : TaskScheduler, IDisposable
    {
        private readonly Thread _thread;

        private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();

        private IGraphicsContext _graphicsContext;

        private DebugProc _debugProc;

        public GLContextTaskScheduler(GLSettings glSettings, IGraphicsContext sharedContext, IWindowInfo info,
            DebugProc debugProc)
        {
            this._debugProc = debugProc;
            _thread = new Thread(() =>
            {
                try
                {
                    Context = new GraphicsContext(glSettings.GraphicsMode, info, sharedContext, glSettings.MajorVersion,
                        glSettings.MinorVersion, glSettings.GraphicsContextFlags);
                    if (!Context.IsCurrent)
                    {
                        Context.MakeCurrent(info);
                    }

                    GL.Enable(EnableCap.DebugOutputSynchronous);
                    GL.Enable(EnableCap.DebugOutput);
                    GL.DebugMessageCallback(debugProc, IntPtr.Zero);
                    foreach (var task in _tasks.GetConsumingEnumerable())
                    {
                        TryExecuteTask(task);
                    }
                }
                catch (Exception e)
                {
                    Debugger.Break();
                }
            });
        }

        public IGraphicsContext Context
        {
            get => _graphicsContext;
            set => _graphicsContext = value;
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);
            if (!_thread.IsAlive)
            {
                _thread.Start();
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