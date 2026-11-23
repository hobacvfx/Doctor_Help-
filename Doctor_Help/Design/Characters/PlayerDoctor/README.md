# Player Doctor — Victorian costume

New modular art generated with the built-in imagegen tool. Sources: opening comic Page02 (doctor costume/hair identity), existing Sherlock outfit/hair (edit targets), the assembled Sherlock and NPC_0 reference renders, and Design/Art/STYLE_GUIDE.md.

- `Assets/2D_Assets/PlayerDoctor/Sprites/Outfit.png`: dark charcoal frock coat, burgundy waistcoat, ivory standing shirt collar, black cravat, antique stethoscope and a pocket thermometer. Same neutral arms-down silhouette family as the existing player.
- `Assets/2D_Assets/PlayerDoctor/Sprites/Hair.png`: chestnut hair with the detective hat removed, open face area and no duplicate ears/skin.
- Existing head, eyes, nose and mouth remain the identity reference and are reused in the assembled preview; no face regeneration is needed.
- Both PNGs have alpha, Sprite Single/FullRect, PPU100, no mipmaps, Android/iPhone ASTC6x6. Registration is evaluated with the actual player renderer transforms instead of assuming source pixel sizes equal world sizes.

The request is drawing/design: the runtime Player_avatar prefab and NPC prefabs have not been overwritten. `Player_Doctor_Preview.png` is the Unity assembly preview, with the current face reused. `Before.png` records the previous appearance. `Registration.txt` records local alignment for later integration. No gameplay/save state was tested or changed.
