# **Getting the Package**

## Prerequisites

*   Git Client
*   Unity 2019.3+ (2019.2 works too, but 2019.3+ is recommended)
*   ~Unity Technologies GitHub Account~

## Getting HLOD System

HLOD System is provided as an individual package. Currently it is available only on GitHub. In later stages of development, it will be available through Unity Package Manager.

HLOD System GitHub Repo URL is [https://github.com/Unity-Technologies/HLODSystem](https://github.com/Unity-Technologies/HLODSystem)

Follow through to get the package to your local PC and work with it.

### CLI

**Step 1.** Run one of the following commands to clone the repo:

$ git clone [https://github.com/Unity-Technologies/HLODSystem.git](https://github.com/Unity-Technologies/HLODSystem.git)

or

$ git clone [git@github.com:Unity-Technologies/HLODSystem.git](mailto:git@github.com) 

<table><tbody><tr><td><p>user@DESKTOP&nbsp;/Dev$ git clone https://github.com/Unity-Technologies/HLODSystem.git<br>Cloning into 'HLODSystem'...</p><p>remote: Enumerating objects: 150, done.</p><p>remote: Counting objects: 100% (150/150), done.</p><p>remote: Compressing objects: 100% (110/110), done.</p><p>remote: Total 4179 (delta 76), reused 80 (delta 39), pack-reused 4029</p><p>Receiving objects: 100% (4179/4179), 139.22 MiB | 16.71 MiB/s, done.</p><p>Resolving deltas: 100% (2684/2684), done.<br>user@DESKTOP&nbsp;/MobileOpenWorldSample</p></td></tr></tbody></table>

  
 

**Step 2.** Next, change directory to the root directory of HLOD, and pull the dependencies, which are included into project as Git Submodules. They are [ConditionalCompilationUtility](https://github.com/Unity-Technologies/ConditionalCompilationUtility) and [UnityMeshSimplifier](https://github.com/Unity-Technologies/UnityMeshSimplifier):

$ cd HLODSystem

$ git submodule update --init --recursive

<table><tbody><tr><td><p>user@DESKTOP&nbsp;/Dev$ cd HLODSystem</p><p>user@DESKTOP&nbsp;/Dev/HLODSystem$ git submodule update --init --recursive<br>Submodule 'com.unity.hlod/Package/ConditionalCompilationUtility' (https://github.com/Unity-Technologies/ConditionalCompilationUtility.git) registered for path 'com.unity.hlod/Package/ConditionalCompilationUtility'</p><p>...</p><p>user@DESKTOP&nbsp;/MobileOpenWorldSample</p></td></tr></tbody></table>

  
 

### Sourcetree

For this particular example, we used [Sourcetree](https://www.sourcetreeapp.com/), but the steps should be fairly similar for the GUI Client of your choice.

  
 

**Step 1.** Open Sourcetree, and click Clone Button:

![](https://lh6.googleusercontent.com/4M9KctX4xEHUgssh4CjVkM4ULEHhzwFS1J0F85hzbV4b2-U7Mvb33VmSN2kP93U0WzL_C-WzoFdd4Z7iCBL9jaqd90wBkKZ4Ot5ucxFDyrUSu-2ZxLeelQq1QdHudqmPAol4jXBcSsqRLCpjOU6uqvKVr5iMd5IEC4cbA5b52Ki4cGPo_gbU_eux3v3y)

  
 

**Step 2.** Input the clone URL, select the destination where the repo is cloned, and wait for a couple of seconds until Sourcetree gets the repo details:

[https://github.com/Unity-Technologies/HLODSystem.git](https://github.com/Unity-Technologies/HLODSystem.git)

or

[git@github.com:Unity-Technologies/HLODSystem.git](mailto:git@github.com)

**Step 3.** Make sure to check Recurse submodules checkbox and click Clone:

  
 

![](https://lh3.googleusercontent.com/Jc0K_60HVH_Vt3ajUZ6OLL63fmepIEG1GGMPEI2BYXUhXb_GC-RzfBw3LgVhdRk6uigV3466Ke8C3HuQtlReaUCKMoDb4LcpdTfMQ10YIP76a11abpvIlInfJBl5rk7qGDYD5gElcKHQufq6j-CtBMtgaMhNKGUkwFJ3T_4WRnk-a9ZSPW71qLqwYlsZ)

## Importing package to a Unity Project

**Step 1.** Open the Unity Project which you want to add HLOD to, and open the Package Manager Window.

**Step 2.** Click the + button on the top-left corner, and select Add package from disk… menu:

![](https://lh5.googleusercontent.com/YwMkuMvfQYicpC6COd93Q-CFMps6j7kPG1dX_M-poToqKOVABzCBhe9Qv_NnxM4_0IW9-9jeTH1Gnl0MXjhsbWb41D4gfbP-6-t-Kd5CxWRovZArnvEpVwCzqFOS_odQyigM3UtUFq7Ops0x0PquEElJkFatgKqDxhzi60_DogbrZ8l10Hdgftaz6NHk)

  
 

**Step 3.** Browse to the location where you cloned HLODSystem repo, open com.unity.hlod folder and select package.json file. Click Open.

![](https://lh4.googleusercontent.com/Fy9WEas8_FjMLSzPr9yzCsqXPHei4nbbN5i824P_6wXJEkNshVCcyTwRFUV0NbXt8-knhn0csyVdODy6MfH8xK3jGhzqGO494CQwCqHaciHcfI7nD1PFnLWIR8DzfmTVC46es67S498fgQv2Qsex3tUxOmKHAGqqmnwyfWsmudnyZse-lbXwsBBqpIX2)

  
 

The Editor will import and add HLOD System to the project. Now the project is ready to use it.

  
 

## Using HLOD in the Project

### Creating the Test Scene

You can follow through the steps and create the test scene with necessary object to use with HLOD, or you can download the Test Scene created in advance and just apply HLOD.

  
 

**Step 1.** Create a new scene and place objects in it. Let’s say, 5 of them. The objects must be:

*   Static
*   With albedo material
*   Add some more features

  
 

HLOD does not work with objects that are not static, have animated mesh, or are destroyed during game-play. 

![](https://lh3.googleusercontent.com/Ug1Pzy1z0HUcM2UWHpOeFFiMmeHdbgfx8kPgY3j5vH08aucgZ_y1TvUw8IpseXD5fmHvne6AUq7osABS4xnuQk1hM7_9UleED5NsEi4n7fCwhtO-1rLOmrgaOy_AWRyTC8BR6zAH6YDXLVE_RHblH8Sp5JmFTz7ZoJJx11o3eUbuDSN9Sy8YN3yI12dR)

  
 

This is how Hierarchy Window looks like after we added our Game Objects:

![](https://lh6.googleusercontent.com/tWVJnUTpFHS4G6mX8Ei_t0Pkw60__Vwxdym8kmsAtSBYMLDWktVrl9P5t-QMpMpxHlwVVaMGgbmQfPD6R-XmUiedzgcn7fBhCxS7jU8Ntj03rHH6WfGAnYOtvozCxOYiiyqTOwqH_xzrykvr8NDvYnlYT3l_AahE2vdG7bXnFiZghIvJNUnHX9VusHnT)

  
 

### Applying HLOD to Objects

  
 

**Step 1.** Create an empty Game Object and name it HLOD or anything else you would like it to be:

![](https://lh5.googleusercontent.com/1MIco_rJe0NYNGV5Q6sYSnj5jCELGwIdShtERzDvs5E4O64lXwZAEvUoMD5atV3BEnvlJTtkikBha6kr4qziIGjRRdEjEGm77P03nvYR6Q5M22JQTT5LvsdpnG0dDTQpIB5LgHRApNIGukYbRcA84LTDgn2Yx6RxMzBDEtg_fGqAIS02hmrsLjmiXc1u)

  
 

**Step 2.** Select your game objects and make them the children of HLOD Game Object:

![](https://lh6.googleusercontent.com/HaeGsuK_UDGtYIeJkoiGz7nkO0wTu1m-sf9Bl6gGIbloGv1R2mESdfFDUTqfO7iks090dLkZy521Yyw8M5wgoenI0eP7gltQHO2BshRJLgpI3pYdhso-bv-KKW88XJ1MYz8llnyODpikLgvqiVom5oCsp5Vw4uYjAvFX34sJU7HsU1W3PvOOjYhWNm6-)

  
 

**Step 3.** Select HLOD Game Object and add HLOD Component to it in Inspector, then click Generate: 

![](https://lh5.googleusercontent.com/Fzbo-tAI23TmC_3E9Qjk7NJjVwhQgTp7xHYgQYUt40-hnhsPnYnwcgWBEuPYhqX1HRej5ZlZ7f2EY9x5ncheVwoxKpNS5bvnYLFHSIKKxoGllH1QT2NsFOsCH6YSZsDi6i3K9MIRLHajaw9zNDvvrh99JMrYbc125Uik7klX8NHXz65gqFFbIRm5h5JY)

**Step 4.**  

Add camera to   
 

For a detailed explanation of the settings of HLOD Component, see [this part](https://docs.google.com/document/d/18HgBIr8oJweKaXtsIHZlh0s5HuXvQVmVfVcMPHAYS1A/edit#heading=h.s3ctt0skijb9) of the document.

  
 

A Default HLOD Controller Component will be added automatically:

![](https://lh6.googleusercontent.com/zrDnDiyO8rMI31_3gRZ8IJYIzthSB6ykJdyIJsOUTKvgy0JX8blXKuClBoKAoTRoMUjB19yvmQ4-4vO9iyhvle1JsfniFv_1WAZJuBMLUjsxprcJMGaxYZtcKqdh1moSp0aEgjgvVIQsuJ4J6MqHY2kxSk565dmSaeNhOYlrGnmRcCNs0I4OxZxX99_A)

  
 

The HLOD Data Structure will be created in the path which OutputDirectory parameter of HLOD Component points to:

![](https://lh6.googleusercontent.com/suYBDHelBTtZM0fjTnohLBCpIx9SxHYKw1mQ31m__CjLtf9rmLs0fEttoFxF3LiWmoMU45jq-gSncoGNnTX5yt4HFkVDkaYs-Om_un9F8mS6AvQKpHEFbz2zzIl7eUA4rLiZefQ2YUCeNHfjSw8sqjTdvQMhoFE3en0vqxes34lOSRRrBT16z17B_aYt)

  
 

If you expand HLOD Data Structure and click the mesh component of it, you can see how Game Object Meshes are combined into a single mesh:

![](https://lh6.googleusercontent.com/O2It5oq40RENCfUj4B9OVhMtSJuqWxg9cU2Ez3ysVZgzd9f1FmvF5edKtJDQQxDUIu87d9JuEE4UeDOx-VtgO16rkS3XZ_p2gz8iZv4Xbv8Lqc9ybb_fXEBHuUoc0hJcpCi-FXU0ydMd_r9cHs2kCFzl-bB_a4EkjBVRg5fG5N4B5OsTb9pN6aTEkPMU)

  
 

**Step 4.** Click Destroy button if you want to destroy and/or re-generate HLOD for a given Game Object.

  
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  
  
 

# **Component**

HLODSystem provides 2 components used to build 2 types of HLOD data-structure:

  
 

*   **HLOD**: This component is used to generate HLOD data-structure for static objects.
*   **TerrainHLOD**: This component is creating HLOD meshes from the Terrain data.

## HLOD

The **HLOD** component generates HLOD data-structure for static objects.

### Common

Typical setting related to HLOD.

![](https://lh5.googleusercontent.com/xt64Gn0hGVQetthF2HTBTcBikNq_91iKDgpkB_gzEfUksNA2_iUNgOZngXDNla_n4MS4moyxg7nYrlP3y2K6JaBa3b4KSNPLaO1FhXL71prBp6nXfEktrHiqYvPOjFAAASruqxBR7WbnTuphet12ZlElRaP1M3rqZL7NLOJhaYHH3LJLIMBnT_EH_cLh)

  
 

**Chunk Size**: For our hierarchy with HLOD component, we define a number of detail levels where each level represents one way to group the meshes into a number of merged meshes. On the top level, all meshes are merged together. On the next level, we partition the meshes into 4 merged mesh (we do not partition along height axis, but only across the horizontal plane). In this way, the HLOD system builds a quadtree data structure.  
  
The “Chunk Size” setting sets the size of the “terminal node” of the HLOD quadtree. Nodes are split into quadtrees until they are smaller than this value at full size.

  
 

**High/Low/Cull**: This setting defines a function that categorizes a mesh into 3 categories: _High_, _Low_, and _Cull_. Looking at the above setting as an example, if a mesh has AABB projected onto the screen occupying less than 1% of the screen, it is categorized as “Cull”. If a mesh has AABB projected onto the screen occupying more than 80% of the screen, it is categorized as “High”.

  
 

When rendering HLOD mesh hierarchy, we traverse the HLOD quadtree. If the root HLOD mesh is “Low”, we render just the combined HLOD mesh. If the root HLOD mesh is “Cull”, we render nothing. If the root HLOD mesh is “High”, we look at the children of the HLOD root node and decide how to render each children recursively in the same fashion.  
  
 

**Min Object Size**: Specifies the minimal size for a mesh to be included in the HLOD data-structure. If the size of a mesh is greater than the set value, it is included in the HLOD system. If a mesh is too small, it is excluded from the HLOD mesh.

### Simplifier

This is a setting shared with TerrainHLOD. Please see [Shared.Simplifier](https://docs.google.com/document/d/18HgBIr8oJweKaXtsIHZlh0s5HuXvQVmVfVcMPHAYS1A/edit#heading=h.ahd7i94e5hy6) section.

### Batcher

This setting determines how the meshes are combined. We provide two options here:

  
 

**MaterialPreservingBatcher**

![](https://lh5.googleusercontent.com/SMcaayRr9OSyX3ntXU1-DgwGCrYi9nVu_hxR8h5J5VcuzalF7EnEVl5p8SdCoBo_sSOzboSxXxN_NSAhhAjnAoIQLkFlG_Gc6Qc8E11_wpX8FC8_CHNE6exsPeW8PAMcDAlhJZWfKixCYb5BU5-MXQWTYiwnSIXXqKSwhLhOimZrUicLdpr0VC2PReR4)

  
 

HLOD meshes are created by grouping meshes with the same material. We use the existing material as it is.

  
 

**SimpleBatcher**

![](https://lh6.googleusercontent.com/on9YMmcqfMwjjci1O8izqAanubU_Z0040z069p3fWTydscwAyckkkQ-epRZe41K55DdhV07wvzOpBNp9XpKnocn14yjgzDoF6nWGYbowdQqT3TTPTrqFPT-TN-VyjF9x1A4uUvAw6fTAY6mTikSQ1QG2JEzdSqpzF1E6EUZhIGdq7r3vLrmQL-9RPVPT)

  
 

Even if the material of adjacent meshes is different, we always merge the meshes and create a new material for the merged mesh. To do this properly, we need to combine the textures referenced by different materials into textures referenced by the new material, and accordingly perform UV-remapping when building the merged mesh. We have a number of settings that specifies how this process should be performed:

  
 

**Pack texture size**: Sets the size of the generated texture atlas.

**Limit texture size**: Sets the maximum area each source texture occupies. If (because of this setting) there is unused space after all source textures are copied to the generated texture atlas, HLOD system will try to reduce the size of the generated texture atlas.

**Material**: Sets the material for the merged mesh. If not set, we’ll use _Standard Shader_.

**Textures:** An entry X-Y here means we find all textures referenced as property X from materials used by meshes in the HLOD hierarchy, merge them together into a combined texture atlas and set this texture atlas as parameter Y in the new generated material.

  
 

Note that this means HLOD system would only work properly if all the materials used by the meshes in the HLOD hierarchy have a similar texture input naming convention.  
  
 

**Update texture properties:** The **Textures** setting tries to collect appropriate material properties for the GUI drop-down, but sometimes this does not work properly. Press this button if some material property you want to use for the **Textures** setting is missing.

  
 

### Streaming

This part is shared with TerrainHLOD. Please see [Shared.Streaming](https://docs.google.com/document/d/18HgBIr8oJweKaXtsIHZlh0s5HuXvQVmVfVcMPHAYS1A/edit#heading=h.gx04uwccgot7) section.

## TerrainHLOD

TerrainHLOD takes a Terrain as input and converts it to a HLOD Mesh.

### Common

![](https://lh6.googleusercontent.com/7TycBgtcUZ4lPUlaKQzntZg9DltSFkONkaX8o0o1HTHyQdx7V59diN8WMMBQPmMaBkWBvhpYZw2NFSDviwDK0IfafPZyjRhGZi0dup95z7TndMDABAnKdct4UaU9HX4DnU7zpnhKPCMtycrcKrs9SJavIIG-7IiozS-TsdMqPILlwFF6ZAJICcY7gk8e)

**Source**: Set the source TerrainData to generate the HLOD.

**Chunk Size**: Sets the size of the terminal node in HLOD. Nodes are split into quadtrees until they are smaller than this value at full size.

**Border Vertex Count**: For each side of a terminal node terrain patch, we allocate this number of regularly spaced vertices. As we combine terrain patch for lower LOD levels, although we can simplify and reduce vertex count for inner vertices, we must preserve vertices at the edge to avoid seams. So for example if we have 3 LOD levels and Border Vertex Count is 256, terminal node have 256 

### Simplifier

This part is shared with HLOD. Please see [Shared.Simplifier](https://docs.google.com/document/d/18HgBIr8oJweKaXtsIHZlh0s5HuXvQVmVfVcMPHAYS1A/edit#heading=h.ahd7i94e5hy6) section.

  
 

### Material

Set up the material to be used for the baked TerrainHLODMesh. Note that we bake the Splat System into single textures for our TerrainHLODMesh material.

![](https://lh4.googleusercontent.com/yL5ABggtnAjCteUDxjOawVW5UZcO89WoHemhymyyM6c27BVIlH9ghRYUC5Dj6hkGmpujQpgf6jNHpnr8J30Ox_HI0cZkJtgFdXoAkYQlBf70ybn_p1FvGm1vKs-5zYuzdSRHZC-FPmYlKqZnpLchNsx8GT55ymJky3AYAegIbo4PT95G1VWAYQV26VvV)

**Material:** Specifies the Material to be used for the TerrainHLODMesh.

**Size:** Specifies the size at which the texture will be baked.

**Texture properties:** We’ll bake 3 textures for the material: Albedo, Normal, and Mask. Mask texture’s R and G channel contains roughness and specular parameter. This property decides which material property these textures are set to.

### Streaming

This part is shared with HLOD. Please see [Shared.Streaming](https://docs.google.com/document/d/18HgBIr8oJweKaXtsIHZlh0s5HuXvQVmVfVcMPHAYS1A/edit#heading=h.gx04uwccgot7) section.

## Shared

### Simplifier

Specifies how to simplify when creating HLODMesh.

Currently, there are two methods available: NotUseSimplifier and UnityMeshSimplifier.

  
 

**None**

![](https://lh6.googleusercontent.com/Zr6QpKYRTKWTe5fARBratzdu5vyOC0Euvmxd6te9mPqHz8KMaa2ps5ul4n1IS9XWSdFi_H0M6pIuHUXR2IS6Ijjuvc8sSMGecJK6xWqmJFZzF8YTNFY8CxXRQ_QPbmMS4dQAhUrx-KM16DdzyePMbbhzJ-G16PVxFB7rkxY9nToj4Eu6b7IV-CeYbnnC)

We do not simplify when creating HLODMesh.

  
 

**UnityMeshSimplifier**

![](https://lh3.googleusercontent.com/9MciVME4ydz2bpr0bixp4_KCSsSsQ9MNgph1AS86lJYjhZQO_28KEMLR0woCguEdBgtCsWHLWx69KyTqxwgxxQtFBturnN8h35Ea2tBH5OTCj7B8mmX9kV3C-iSRnDEMKNIKfDfHvSUv-4M0fbfwPyzsGw4nB-S88LoVvU-f6UGHcO4Q0QekxCQrU5Ry)

We simplify using [UnityMeshSimplifier](https://github.com/Unity-Technologies/UnityMeshSimplifier).

  
 

**Polygon Ratio**: Sets the rate at which the polygon is reduced.

**Triangle Range**: Sets the maximum/minimum number of polygons after simplification.

  
 

### Streaming

This part is regarding how the HLOD mesh is loaded into the scene. The HLOD streaming system creates a controller component on top of the gameObject with the HLOD component. This controller is responsible for the actual HLOD mesh loading behavior. There are two modes available:

  
 

\- NotSupportStreaming  
\- AddressableStreaming

  
 

**Unsupported**

![](https://lh5.googleusercontent.com/n_3lAVx85LKPBF3nVTzrLHZd4C3PHFcx6QrAVjp8HlHlwnPJMN-1xPj1ddVXVo1CJg6mw--0xOsdYRXXm6N3HxX_jx95Fs1W9-UZtGP7UovtMaCe7kMJUHG8f4Rm05LbgEkLtpeta4P8bIX8NFi_bzae643CMdRxK6NVNV5qjHO4PIlkaLTbRvRgVHPs)

Under this mode, we do not support streaming. We add HLOD meshes at every detail level into the scene, and during gameplay we enable/disable meshes the HLOD system wants to show/hide.

  
 

**OutputDirectory**: Specify where the resource file will be created. The location must be under an Assets folder.

  
 

**Compress Format**: Specifies texture compression format  by the platform. Texture compression happens when resources are imported or when the platform changes.

**AddressableStreaming**

![](https://lh5.googleusercontent.com/ARINewFUB_1H3eI5yGSqcaY0x6XvH-kFUHAPtZ8z87rZuC4goHpwflhes4HFbocyMLpuTtfqNSVpBHUEjKcNEREiaEkfEEzloYruvTmE6_cYdeagBo4Us6HSzzT8DRFsTVqZot78JkuRtiLVQKM-et6cgkFmp9G6YQAIdDloWhaYG3fczd8z-TyGKTrc)

**Implement**

<table><tbody><tr><td>Streaming using the Addressable system. Using this mode requires user install a separate package with package ID "<i>com.unity.hlod.addressable</i>" which in turn has dependency on the Addressable system package.</td></tr></tbody></table>

  
 

  
Under this mode, we “appropriately” subdivide the HLOD quad-tree hierarchy into a number of separate addressable assets, each containing a group of quad-tree nodes. When a node in some group is needed, we load the addressable asset for that group into memory, then instantiate the needed node into the scene.  
  
 

**OutputDirectory**: Specify where the resource file will be created. The location must be under an Assets folder.

  
 

**Compress Format**: Specifies texture compression format by platform. Texture compression happens when texture resources are imported or when the platform changes.
