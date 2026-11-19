# End-of-day newspaper

The approved visual direction is `approved-demo.png` (the first concept, aged parchment). `implemented-preview.png` is a real 1080×1920 Unity Play Mode render with isolated demonstration records; its displayed patient conditions are test examples, not edits to NpcProfile.

## Editing

- `Assets/Resources/UI/NewspaperResultsPopup.prefab`: headline, layout, fonts, paper, scroll region and nested Continue button. **Success Article Content** is shown only when the previous day's result is entirely correct (`DayTreatmentResult.IsSuccess`). **Article Content** retains the poisoning report for any incorrect case. Both paragraphs are editable; a future story system can set `SuccessArticleContent` and `ArticleContent` before presentation.
- `Assets/Resources/UI/NewspaperPatientRow.prefab`: portrait size, three text fields, spacing and correct/incorrect ink colors. Row height expands with wrapped text.
- `Assets/Resources/UI/NewspaperPortraits.asset`: source NPC profile and portraits keyed by stable NPC ID. Use **Capture / refresh NPC portraits** after editing saved NPC appearance or adding NPCs. Capture renders saved prefab sprites directly in an isolated Unity preview scene, then converts the image to warm monochrome; it does not change the prefab or shared materials.
- `Assets/Art/UI/Newspaper`: parchment and button art, generated with the built-in imagegen tool from the approved first concept. Old Standard TT fonts are from Google Fonts, with OFL license included beside the font files.

The popup is a saved nested prefab inside Home's ResultsPopup, above the scene's black cover. ResultsVideoPresenter awaits it after the final `new day has started.` caption has faded out. Continue completes presentation, then the existing state machine reveals Preparation. It cannot start the next day while the newspaper is open.

TreatmentEvaluator creates immutable patient summaries alongside the existing day verdict before the checkpoint. DayCompletionService retains them after journal cleanup and for presentation retries. Required medicines supplied are green; missing medicines and incorrect police decisions are red; extra medicine uses normal brown ink. Multiple illnesses require their combined recipes; Vampire/Zombie retain police priority and the reported infection type is shown. Missing portrait configuration produces an explicit retryable error.

No additional save, inventory transaction, monetary result or patient mortality rule is introduced by the popup.

## Verification

57 checks passed in Unity 6000.3.9f1 Play Mode: mixed correct/incorrect medicine and police summaries, extra and missing medicines, actual paper/portrait sprites, text bounds, Continue raycast reachability, list scrolling, reopen/reset, double confirmation, cancellation, and two consecutive native cutscene → newspaper → Preparation cycles. Those cycles used a separate profile session and checkpoint directory; the real player save was confirmed unchanged. Actual top/bottom and daily report renders were inspected. Temporary setup/validation scripts and test saves were removed afterward. Android device/touch testing was not performed.

The conditional hero article was additionally checked in Play Mode with success → one incorrect patient → success results. Correct copy selection, wrapping without clipping, and repeated Continue were verified, without profile mutations or checkpoints.

Home's pre-edit disk scene, live unsaved-scene snapshot and ResultsPopup backup are retained in `Logs/NewspaperBackup`. The user's pre-existing scene changes were saved and retained.

## Art generation prompts

Built-in imagegen was used (not the CLI). Reference: approved-demo.png.

Paper prompt: Create a reusable blank UI background asset based EXACTLY on this first newspaper design. Preserve its first-version aged realistic parchment texture, warm light sepia color, thin worn edges and Victorian engraved London skyline with heraldic emblem at the very top. REMOVE ALL text, ALL portraits, ALL list boxes/dividers/scrollbar, ALL buttons and ALL interior ornaments beneath the skyline. The entire sheet beneath the small top skyline must be uninterrupted blank light parchment so live game UI text can be overlaid. Skyline takes only the top 13 percent of sheet height. Flat front view, portrait 9:16 sheet, fills canvas almost to edges with very slim margins, same overall silhouette as reference. Transparent background outside the newspaper sheet. No black background, no letters, no watermark, no shadows outside paper. This is a single clean background sprite for a working UI.

Button prompt: Extract and recreate only the antique parchment CONTINUE button frame near the bottom of this first newspaper UI as a standalone reusable game UI sprite. Remove ALL letters, text, arrow, and all surrounding newspaper and ornaments. A single wide horizontal empty button with warm faded tan parchment inside and three thin irregular dark brown Victorian engraved outlines, clipped decorative stepped corners matching the reference button. Aspect ratio approximately 5:1. Front view flat. The button fills the output width with slim transparent margins; transparent background outside this one button. No other elements. The blank interior is for live text to be overlaid in Unity.
