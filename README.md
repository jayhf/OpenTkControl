

# Declaration
I've experienced a flickering on intel gpu when low fps on [GLWPFControl](https://github.com/opentk/GLWpfControl), 
I tried to solved it by use GL.Finish() but it'll be blocked frequently as opentk render run in ui thread. 
So I commenced combination of  [official project](https://github.com/opentk/GLWpfControl) and [background implementation](https://github.com/jayhf/OpenTkControl).

## Feature

0. Solved problem flickering in intel uhd gpu on low framerate when use offical [official library](https://github.com/opentk/GLWpfControl) (As offical library use d3dimage, It's very strange that intel uhd gpu will flicing on low framerate but my gtx970 will not. I'll discuss the phenomenon below.)

1. Render procedure run in separate thread, no blocking in ui thread.

2. Both d3dimage and writeablebitmap approaches, high flexiblity.

3. Async GL.ReadPixel and double pixel buffer object in writeablebitmap.

4. Can render user invisible frame.

6. Can set framerate.

7. Can manually call render.

8. Provider a 2d coordinate chart example.

![Mdpng](mdpng.png)
