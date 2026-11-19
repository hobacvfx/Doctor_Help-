# Doctor Help experimental Android services

Unity 6000.3.9f1. Android package: `com.doctorhelp.game`. Portrait, ARM64.

## Reference project

Reviewed `F:\WaterSort_Git_2\WaterSort`: AdMobManager, ad ID selection, FirebaseBootstrap,
FirebaseAnalyticsTracker, Android Gradle templates and dependency declarations.
Reused vendor SDKs: Google Mobile Ads Unity 11.1.0, Firebase Analytics/Crashlytics Unity
13.11.0, External Dependency Manager 1.2.187. Vendor code, metadata and license files
remain intact. WaterSort game code, production IDs, Firebase config, signing keys,
save data and analytics queue were not copied.

## Test ads only

- Android Google sample App ID: `ca-app-pub-3940256099942544~3347511713`.
- Android Google sample interstitial: `ca-app-pub-3940256099942544/1033173712`.
- These constants apply to development AND release builds. There is no production toggle.
- AndroidTestBuildGuard rejects a non-sample App ID in SDK settings or Android manifests.
- Only Android devices request ads. Editor Play Mode deliberately skips ads.
- No banner, rewarded ad, app-open ad, mediation adapters or real ad units.
- Preload during gameplay. Attempt one interstitial after Continue dismisses the end-of-day
  correct/incorrect newspaper, before returning to Home Preparation. Wait for close/failure.
- Missing/offline/unready ads are skipped at that boundary, never shown later during play.
  Background loads retry with 5–60 second backoff. Expired ads are discarded.
- A day placement is consumed before showing. Retrying the result flow cannot replay it.
  Ads do not write saves, grant rewards or advance the day. Existing checkpoint rules remain.

Google sample units are not associated with the developer's AdMob account:
https://developers.google.com/admob/unity/test-ads
Interstitial lifecycle: https://developers.google.com/admob/unity/interstitial

## Firebase needs Doctor Help configuration

No `google-services.json` was supplied when integrating. Firebase collection defaults to
disabled in the Android manifest; the runtime service does not initialize Firebase without
its validated configuration. AdMob sample ads work independently of Firebase.

To connect later, register an Android app with package `com.doctorhelp.game` in the intended
Firebase project, download its own `google-services.json`, and place it at
`Assets/google-services.json`. Do not edit WaterSort's file to change its package name.
Resolve Android dependencies and rebuild. The build guard validates the package and creates
`Assets/Resources/DoctorHelpFirebaseConfiguration.json` for runtime identity checking.
After dependency and identity checks, Analytics and Crashlytics collection is enabled.
Editor does not collect telemetry. No player profile is sent to Firebase.

https://firebase.google.com/docs/unity/setup

## Importing the Unity package into another project

The `.unitypackage` contains Assets, including game scenes, prefabs and SDKs. Unity packages
do not carry the source project's Packages or ProjectSettings. Prefer using this configured
source project, or use the companion package manifest/settings snapshot supplied with the
export when reproducing the environment. Install Unity Android Build Support, SDK, NDK and
OpenJDK. Select Android, package `com.doctorhelp.game`, ARM64, Portrait, minimum API 25 or newer.
Add Home then Office to the scene list. Use URP 17.3.0 and the supplied package manifest.
External Dependency Manager must resolve the local Firebase Maven repository and Google
Android Maven dependencies. Keep custom main/settings/properties Gradle templates enabled.
No APK/AAB is implied by exporting a `.unitypackage`.

## Before any future production release

Android Lint's JAR parser in the bundled toolchain misinterprets `!` in a Windows
project path as an archive separator (for example `F:\Doctor_Help!`). The custom
main Gradle template stages local Java JAR dependencies into
`java.io.tmpdir/DoctorHelpAndroidJars/<project-path-hash>/<module>` only for affected
paths. Gradle syncs the original JAR contents; compile, lint and packaging use the
same copies. No lint detectors are disabled. This cache is intentional and is
recreated automatically if the operating system clears its temporary directory.

This package is an experimental test-only configuration. Production ad IDs, consent/UMP,
audience settings, privacy disclosures and release validation require a separate explicit
change. Do not copy WaterSort's child-audience declarations into this different game.
Actual ad display and Firebase ingestion require verification on an Android device.
