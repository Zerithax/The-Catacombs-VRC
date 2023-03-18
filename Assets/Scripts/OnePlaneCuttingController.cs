using UdonSharp;
using UnityEngine;

public class OnePlaneCuttingController : UdonSharpBehaviour
{
    public GameObject clippingPlane;
    public Renderer maskedObjectRenderer;

    void Start() { if (maskedObjectRenderer == null) maskedObjectRenderer = GetComponent<Renderer>(); }

    void Update() { UpdateShaderProperties(); }

    private void UpdateShaderProperties()
    {
        maskedObjectRenderer.materials[0].SetVector("_PlaneNormal", clippingPlane.transform.TransformVector(new Vector3(0, 0, -1)));
        maskedObjectRenderer.materials[0].SetVector("_PlanePosition", clippingPlane.transform.position);
    }
}