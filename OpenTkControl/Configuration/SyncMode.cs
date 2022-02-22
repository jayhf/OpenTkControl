namespace OpenTkWPFHost.Configuration
{
    public enum SyncMode : int
    {
        /// <summary>Vsync disabled.</summary>
        Off = 0,

        /// <summary>VSync enabled.</summary>
        On = 1,

        /// <summary>
        /// VSync enabled, unless framerate falls below one half of target framerate.
        /// If no target framerate is specified, this behaves exactly like <see cref="F:OpenTK.VSyncMode.On" />.
        /// </summary>
        Adaptive = -1,
    }
}