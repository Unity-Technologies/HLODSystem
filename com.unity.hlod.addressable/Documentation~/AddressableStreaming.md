# AddressableStreaming

![](.//media/image5.png)

Implement Streaming using the Addressable system. Using this mode
requires user install a separate package with package ID
"com.unity.hlod.addressable" which in turn has dependency on the
Addressable system package.

Under this mode, we "appropriately" subdivide the HLOD quad-tree
hierarchy into a number of separate addressable assets, each
containing a group of quad-tree nodes. When a node in some group is
needed, we load the addressable asset for that group into memory, then
instantiate the needed node into the scene.

**OutputDirectory**: Specify where the resource file will be created.
The location must be under an Assets folder.

**Compress Format**: Specifies texture compression format by platform.
Texture compression happens when texture resources are imported or
when the platform changes.
