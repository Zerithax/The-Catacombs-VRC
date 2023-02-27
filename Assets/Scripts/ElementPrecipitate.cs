using Argus.ItemSystem.Editor;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

namespace Catacombs.ElementSystem.Runtime
{
    public enum ElementPrecipitates
    {
        None,
        Dust = 1,
        Drip = 2
    }

    public class ElementPrecipitate : RuntimeElement
    {
        [Header("Precipitate Type")]
        public ElementPrecipitates elementPrecipitateType;

        [Header("Momentum Scaling Settings")]
        public bool minimumSpeed;
        public float targetSpeed;
        public float momentumScale = 1.01f;
        [SerializeField] private TrailRenderer trailRend;

        private Vector3 lastPos = Vector3.zero;
        private Vector3 curPos = Vector3.zero;

        protected override void AdditionalStart()
        {
            base.AdditionalStart();

            if (minimumSpeed && trailRend == null) trailRend = GetComponent<TrailRenderer>();
        }

        protected override void AdditionalUpdate()
        {
            //TODO: Rewrite to support a maximumSpeed as well
            if (minimumSpeed)
            {
                curPos = transform.position;

                //If moving slower than targetSpeed (and isn't inside Container or on Environment), scale up velocity
                //TODO: To remove the Environment reestriction, get the slope of the ground and only speed up if it's greater than an element-dependent threshold
                if ((curPos - lastPos).magnitude <= targetSpeed)
                {
                    if (!isGrounded && !isContained) rb.velocity *= momentumScale;
                }

                lastPos = curPos;
            }
        }

        public override void PullElementType()
        {
            //ELEMENT DATA
            ElementData elementData = elementTypeManager.elementTypeData[(int)elementTypeId];

            elementPrecipitateType = elementData.elementPrecipitateType;
            minimumSpeed = elementData.scaleVelocity;
            targetSpeed = elementData.targetVelocity;
            momentumScale = elementData.momentumScale;

            //COLOR
            base.PullElementType();

            switch (elementPrecipitateType)
            {
                case ElementPrecipitates.Dust:
                    //TODO: Make this a random amount beteween mostly-colored and mostly-green
                    GetComponent<Renderer>().material.color = new Color(elementColor.r + Color.green.r / 2, elementColor.g + Color.green.g / 2, elementColor.b + Color.green.b / 2);
                    break;

                case ElementPrecipitates.Drip:
                    GetComponent<Renderer>().material.color = elementColor;
                    trailRend.material.SetColor("_Emission", elementColor);
                    break;
            }

            //NAME
            name = $"{elementTypeId.ToString()} {elementPrecipitateType.ToString()}";
        }

        protected override void AdditionalTriggerEnter(Collider other) { if (hideWhenContained) trailRend.emitting = false; }

        protected override void AdditionalTriggerExit(Collider other) { if (hideWhenContained) trailRend.emitting = true; }
    }
}