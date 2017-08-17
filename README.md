# OpenTkControl

This project aims to make it possible to achieve better performance with OpenTk in WPF and it achieves this by copying the data less than existing solutions, such as the examples here: https://github.com/freakinpenguin/OpenTK-WPF. It also allows for all of the rendering to be performed off of the UI thread to improve responsiveness and provides a variety of settings that can be used to further improve performance.

## Getting Started

1. Install the nuget package available here: https://www.nuget.org/packages/JayFleischer.OpenTkControl
2. Add either a ThreadOpenTkControl or UiOpenTkControl to a WPF window
3. Subscribe to the GlRender event and draw something!

## Class Overview

This library provides two different implementations:

UiOpenTkControl - Performs all rendering on the UI thread. In general, this version will perform worse, even if it's the only thing drawn on the screen and therefore I recomment using ThreadOpenTkControl, unless you really need rendering to occur on the UI thread for some reason

ThreadOpenTkControl - Performs all OpenGL rendering on a dedicated update thread to improve performance.

## Features

* Rendering - The GlRender event will be called whenever it is time to render
* Continous Mode - The control will either repaint constantly or only when RequestRepaint is called depending on what Continous is set to.
* Screenshots - Screenshots are rendered separately from the display rendering, which makes it possible for them to have a different size from the display resolution
* Thread Safety - RequestRepaint and Screenshot are thread safe
* Error Reporting - All exceptions are reported to the ExceptionOccurred event
* Other useful properties include FrameRateLimit, OpenGlVersion, MaxPixels, PixelScale and ThreadName
* Changing any of these properties will take effect immediately, except ThreadName and OpenGlVersion

## Bugs

Please report any issues on GitHub. I also welcome pull requests with bug fixes and improvements.
