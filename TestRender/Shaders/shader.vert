#version 330 core

layout(location = 0) in vec2 aPosition;
uniform mat4 transform;

void main(void)
{
    //gl_Position = vec4(vec3(aPosition, 1.0) * transform, 1.0);

    gl_Position = vec4(aPosition, 1.0 , 1.0) * transform;
    //gl_Position = vec4(aPosition, 1.0 , 1.0);
}
