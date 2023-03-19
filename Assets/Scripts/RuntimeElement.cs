using Argus.ItemSystem;
using Argus.ItemSystem.Editor;
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
        public Collider physicsCollider;
        public bool isCollidingSurface = false;
        public bool isGrounded = false;
        [HideInInspector] public float itemMass;

        [Header("Element Type Settings")]
        //This should already be set for pre-placed Element Prefabs
        public ElementTypeManager elementTypeManager;
        public ElementTypes elementTypeId;
        public Color elementColor;
        private bool elementInitialized;

        [Header("Death Settings")]
        public bool canDespawn = true;
        [Min(2)] public int despawnTime = 60;
        [HideInInspector] public bool startTimeout = false;
        public float killVelocity = Mathf.Infinity;

        private float lastLandTime;
        private float lastInteractTime;

        [Header("Containment Settings")]
        public bool isContained;
        public bool hideWhenContained;
        public Renderer elementRenderer;
        [SerializeField] protected ElementContainer parentContainer;

        [HideInInspector] public bool hideDebugsOverride;

        private void Start()
        {
            if (parentObject == null) parentObject = gameObject;
            if (rb == null) rb = parentObject.GetComponent<Rigidbody>();
            elementRenderer = parentObject.GetComponent<Renderer>();

            itemMass = rb.mass;

            _PullElementType();
        }

        private void Update() { if (elementInitialized) AdditionalUpdate(); }

        [RecursiveMethod]
        public virtual bool _PullElementType()
        {
            if (!elementTypeManager.isInitialized)
            {
                Log("Element Type Manager not yet initialized, trying again in 0.2 sec...");
                SendCustomEventDelayedSeconds(nameof(_PullElementType), 0.2f);
                return false;
            }

            ElementData elementData = elementTypeManager.elementDataObjs[(int)elementTypeId];

            parentObject.name = $"{elementData.name.Remove(elementData.name.Length - 8)}";

            elementColor = elementData.elementColor;
            despawnTime = elementData.despawnTime;

            //if (Time.time < 1) parentObject.name = $"{elementData.name} {parentObject.name}";
            //else parentObject.name = $"{elementData.name} {parentObject.name.Remove(parentObject.name.Length - 7)}";

            //parentObject.name = Time.time < 1 ? $"{elementData.name} {parentObject.name}" : $"{elementData.name} {parentObject.name.Remove(parentObject.name.Length - 15)}";

            AdditionalStart();

            elementInitialized = true;

            return true;
        }

        public void _RecursiveBounceTest() { SendCustomEventDelayedSeconds(nameof(_PullElementType), 0.1f); }

        protected virtual void AdditionalStart() { }

        protected virtual void AdditionalUpdate() { }

        protected virtual void AdditionalTriggerEnter(Collider other) { }

        protected virtual void AdditionalTriggerExit(Collider other) { }

        public virtual void Grabbed() { parentObject.transform.parent = null; rb.isKinematic = true; }

        public virtual void Dropped() { }

        public virtual void _DelayedKill() { Destroy(parentObject); }

        public void OnCollisionEnter(Collision collision) { ParentCollisionEnter(collision); }
        private void OnCollisionExit(Collision collision) { ParentCollisionExit(collision); }

        //Called when parentObject OnCollision (if exists)
        public void ParentCollisionEnter(Collision collision)
        {
            if (collision == null || collision.gameObject == null) return;

            //Instantly die if smashing into a surface at a velocity higher than killVelocity
            if (!isCollidingSurface && rb.velocity.magnitude > killVelocity)
            {
                Log($"Smashed into something too hard");
                Destroy(parentObject);
            }

            if (collision.collider.gameObject.layer == 11)
            {
                isGrounded = true;

                //Initiate despawn countdown
                if (canDespawn)
                {
                    lastLandTime = Time.time;
                    lastInteractTime = lastLandTime;

                    if (!isContained)
                    {
                        Log($"Starting Despawn timer");
                        SendCustomEventDelayedSeconds(nameof(_AttemptDespawn), despawnTime);
                    }
                }
            }

            //Element layer (24) shouldn't affect surface collision detection
            if (collision.collider.gameObject.layer != 24) isCollidingSurface = true;
        }

        public void ParentCollisionExit(Collision collision)
        {
            if (collision == null || collision.gameObject == null) return;

            if (collision.collider.gameObject.layer == 11)
            {
                isGrounded = false;
                lastInteractTime = Time.time;
            }

            //Element layer (24) shouldn't affect surface collision detection
            if (collision.collider.gameObject.layer != 24) isCollidingSurface = false;
        }

        public virtual void _AttemptDespawn()
        {
            if (lastLandTime == lastInteractTime)
            {
                Log("Timed out, destroying...");
                Destroy(parentObject);
            }
        }

        //Container Enter
        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.gameObject == null) return;

            if (other.gameObject.layer == 22)
            {
                //Don't let other containers steal this Element!
                if (!isContained)
                {
                    parentObject.transform.parent = other.transform;
                    //Log($"{parentObject.name} has entered Container [{other.transform.parent.name}]");

                    isContained = true;
                    lastInteractTime = Time.time;
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
                //Don't let other containers interfere with current container!
                if (other.GetComponent<ElementContainer>() != parentContainer) return;

                Log($"{parentObject.name} has exited Container [{transform.parent.name}]");
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

            AdditionalTriggerExit(other);
        }

        protected void Log(string message)
        {
            int r = (int)(elementColor.r * 255), g = (int)(elementColor.g * 255), b = (int)(elementColor.b * 255);
            string elementColorHex = r.ToString("X2") + g.ToString("X2") + b.ToString("X2");

            if (!hideDebugsOverride) Debug.Log($"<color=#{elementColorHex}>[{parentObject.name}]</color> {message:G}", this);
        }
    }
}