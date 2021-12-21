namespace OpenTkWPFHost.Configuration
{
    public enum RenderTrigger
    {
        /// <summary>
        /// listen to <see cref="CompositionTarget.Rendering"/>
        /// </summary>
        CompositionTarget = 0,

        /// <summary>
        /// use a discrete loop to drive render
        /// <para>warning: event rate of <see cref="CompositionTarget "/> will surge </para> 
        /// </summary>
        Internal = 1,
    }
}