
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using Catacombs.ElementSystem.Runtime;

namespace Catacombs.Base
{
    //This class passes its OnPickup & OnDrop VRC Methods to a child RuntimeElement object
    public class ElementInteractionHandler : UdonSharpBehaviour
    {
        public RuntimeElement childElement;

        private void Start() { if (childElement == null) childElement = GetComponentInChildren<RuntimeElement>(); }

        public override void OnPickup() { childElement.Grabbed(); }
        public override void OnDrop() { childElement.Dropped(); }

        private void OnCollisionEnter(Collision collision) { childElement.ParentCollisionEnter(collision); }

        private void OnCollisionExit(Collision collision) { childElement.ParentCollisionExit(collision); }
    }
}