using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Argus.ItemSystem;
using Argus.ItemSystem.Editor;

namespace Catacombs.ElementSystem.Runtime
{
    public class GrowableLink : GrownObject
    {
        [Header("Growable Link Settings")]
        public Light torchLight;

        public Color torchColor;

        [SerializeField] private float etherAmt = 100;
        [SerializeField] private float drainSpeed = 0.1f;
        [SerializeField] private bool consumeEther;

        public override bool _PullElementType()
        {
            if (!base._PullElementType()) return false;

            torchLight = transform.GetChild(elementTypeData.GrownObjectGrowthPrefabs.Length - 1).GetChild(0).GetComponent<Light>();
            torchLight.color = torchColor;

            transform.GetChild(elementTypeData.GrownObjectGrowthPrefabs.Length - 1).GetComponent<Renderer>().materials[2].SetColor("_EmissionColor", torchColor);

            parentObject.name = $"GrowableLink";

            Log($"Retrieved GrowableLink data from {elementTypeData.name}");
            return true;
        }

        public override void KillElement()
        {
            etherAmt = 100;
            consumeEther = false;
            drainSpeed = 0.1f;

            base.KillElement();
        }

        protected override void AdditionalUpdate()
        {
            base.AdditionalUpdate();

            if (matured)
            {
                //FireFlickers();

                if (consumeEther)
                {
                    etherAmt -= drainSpeed;

                    if (etherAmt <= 0) KillElement();
                }
            }
        }

        public override void Dropped() { rb.isKinematic = false; return; }

        protected override void RemoveFromPlot()
        {
            base.RemoveFromPlot();

            if (matured) consumeEther = true;
        }
    }
}