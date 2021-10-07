#version 430

out vec4 outputColor;

uniform vec4 linecolor;

void main()
{
    outputColor = vec4(linecolor);
}