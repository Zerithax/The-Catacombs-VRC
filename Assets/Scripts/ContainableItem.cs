
using System.ComponentModel;
using System.Threading;
using System.Timers;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
using static VRC.Core.ApiAvatar;

namespace Catacombs.Base
{

    public class ContainableItem : UdonSharpBehaviour
    {
        //Containable Items 
        [Header("Req. Item Fields")]
        public GameObject parentObject;
        public Rigidbody rb;
        [HideInInspector] public float itemMass;
        public bool isColliding = false;
        public bool isGrounded = false;

        [Header("Item Death Settings")]
        public int despawnTime = 60;
        [HideInInspector] public bool startTimeout = false;

        public float killVelocity = Mathf.Infinity;
        [HideInInspector] public float timer;

        [Header("Containment Settings")]
        public bool hideWhenContained = false;
        public Renderer itemRend;
        public bool isContained;

        private void Start() { itemMass = rb.mass; AdditionalStart(); }

        private void Update() { ManageDeathTimeout(); AdditionalUpdate(); }

        protected virtual void AdditionalStart() { }

        protected virtual void AdditionalUpdate() { }

        protected virtual void AdditionalTriggerEnter(Collider other) { }

        protected virtual void AdditionalTriggerExit(Collider other) { }

        protected virtual void AdditionalCollisionEnter(Collision collision) { }

        protected virtual void AdditionalCollisionExit(Collision collision) { }

        private void ManageDeathTimeout()
        {
            if (startTimeout && !isContained)
            {
                timer += Time.deltaTime;

                if (timer > despawnTime)
                {
                    Debug.Log($"{parentObject.name} timed out on the floor");
                    Destroy(gameObject);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision == null || collision.gameObject == null) return;
            //Instantly die if smashing into a surface at a velocity higher than killVelocity
            if (!isColliding && rb.velocity.magnitude > killVelocity)
            {
                Debug.Log($"{parentObject.name} smashed into something too hard");
                Destroy(gameObject);
            }

            //Otherwise, initiate death countdown if on Environment
            if (collision.collider.gameObject.layer == 11)
            {
                startTimeout = true;
                isGrounded = true;
            }

            isColliding = true;

            AdditionalCollisionEnter(collision);
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision == null || collision.gameObject == null) return;
            //Stop & reset timer if exiting Environment layer
            if (collision.collider.gameObject.layer == 11)
            {
                timer = 0;
                startTimeout = false;
                isGrounded = false;
            }

            isColliding = false;

            AdditionalCollisionExit(collision);
        }

        //Container Enter
        private void OnTriggerEnter(Collider other)
        {
            if (other == null || other.gameObject == null) return;
            if (other.gameObject.layer == 22)
            {
                parentObject.transform.parent = other.transform.parent;
                Debug.Log($"{parentObject.name} has entered Container [{other.transform.parent.name}]");

                isContained = true;
                rb.mass = 0;

                if (hideWhenContained) itemRend.enabled = false;
            }

            AdditionalTriggerEnter(other);
        }

        //Container Exit
        private void OnTriggerExit(Collider other)
        {
            if (other == null || other.gameObject == null) return;
            if (other.gameObject.layer == 22)
            {
                Debug.Log($"{parentObject.name} has exited Container [{transform.parent.name}]");
                parentObject.transform.parent = null;

                isContained = false;
                rb.mass = itemMass;

                if (hideWhenContained) itemRend.enabled = true;
            }

            AdditionalTriggerExit(other);
        }
    }
}