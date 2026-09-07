========================================================================
  Jampot - 3D Chests Pack
  5 game-ready models · separate hinged lids · baked PBR textures
  (FREE TASTER)
========================================================================

Thank you for downloading! Everything you need is in this folder.

------------------------------------------------------------------------
WHAT'S INSIDE  (5 chests)
------------------------------------------------------------------------
    - Iron (Standard)   [SM_Chest_Iron_M_01]
    - Plank (Standard)   [SM_Chest_Plank_M_01]
    - Silver (Standard)   [SM_Chest_Silver_M_01]
    - Mossy (Standard)   [SM_Chest_Mossy_M_01]
    - Supply Crate 01   [Box_01]

  Every chest is a separate model with:
    - a two-part build: Body + Lid (the lid is its own object)
    - the lid on a rear hinge pivot, ready to swing open in-engine
    - its own baked texture set (clean per-chest maps, no shared atlas)
    - real-world scale, upright, origin on the floor (Z = 0)

------------------------------------------------------------------------
WANT THE OTHER 55?
------------------------------------------------------------------------
  This is the free 5-chest taster. The complete 55-chest collection —
  legendary gold, pirate, cursed, frost, infernal, arcane and royal sets,
  plus animated-lid heroes — is the full "Jampot - 3D Chests Pack" on the
  same itch.io page.

------------------------------------------------------------------------
FOLDERS
------------------------------------------------------------------------
  GLB/        5 .glb - textures embedded, Y-up. Godot, Three.js,
              Babylon, Unreal (glTF), Sketchfab.
  FBX/        5 .fbx - Z-up, textures embedded. Unity, Max/Maya.
  Textures/   Raw PNG maps (20 files) if you want them separately.

------------------------------------------------------------------------
TECH SPECS
------------------------------------------------------------------------
  Parts per model   2 (Body + Lid)
  Triangles         ~2,100 - 11,308 per chest (median ~5,900), mid-poly
  Scale             Real-world, 1 unit = 1 metre (~0.75 m - 2.6 m tall)
  Orientation       GLB: Y-up   ·   FBX: Z-up
  Origins           Body = base centre on floor; Lid = on rear hinge axis
  Textures          Per part (4 maps/chest): Base Color 1024 sRGB +
                    Roughness 512 Non-Color. One PBR material per part.

------------------------------------------------------------------------
ENGINE NOTES
------------------------------------------------------------------------
  UNITY   Import the FBX (scale 1). Base Color -> Albedo, Roughness ->
          Smoothness (invert). Lid is a child transform - animate its
          localRotation to open.
  UNREAL  Import GLB or FBX; real-world scale; Base Color + Roughness.
  GODOT   Drag the .glb in - materials and the lid transform come through.
  WEB     GLB is self-contained (embedded textures) for Three.js / Sketchfab.

------------------------------------------------------------------------
KNOWN LIMITATIONS (honest notes)
------------------------------------------------------------------------
  - Interiors are not hollow-carved: an open lid reveals a solid top deck,
    not a deep cavity. Great for closed props and "pop open" moments.
  - Gem/lava glow is baked into Base Color as flat colour (no self-illum
    by default) - mask those areas into an emissive input to make them glow.

------------------------------------------------------------------------
LICENSE
------------------------------------------------------------------------
  Royalty-free commercial (see LICENSE.txt). Use in unlimited personal &
  commercial projects, no attribution required. Do NOT resell/redistribute
  the raw files. This free pack may be used commercially under the same terms.

  Jampot - 3D Chests Pack.  Enjoy - and go fill them with loot.
========================================================================
