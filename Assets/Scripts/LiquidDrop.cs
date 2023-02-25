﻿
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Catacombs.Base
{
    public enum LiquidTypes
    {
        None = 0,
        Water = 1,
        Oil = 2,
        BlueberryJuice = 3
    }

    public class LiquidDrop : ContainableItem
    {
        [Header("Liquid Drop Settings")]
        public LiquidTypes liquidType;

        [SerializeField] private float targetSpeed;
        [SerializeField] private float momentumScale = 1.01f;
        [SerializeField] private TrailRenderer trailRend;

        [HideInInspector] public GameObject containerSpawnedFrom;

        private Vector3 lastPos = Vector3.zero;
        private Vector3 curPos = Vector3.zero;

        protected override void AdditionalUpdate()
        {
            curPos = transform.position;

            //If moving slower than targetSpeed (and isn't inside Container or on Environment), scale up velocity
            if ((curPos - lastPos).magnitude <= targetSpeed)
            {
                if (!isGrounded && !isContained) rb.velocity *= momentumScale;
            }

            lastPos = curPos;
        }

        public void DelayedKill()
        {
            Destroy(gameObject);
        }

        public void DelayedEnable()
        {
            GetComponent<Collider>().enabled = true;
        }

        protected override void AdditionalTriggerEnter(Collider other) { if (hideWhenContained) trailRend.emitting = false; }
        protected override void AdditionalTriggerExit(Collider other) { if (hideWhenContained) trailRend.emitting = true; }
    }
}