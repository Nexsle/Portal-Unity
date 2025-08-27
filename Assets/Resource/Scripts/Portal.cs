using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Main Settings")]
    public Portal linkedPortal;
    public MeshRenderer screen;
    public int recursionLimit = 3;

    [Header("Advanced Settings")]
    public float nearClipOffset = 0.05f;
    public float nearClipLimit = 0.2f;

    // Private variables
    RenderTexture viewTexture;
    Camera portalCam;
    Camera playerCam;
    MeshFilter screenMeshFilter;

    private void Awake()
    {
        portalCam = GetComponentInChildren<Camera>();
        playerCam = Camera.main;
        portalCam.enabled = false;
        screenMeshFilter = screen.GetComponent<MeshFilter>();

        if (screen.material != null )
        {
            screen.material.SetInteger("displayMask", 1);
        }
    }

    private void Start()
    {
        if(linkedPortal != null && linkedPortal.linkedPortal != this)
        {
            linkedPortal.linkedPortal = this;
        }
    }

    public void PrePortalRender()
    {
        return;
    }


    public void Render()
    {


        if (!CameraUtility.VisibleFromCamera(linkedPortal.screen, playerCam))
        {
            return;
        }
        CreateViewTexture();

        // Render with recursion levels
        for (int recursionLevel = recursionLimit - 1; recursionLevel >= 0; recursionLevel--)
        {
            Debug.Log($"Rendering recursion level {recursionLevel}");
            // Start fresh from player camera each time
            portalCam.transform.position = playerCam.transform.position;
            portalCam.transform.rotation = playerCam.transform.rotation;

            // Apply portal transformation multiple times for this recursion level
            for (int i = 0; i <= recursionLevel; i++)
            {
                // Position the camera behind the other portal
                Vector3 relativePos = transform.InverseTransformPoint(portalCam.transform.position);
                relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
                portalCam.transform.position = linkedPortal.transform.TransformPoint(relativePos);

                // Rotate the camera to look through the other portal
                Quaternion relativeRot = Quaternion.Inverse(transform.rotation) * portalCam.transform.rotation;
                relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
                portalCam.transform.rotation = linkedPortal.transform.rotation * relativeRot;
            }

            SetNearClipPlane();
            portalCam.Render();

            // Only need one render for the deepest level
            if (recursionLevel == 0) break;
        }
    }

    public void PostPortalRender()
    {
        ProtectScreenFromClipping(playerCam.transform.position);
    }

    void CreateViewTexture()
    {
        if (viewTexture == null || viewTexture.width != Screen.width || viewTexture.height != Screen.height)
        {
            if (viewTexture != null)
            {
                viewTexture.Release();
            }

            viewTexture = new RenderTexture(Screen.width, Screen.height, 24);
            portalCam.targetTexture = viewTexture;
            linkedPortal.screen.material.SetTexture("_MainTex", viewTexture);

            Debug.Log("Created view texture for " + gameObject.name);
        }
    }

    void SetNearClipPlane()
    {
        // Learning resource:
        // http://www.terathon.com/lengyel/Lengyel-Oblique.pdf
        Transform clipPlane = transform;
        int dot = System.Math.Sign(Vector3.Dot(clipPlane.forward, transform.position - portalCam.transform.position));

        Vector3 camSpacePos = portalCam.worldToCameraMatrix.MultiplyPoint(clipPlane.position);
        Vector3 camSpaceNormal = portalCam.worldToCameraMatrix.MultiplyVector(clipPlane.forward) * dot;
        float camSpaceDst = -Vector3.Dot(camSpacePos, camSpaceNormal) + nearClipOffset;

        // Don't use oblique clip plane if very close to portal as it seems this can cause some visual artifacts
        if (Mathf.Abs(camSpaceDst) > nearClipLimit)
        {
            Vector4 clipPlaneCameraSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);

            // Update projection based on new clip plane
            // Calculate matrix with player cam so that player camera settings (fov, etc) are used
            portalCam.projectionMatrix = playerCam.CalculateObliqueMatrix(clipPlaneCameraSpace);
        }
        else
        {
            portalCam.projectionMatrix = playerCam.projectionMatrix;
        }
    }

    float ProtectScreenFromClipping(Vector3 viewPoint)
    {
        float halfHeight = playerCam.nearClipPlane * Mathf.Tan(playerCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * playerCam.aspect;
        float dstToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight, playerCam.nearClipPlane).magnitude;
        float screenThickness = dstToNearClipPlaneCorner;

        Transform screenT = screen.transform;
        bool camFacingSameDirAsPortal = Vector3.Dot(transform.forward, transform.position - viewPoint) > 0;

        //Debug.Log($"Camera facing same dir as portal: {camFacingSameDirAsPortal}");
        //Debug.Log($"Transform forward: {transform.forward}");
        //Debug.Log($"Calculated offset: {Vector3.forward * screenThickness * ((camFacingSameDirAsPortal) ? 0.5f : -0.5f)}");
        //Debug.Log($"Screen position BEFORE: {screenT.localPosition}");
        //Debug.Log($"Screen scale BEFORE: {screenT.transform.localScale}");

        screenT.localScale = new Vector3(screenT.localScale.x, screenThickness, screenT.localScale.z);

        Vector3 currentPos = screenT.localPosition;
        float zOffset = screenThickness * ((camFacingSameDirAsPortal) ? 0.5f : -0.5f);
        screenT.localPosition = new Vector3(currentPos.x, currentPos.y, zOffset);

        //Debug.Log($"Screen position AFTER: {screenT.localPosition}");

        //Debug.Log($"Screen scale AFTER: {screenT.transform.localScale}");

        return screenThickness;
    }
}
