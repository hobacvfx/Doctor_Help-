using System;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
#endif

namespace DoctorHelp
{
    // No WaterSort configuration or persistent analytics queue is reused.
    public sealed class DoctorHelpFirebaseService : MonoBehaviour
    {
        private async void Start()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var configuration = Resources.Load<TextAsset>("DoctorHelpFirebaseConfiguration");
            if (configuration == null)
            {
                Debug.Log("[Firebase] Disabled: supply Doctor Help's own google-services.json and rebuild.");
                return;
            }
            try
            {
                var config = FirebaseAndroidConfiguration.Parse(configuration.text);
                var client = config.FindClient(Application.identifier);
                if (client == null) throw new InvalidOperationException("Firebase package does not match this application.");
                var dependencies = await FirebaseApp.CheckAndFixDependenciesAsync();
                if (this == null || destroyCancellationToken.IsCancellationRequested) return;
                if (dependencies != DependencyStatus.Available)
                    throw new InvalidOperationException("Firebase dependencies: " + dependencies);
                var app = FirebaseApp.DefaultInstance;
                if (app.Options.ProjectId != config.project_info.project_id || app.Options.AppId != client.client_info.mobilesdk_app_id)
                    throw new InvalidOperationException("Native Firebase configuration does not match Doctor Help configuration.");
                FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                Crashlytics.IsCrashlyticsCollectionEnabled = true;
                Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                Debug.Log("[Firebase] Analytics and Crashlytics ready for Doctor Help.");
            }
            catch (Exception error) { Debug.LogWarning("[Firebase] Unavailable: " + error.Message); }
#else
            await Task.CompletedTask;
#endif
        }
    }

    [Serializable]
    public sealed class FirebaseAndroidConfiguration
    {
        public ProjectInfo project_info;
        public Client[] client;
        [Serializable] public sealed class ProjectInfo { public string project_id; public string project_number; }
        [Serializable] public sealed class Client { public ClientInfo client_info; }
        [Serializable] public sealed class ClientInfo { public string mobilesdk_app_id; public AndroidInfo android_client_info; }
        [Serializable] public sealed class AndroidInfo { public string package_name; }

        public static FirebaseAndroidConfiguration Parse(string json)
        {
            var value = JsonUtility.FromJson<FirebaseAndroidConfiguration>(json);
            if (value?.project_info == null || string.IsNullOrWhiteSpace(value.project_info.project_id) || value.client == null)
                throw new InvalidOperationException("Invalid google-services.json.");
            return value;
        }
        public Client FindClient(string packageName)
        {
            if (client == null) return null;
            foreach (var item in client)
                if (item?.client_info?.android_client_info?.package_name == packageName &&
                    !string.IsNullOrWhiteSpace(item.client_info.mobilesdk_app_id)) return item;
            return null;
        }
    }
}
