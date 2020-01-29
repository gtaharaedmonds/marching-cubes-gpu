# Marching Cubes Demo (for CPU/GPU)
Here is my implementation of Marching Cubes with C# and HLSL/Compute Shaders on Unity3D. The GPU version is extremely performant and can handle pretty large worlds in real time.




## Features

- Limitless terrain generation for infinite worlds. GPU version can support a view distance of >500m at a respectable resolution

- Simple level of detail system

- Calculates normals for nice lighting

- Layered 3D noise for terrain generation with smoothing applied

- Simple physics (unfortunately doesn't work with infinite world mode)




## Demo Scenes

Controls: Mouse to look, WASD to move, E/Q to rise/descend, left click to throw a cube (in physics scenes only). 

There are certain scenes that are higher definition which take more performance than the lower detail ones. 



## Notes

GPU Marching Cubes src: Assets/Resources

CPU Marching Cubes src: Assets/CPU Version

There is still lots to be done: Physics in infinite mode, better terrain RNG, smooth LOD transitions, real-time modification, optimization
