using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace OpenTkWPFHost.Core
{
    public class Pipeline<T>
    {
        private ITargetBlock<T> headTargetBlock;

        private IDataflowBlock tailDataflowBlock;

        public Pipeline(ITargetBlock<T> headTargetBlock, IDataflowBlock tailDataflowBlock)
        {
            this.headTargetBlock = headTargetBlock;
            this.tailDataflowBlock = tailDataflowBlock;
        }

        public void Complete()
        {
            headTargetBlock.Complete();
        }

        public bool Post(T t)
        {
            return headTargetBlock.Post(t);
        }

        public Task<bool> SendAsync(T t, CancellationToken token)
        {
            return headTargetBlock.SendAsync(t, token);
        }

        public Task<bool> SendAsync(T t)
        {
            return headTargetBlock.SendAsync(t);
        }

        public void Fault(Exception exception)
        {
            headTargetBlock.Fault(exception);
        }

        public Task Completion => tailDataflowBlock.Completion;

        public Task Finish()
        {
            headTargetBlock.Complete();
            return tailDataflowBlock.Completion;
        }
    }
}