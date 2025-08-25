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
        // Skip rendering if player is not looking at the linked portal
        if (!CameraUtility.VisibleFromCamera(linkedPortal.screen, playerCam))
        {
            return;
        }
        CreateViewTexture();

        var localToWorldMatrix = playerCam.transform.localToWorldMatrix;
        var renderPositions = new Vector3[recursionLimit];
        var renderRotation = new Quaternion[recursionLimit];

        int startIndex = 0;
        portalCam.projectionMatrix = playerCam.projectionMatrix;

        //calculate recursion portal
        for(int i = 0; i < recursionLimit; i++)
        {
            if(i > 0)
            {
                //no need to recursive render if portal is not visble through other portal
                if(!CameraUtility.BoundsOverlap(screenMeshFilter, linkedPortal.screenMeshFilter, portalCam))
                {
                    break;
                }
            }

            localToWorldMatrix = transform.localToWorldMatrix * linkedPortal.transform.worldToLocalMatrix * localToWorldMatrix;
            int renderOrderIndex = recursionLimit - i - 1;
            renderPositions[renderOrderIndex] = localToWorldMatrix.GetColumn(3);
            renderRotation[renderOrderIndex] = localToWorldMatrix.rotation;

            Quaternion flippedRotation = renderRotation[renderOrderIndex] * Quaternion.Euler(0, 180, 0);

            Debug.Log($"Recursion {i}: Portal camera position: {renderPositions[renderOrderIndex]}");
            Debug.Log($"Recursion {i}: Portal camera rotation: {renderRotation[renderOrderIndex].eulerAngles}");
            Debug.Log($"Player position: {playerCam.transform.position}");
            Debug.Log($"Player rotation: {playerCam.transform.rotation.eulerAngles}");
            

            portalCam.transform.SetPositionAndRotation(renderPositions[renderOrderIndex], flippedRotation);
            portalCam.transform.RotateAround(linkedPortal.transform.position, Vector3.up, 180);

            Debug.Log($"Portal cam rotation AFTER: {portalCam.transform.rotation.eulerAngles}");
            startIndex = renderOrderIndex;
        }

        //hide screen so camera can look through
        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        linkedPortal.screen.material.SetInteger("displayMask", 0);

        for(int i = startIndex; i < recursionLimit; i++)
        {
            portalCam.transform.SetPositionAndRotation(renderPositions[i], renderRotation[i]);
            SetNearClipPlane();
            portalCam.Render();

            if(i == startIndex)
            {
                linkedPortal.screen.material.SetInteger("displayMask", 1);
            }

        }

        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;


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
