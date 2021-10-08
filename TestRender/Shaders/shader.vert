#version 430 core

 layout(std430, binding=0) buffer YAxisScale{
    int Array[];
};


layout(location = 0) in vec2 aPosition;
uniform mat4 transform;

void main(void)
{
    //gl_Position = vec4(vec3(aPosition, 1.0) * transform, 1.0);

    gl_Position = vec4(aPosition, 1.0 , 1.0) * transform;
	Array[0] = 123;
/*	if((gl_Position.x > -1) && (gl_Position.x < 1))
	{
		int index = int((gl_Position.y + 1) * 100);
		if(index < 300)
		{
			Array[index] = 1;
		}
	}
	*/
	
}
