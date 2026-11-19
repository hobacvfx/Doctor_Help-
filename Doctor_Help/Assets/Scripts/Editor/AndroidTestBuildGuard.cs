using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DoctorHelp.Editor
{
    // Keep this guard while the project is experimental. It applies to release builds too.
    public sealed class AndroidTestBuildGuard : IPreprocessBuildWithReport
    {
        public int callbackOrder => -1000;
        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.Android)
                throw new BuildFailedException("Doctor Help experimental integration supports Android builds only.");
            ValidateTestAds();
            ValidateAndroidAdsBridge();
            PrepareFirebaseConfiguration();
        }

        // The C# SDK and play-services-ads are not sufficient: JNI calls require
        // this Unity-specific Java bridge in the Android player as well.
        internal static void ValidateAndroidAdsBridge()
        {
            const string path = "Assets/Plugins/Android/googlemobileads-unity.aar";
            if (!File.Exists(path))
                throw new BuildFailedException("Missing Google Mobile Ads Android bridge: " + path + ". Restore it from the matching Unity SDK package.");
            var importer = AssetImporter.GetAtPath(path) as PluginImporter;
            if (importer == null || !importer.GetCompatibleWithPlatform(BuildTarget.Android))
                throw new BuildFailedException("Google Mobile Ads Android bridge is not enabled for Android: " + path);
            try
            {
                using (var archive = ZipFile.OpenRead(path))
                {
                    var classes = archive.GetEntry("classes.jar");
                    if (classes == null) throw new InvalidDataException("classes.jar missing");
                    using (var stream = classes.Open())
                    using (var buffer = new MemoryStream())
                    {
                        stream.CopyTo(buffer); buffer.Position = 0;
                        using (var jar = new ZipArchive(buffer, ZipArchiveMode.Read))
                            foreach (string name in new[] { "UnityMobileAds", "Interstitial" })
                                if (jar.GetEntry("com/google/unity/ads/" + name + ".class") == null)
                                    throw new InvalidDataException("Missing Java bridge class: " + name);
                    }
                }
            }
            catch (Exception error)
            { throw new BuildFailedException("Invalid Google Mobile Ads Android bridge: " + error.Message); }
        }

        internal static void ValidateTestAds()
        {
            if (PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) != "com.doctorhelp.game")
                throw new BuildFailedException("Expected Android package com.doctorhelp.game.");
            var settings = AssetDatabase.LoadMainAssetAtPath("Assets/GoogleMobileAds/Resources/GoogleMobileAdsSettings.asset");
            if (settings == null) throw new BuildFailedException("Google Mobile Ads settings are missing.");
            var serialized = new SerializedObject(settings);
            if (serialized.FindProperty("adMobAndroidAppId").stringValue != DayEndInterstitialService.AndroidSampleAppId)
                throw new BuildFailedException("Experimental builds require Google's sample AdMob app ID. Production IDs are blocked.");
            foreach (string file in Directory.GetFiles("Assets/Plugins/Android", "AndroidManifest.xml", SearchOption.AllDirectories))
            {
                var xml = new System.Xml.XmlDocument(); xml.Load(file);
                var ns = new System.Xml.XmlNamespaceManager(xml.NameTable);
                ns.AddNamespace("android", "http://schemas.android.com/apk/res/android");
                var nodes = xml.SelectNodes("//meta-data[@android:name='com.google.android.gms.ads.APPLICATION_ID']", ns);
                foreach (System.Xml.XmlNode node in nodes)
                    if (node.Attributes["value", "http://schemas.android.com/apk/res/android"]?.Value != DayEndInterstitialService.AndroidSampleAppId)
                        throw new BuildFailedException("Production AdMob app ID blocked in " + file);
            }
        }

        internal static void PrepareFirebaseConfiguration()
        {
            const string destination = "Assets/Resources/DoctorHelpFirebaseConfiguration.json";
            var files = Directory.GetFiles("Assets", "google-services.json", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                if (File.Exists(destination)) AssetDatabase.DeleteAsset(destination);
                Debug.Log("[Firebase] No Doctor Help config supplied. Firebase collection stays disabled.");
                return;
            }
            if (files.Length != 1) throw new BuildFailedException("Keep exactly one Doctor Help google-services.json under Assets.");
            var json = File.ReadAllText(files[0]);
            var configuration = FirebaseAndroidConfiguration.Parse(json);
            if (configuration.FindClient("com.doctorhelp.game") == null)
                throw new BuildFailedException("google-services.json must contain com.doctorhelp.game. Do not reuse WaterSort's Android app.");
            Directory.CreateDirectory("Assets/Resources");
            if (!File.Exists(destination) || File.ReadAllText(destination) != json) File.WriteAllText(destination, json);
            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
