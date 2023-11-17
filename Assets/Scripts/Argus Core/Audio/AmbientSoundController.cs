using UdonSharp;
using UnityEngine;

namespace Argus.Audio
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class AmbientSoundController : UdonSharpBehaviour
    {
        [SerializeField, HideInInspector] private AudioManager audioManager;
        
        public float lerpSpeed = 0.5f;

        public float volumeMultiplier;
        public AudioSource ambientSounds;
        
        private bool playerInSafeZone;

        public void EnableAmbientSounds()
        {
            enabled = true;
        }
        
        private void OnEnable()
        {
            UpdateSafeZoneState(playerInSafeZone);
        }
        
        public void UpdateSafeZoneState(bool newState)
        {
            playerInSafeZone = newState;

            if (!enabled) return;

            if (!playerInSafeZone)
            {
                if (!ambientSounds.isPlaying) ambientSounds.Play();
            }
        }

        public void Update()
        {
            if (playerInSafeZone)
            {
                ambientSounds.volume = Mathf.Lerp(ambientSounds.volume, 0, Time.deltaTime * lerpSpeed);
            
                if (ambientSounds.volume <= 0.01f)
                {
                    ambientSounds.volume = 0;
                    ambientSounds.Stop();
                }
            }
            else
            {
                ambientSounds.volume = Mathf.Lerp(ambientSounds.volume, audioManager.ambientVolume * volumeMultiplier, Time.deltaTime * lerpSpeed);
            }
        }
    }
}