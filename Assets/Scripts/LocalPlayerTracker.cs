using System;
using UdonSharp;
using UnityEngine;
using Argus.Audio;
using VRC.SDKBase;
using BestHTTP.SecureProtocol.Org.BouncyCastle.Asn1.Esf;
using static VRC.Core.ApiAvatar;
using UnityEngine.SocialPlatforms;

namespace Catacombs.Base
{
    public enum PlayerEffect
    {
        None = 0,
        SpeedBoost = 1,
        JumpBoost = 2,
        Hoarseness = 3
    }

    public class LocalPlayerTracker : UdonSharpBehaviour
    {
        //This class will manage tracking the player's interactions that will eventually be saved with NUSS
        //This class will manage tracking and managing collisions for the player (since OnPlayerTriggerEnter doesn't work in client sim)

        [Header("Singletons")]
        [SerializeField] public VRCPlayerApi Owner;
        [SerializeField] private BGMManager bgmManager;
        [SerializeField] private AudioManager audioManager;
        [SerializeField] private float heightOffset = 1;

        [Header("Player Effects")]
        public PlayerEffect[] playerEffects;
        public int[] playerEffectsStrengths;
        public float[] playerEffectsDurations;
        public float[] playerEffectsStartTimes;

        [SerializeField] private float maximumEffectStrength = 10;
        [SerializeField] private float defaultRunSpeed;
        [SerializeField] private float defaultJumpImpulse;

        [Header("Footsteps")]
        [SerializeField] private float walkSpeed = 2f;
        [SerializeField] private float maxTimeBetweenSteps = 2f;
        [SerializeField] private float minTimeBetweenSteps = 0.3f;
        [SerializeField] private int framesPerUpdate = 20;

        [NonSerialized] public bool isSwimming;
        [NonSerialized] private bool footstepSFXGrounded;

        private float timeBetweenSteps = 0.5f;
        private float stepsTimer;
        private bool lastIsGrounded = true;
        private float lastTime;

        [Header("Other")][SerializeField] private LayerMask groundedMask;

        private void Start()
        {
            Owner = Networking.LocalPlayer;

            if (playerEffects.Length == 0) playerEffects = new PlayerEffect[10];

            playerEffectsStrengths = new int[playerEffects.Length];
            playerEffectsDurations = new float[playerEffects.Length];
            playerEffectsStartTimes = new float[playerEffects.Length];

            defaultRunSpeed = Owner.GetRunSpeed();
            defaultJumpImpulse = Owner.GetJumpImpulse();

            _RemoveOldEffects();
            FootstepInit();
        }

        private void FixedUpdate()
        {
            Vector3 playerPos = Owner.GetPosition();
            transform.SetPositionAndRotation(Vector3.Lerp(transform.position, new Vector3(playerPos.x, playerPos.y + heightOffset, playerPos.z),
                Time.fixedDeltaTime * 10), Owner.GetRotation());
        }

        private void Update()
        {
            footstepSFXGrounded = isGrounded();
        }

        public static int GetZoneID(string name)
        {
            if (!name.Contains("_")) return -1;

            string[] sp = name.Split('_');
            if (sp.Length < 2) return -1;
            int num = -1;
            int.TryParse(sp[1], out num);
            return num;
        }

        private void OnTriggerEnter(Collider other)
        {
            int possibleZoneID = GetZoneID(other.name);
            if (possibleZoneID != -1)
            {
                bgmManager.ZoneEnter(possibleZoneID);
                return;
            }
            
            LiftZoneTrigger liftZone = other.GetComponent<LiftZoneTrigger>();
            
            if (liftZone != null) liftZone.LiftEntered(this);
        }

        private void OnTriggerExit(Collider other)
        {
            bgmManager.ZoneExit(GetZoneID(other.gameObject.name));   
        }

        private bool isGrounded()
        {
            //if (!hasOwner) return false;
            //if (!Utilities.IsValid(Owner)) return false;

            return Physics.Raycast(Owner.GetPosition() + Vector3.up * 0.05f, Vector3.down, out var hit, 0.15f, groundedMask, QueryTriggerInteraction.Ignore);
            //return Physics.SphereCast(Owner.GetPosition() + Vector3.up, 0.2f, Vector3.down, out var unusedHit, 1.2f, groundedMask);
        }

        #region Player Effects

        public void AttemptAddEffect(PlayerEffect playerEffect, int effectStrength, float effectDuration)
        {
            for (int i = 0; i < playerEffects.Length; i++)
            {
                if (playerEffects[i] == PlayerEffect.None)
                {
                    playerEffects[i] = playerEffect;
                    playerEffectsStrengths[i] = effectStrength;
                    playerEffectsDurations[i] = effectDuration;
                    playerEffectsStartTimes[i] = Time.time;

                    switch (playerEffect)
                    {
                        case PlayerEffect.None:
                            break;

                        case PlayerEffect.SpeedBoost:
                            Debug.Log($"[{name}] Setting Speed boost to {Mathf.Clamp(Networking.LocalPlayer.GetRunSpeed() + effectStrength, defaultRunSpeed - maximumEffectStrength, defaultRunSpeed + maximumEffectStrength)}");

                            Networking.LocalPlayer.SetRunSpeed(Mathf.Clamp(Networking.LocalPlayer.GetRunSpeed() + effectStrength, defaultRunSpeed - maximumEffectStrength, defaultRunSpeed + maximumEffectStrength));
                            break;

                        case PlayerEffect.JumpBoost:
                            Owner.SetJumpImpulse(Mathf.Clamp(Owner.GetJumpImpulse() + effectStrength, defaultJumpImpulse - maximumEffectStrength, defaultJumpImpulse + maximumEffectStrength));
                            break;

                        case PlayerEffect.Hoarseness:
                            break;
                    }

                    Debug.Log($"[{name}] Successfully added playerEffect {playerEffect} to list of effects");
                    return;
                }
            }

            Debug.Log($"[{name}] Not enough room to add playerEffect {playerEffect} to list of effects");
        }

        [RecursiveMethod]
        public void _RemoveOldEffects()
        {
            for (int i = 0; i < playerEffects.Length; i++)
            {
                if (playerEffects[i] != PlayerEffect.None)
                {
                    if (Time.time > playerEffectsStartTimes[i] + playerEffectsDurations[i])
                    {
                        switch (playerEffects[i])
                        {
                            case PlayerEffect.None:
                                break;

                            case PlayerEffect.SpeedBoost:
                                Owner.SetRunSpeed(Owner.GetRunSpeed() - playerEffectsStrengths[i]);
                                break;

                            case PlayerEffect.JumpBoost:
                                Owner.SetJumpImpulse(Owner.GetRunSpeed() + playerEffectsStrengths[i]);
                                break;

                            case PlayerEffect.Hoarseness:
                                break;
                        }

                        Debug.Log($"[{name}] Successfully removed {playerEffects[i]} from list of effects");

                        playerEffects[i] = PlayerEffect.None;
                        playerEffectsStrengths[i] = 0;
                        playerEffectsDurations[i] = 0;
                        playerEffectsStartTimes[i] = 0;
                    }
                }
            }

            SendCustomEventDelayedSeconds(nameof(_RemoveOldEffects), 5);
        }
        #endregion

        #region Footsteps

        private void FootstepInit()
        {
            lastTime = Time.time;
            CheckFootstepFrequency();
            _FootstepUpdate();
        }

        public void _FootstepUpdate()
        {
            //if (!hasOwner || !Utilities.IsValid(VRC.Core.ApiAvatar.Owner)) return;

            SendCustomEventDelayedSeconds(nameof(_FootstepUpdate), minTimeBetweenSteps / 2f);

            Vector3 ownerPosition = Owner.GetPosition();

            bool landed = !lastIsGrounded && footstepSFXGrounded && !isSwimming;

            lastIsGrounded = footstepSFXGrounded;

            //Landing SFX
            if (landed)
            {
                audioManager.PlayLandSFX(ownerPosition);
                stepsTimer = 0.75f;
                CheckFootstepFrequency();
                return;
            }

            //Footsteps
            //float playerVelocity = isLocal ? localPlayer.GetVelocity().magnitude : Owner.GetVelocity().magnitude;
            float playerVelocity = Owner.GetVelocity().magnitude;
            if (!(playerVelocity > 0) || !footstepSFXGrounded || isSwimming) return;

            stepsTimer -= Time.time - lastTime;
            lastTime = Time.time;

            if (!(stepsTimer < 0)) return;

            Log($"Steps timer pass");

            float volume = Mathf.Clamp01(playerVelocity / walkSpeed);

            audioManager.PlayFootstepSFX(ownerPosition, volume);

            CheckFootstepFrequency();
        }

        private void CheckFootstepFrequency()
        {
            //float playerVelocity = isLocal ? localPlayer.GetVelocity().magnitude : Owner.GetVelocity().magnitude;
            float playerVelocity = Owner.GetVelocity().magnitude;
            stepsTimer = Mathf.Clamp(timeBetweenSteps / (playerVelocity / walkSpeed * 1.2f), minTimeBetweenSteps, maxTimeBetweenSteps);
        }

        #endregion

        private void Log(string message)
        {
            Debug.Log($"[{name}] {message}");
        }
    }
}