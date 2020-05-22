# Streaming

This part is regarding how the HLOD mesh is loaded into the scene. The
HLOD streaming system creates a controller component on top of the
gameObject with the HLOD component. This controller is responsible for
the actual HLOD mesh loading behavior. There is a modes available:

- Unsupported

# Unsupported

![](.//media/image17.png)

Under this mode, we do not support streaming. We add HLOD meshes at
every detail level into the scene, and during gameplay we
enable/disable meshes the HLOD system wants to show/hide.

**OutputDirectory**: Specify where the resource file will be created.
The location must be under an Assets folder.

**Compress Format**: Specifies texture compression format by the
platform. Texture compression happens when resources are imported or
when the platform changes.