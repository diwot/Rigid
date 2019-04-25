# Rigid
Deformable 3D Meshes

![](https://raw.githubusercontent.com/diwot/Rigid/tree/develop/Video/Rigid.gif)

How to use:
1. Open an *.OFF file (there are some in the test data folder, please check the readme of the meshes)
2. Draw a rubber band rectangle while holding the right mouse button pressed to select a group of points on the mesh that you wish to move
3. Select a point of a coordinate system using the left mouse button and drag it to the desired location 
4. Use the middle mouse button to rotate the scene (if the shift key if pressed as well, then the scene gets paned sideways), use the mouse wheel to zoom

External components used:

The rigid body deformation is based on libigl's example "As rigid as possible"
https://github.com/libigl/libigl

The mesh simplification is based on MeshSimplify
https://github.com/smiley22/MeshSimplify
