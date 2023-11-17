using System;
using JetBrains.Annotations;
//using Argus.UI;
//using Argus.Utility;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using VRC.Udon.Serialization.OdinSerializer;
using VRRefAssist;
using Random = UnityEngine.Random;

namespace Argus.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)] [Singleton]
    public class AudioManager : UdonSharpBehaviour
    {
        #region SINGLETONS

        //[SerializeField] private Debugger debugger;

        #endregion

        [Header("Volumes")]
        [SerializeField] [Range(0, 1)] public float sfxVolume = 1f;

        [SerializeField] [Range(0, 1)] public float musicVolume = 1f;
        [SerializeField] [Range(0, 1)] public float ambientVolume = 1f;

        [Header("Audio Channels")]
        [SerializeField] private AudioSource sfxAudioSource2D;

        [SerializeField] private AudioSource musicAudioSource2D;
        [SerializeField] private AudioSource ambientAudioSource2D;

        [Header("Audio Pool References")]
        [SerializeField] public int poolSize;

        [SerializeField] public AudioSource[] audioSources;
        [SerializeField] public Transform[] audioSourceTransforms;

        [Header("UI Sounds")]
        [SerializeField] private AudioClip mainMenuOpen;

        [SerializeField] private AudioClip mainMenuClose;
        [SerializeField] public AudioClip uiSelect;
        [SerializeField] public AudioClip uiUnselect;

        [Header("Combat Sounds")]
        [SerializeField] private AudioClip[] weaponHitSounds;

        private VRCPlayerApi localPlayer;
        private bool initialized;

        #region VOLUME PROPERTIES

        public float SfxVolume
        {
            set
            {
                sfxVolume = Mathf.Clamp01(value);
                sfxAudioSource2D.volume = sfxVolume;
                RefreshLerpType(SoundType.SFX);
            }

            get => sfxVolume;
        }

        public float MusicVolume
        {
            set
            {
                musicVolume = Mathf.Clamp01(value);
                musicAudioSource2D.volume = musicVolume;
                RefreshLerpType(SoundType.Music);
                NotifyMusicSubscribers();
            }

            get => musicVolume;
        }

        public float AmbientVolume
        {
            set
            {
                ambientVolume = Mathf.Clamp01(value);
                RefreshLerpType(SoundType.Ambient);
                ambientAudioSource2D.volume = ambientVolume;
            }

            get => ambientVolume;
        }

        #endregion

        [NonSerialized] public UdonSharpBehaviour[] musicSubscribedBehaviours;
        [NonSerialized] public string[] musicEventNames;
        private int musicSubscriptionCount = 0;

        public void _SubscribeMusicVolume(UdonSharpBehaviour udon, string eventName)
        {
            if (!initialized) Initialize();

            musicSubscribedBehaviours[musicSubscriptionCount] = udon;
            musicEventNames[musicSubscriptionCount] = eventName;

            musicSubscriptionCount++;
        }

        private void NotifyMusicSubscribers()
        {
            for (int i = 0; i < musicSubscriptionCount; i++)
            {
                musicSubscribedBehaviours[i].SendCustomEvent(musicEventNames[i]);
            }
        }


        void Start()
        {
            if (!initialized) Initialize();
        }

        private void Initialize()
        {
            localPlayer = Networking.LocalPlayer;

            musicSubscribedBehaviours = new UdonSharpBehaviour[10];
            musicEventNames = new string[10];

            initialized = true;
        }

        public void _PlayWeaponHit(Vector3 point, float volume)
        {
            _PlayClipAtPoint(_GetRandomWeaponHitSound(), point, SoundType.SFX, volume, 1, 10);
        }

        public AudioClip _GetRandomWeaponHitSound()
        {
            return weaponHitSounds[Random.Range(0, weaponHitSounds.Length)];
        }

        public void _Play2DSound(AudioClip clip, float volume, SoundType soundType)
        {
            GetAudioSource(soundType).PlayOneShot(clip, volume);
        }

        private AudioSource GetAudioSource(SoundType soundType)
        {
            switch (soundType)
            {
                default:
                case SoundType.SFX:
                    return sfxAudioSource2D;
                case SoundType.Music:
                    return musicAudioSource2D;
                case SoundType.Ambient:
                    return ambientAudioSource2D;
            }
        }

        #region UI SOUNDS

        public void _PlayMainMenuOpen()
        {
            _Play2DSound(mainMenuOpen, 0.8f, SoundType.SFX);
        }

        public void _PlayMainMenuClose()
        {
            _Play2DSound(mainMenuClose, 0.8f, SoundType.SFX);
        }

        public void _PlayUISelect()
        {
            _Play2DSound(uiSelect, 0.15f, SoundType.SFX);
        }

        public void _PlayUIUnselect()
        {
            _Play2DSound(uiUnselect, 0.15f, SoundType.SFX);
        }

        #endregion

        /*
        #region PUBLIC VOLUME METHODS
        [HideInInspector, SerializeField] private NotificationHandler notificationHandler;

        private float adjustmentAmount = 0.1f;

        private Notification _volumeNotification;

        [PublicAPI]
        public void _IncreaseMusicVolume()
        {
            MusicVolume += adjustmentAmount;
            SetVolumeNotification("Music Volume", $"has been set to {MusicVolume:P0}");
        }

        [PublicAPI]
        public void _DecreaseMusicVolume()
        {
            MusicVolume -= adjustmentAmount;
            SetVolumeNotification("Music Volume", $"has been set to {MusicVolume:P0}");
        }

        [PublicAPI]
        public void _IncreaseSfxVolume()
        {
            SfxVolume += adjustmentAmount;
            SetVolumeNotification("SFX Volume", $"has been set to {SfxVolume:P0}");
        }

        [PublicAPI]
        public void _DecreaseSfxVolume()
        {
            SfxVolume -= adjustmentAmount;
            SetVolumeNotification("SFX Volume", $"has been set to {SfxVolume:P0}");
        }

        [PublicAPI]
        public void _IncreaseAmbientVolume()
        {
            AmbientVolume += adjustmentAmount;
            SetVolumeNotification("Ambience Volume", $"has been set to {AmbientVolume:P0}");
        }

        [PublicAPI]
        public void _DecreaseAmbientVolume()
        {
            AmbientVolume -= adjustmentAmount;
            SetVolumeNotification("Ambience Volume", $"has been set to {AmbientVolume:P0}");
        }

        public void SetVolumeNotification(string header, string body)
        {
            if (!_volumeNotification)
            {
                _volumeNotification = notificationHandler._SetupNotification(header, body, 0.85f);
                return;
            }

            _volumeNotification.SetText(header, body);
        }

        #endregion
        */

        #region FOOTSTEP SFX

        [Header("Footstep Sounds")]
        [SerializeField] public int[] footstepSFXIndices;
        [SerializeField] public Texture2D[] footstepSFXTextures;
        [SerializeField] public string[] footstepSFXNames;
        
        [OdinSerialize] public AudioClip[][] footstepSFXs;
        [OdinSerialize] public AudioClip[][] landSFXs;

        [SerializeField] public int defaultFootstepSFXIndex;

        [SerializeField] private LayerMask raycastLayers;

        private const float stepVolume = 1f;
        private const float landVolume = 1f;

        public int GetTextureIndexAtPosition(Vector3 position, out bool grounded)
        {
            grounded = true;
            if (!Physics.Raycast(position + new Vector3(0, 0.05f, 0), Vector3.down, out var hit, 0.15f, raycastLayers, QueryTriggerInteraction.Ignore))
            {
                grounded = false;
                return -1;
            }
            /*
            TerrainTextureDetector terrainTexture = hit.collider.GetComponent<TerrainTextureDetector>();

            if (terrainTexture != null)
            {
                return terrainTexture.GetDominantTextureIndexAt(position);
            }
            */

            Renderer rend = hit.collider.GetComponent<Renderer>();
            if (rend != null)
            {
                Texture2D texture = (Texture2D) rend.materials[0].mainTexture;
                return Array.IndexOf(footstepSFXTextures, texture);
            }

            return defaultFootstepSFXIndex;
        }

        public void PlayFootstepSFX(Vector3 point, float volume)
        {
            Log("Playing Footstep sound...");

            int textureIndex = GetTextureIndexAtPosition(point, out bool grounded);

            if (!grounded)
            {
                return;
            }
            
            if (textureIndex < 0 || textureIndex >= footstepSFXIndices.Length) textureIndex = defaultFootstepSFXIndex;

            _PlayClipAtPoint(_GetRandomFootstepSFX(textureIndex), point, SoundType.SFX, volume * stepVolume, 1, 20);
        }

        private AudioClip _GetRandomFootstepSFX(int index)
        {
            int clipSetIndex = footstepSFXIndices[index];
            AudioClip clip = footstepSFXs[clipSetIndex][Random.Range(0, footstepSFXs[clipSetIndex].Length)];
            return clip;
        }

        public void PlayLandSFX(Vector3 point, float volume = 1f)
        {


            int texIndex = GetTextureIndexAtPosition(point, out bool grounded);
            
            if (!grounded)
            {
                return;
            }
            
            if (texIndex < 0 || texIndex >= footstepSFXIndices.Length) texIndex = defaultFootstepSFXIndex;

            _PlayClipAtPoint(_GetRandomLandSFX(texIndex), point, SoundType.SFX, volume * landVolume, 1, 30);
        }

        private AudioClip _GetRandomLandSFX(int index)
        {
            int clipSetIndex = footstepSFXIndices[index];
            return landSFXs[clipSetIndex][Random.Range(0, landSFXs[clipSetIndex].Length)];
        }

        #endregion

        public void _PlayClipAtPoint(AudioClip clip, Vector3 point, SoundType soundType, float volume = 1, float minDistance = 1f, float maxDistance = 50f)
        {
            if (!initialized) Initialize();

            if (Vector3.Distance(localPlayer.GetPosition(), point) > maxDistance) return;

            int index = GetNextAvailableIndex();
            if (index == -1) return;

            audioSourceTransforms[index].position = point;
            audioSources[index].clip = clip;
            audioSources[index].volume = _GetVolume(soundType) * volume;
            audioSources[index].minDistance = minDistance;
            audioSources[index].maxDistance = maxDistance;
            audioSources[index].Play();
        }

        public float _GetVolume(SoundType soundType)
        {
            switch (soundType)
            {
                case SoundType.SFX:
                    return sfxVolume;
                case SoundType.Music:
                    return musicVolume;
                case SoundType.Ambient:
                    return ambientVolume;
            }

            return 1f;
        }

        #region AUDIOSOURCE FADE

        public AudioSource[] fadingSources = new AudioSource[0];
        public float[] startVolumes = new float[0];
        public float[] targetVolumes = new float[0];
        public float[] fadeDurations = new float[0];
        public float[] fadeTimers = new float[0];
        public SoundType[] soundTypes = new SoundType[0];
        public bool[] lerp = new bool[0];

        bool fading = false;

        const float volumeTolerance = 0.01f;
        
        public void _FadeAudioSource(AudioSource source, SoundType soundType, float targetVolume, float fadeDuration = 1f, float startVolume = -1f)
        {
            if(startVolume == -1f) startVolume = source.volume;
            
            int index = Array.IndexOf(fadingSources, source);

            if (index == -1)
            {
                fadingSources = fadingSources.Add(source);
                source.enabled = true;
                source.Play();

                startVolumes = startVolumes.Add(startVolume);
                targetVolumes = targetVolumes.Add(targetVolume);
                fadeDurations = fadeDurations.Add(fadeDuration);
                fadeTimers = fadeTimers.Add(0f);
                soundTypes = soundTypes.Add(soundType);
                lerp = lerp.Add(true);
                
                Log("Added new audio source to fade: " + source.name);
                
            } else {
                startVolumes[index] = startVolume;
                targetVolumes[index] = targetVolume;
                fadeDurations[index] = fadeDuration;
                fadeTimers[index] = 0f;
                soundTypes[index] = soundType;
                lerp[index] = Math.Abs(source.volume - targetVolume) > volumeTolerance;
            }

            if (!fading)
            {
                fading = true;
                _UpdateFadeAudioSources();   
            }
        }

        private void RefreshLerpType(SoundType type)
        {
            for (int i = 0; i < fadingSources.Length; i++)
            {
                if((int)soundTypes[i] != (int)type) continue;
                
                lerp[i] = true;
            }

            if (!fading)
            {
                fading = true;
                _UpdateFadeAudioSources();   
            }
        }

        public void _UpdateFadeAudioSources()
        {
            for (int i = 0; i < fadingSources.Length; i++)
            {
                float userTargetVolume = targetVolumes[i] * _GetVolume(soundTypes[i]);
                
                fadeTimers[i] += Time.deltaTime/fadeDurations[i];
                fadingSources[i].volume = Mathf.Lerp(startVolumes[i], userTargetVolume, fadeTimers[i]);

                if (Math.Abs(fadingSources[i].volume - userTargetVolume) < volumeTolerance)
                {
                    fadingSources[i].volume = userTargetVolume;
                    lerp[i] = false;
                    
                    //Stop audio source if volume is 0, but only if target volume is 0 (userTargetVolume can be 0 if the sound type is muted)
                    if (fadingSources[i].volume == 0f && targetVolumes[i] == 0f)
                    {
                        fadingSources[i].Stop();
                        fadingSources[i].enabled = false;
                        
                        Log("Removed audio source from fade: " + fadingSources[i].name);
                    
                        fadingSources = fadingSources.RemoveAt(i);
                        startVolumes = startVolumes.RemoveAt(i);
                        targetVolumes = targetVolumes.RemoveAt(i);
                        fadeDurations = fadeDurations.RemoveAt(i);
                        fadeTimers = fadeTimers.RemoveAt(i);
                        soundTypes = soundTypes.RemoveAt(i);
                        lerp = lerp.RemoveAt(i);
                    }
                }
            }

            if (fadingSources.Length == 0)
            {
                fading = false;
                return;
            }
            
            SendCustomEventDelayedFrames(nameof(_UpdateFadeAudioSources),1);
        }

        #endregion

        #region LOGGING

        private void Log(string message)
        {
            //debugger.Log("<color=#FAA61A>[AudioManager]</color> " + message);
            Debug.Log("<color=#FAA61A>[AudioManager]</color> " + message);
        }

        #endregion

        #region AUDIO POOL

        private int GetNextAvailableIndex()
        {
            for (int i = 0; i < poolSize; i++)
            {
                if (!audioSources[i].isPlaying) return i;
            }

            return -1;
        }

        #endregion
    }

    public enum SoundType
    {
        SFX,
        Music,
        Ambient
    }
}