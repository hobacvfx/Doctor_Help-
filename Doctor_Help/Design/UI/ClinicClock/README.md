# Office clock presentation

- `SceneGameplayUI` no longer shows the examination, patient-arrival or patient-departure status strings. The flow steps and patient animation lifecycle are unchanged. Other existing treatment/error/waiting notices retain their behavior.
- `Assets/Resources/UI/ClinicClock.prefab` uses `Assets/Art/UI/Newspaper/Fonts/OldStandardTT-Bold.ttf`, the same font used by Headbar. Existing Office positioning and 26px font-size override are preserved.
- Its non-raycast `Period Icon` child uses the two embedded sprites in `Assets/Art/UI/ClinicClock/DayNightIcons.png`. It hides/shows with the clock object.
- `ClinicClockPeriodIcon.SetMinute` selects the icon from the game clock's MinuteOfDay. Inspector defaults: day begins 06:00, night begins 18:00. Both hours are editable. It does not change game time, day length, pause behavior or day completion.
- Icons generated with built-in image_gen; exact prompt in Generation.prompt.txt. Imported as two sprites with ASTC4x4 Android/iPhone, sRGB, transparent alpha, no mipmaps and max512.

Validation: actual Office Play Mode using a unique isolated tutorial-preview save directory, with daily-flow startup disabled during this focused presentation check. Arrival/examination/departure view methods all left the status label hidden. Icon cases 05:59, 06:00, 17:59, 18:00 and 21:00 passed. Real 08:00/sun and 20:00/moon UI screenshots are saved here; font reference and text fitting checked. Editor scene/play-start configuration was restored and temporary test data removed. No patient/day-completion simulation or device performance testing was part of this change.
