using System;
using System.Windows;

namespace OpenTkWPFHost.Core
{
    /// <summary>
    /// The event arguments that are sent when a <see cref="GlRender"/> event occurs
    /// </summary>
    public class GlRenderEventArgs : EventArgs
    {
        /// <summary>
        /// If set, the OpenGL context has been recreated and any existing OpenGL objects will be invalid.
        /// </summary>
        public bool NewContext { get; }

        public PixelSize PixelSize => new PixelSize(Width, Height);

        /// <summary>
        /// The width of the drawing area in pixels
        /// </summary>
        public int Width { get; }

        /// <summary>
        /// The height of the drawing area in pixels
        /// </summary>
        public int Height { get; }

        /// <summary>
        /// Can be set to only redraw a certain part of the canvas. Not used for screenshots
        /// </summary>
        public Int32Rect RepaintRect { get; set; }

        //need this property？
        // public bool IsSizeChanged { get; set; }

        /// <summary>
        /// Creates a <see cref="GlRenderEventArgs"/>
        /// </summary>
        /// <param name="width"><see cref="Width"/></param>
        /// <param name="height"><see cref="Height"/></param>
        /// <param name="newContext"></param>
        public GlRenderEventArgs(int width, int height, bool newContext)
        {
            Width = width;
            Height = height;
            RepaintRect = new Int32Rect(0, 0, Width, Height);
            NewContext = newContext;
        }
    }
}