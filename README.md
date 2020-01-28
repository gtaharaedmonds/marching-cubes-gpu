# Marching Cubes Demo (for CPU/GPU)
Here is my implementation of Marching Cubes with C# and HLSL/Compute Shaders on Unity3D. The GPU version is extremely performant and can handle pretty large worlds in real time.




## Features:

-Limitless terrain generation for infinite worlds. GPU version can support a view distance of >500m at a respectable resolution

-Simple level of detail system

-Calculates normals for nice lighting

-Layered 3D noise for terrain generation with smoothing applied

-Simple physics (unfortunately doesn't work with infinite world mode)




## Demo Scenes

1. Marching cubes demo with physics (GPU)

  Has a simple grid of terrain with physics enabled. Click to throw cubes at the world. 
  
2. Infinite world demo (GPU)

  Demo of the infinite terrain system with LOD enabled. The different colors represent different levels of detail.
  
3. Infinite cave demo (GPU)

  Same as the infinite world demo but except inverted so seems like a cave.
