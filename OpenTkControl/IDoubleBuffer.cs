﻿namespace OpenTkControl
{
    /// <summary>
    /// double buffer 
    /// </summary>
    public interface IDoubleBuffer
    {
        IImageBuffer GetFrontBuffer();

        IImageBuffer GetBackBuffer();

        void SwapBuffer();
    }
}