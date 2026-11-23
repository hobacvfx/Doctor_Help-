# Three new basement defense plants — concept only

User confirmed three plants first (initial mention of four was superseded). This pass makes concept art only. Do not infer implementation, prefabs, animations or profiles from the drawings.

## Shared art

Same old terracotta soil pots and pencilled/inked spooky Victorian 2D rendering family as RatEater_1. Hero designs plus small attack/reload studies. The ice sunflower is the premium plant and should have an especially beautiful, striking silhouette.

### Rendering revision requested after v1

**Latest direction after reviewing v2:** v2 was also rejected, as too cartoonish/clean. Follow AGENTS.md and Design/Art/STYLE_GUIDE.md plus the actual approved Medicine_Collection and VictorianRedraw references. Retain variable black pencil contours, plentiful fine offset sketch strokes, selective grouped hatching, muted distinct colors and 2–3 clear cel-shaded planes. Spine-friendly means simple part connections and stable perspective; it does not mean uniform thick outlines or removing the project's hand-drawn material character. Earlier simplification instructions below describe the superseded v1→v2 attempt, not the current target. Version 3 is generated from approved project references rather than either rejected plant concept.

Version 1 was rejected as too realistic and volumetric. The revised design must use broad flat color areas, a base plus one hard-edged shadow per part, strong irregular ink contours, and very little interior texture. Avoid dense engraving, realistic mushroom gills, detailed ice filigree and sculptural lighting. Premium beauty comes from silhouette, proportions and color.

Plan for Spine Essential bone/region animation: static pot; cactus central body and independently attached arms/needles; mushroom cap and simple stalk; sunflower stalk, two separate leaves, circular front-facing seed disk and individually removable/reusable petals. Keep clean overlaps at joints. Sunflower spins in the picture plane with no changing perspective. This revision is still assembled concept art, not an actual modular export or working rig.

## Range requirement for a later coding task

Add Range as a displayed stat with three ranks:
- Melee: close contact, e.g. RatEater.
- Normal: ranged, default around 5 metres; expose actual distance for editing.
- Far: can target across the whole map.

No Range code or UI changes are made in this concept pass.

## Cactus

- Shoots needles towards targets; compresses then forcefully expands for its attack.
- Damage 2; Targets 3; Attack Cooldown 3 seconds; Range Normal.

## Glowing mushroom

- Latest visual request: visible cyan glow emitted from below the cap/gills, with localized radiance and some reflected light on upper stalk.

- Emits an electric stream towards one nearest target. Future implementation requested with LineRenderer.
- Damage 10; Targets 1; Attack Cooldown 6 seconds; Range Normal.

## Ice sunflower

- Latest visual request: richer prettier blue colors; leaves resemble polished, faceted diamonds with glossy highlights and sparkle. Preserve modular leaf silhouettes and circular front-facing head for Spine. These explicitly requested effects supersede earlier no-gloss/no-glow guidance for the specified plant parts only.

- Single target; Damage 0.5; Attack Cooldown 0.2 seconds; Range Far.
- Flower spins like an organic gatling gun; each petal is a projectile.
- After petals are depleted, reload animation regrows petals before firing again.
- Projectiles slow by 30% for 3 seconds; slow does not stack.
- Concept petal count is illustrative. Magazine petal count and reload duration are not finalized by this art pass.

Names on the English concept board are working design labels, not stable profile IDs. No new plants or attacks are implemented. The specified stats above are user requirements; unspecified prices, upgrades, projectile speeds and balance remain unassigned.
