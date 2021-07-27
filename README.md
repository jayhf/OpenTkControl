

# Declaration
I've experienced a flickering on intel gpu (10700 uhd630) when low fps on [GLWPFControl](https://github.com/opentk/GLWpfControl), 
I tried to solved it by use GL.Finish() but it'll be blocked frequently as opentk render run in ui thread. 
So I commenced combination of  [official project](https://github.com/opentk/GLWpfControl) and [background implementation](https://github.com/jayhf/OpenTkControl).

