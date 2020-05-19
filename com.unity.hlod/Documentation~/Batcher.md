# Batcher

This setting determines how the meshes are combined. We provide two options here:

## MaterialPreservingBatcher

![](.//media/image21.png)

HLOD meshes are created by grouping meshes with the same material. We use the existing material as it is.

## SimpleBatcher

![](.//media/image8.png)

Even if the material of adjacent meshes is different, we always merge
the meshes and create a new material for the merged mesh. To do this
properly, we need to combine the textures referenced by different
materials into textures referenced by the new material, and
accordingly perform UV-remapping when building the merged mesh. We
have a number of settings that specifies how this process should be
performed:

**Pack texture size**: Sets the size of the generated texture atlas.

**Limit texture size**: Sets the maximum area each source texture
occupies. If (because of this setting) there is unused space after all
source textures are copied to the generated texture atlas, HLOD system
will try to reduce the size of the generated texture atlas.

**Material**: Sets the material for the merged mesh. If not set, we'll
use *Standard Shader*.

**Textures:** An entry X-Y here means we find all textures referenced
as property X from materials used by meshes in the HLOD hierarchy,
merge them together into a combined texture atlas and set this texture
atlas as parameter Y in the new generated material.

Note that this means HLOD system would only work properly if all the
materials used by the meshes in the HLOD hierarchy have a similar
texture input naming convention.

**Update texture properties:** The **Textures** setting tries to
collect appropriate material properties for the GUI drop-down, but
sometimes this does not work properly. Press this button if some
material property you want to use for the **Textures** setting is
missing.