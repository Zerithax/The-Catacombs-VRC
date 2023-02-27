
using Argus.ItemSystem;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Catacombs.ElementSystem.Runtime
{
    public enum ElementTypes
    {
        None = 0,
        Linkberry = 1,
        Arieberry = 2,
        Blueberry = 3,
        Water = 4,
        Oil = 5
    }

    public class RuntimeElement : UdonSharpBehaviour
    {
        //These should already be set for pre-placed Element Prefabs
        [Header("Element Type Settings")]
        public ElementTypeManager elementTypeManager;
        public ElementTypes elementTypeId;
        public Color elementColor;

        [Header("Req. Item Fields")]
        public GameObject parentObject;
        public Rigidbody rb;
        public Collider physicsCollider;
        [HideInInspector] public float itemMass;
        [HideInInspector] public bool isColliding = false;
        [HideInInspector] public bool isGrounded = false;

        [Header("Death Settings")]
        public bool canDespawn = true;
        [Min(2)] public int despawnTime = 60;
        [HideInInspector] public bool startTimeout = false;
        [HideInInspector] public float timer;
        public float killVelocity = Mathf.Infinity;

        private int totalDeathLoops;
        private int curDeathLoop;
        private int recursiveDeathDelay = 1;
        private float lastLandTime;

        [Header("Containment Settings")]
        public bool hideWhenContained;
        public Renderer elementRenderer;
        public bool isContained;

        private void Start()
        {
            if (parentObject == null) parentObject = gameObject;
            if (rb == null) rb = parentObject.GetComponent<Rigidbody>();
            elementRenderer = parentObject.GetComponent<Renderer>();

            itemMass = rb.mass;

            if (canDespawn)
            {
                int startingDeathLoops = 2;

                while (despawnTime / startingDeathLoops > 5) startingDeathLoops += 2;

                totalDeathLoops = startingDeathLoops;
                recursiveDeathDelay = despawnTime / totalDeathLoops;
            }

            AdditionalStart();

            if (elementTypeManager.elementTypeData[0] != null) PullElementType();
            else SendCustomEventDelayedFrames(nameof(PullElementType), 5);
        }

        private void Update() { AdditionalUpdate(); }

        //If this doesn't work, make Element Spawners put the elementTypeId in their name, that way objects can retrieve their own IDs then rename themselves without the extra numbers
        public virtual void PullElementType()
        {
            if (elementTypeManager == null)
            {
                SendCustomEventDelayedFrames(nameof(PullElementType), 5);
                return;
            }

            elementColor = elementTypeManager.elementTypeData[(int)elementTypeId].elementColor;
        }

        protected virtual void AdditionalStart() { }

        protected virtual void AdditionalUpdate() { }

        protected virtual void AdditionalTriggerEnter(Collider other) { }

        protected virtual void AdditionalTriggerExit(Collider other) { }

        public virtual void Grabbed() { parentObject.transform.parent = null; rb.isKinematic = true; }

        public virtual void Dropped() { }

        public virtual void _DelayedKill() { Destroy(parentObject); }

        //TODO: for some reason the recursive bit doesn't actually seem to be running (compare only runs once?)
        [RecursiveMethod]
        private void _RecursiveDeathTimer()
        {
            Log($"Comparing {lastLandTime + (recursiveDeathDelay * curDeathLoop) - Time.time} <= 0.05");

            if (lastLandTime + (recursiveDeathDelay * curDeathLoop) - Time.time <= 0.05f)
            {
                if (curDeathLoop == totalDeathLoops) Destroy(gameObject);

                Log($"[{parentObject.name}] Death loop {curDeathLoop}");

                curDeathLoop++;
                SendCustomEventDelayedSeconds(nameof(_RecursiveDeathTimer), recursiveDeathDelay);
            }
        }

        //Called when parentObject OnCollision (if exists)
        public void ParentCollisionEnter(Collision collision)
        {
            if (collision == null || collision.gameObject == null) return;

            //Instantly die if smashing into a surface at a velocity higher than killVelocity
            if (!isColliding && rb.velocity.magnitude > killVelocity)
            {
                Log($"{parentObject.name} smashed into something too hard");
                Destroy(gameObject);
            }

            //Initiate death countdown if on Environment
            if (canDespawn)
            {
                if (collision.collider.gameObject.layer == 11)
                {
                    if (!isContained)
                    {
                        lastLandTime = Time.time;

                        Log($"Starting Death timer...");
                        _RecursiveDeathTimer();
                    }

                    isGrounded = true;
                }
            }

            isColliding = true;
        }

        public void ParentCollisionExit(Collision collision)
        {
            if (collision == null || collision.gameObject == null) return;

            //Stop & reset timer if exiting Environment layer
            if (collision.collider.gameObject.layer == 11)
            {
                timer = 0;
                startTimeout = false;
                isGrounded = false;

                curDeathLoop = 0;
            }

            isColliding = false;
        }

        //Container Enter
        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.gameObject == null) return;
            if (other.gameObject.layer == 22)
            {
                parentObject.transform.parent = other.transform.parent;
                Log($"{parentObject.name} has entered Container [{other.transform.parent.name}]");

                isContained = true;
                rb.mass = 0;

                if (hideWhenContained)
                {
                    elementRenderer.enabled = false;
                    rb.isKinematic = true;
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
                Log($"{parentObject.name} has exited Container [{transform.parent.name}]");
                parentObject.transform.parent = null;

                isContained = false;
                rb.mass = itemMass;

                if (hideWhenContained) elementRenderer.enabled = true;
            }

            AdditionalTriggerExit(other);
        }

        protected void Log(string message)
        {
            int r = (int)(elementColor.r * 255), g = (int)(elementColor.g * 255), b = (int)(elementColor.b * 255);
            string elementColorHex = r.ToString("X2") + g.ToString("X2") + b.ToString("X2");

            Debug.Log($"<color=#{elementColorHex}>[{name}]</color> {message}", this);
        }
    }
}