using UnityEngine;

public class PortalCameraController : MonoBehaviour
{
	[Header("References")]
    public Camera mainCamera;   //  Main Camera
    private Camera portalCamera;


	[Header("View Setting")]
	public float tiltAngle = 60f;
	public Vector3 offset = new(0, 10f, -10f);


	void Awake()
	{
		if (!mainCamera) mainCamera = Camera.main;
		portalCamera = GetComponent<Camera>();
	
	}

	void LateUpdate()
    {
        if (!mainCamera || !portalCamera) return;

		// Keep the postion and rotation of portal camera following with the main camera
		portalCamera.transform.SetPositionAndRotation(
            mainCamera.transform.position,
            mainCamera.transform.rotation
        );

        // Sync FOV & clipping planes
        portalCamera.projectionMatrix = mainCamera.projectionMatrix;
	}
}
