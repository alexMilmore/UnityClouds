# UnityClouds
A set of shaders and some C# code used to generate dynamically lit 2d clouds in unity. The compute shader generates a bump map which is read by the vertex frag shader to calculate the light. Shaders are run using the GPU meaning that they maintain high frame rate even with many cloud object on screen at the same time.
