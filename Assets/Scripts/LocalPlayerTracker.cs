using System;
using UdonSharp;
using UnityEngine;
using VRC.SDK3.ClientSim;
using VRC.SDKBase;

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

        [SerializeField] public VRCPlayerApi localPlayer;
        [SerializeField] private float heightOffset = 1;

        public PlayerEffect[] playerEffects;
        public int[] playerEffectsStrengths;
        public float[] playerEffectsDurations;
        public float[] playerEffectsStartTimes;

        [SerializeField] private float maximumEffectStrength = 10;
        [SerializeField] private float defaultRunSpeed;
        [SerializeField] private float defaultJumpImpulse;

        private void Start()
        {
            localPlayer = Networking.LocalPlayer;

            if (playerEffects.Length == 0) playerEffects = new PlayerEffect[10];

            playerEffectsStrengths = new int[playerEffects.Length];
            playerEffectsDurations = new float[playerEffects.Length];
            playerEffectsStartTimes = new float[playerEffects.Length];

            defaultRunSpeed = localPlayer.GetRunSpeed();
            defaultJumpImpulse = localPlayer.GetJumpImpulse();

            _RemoveOldEffects();
        }

        private void Update()
        {
            Vector3 playerPos = localPlayer.GetPosition();
            transform.position = new Vector3(playerPos.x, playerPos.y + heightOffset, playerPos.z);

            transform.rotation = localPlayer.GetRotation();
        }

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
                            localPlayer.SetJumpImpulse(Mathf.Clamp(localPlayer.GetJumpImpulse() + effectStrength, defaultJumpImpulse - maximumEffectStrength, defaultJumpImpulse + maximumEffectStrength));
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
                                localPlayer.SetRunSpeed(localPlayer.GetRunSpeed() - playerEffectsStrengths[i]);
                                break;

                            case PlayerEffect.JumpBoost:
                                localPlayer.SetJumpImpulse(localPlayer.GetRunSpeed() + playerEffectsStrengths[i]);
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
    }
}