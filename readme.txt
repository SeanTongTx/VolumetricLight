*******************
** Documentation **
*******************
Please visit: http://saladgamer.com/vlb-doc/

*******************
** Sample Scenes **
*******************
- demoScene: showcase multiple features
- demoStressTest: features 400 dynamic Volumetric Spotlights


*******************
** Configuration **
*******************
In your project file, look for a file named Config.asset under the folder Plugins/VolumetricLightBeam/Resources. In the inspector, you can configure the following properties:
- Beam Geometry:
  - Layer: controls on which layer the beam geometry meshes will be created in.
  - Shader: Main shader applied to the cone beam geometry
- Global 3D Noise:
  - Scale: Global 3D Noise texture scaling. Higher scale make the noise more visible, but potentially less realistic.
  - Velocity: Global World Space direction and speed of the noise scrolling, simulating the fog/smoke movement.
- 3D Noise Texture Data:
  - Binary file: Binary file holding the 3D Noise texture data (a 3D array). Must be exactly Size * Size * Size bytes long.
  - Data dimension: Size (of one dimension) of the 3D Noise data. Must be power of 2. So if the binary file holds a 32x32x32 texture, this value must be 32.
- Volumetric Dust Particles:
  - Prefab: ParticleSystem prefab instantiated for the Volumetric Dust Particles feature (Unity 5.5 or above)


****************************************
** Volumetric Light Beam - Properties **
****************************************
Basic:
- Spot Angle: Define the angle (in degrees) at the base of the beam's cone. [if attached to a Spotlight, check the toggle to get this value from it]
- Color: Use the color picker to set the color of the beam (takes account of the alpha value). [if attached to a Spotlight, check the toggle to get this value from it]
- Side Thickness: Thickness of the beam when looking at it from the side. 1 = the beam is fully visible (no difference between the center and the edges), but produces hard edges. Lower values produce softer transition at beam edges. If you set the lowest possible value and want to make the beam even thinner, just lower the 'Spot Angle' and/or the 'Truncated Radius' properties.
- Track Changes During Playtime: If true, the light beam will keep track of the changes of its own properties and the spotlight attached to it (if any) during playtime. This would allow you to modify the light beam in realtime from Script, Animator and/or Timeline. Enabling this feature is at very minor performance cost. So keep it disabled if you don't plan to modify this light beam during playtime.

Inside:
- Alpha: Modulate the opacity of the inside geometry of the beam. Is multiplied to Color's alpha.
- Glare Frontal: Boost intensity factor when looking at the beam from the inside directly at the source.

Outside:
- Alpha: Modulate the opacity of the outside geometry of the beam. Is multiplied to Color's alpha.
- Glare Behind: Boost intensity factor when looking at the beam from behind.

Attenuation:
- Equation: Attenuation equation used to compute fading between 'Fade Start Distance' and 'Range Distance'.
  * Linear: Simple linear attenuation
  * Quadratic: Quadratic attenuation, which usually gives more realistic results
  * Blend: Custom blending mix between linear and quadratic attenuation (can be customize using a slider)
- Range Distance: Distance from the light source (in units) the beam is entirely faded out (alpha = 0, no more cone mesh). [if attached to a Spotlight, check the toggle to get this value from it]
- Fade Start Distance: Distance from the light source (in units) the beam intensity will start to fall off.

3D Noise:
- Enabled: Enable 3D Noise effect.
- Intensity: Higher intensity means the noise contribution is stronger and more visible.
- Scale: 3D Noise texture scaling. Higher scale make the noise more visible, but potentially less realistic. [if the toggle 'Use Global' is checked, it will use the Scale property set in Config.asset]
- Velocity: World Space direction and speed of the noise scrolling, simulating the fog/smoke movement. [if the toggle 'Use Global' is checked, it will use the Velocity property set in Config.asset]

Soft Intersections Blending Distances:
- Camera: Distance from the camera the beam will fade. 0 = hard intersection. Higher values produce soft intersection when the camera is near the cone triangles.
- Opaque Geometry: Distance from the world geometry the beam will fade. 0 = hard intersection Higher values produce soft intersection when the beam intersects other opaque geometry.

Cone Geometry:
- Truncated Radius: Radius (in units) at the beam's source (the top of the cone). 0 will generate perfect cone geometry. Higher values will generate truncated cones.
- Sides: Number of Sides of the cone. Higher values give better looking results, but require more memory and graphic performance.
- Cap Geom: Show the cap of the cone or not (only visible from the inside).


********************************************
** Volumetric Dust Particles - Properties **
********************************************
Rendering:
- Alpha: Max alpha of the particles
- Size: Max size of the particles

Direction & Velocity:
- Direction: Direction of the particles (Beam: particles follows the cone/beam direction ; Random: random direction)
- Speed: Movement speed of the particles

Culling:
- Enabled: Enable particles culling based on the distance to the Main Camera. We highly recommend to enable this feature to keep good runtime performances.
- Max Distance: The particles will not be rendered if they are further than this distance to the Main Camera.

Spawning:
- Density: Control how many particles are spawned. The higher the density, the more particles are spawned, the higher the performance cost is.
- Max Distance: The maximum distance (from the light source) where the particles are spawned. The lower it is, the more the particles are gathered near the light source.


********************************************
** Dynamic Occlusion - Properties **
********************************************
Raycasting:
- Layer Mask: On which layers the beam will perform raycasts to check for colliders.
- Min Occluder Area: Minimum 'area' of the collider to become an occluder. Colliders smaller than this value will not block the beam.
- Wait frame count: How many frames the system will wait before performing the next occlusion tests (raycast)?

Clipping Plane:
- Alignment: Alignment of the computed clipping plane
    * Surface: align to the surface normal which blocks the beam. Works better for large occluders such as floors and walls.
    * Beam: keep the plane aligned with the beam direction. Works better with more complex occluders or with corners.
- Offset Units: Apply a translation to the plane. We recommend to set a small positive offset in order to handle non-flat and complex surface better (as long as your occluders are not super thin walls).

Editor Debug:
- Show Debug Plane: Draw debug information on the scene view to show the virtual clipping plane.
- Update in Editor: Perform occlusion tests and raycasts in Editor.


********************************************
** Soft intersection with opaque geometry **
********************************************
To support the "Soft intersection with opaque geometry" feature, your camera must use "DepthTextureMode.Depth". By default, the rendering camera will be forced to the proper DepthTextureMode value, just before rendering a LightBeam.
If you are sure that you camera is using the "DepthTextureMode.Depth" mode, you can disable this behavior for minor performance gain. To do so, comment the 1st line in BeamGeometry.cs:
// #define FORCE_CURRENT_CAMERA_DEPTH_TEXTURE_MODE


***********************
** Platform Specific **
***********************
- The Volumetric Light Beam shader is a 2 pass shader using a Shader Model as low as 3.0
- 3D Noise feature requires shader capabilities equal or higher than Shader Model 3.5 / OpenGL ES 3.0. Any mobile devices released after 2012 should support it.
- 'Volumetric Dust Particles' feature is only supported on Unity 5.5 or above.
