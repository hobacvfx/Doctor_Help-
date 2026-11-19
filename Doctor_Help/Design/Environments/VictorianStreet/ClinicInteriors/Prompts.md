# Nền nội thất phòng khám Victorian

Ảnh PNG 1536 × 1024, tạo bằng ImageGen tích hợp. Nền Doctor Help là cấp 2 và được dùng làm mẫu bố cục.

- Mẫu bố cục trong Unity `Assets/2D_Assets/House_Prop/NenPhongKhamTrong.png`.
- Mẫu mặt tiền là bản xáo trộn đính kèm `exec-291fa589-7488-4a48-868b-617a4bfa0f61.png`. Cấp và tên đối chiếu theo `../Prompt_Shuffled.md` và `../Prompt_Named.md`.

## Prompt tạo nền

Ghép prompt chung với đoạn riêng của từng phòng khám.

```text
Use case: precise-object-edit / stylized-concept. Create ONE revised 2D side-scroller interior background. IMAGE 1 is the actual editable base plate and the absolute geometry reference. IMAGE 2 is ONLY the exterior material/door reference for the particular storefront specified below; do not depict the street, facade, upper windows, or signage. Keep image 1's exact 1536 × 1024 3:2 framing and perpendicular orthographic camera. KEEP fixed: outer brick rectangular silhouette and its thin black margins, the horizontal mid-floor band at about y=490–519, the lower floor threshold band at y=936–968, two rooms on the left ending at x≈1225, the vacant dark wood-lined shaft bay on the far right at x≈1225–1486 on both levels, the vertical shaft posts and both room boundaries. No shift, reframe, zoom, symmetry change, lighting change to exterior bricks, floor angle, new openings or third floor. Keep the one doorway on the far left of the lower room, x≈49–159 and base y≈935; fill only that existing doorway with one full closed period entrance matching the selected facade, including any fanlight inside that same opening. Slightly arch its inner trim only if specified. The design changes are wallcovering and lower wall wood wainscoting in BOTH rooms, plus the one entrance door and its trim. Keep the same wainscot height and the floor surfaces, the pencil contours, selective hatch, two-to-three-tone cel shading and dim soft ambient shadows of the base. Two blank unbroken wall surfaces for future game props. No elevator car, gates, lift machinery, lights or lamps, furniture, curtains, picture frames, ads, signs, windows, objects, characters, annotations or text. Preserve the source image geometry, not just a similar layout. Output the full single landscape interior plate.
```

### L1_LanternSurgery

```text
Luxury level 1, Lantern Surgery, the SECOND storefront from left in Image 2, soot-black brick facade. Most worn and inexpensive. Wallpaper in faded ochre-grey and smoky olive, a simple sparse cheap stamped sprig pattern with faded ink, irregular tears exposing plaster in several small areas, avoid giant patches. Narrow vertical mismatched rough grey-brown pine wainscot with simple battens, scuffed surfaces and chipped plain rails, sparse rusty nails. Closed distressed grey plank door in the existing squared opening, antique rusted iron strap hinges, dark iron ring pull, no fanlight. Make this visibly poorer than base.
```

### L3_AshburyClinic

```text
Luxury level 3, Ashbury Clinic, the SIXTH and far-right storefront in Image 2, warm burgundy brick and respectable walnut door. Decent modest middle-class practice. Muted warm olive and ivory small damask wallpaper with little wear. Well-kept deep honey-brown walnut rectangular recessed panels, plainly shaped rails, simple bevels, one restrained carved line. Closed warm walnut door with brass ring and small amber semicircular fanlight within its existing opening. It must read as the neatest modest tier, below carved luxury.
```

### L4_BramwellClinic

```text
Luxury level 4, Bramwell Clinic, the FIRST and far-left storefront in Image 2, oxblood carved wood. Prosperous practice. Muted burgundy and dusty rose wallpaper with sparse ivory botanical motifs, slightly lighter and warmer than base. Deep warm oxblood-stained mahogany wainscot with regular recessed panels, shaped stiles and restrained repeating small carved details. Closed burgundy mahogany door with a rectangular amber transom, slim carved pilasters INSIDE existing frame width, and one subdued brass knob. Keep warm and professional, not heavily gilded.
```

### L5_IvyClinic

```text
Luxury level 5, Ivy Clinic, the THIRD storefront in Image 2, deep teal and triangular pediment. Rich practice with refined jewel-tone materials. Dusty peacock teal and muted gold botanical wallpaper with ample spacing. Fine dark walnut and deep teal wainscot, very thin antiqued brass fillets and crisp raised rectangular panels, small restrained corner scrolls. Closed dark walnut paneled door with narrow deep-teal stained glass inserts and small gold diamond leading, polished brass hardware. Coordinated teal, walnut and aged gold, legible and refined.
```

### L6_CrownClinic

```text
Luxury level 6, Crown Clinic, the FIFTH storefront in Image 2, black marble and ornate gold arched pediment. Top luxury, highest late-Victorian workmanship. Elegant warm parchment ivory and sage-gold silk damask wallpaper, noticeably brighter than base but under the same ambient shadows. Fine ebonized walnut raised rectangular wainscot with cream insets, precise small gold leaf accents and fine stepped molding, architectural scale. Closed ebonized wood door with ivory stone-edged trim, an amber arched fanlight within the opening, and bright brass ring pull. Finely drawn coordinated gold details, premium materials rather than oversized ornament. Keep room surfaces visibly blank for game props.
```

## Prompt hoàn thiện cấp 4–6

Áp dụng riêng cho ảnh đã tạo của Bramwell, Ivy và Crown để phục hồi giấy và gỗ.

```text
Make a localized restoration edit of this exact empty game background. Remove EVERY exposed plaster tear, ragged peel, brown gouge and damaged patch in the wallpaper on BOTH floors, including the corners and vertical tears above the wainscot. Fill each one with the same immediately surrounding wallpaper color and correctly continued original pattern. Repair any chipped or flaking patches in the wooden wainscot to match the neighboring intact wood finish, preserving its original panel profile and colors. Make it look well maintained, with the existing drawn texture, grain and ambient shading. Leave ALL remaining pixels and layout as close to the input as possible: exact brick perimeter and camera, two floor bands at identical heights, doors and their details, empty shaft on right with original posts, existing panel design, wallpaper color and motif, and no new objects, text, fixtures, gates or windows. Keep 1536 by 1024 full landscape framing.
```

- L4_BramwellClinic — muted oxblood red sprig wallpaper and red mahogany paneling
- L5_IvyClinic — deep peacock teal wallpaper with golden botanical vines, teal and walnut paneling
- L6_CrownClinic — ivory and gold silk damask wallpaper and ebonized cream-inset gilded paneling

