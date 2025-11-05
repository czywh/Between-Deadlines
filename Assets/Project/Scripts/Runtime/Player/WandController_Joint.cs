using UnityEngine;

public class WandController_Joint : MonoBehaviour
{
    [Header("Essentials")]
    public Camera cam;
    public LayerMask hittableLayers;
    public KeyCode grabKey = KeyCode.Mouse1; //click to grab
    public KeyCode rotateModeKey = KeyCode.LeftAlt;

    [Header("Aiming")]
    public float aimSphereRadius = 0.12f;
    public float maxRayDistance = 60f;

    [Header("Hold Distance")]
    public float defaultHoldDistance = 6f;
    public float minHoldDistance = 1.5f;
    public float maxHoldDistance = 20f;
    public float scrollSensitivity = 2.5f;
    public float anchorLerp = 0.6f;

    [Header("Spring Joint Settings")]
    public float spring = 900f;
    public float damper = 90f;
    public float maxForce = 5000f;
    public float jointMaxDistance = 0f;

    [Header("Throwing")]
    public float forwardThrowBoost = 4f;
    public float anchorVelSmoothing = 0.25f;

    [Header("Hold Stabilization")]
    [Tooltip("If the anchor is nearly still and the object is close to the anchor, zero out velocities to stop wiggling.")]
    public float steadyDistanceEps = 0.02f; // meters
    public float steadySpeedEps = 0.02f; // m/s of anchor
    public float holdExtraDrag = 5f; // linear drag while held

    [Header("Obstruction Handling")]
    public bool preventThroughWall = true;

    [Header("Rotation Control")]
    public float mouseRotateTorque = 8f;
    public float maxAngularVelocity = 20f;

    private RigidbodyConstraints _rbOldConstraints;
    private float _rbOldAngularDrag;
    private float _rbOldDrag;

    private bool _rbWasKinematic = false;                               
    private RigidbodyInterpolation _rbOldInterpolation;                 
    private CollisionDetectionMode _rbOldCollisionDetectionMode;        

    private Transform _anchor;
    private Rigidbody _anchorRB;
    private Vector3 _anchorPrevPos;
    private Vector3 _anchorVelFiltered;
    private float _holdDistance;

    private Levitatable _target;
    private Rigidbody _rb;
    private SpringJoint _joint;

    private void Start()
    {
        if (!cam) cam = Camera.main;

        var anchorGO = new GameObject("GrabAnchor");
        _anchor = anchorGO.transform;
        _anchorRB = anchorGO.AddComponent<Rigidbody>();
       

        _holdDistance = Mathf.Clamp(defaultHoldDistance, minHoldDistance, maxHoldDistance);
    }

    private void Update()
    {
        // Adjust hold distance with scroll wheel
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.0001f)
            _holdDistance = Mathf.Clamp(_holdDistance + scroll * scrollSensitivity, minHoldDistance, maxHoldDistance);

        Ray cursorRay = cam.ScreenPointToRay(Input.mousePosition);
        Vector3 desiredAnchor = cursorRay.origin + cursorRay.direction * _holdDistance;

        // Prevent anchor from going through walls
        if (preventThroughWall && Physics.Raycast(cursorRay, out RaycastHit hit, _holdDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            //if not dragging this same rigidbody, stop the anchor in front of the wall
            if (_target == null || hit.rigidbody != _rb)
                desiredAnchor = hit.point - cursorRay.direction * 0.1f;
        }

        _anchor.position = _anchor.position == Vector3.zero
            ? desiredAnchor
            : Vector3.Lerp(_anchor.position, desiredAnchor, anchorLerp);

        if (Input.GetKeyDown(grabKey)) TryAcquireTarget();
        else if (Input.GetKeyUp(grabKey)) Release();

        HighlightCandidate();
    }

    private void FixedUpdate()
    {
        Vector3 rawVel = (_anchor.position - _anchorPrevPos) / Mathf.Max(Time.fixedDeltaTime, 1e-5f);
        _anchorVelFiltered = Vector3.Lerp(_anchorVelFiltered, rawVel, anchorVelSmoothing);
        _anchorPrevPos = _anchor.position;

        if (_rb != null)
        {
            _rb.maxAngularVelocity = maxAngularVelocity;

            // If the anchor is nearly stationary and the body is very close to the anchor, kill residual jiggle
            float distToAnchor = Vector3.Distance(_rb.worldCenterOfMass, _anchor.position);
            if (distToAnchor < steadyDistanceEps && _anchorVelFiltered.magnitude < steadySpeedEps)
            {
                _rb.linearVelocity = Vector3.zero;
                _rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private void TryAcquireTarget()
    {
        if (_target != null) return;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.SphereCast(ray, aimSphereRadius, out RaycastHit hitInfo, maxRayDistance,
    ~0, QueryTriggerInteraction.Ignore)) return;

        // 只有带 HittableTag 的对象能被抓
        if (!hitInfo.collider.GetComponentInParent<HittableTag>()) return;
        var cand = hitInfo.collider.GetComponentInParent<Levitatable>();
        if (cand == null) return;

        float dist = Vector3.Distance(cam.transform.position, hitInfo.point);
        if (dist > cand.maxGrabDistance) return;

        _target = cand;
        _rb = cand.GetComponent<Rigidbody>();
        //_rb.useGravity = true;

        
        _rbOldInterpolation = _rb.interpolation;                         
        _rbOldCollisionDetectionMode = _rb.collisionDetectionMode;      
        _rb.interpolation = RigidbodyInterpolation.Interpolate;          
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous; 

    
        _rbWasKinematic = _rb.isKinematic;   
        _rb.isKinematic = false;            

        // Freeze rotation while holding to stop wobble/auto-spins
        _rbOldConstraints = _rb.constraints;
        _rbOldAngularDrag = _rb.angularDamping;
        _rbOldDrag = _rb.linearDamping;
        _rb.constraints |= RigidbodyConstraints.FreezeRotation;
        _rb.angularDamping = Mathf.Max(_rb.angularDamping, 10f); // heavier damping while held
        _rb.linearDamping = Mathf.Max(_rb.linearDamping, holdExtraDrag); // stronger linear damping while held

        // Small upward nudge so the object feels like it lifts when grabbed
        _rb.AddForce(Vector3.up * Mathf.Clamp(2f * _rb.mass, 0f, 30f), ForceMode.Impulse);

        _target.SetHighlighted(true);
        _holdDistance = Mathf.Min(_holdDistance, cand.maxHoldDistance);

        _joint = _rb.gameObject.AddComponent<SpringJoint>();
        _joint.autoConfigureConnectedAnchor = false;
        _joint.connectedBody = _anchorRB;
        _joint.anchor = Vector3.zero;
        _joint.connectedAnchor = Vector3.zero;
        _joint.spring = spring * cand.massScale;
        _joint.damper = damper * cand.massScale;
        _joint.maxDistance = jointMaxDistance;
        _joint.minDistance = 0f;
        _joint.tolerance = 0.01f;
        _joint.enableCollision = true;
        _joint.enablePreprocessing = true;
    }

    private void Release()
    {
        if (_target == null) return;
        if (_joint) Destroy(_joint);

        if (_rb)
        {
            
            if (!_rbWasKinematic)                                           
            {
                Vector3 throwVel = _anchorVelFiltered + cam.transform.forward * forwardThrowBoost;
                _rb.linearVelocity = throwVel;
                _rb.angularVelocity *= 0.8f;
            }

            // Restore original rotation freedoms and damping
            _rb.constraints = _rbOldConstraints;
            _rb.angularDamping = _rbOldAngularDrag;
            _rb.linearDamping = _rbOldDrag;

           
            _rb.isKinematic = _rbWasKinematic;                                
            _rb.interpolation = _rbOldInterpolation;                          
            _rb.collisionDetectionMode = _rbOldCollisionDetectionMode;       
        }

        _target.SetHighlighted(false);
        _target = null;
        _rb = null;
        _joint = null;
    }

    private void HighlightCandidate()
    {
        if (_target != null) return;

        var ray = cam.ScreenPointToRay(Input.mousePosition);
        if (Physics.SphereCast(ray, aimSphereRadius, out RaycastHit hitInfo, maxRayDistance,
            hittableLayers, QueryTriggerInteraction.Ignore))
        {
            var cand = hitInfo.collider.GetComponentInParent<Levitatable>();
            cand?.SetHighlighted(true);
        }
    }

    private void OnDisable()
    {
        if (_joint) Destroy(_joint);
        if (_target) _target.SetHighlighted(false);
    }
}