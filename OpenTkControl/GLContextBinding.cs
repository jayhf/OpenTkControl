using OpenTK.Graphics;
using OpenTK.Platform;

namespace OpenTkWPFHost
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

        public void CheckAccess()
        {
            if (Context.IsCurrent)
            {
                Context.MakeCurrent(Info);
            }
        }
    }
}