# Metal bedroom variant

Created with built-in imagegen on 2026-11-23, using the latest wooden bedroom composite and STYLE_GUIDE as references. Prompts for the full room, bed extraction, background plate and exterior alpha cleanup are in `Generation.prompt.txt`.

## Assets

- `Assets/2D_Assets/House_Prop/Bedroom_Metal/Bedroom_Metal_Background.png`: bed-free room with dusty violet-grey striped botanical wallpaper, slate wainscot, low riveted steel chest of drawers, oval iron-framed mirror and round brass wall clock.
- `Assets/2D_Assets/House_Prop/Bedroom_Metal/Bedroom_Metal_Bed.png`: separate wrought-iron bed, high ivory pillows, cream sheet and navy/petrol-blue blanket over the foot half.
- `Bedroom_Metal_Composite.png` in this Design folder: full-room design preview before extraction/alpha cleanup.

This is a separate room variant, not a replacement for the original wooden bedroom. The small one-storey footprint, brick perimeter, floor line and side-view camera match that room and the bathroom. New furnishings and the blue/violet palette distinguish it. Architectural floor/structural timbers remain compatible with the house; furnishing frames/cases are aged metal.

Both PNG sprites use a 1536×1024 canvas with matching bed registration, 100 PPU, centre pivot, FullRect mesh, no mipmaps and ASTC6×6 overrides for Android/iPhone. Use identical position and scale and a higher sorting order for the bed. The sprites have not been placed into a scene or given gameplay logic.

Actual alpha channels were inspected: all four background corners and sampled exterior margins are alpha zero after cleanup; the bed's surrounding areas and sampled open spaces are transparent. Antialiased low-alpha edge pixels may remain around fine ironwork. The generated RGB preview may show coloured haze in pixels that are transparent in the PNG. Room/bedding colours and complete outlines were visually reviewed. No player save or existing room asset was changed.
