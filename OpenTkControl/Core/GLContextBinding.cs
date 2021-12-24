using OpenTK.Graphics;
using OpenTK.Platform;

namespace OpenTkWPFHost.Core
{
    public class GLContextBinding
    {
        public GLContextBinding(IGraphicsContext context, IWindowInfo info)
        {
            Context = context;
            Info = info;
        }

        public IGraphicsContext Context { get; }

        public IWindowInfo Info { get; }

        public void BindCurrentThread()
        {
            if (!Context.IsCurrent)
            {
                Context.MakeCurrent(Info);
            }
        }

        public void BindNull()
        {
            Context.MakeCurrent(new EmptyWindowInfo());
        }
    }
}