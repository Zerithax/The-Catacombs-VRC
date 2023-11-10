using Argus.ItemSystem;
using Argus.ItemSystem.Editor;
using System;
using System.ComponentModel;
using System.Linq;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Catacombs.ElementSystem.Runtime
{
    public class RuntimeElement : UdonSharpBehaviour
    {
        [Header("Req. Item Fields")]
        public GameObject parentObject;
        public Rigidbody rb;
        public ItemPooler itemPooler;
        public ElementTypeManager elementTypeManager;
        public ElementData elementTypeData;
        public ElementTypes elementTypeId;

        [Header("Death Settings")]
        public bool canDespawn;
        public float despawnTime;
        public float killVelocity;
        public float lastLandTime;
        public float lastInteractTime;

        [Header("Containment Settings")]
        public bool disableContainment;
        public bool hideWhenContained;
        public bool isContained;
        public Renderer elementRenderer;
        public ElementContainer parentContainer;

        [Header("Debug")]
        public bool hideDebugsOverride;
        public bool elementInitialized;
        public bool isCollidingSurface;

        public bool isGrounded;
        public float itemMass;
        public Color elementColor;

        private float lastVelocityMagnitude;

        private void Start()
        {
            if (parentObject == null) parentObject = gameObject;
            if (rb == null) rb = parentObject.GetComponent<Rigidbody>();
            elementRenderer = parentObject.GetComponent<Renderer>();

            //If this object already exists within the first second of scene init, manually PullElementType()
            if (Time.time < 1) _PullElementType();
        }

        private void Update()
        {
            if (elementInitialized)
            {
                AdditionalUpdate();

                lastVelocityMagnitude = rb.velocity.magnitude;
            }
        }

        [RecursiveMethod]
        public virtual bool _PullElementType()
        {
            if (!elementTypeManager.isInitialized)
            {
                Log("Element Type Manager not yet initialized, trying again in 0.2 sec...");
                SendCustomEventDelayedSeconds(nameof(_PullElementType), 0.2f);
                return false;
            }

            elementTypeData = elementTypeManager.elementDataObjs[(int)elementTypeId];

            Log($"Growth time will be {elementTypeManager.elementDataObjs[(int)elementTypeId].grownObjectGrowTime}");

            //Log($"Set Element Tpye Data to {(int)elementTypeId} of elementDataObjs:");
            //Log($"{elementTypeManager.elementDataObjs[(int)elementTypeId]}");

            parentObject.name = $"{elementTypeData.name.Remove(elementTypeData.name.Length - 8)}";

            elementColor = elementTypeData.elementColor;

            if (elementTypeData.canDespawn)
            {
                canDespawn = elementTypeData.canDespawn;
                despawnTime = elementTypeData.despawnTime;
                killVelocity = elementTypeData.killVelocity;
            }

            AdditionalStart();

            elementInitialized = true;

            return true;
        }

        public void _RecursiveBounceTest() { SendCustomEventDelayedSeconds(nameof(_PullElementType), 0.1f); }

        protected virtual void AdditionalStart() { }

        protected virtual void AdditionalUpdate() { }

        protected virtual void AdditionalTriggerEnter(Collider other) { }

        protected virtual void AdditionalTriggerExit(Collider other) { }

        public virtual void Grabbed()
        {
            parentObject.transform.parent = null;
            rb.isKinematic = true;

            lastInteractTime = (float)Math.Round(Time.time, 3);
        }

        public virtual void Dropped() { }

        public virtual void KillElement() { }

        public void OnCollisionEnter(Collision collision) { ParentCollisionEnter(collision); }
        private void OnCollisionExit(Collision collision) { ParentCollisionExit(collision); }

        //Called when parentObject OnCollision (if exists)
        public void ParentCollisionEnter(Collision collision)
        {
            if (collision == null || collision.gameObject == null) return;

            if (collision.gameObject.layer == 24) return;

            if (!isCollidingSurface)
            {
                //Die if colliding with a surface at a velocity higher than killVelocity
                if (elementTypeData.canDespawn && lastVelocityMagnitude > elementTypeData.killVelocity)
                {
                    Log($"Smashed into something too hard");

                    KillElement();
                }
            }

            isCollidingSurface = true;

            //If on Environment layer
            if (collision.collider.gameObject.layer == 11)
            {
                isGrounded = true;

                //Initiate despawn countdown
                if (elementTypeData.canDespawn)
                {
                    lastLandTime = (float)Math.Round(Time.time, 3);
                    lastInteractTime = lastLandTime;

                    if (!isContained)
                    {
                        Log($"Starting Despawn timer");
                        SendCustomEventDelayedSeconds(nameof(_AttemptDespawn), despawnTime);
                    }
                }
            }
        }

        public void ParentCollisionExit(Collision collision)
        {
            if (collision == null || collision.gameObject == null) return;

            if (collision.gameObject.layer == 24) return;

            isCollidingSurface = false;

            if (collision.collider.gameObject.layer == 11)
            {
                isGrounded = false;
                lastInteractTime = (float)Math.Round(Time.time, 3);
            }
        }

        public virtual void _AttemptDespawn() {  }

        //Container Enter
        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.gameObject == null) return;

            if (other.gameObject.layer == 22)
            {
                //Don't let other containers steal this Element!
                if (!isContained && !disableContainment)
                {
                    parentObject.transform.parent = other.transform;
                    Log($"{parentObject.name} has entered Container [{other.transform.parent.name}]");

                    isContained = true;
                    lastInteractTime = (float)Math.Round(Time.time, 3);
                    parentContainer = other.GetComponent<ElementContainer>();
                    rb.mass = 0;

                    if (hideWhenContained)
                    {
                        elementRenderer.enabled = false;
                        rb.isKinematic = true;
                    }
                }
            }

            AdditionalTriggerEnter(other);
        }

        //Container Exit
        private void OnTriggerExit(Collider other)
        {
            if (other == null || other.gameObject == null) return;

            if (other.gameObject.layer == 22)
            {
                if (!disableContainment)
                {
                    //Don't let other containers interfere with current container!
                    if (other.GetComponent<ElementContainer>() != parentContainer) return;

                    Log($"{parentObject.name} has exited Container [{other.transform.parent.name}]");

                    parentContainer = null;
                    parentObject.transform.parent = null;

                    isContained = false;
                    rb.mass = itemMass;

                    if (hideWhenContained) elementRenderer.enabled = true;

                    if (isGrounded)
                    {
                        Log($"Starting Despawn timer");
                        SendCustomEventDelayedSeconds(nameof(_AttemptDespawn), despawnTime);
                    }
                }
            }

            AdditionalTriggerExit(other);
        }

        protected void Log(string message)
        {
            int r = (int)(elementColor.r * 255), g = (int)(elementColor.g * 255), b = (int)(elementColor.b * 255);
            string elementColorHex = r.ToString("X2") + g.ToString("X2") + b.ToString("X2");

            if (!hideDebugsOverride) Debug.Log($"<color=#{elementColorHex}>[{parentObject.name}]</color> {message:G}", this);
        }

        protected void LogWarning(string message)
        {
            if (!hideDebugsOverride) Debug.LogWarning($"[{parentObject.name}] {message}", this);
        }
    }
}