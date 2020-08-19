# TerrainHLOD

TerrainHLOD takes a Terrain as input and converts it to a HLOD Mesh.

## Common

![](.//media/image7.png)

**Source**: Set the source TerrainData to generate the HLOD.

**Destroy terrain**: The original terrain will be deleted on build when you check this.
It is deleted only the Terrain when using same TerrainData on same scene.

**Chunk Size**: Sets the size of the terminal node in HLOD. Nodes are
split into quadtrees until they are smaller than this value at full
size.

**Border Vertex Count**: For each side of a terminal node terrain
patch, we allocate this number of regularly spaced vertices. As we
combine terrain patch for lower LOD levels, although we can simplify
and reduce vertex count for inner vertices, we must preserve vertices
at the edge to avoid seams. So for example if we have 3 LOD levels and
Border Vertex Count is 256, terminal node have 256

## Simplifier

See section [Simplifier](Simplifier.md).

## Material

Set up the material to be used for the baked TerrainHLODMesh. Note
that we bake the Splat System into single textures for our
TerrainHLODMesh material.

![](.//media/image19.png)

**Material:** Specifies the Material to be used for the
TerrainHLODMesh.

**Size:** Specifies the size at which the texture will be baked.

**Texture properties:** We'll bake 3 textures for the material:
Albedo, Normal, and Mask. Mask texture's R and G channel contains
roughness and specular parameter. This property decides which material
property these textures are set to.

## Streaming

See section [Streaming](Streaming.md).