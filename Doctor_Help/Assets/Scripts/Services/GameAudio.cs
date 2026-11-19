using UnityEngine;

namespace DoctorHelp
{
    internal sealed class GameAudio : MonoBehaviour
    {
        public static GameAudio Instance { get; private set; }
        public float MusicVolume { get; private set; }
        public float EffectsVolume { get; private set; }

        private AudioSource music;
        private string musicKey, effectsKey;
        private bool preferencesDirty;

        private void Awake()
        {
            string prefix = "DoctorHelp.Audio.";
#if UNITY_EDITOR
            if (TutorialPreviewSession.IsActive || UnityEditor.SessionState.GetBool("DoctorHelp.PlayerHomePreview", false))
                prefix += "Preview.";
#endif
            musicKey = prefix + "Music";
            effectsKey = prefix + "Effects";
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(musicKey, 1f));
            EffectsVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(effectsKey, 1f));

            music = gameObject.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            music.spatialBlend = 0;
            // The listener controls all scene audio. The music source has its own volume.
            music.ignoreListenerVolume = true;
            music.clip = Resources.Load<AudioClip>("Audio/ClinicMusic");
            music.volume = MusicVolume;
            AudioListener.volume = EffectsVolume;
        }

        private void OnEnable() => Instance = this;

        private void Start()
        {
            if (music.clip != null) music.Play();
        }

        public void SetMusicVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            music.volume = MusicVolume;
            PlayerPrefs.SetFloat(musicKey, MusicVolume);
            preferencesDirty = true;
        }

        public void SetEffectsVolume(float value)
        {
            EffectsVolume = Mathf.Clamp01(value);
            AudioListener.volume = EffectsVolume;
            PlayerPrefs.SetFloat(effectsKey, EffectsVolume);
            preferencesDirty = true;
        }

        public void PauseMusic(bool paused)
        {
            if (paused) music.Pause();
            else music.UnPause();
        }

        public void SavePreferences()
        {
            if (!preferencesDirty) return;
            PlayerPrefs.Save();
            preferencesDirty = false;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) SavePreferences();
        }

        private void OnDestroy()
        {
            SavePreferences();
            if (Instance != this) return;
            Instance = null;
            AudioListener.volume = 1f;
        }
    }
}
