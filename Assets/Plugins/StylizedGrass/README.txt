STYLIZED GRASS (URP)  -  setup
1. Drop the "StylizedGrass" folder into your Unity project's Assets/.
2. GameObject > Stylized Grass > Grass Field.
3. Set "Grow On Layers" to your ground layer(s). Ground needs COLLIDERS.
   Optional: "Block Layers" for paths/rocks/buildings (no grass/details on or under them).
4. Fit the green box (Scene view handles) around the area, press "Bake Everything".
5. Objects tagged Player / Enemy get interactors automatically at runtime
   (or add "Grass Interactor" manually).

!! v3 bake format: you MUST re-bake (old bakes show an error asking for it). 4 bytes per instance.

GRAPHICS SETTINGS (runtime, saved to PlayerPrefs)
  GrassGraphicsSettings.GrassEnabled / DetailsEnabled          bool
  GrassGraphicsSettings.GrassQuality / DetailsQuality          0.05 .. 1   (fraction of instances drawn)
  GrassGraphicsSettings.GrassDistance / DetailsDistance        0.1 .. 1.5  (x the distance you authored)
  GrassGraphicsSettings.ApplyPreset(GrassQualityPreset.Low)    Off / Low / Medium / High / Ultra
UI: add "Grass Settings Controller" to any object, then wire Toggle / Slider "On Value Changed" events
    to its SetGrassEnabled / SetGrassQuality / SetGrassDistance / SetDetails... methods (Dynamic parameter).
    Read current values from its properties to initialize your sliders.
Menu: Tools > Stylized Grass > Reset Saved Graphics Settings.

OPTIMIZATION NOTES
- Quality = quantity. Instances are shuffled at bake time, so quality / distance thinning is uniform and the CPU
  never even submits the hidden instances.
- Frustum + distance culling per chunk; off-screen chunks only draw (shadows-only) if they can cast into view.
- Far grass uses a 3-triangle tuft. Shader skips all math for culled instances. Layers with wind/flatten at 0 skip that math.
- Material properties are only re-applied when something changes. Call grassRenderer.MarkDirty() if you edit look fields from code.
- Costly extras are OFF by default: Cast Shadows, Extra Lights, Write Depth Texture.
