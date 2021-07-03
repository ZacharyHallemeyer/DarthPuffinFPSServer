
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // Generic variables
    public int id;
    public string username;
    public float health;
    public float maxHealth = 100;
    public Rigidbody rb;
    public Transform orientation;

    // Input
    private bool[] inputsBool;
    private Vector2[] inputsVector2;

    // Movement variables
    private readonly int moveSpeed = 4500;
    private readonly int maxBaseSpeed = 20;
    public LayerMask whatIsGround;
    public bool isGrounded;

    // Ground Check
    public Transform groundCheck;
    public float groundDistance = 1;

    // Camera
    public Transform playerCamPosition;
    private float mouseX;
    private float mouseY;
    private float xRotation;
    private float desiredX;
    private float normalFOV = 60;
    private float adsFOV = 40;
    public float sensitivity = 2000f;
    private float normalSensitivity = 2000f;
    private float adsSensitivity = 500f;
    // Default value for sens multipliers are 1 

    public float sensMultiplier { get; set; } = 1f;
    public float adsSensMultiplier { get; set; } = 1f;

    // Gravity variables (Disable use gravity for rigid body)
    public LayerMask whatIsGravityObject;
    public float gravityMaxDistance = 20;
    public float gravityForce = 4500;
    public float maxDistanceFromOrigin = 600, forceBackToOrigin = 4500f;
    public Quaternion lastOrientationRotation;

    // JetPack 
    public float jetPackForce = 1550f;
    public float maxJetPackTime = 5f;
    public float currentJetPackTime;
    public float jetPackTimeIncrementor = .01f;
    public float jetPackRecoveryRepeatTime = .01f;
    public float jetPackBurstCost = .5f;
    public bool isJetPackRecoveryActive = false;

    // Grappling Variables ==================
    // Components
    private SpringJoint joint;
    public LayerMask whatIsGrapple;

    // Numerical variables
    private float maxGrappleDistance = 10000f, minGrappleDistance = 5f;
    public float maxGrappleTime = 1000f, grappleRecoveryIncrement = 50f;
    public float timeLeftToGrapple;

    public bool IsGrappleRecoveryInProgress { get; set; } = false;
    public bool IsGrappling { get; private set; }
    public Vector3 GrapplePoint { get; private set; }

    private void Start()
    {
        maxDistanceFromOrigin = EnvironmentGenerator.BoundaryDistanceFromOrigin;
        currentJetPackTime = maxJetPackTime;
    }

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
        health = maxHealth;

        inputsBool = new bool[7];
        inputsVector2 = new Vector2[1];
    }

    /// <summary>Processes player input and moves the player.</summary>
    public void FixedUpdate()
    {
        Debug.DrawRay(transform.position, -orientation.up);
        if (health <= 0) return;
        GetInput();
        GravityController();
        if (IsGrappling)
        {
            if (timeLeftToGrapple > maxGrappleTime / 4)
                ContinueGrapple();
            else
            {
                Debug.Log("Stop Grapple called from fixed update");
                StopGrapple();
                ServerSend.PlayerStopGrapple(id);
            }
        }
    }

    private void Update()
    {
        // might move this line to fixed update because it is the only line in update
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, whatIsGround);
    }

    /// <summary>Updates the player input with newly received input.</summary>
    /// <param name="_inputs">The new key inputs.</param>
    /// <param name="_rotation">The new rotation.</param>
    public void SetInput(bool[] _inputsBools, Vector2[] _inputsVector2, Quaternion _rotation)
    {
        inputsBool = _inputsBools;
        inputsVector2 = _inputsVector2;

        orientation.localRotation = _rotation;
    }

    private void GetInput()
    {
        Vector2 _inputDirection = Vector2.zero;
        if (inputsBool[0]) // W
        {
            _inputDirection.y += 1;
        }
        if (inputsBool[1]) // S
        {
            _inputDirection.y -= 1;
        }
        if (inputsBool[2]) // A
        {
            _inputDirection.x -= 1;
        }
        if (inputsBool[3]) // D
        {
            _inputDirection.x += 1;
        }

        if (isGrounded)
            Movement(_inputDirection);
        else if (_inputDirection.x != 0 || _inputDirection.y != 0)
            JetPackMovement(_inputDirection);

        // JetPack
            // TODO
        // ADS
            // TODO
        ServerSend.PlayerPosition(this);
        //ServerSend.PlayerRotation(this);
    }

    #region Movement

    /// <summary>Calculates the player's desired movement direction and moves him.</summary>
    /// <param name="_inputDirection"></param>
    private void Movement(Vector2 _inputDirection)
    {
        rb.AddForce(orientation.forward * _inputDirection.y * moveSpeed * Time.deltaTime);
        rb.AddForce(orientation.right * _inputDirection.x * moveSpeed * Time.deltaTime);

        ServerSend.PlayerPosition(this);
        //ServerSend.PlayerRotation(this);
    }

    #region JetPack
    // if grounded - normal movement else jetpack
    public void JetPackMovement(Vector2 _inputDirection)
    {
        if (isJetPackRecoveryActive)
        {
            isJetPackRecoveryActive = false;
            CancelInvoke("JetPackRecovery");
        }

        currentJetPackTime -= Time.deltaTime;
        //playerUIScript.SetJetPack(currentJetPackTime);
        rb.AddForce(orientation.forward * _inputDirection.y * jetPackForce * Time.deltaTime);
        rb.AddForce(orientation.right * _inputDirection.x * jetPackForce * Time.deltaTime);

        ServerSend.PlayerPosition(this);
        //ServerSend.PlayerRotation(this);
    }

    public void JetPackUp()
    {
        if (isJetPackRecoveryActive)
        {
            isJetPackRecoveryActive = false;
            CancelInvoke("JetPackRecovery");
        }
        currentJetPackTime -= Time.deltaTime;
        //playerUIScript.SetJetPack(currentJetPackTime);
        rb.AddForce(orientation.up * jetPackForce * Time.deltaTime);
        
        ServerSend.PlayerPosition(this);
        //ServerSend.PlayerRotation(this);
    }

    public void JetPackDown()
    {
        if (isJetPackRecoveryActive)
        {
            isJetPackRecoveryActive = false;
            CancelInvoke("JetPackRecovery");
        }
        currentJetPackTime -= Time.deltaTime;
        //playerUIScript.SetJetPack(currentJetPackTime);
        rb.AddForce(-orientation.up * jetPackForce * Time.deltaTime);
        
        ServerSend.PlayerPosition(this);
        //ServerSend.PlayerRotation(this);
    }

    /// <summary>
    /// Use invoke repeating to call this method
    /// </summary>
    public void JetPackRecovery()
    {
        //playerUIScript.SetJetPack(currentJetPackTime);
        currentJetPackTime += jetPackTimeIncrementor;
        if (currentJetPackTime > maxJetPackTime)
        {
            isJetPackRecoveryActive = false;
            CancelInvoke("JetPackRecovery");
        }
    }
    #endregion

    #endregion

    #region Artificial Gravity

    public void GravityController()
    {
        if (transform.position.magnitude > maxDistanceFromOrigin)
        {
            //rb.velocity /= 2;
            rb.AddForce(-transform.position.normalized * forceBackToOrigin * Time.deltaTime);
            return;
        }

        if (IsGrappling) return;

        Transform[] _gravityObjects = FindGravityObjects();

        for (int i = 0; i < _gravityObjects.Length; i++)
        {
            if (_gravityObjects[i] != null)
            {
                ApplyGravity(_gravityObjects[i]);
            }
        }
    }

    public Transform[] FindGravityObjects()
    {
        int index = 0;

        Collider[] _gravityObjectColiiders = Physics.OverlapSphere(transform.position, gravityMaxDistance, whatIsGravityObject);
        Transform[] _gravityObjects = new Transform[_gravityObjectColiiders.Length];
        foreach (Collider _gravityObjectCollider in _gravityObjectColiiders)
        {
            _gravityObjects[index] = _gravityObjectCollider.transform;
            index++;
        }

        return _gravityObjects;
    }


    public void ApplyGravity(Transform _gravityObject)
    {
        rb.AddForce((_gravityObject.position - transform.position) * gravityForce * Time.deltaTime);

        // Rotate Player
        if (isGrounded )
        {
            // Rotate Player
            Quaternion desiredRotation = Quaternion.FromToRotation(_gravityObject.up, -(_gravityObject.position - transform.position).normalized);
            desiredRotation = Quaternion.Lerp(transform.localRotation, desiredRotation, Time.deltaTime * 2);

            rb.MoveRotation(desiredRotation);
            /*
            if(!Physics.Raycast(transform.position, -orientation.up, transform.localScale.y))
            {
                Debug.Log("This is running");
                rb.MoveRotation(Quaternion.Euler(
                                            desiredRotation.eulerAngles.x,
                                            desiredRotation.eulerAngles.y,
                                            desiredRotation.eulerAngles.z
                                            ));
            }
             */
            // Rotate camera
            // TODO (Maybe)

            // Add extra force to stick player to planet surface
            rb.AddForce((_gravityObject.position - transform.position) * gravityForce * 1 * Time.deltaTime);
        }
        ServerSend.PlayerPosition(this);
        //ServerSend.PlayerRotation(this);
        lastOrientationRotation = orientation.localRotation;
    }


    #endregion

    #region Weapons

    public void Shoot(Vector3 _shootDirection)
    {
        if (health <= 0) return;

        /*
        if (Physics.Raycast(shootOrigin.position, _shootDirection, out RaycastHit _hit, 25f))
        {
            if (_hit.collider.CompareTag("Player"))
                _hit.collider.GetComponent<Player>().TakeDamage(50f);
        }
        */
    }

    #endregion

    #region Grapple

    /// <summary>
    /// Turns off gravity, creates joints for grapple
    /// Call whenever player inputs for grapple
    /// Dependencies: DrawRope
    /// </summary>
    public void StartGrapple(Vector3 _direction)
    {
        Debug.Log("Start Grapple is called");
        Ray ray = new Ray(transform.position, _direction);
        Debug.Log(ray.ToString());
        if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, whatIsGrapple))
        {
            Debug.Log("Start Grapple started");
            if (Vector3.Distance(transform.position, hit.point) < minGrappleDistance)
                return;

            if (IsGrappleRecoveryInProgress)
            {
                IsGrappleRecoveryInProgress = false;
                CancelInvoke("GrappleRecovery");
            }

            IsGrappling = true;

            // Create joint ("Grapple rope") and anchor to player and grapple point
            GrapplePoint = hit.point;
            joint = transform.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = GrapplePoint;

            joint.spring = 0f;
            joint.damper = 0f;
            joint.massScale = 0f;

            ServerSend.PlayerStartGrapple(id);
        }
        //else
            //StopGrapple();
    }

    /// <summary>
    /// Call every frame while the player is grappling
    /// </summary>
    public void ContinueGrapple()
    {
        timeLeftToGrapple -= Time.deltaTime;
        if (timeLeftToGrapple < 0)
        {
            StopGrapple();
        }

        // Pull player to grapple point
        Vector3 direction = (GrapplePoint - transform.position).normalized;
        rb.AddForce(direction * 100 * Time.deltaTime, ForceMode.Impulse);

        // Prevent grapple from phasing through/into objectsz
        // (Game objects such as buildings must have a rotation for this section to work)
        if (Physics.Raycast(GrapplePoint, (transform.position - GrapplePoint), Vector3.Distance(GrapplePoint, transform.position) - 5, whatIsGrapple))
        {
            StopGrapple();
        }
        ServerSend.PlayerPosition(this);
    }

    /// <summary>
    /// Erases grapple rope, turns player gravity on, and destroys joint
    /// </summary>
    public void StopGrapple()
    {
        if (!IsGrappleRecoveryInProgress)
        {
            IsGrappleRecoveryInProgress = true;
            InvokeRepeating("GrappleRecovery", 0f, .1f);
        }
        IsGrappling = false;
        Destroy(joint);
        ServerSend.PlayerStopGrapple(id);
    }


    /// <summary>
    /// adds time to player's amount of grapple left. Must be called through invoke repeating with
    /// a repeat time of .1 seconds of scaled time
    /// </summary>
    public void GrappleRecovery()
    {
        if (timeLeftToGrapple <= maxGrappleTime)
        {
            timeLeftToGrapple += grappleRecoveryIncrement;
        }
        else
            CancelInvoke("GrappleRecovery");
    }

    #endregion

    #region Stats and Generic

    public void TakeDamage(float _damage)
    {
        if (health <= 0)
            return;

        health -= _damage;

        if (health <= 0)
        {
            health = 0;
            //controller.enabled = false;
            transform.position = new Vector3(0, 25f, 0);
            ServerSend.PlayerPosition(this);
            StartCoroutine(Respawn());
        }

        ServerSend.PlayerHealth(this);
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(5f);

        health = maxHealth;
        //controller.enabled = true;
        ServerSend.PlayerRespawned(this);
    }

    #endregion
}

/*
if (Physics.Raycast(transform.position, Vector3.up, distance1))
{
    Debug.Log("UP!");
    Debug.DrawRay(transform.position, Vector3.up);
    transform.localRotation = Quaternion.Euler(
                                            desiredRotation.eulerAngles.x,
                                            transform.localRotation.y,
                                            desiredRotation.eulerAngles.z
                                            );
}
if (Physics.Raycast(transform.position, Vector3.down, distance1))
{
    Debug.Log("DOWN!");
    Debug.DrawRay(transform.position, Vector3.down);
    transform.localRotation = Quaternion.Euler(
                                          desiredRotation.eulerAngles.x,
                                          transform.localEulerAngles.y,
                                          desiredRotation.eulerAngles.z
                                         );
}
if(Physics.Raycast(transform.position, Vector3.left, distance2))
{
    Debug.Log("LEFT!");
    Debug.DrawRay(transform.position, Vector3.left);
    transform.localRotation = Quaternion.Euler(
                                          transform.localRotation.x,
                                          desiredRotation.eulerAngles.y,
                                          desiredRotation.eulerAngles.z
                                         );
}
if (Physics.Raycast(transform.position, Vector3.right, distance2))
{
    Debug.Log("RIGHT!");
    Debug.DrawRay(transform.position, Vector3.right);
    transform.localRotation = Quaternion.Euler(
                                          transform.localEulerAngles.x,
                                          desiredRotation.eulerAngles.y,
                                          desiredRotation.eulerAngles.z
                                         );
}
if(Physics.Raycast(transform.position, Vector3.forward, distance2))
{
    Debug.Log("FORWARD!");
    Debug.DrawRay(transform.position, Vector3.forward);
    transform.localRotation = Quaternion.Euler(
                                          desiredRotation.eulerAngles.x,
                                          desiredRotation.eulerAngles.y,
                                          transform.localRotation.z
                                         );
}
if (Physics.Raycast(transform.position, Vector3.back, distance2))
{
    Debug.Log("BACK!");
    Debug.DrawRay(transform.position, Vector3.back);
    transform.localRotation = Quaternion.Euler(
                                          desiredRotation.eulerAngles.x,
                                          desiredRotation.eulerAngles.y,
                                          transform.localEulerAngles.z
                                         );
}
*/