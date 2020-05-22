# Simplifier

Specifies how to simplify when creating HLODMesh.

Currently, there are two methods available: None and UnityMeshSimplifier.

## None

![](.//media/image11.png)

We do not simplify when creating HLODMesh.

## UnityMeshSimplifier

![](.//media/image18.png)

We simplify using [UnityMeshSimplifier](https://github.com/Unity-Technologies/UnityMeshSimplifier).

**Polygon Ratio**: Sets the rate at which the polygon is reduced.

**Triangle Range**: Sets the maximum/minimum number of polygons after simplification.