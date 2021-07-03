
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
    private readonly Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
    public LayerMask whatIsGravityObject;
    public float gravityMaxDistance = 20;
    public float gravityForce = 4500;
    public float maxDistanceFromOrigin = 600, forceBackToOrigin = 4500f;

    // JetPack 
    public float jetPackForce = 1550f;
    public float maxJetPackTime = 5f;
    public float currentJetPackTime;
    public float jetPackTimeIncrementor = .01f;
    public float jetPackRecoveryRepeatTime = .01f;
    public float jetPackBurstCost = .5f;
    public bool isJetPackRecoveryActive = false;

    private void Start()
    {
        currentJetPackTime = maxJetPackTime;
    }

    public void Initialize(int _id, string _username)
    {
        id = _id;
        username = _username;
        health = maxHealth;

        inputsBool = new bool[5];
        inputsVector2 = new Vector2[1];
    }

    /// <summary>Processes player input and moves the player.</summary>
    public void FixedUpdate()
    {
        if (health <= 0) return;
        GetInput();
        GravityController();
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

        //transform.localRotation = Quaternion.Euler(_rotation.eulerAngles.x , transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
        //orientation.localRotation = Quaternion.Euler(_rotation.eulerAngles.x, transform.rotation.eulerAngles.y, transform.rotation.eulerAngles.z);
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

        // Look
        //Look();

        // JetPack
            // TODO
        // ADS
            // TODO
        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);
    }

    #region Movement

    /// <summary>Calculates the player's desired movement direction and moves him.</summary>
    /// <param name="_inputDirection"></param>
    private void Movement(Vector2 _inputDirection)
    {
        rb.AddForce(orientation.forward * _inputDirection.y * moveSpeed * Time.deltaTime);
        rb.AddForce(orientation.right * _inputDirection.x * moveSpeed * Time.deltaTime);

        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this);
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
        ServerSend.PlayerRotation(this);
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
        ServerSend.PlayerRotation(this);
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
        ServerSend.PlayerRotation(this);
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

    #region Player Camera

    public void Look()
    {
        Vector3 rot = playerCamPosition.localRotation.eulerAngles;
        desiredX = rot.y + inputsVector2[0].x;

        //Rotate, and also make sure we dont over- or under-rotate.
        xRotation -= inputsVector2[0].y;
        xRotation = Mathf.Clamp(xRotation, -85f, 85f);

        // Perform the rotations
        playerCamPosition.localRotation = Quaternion.Euler(xRotation, desiredX, 0);
        orientation.localRotation = Quaternion.Euler(0, desiredX, 0);
    }

    #endregion

    #region Artificial Gravity

    public void GravityController()
    {
        ServerSend.PlayerRotation(this);
        ServerSend.PlayerPosition(this);

        if (transform.position.magnitude > 600)
        {
            rb.AddForce(-transform.position.normalized * forceBackToOrigin * Time.deltaTime);
            return;
        }

        //if (playerActions.IsGrappling) return;

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
        //if(grounded && ((mouseX == 0 && mouseY == 0) || !Physics.Raycast(transform.position, -transform.up, 5f)))
        if (isGrounded)
        {
            // Rotate Player
            Quaternion desiredRotation = Quaternion.FromToRotation(_gravityObject.up, -(_gravityObject.position - transform.position).normalized);
            //transform.rotation = Quaternion.Lerp(transform.rotation, desiredRotation, Time.deltaTime * 5);
            desiredRotation = Quaternion.Lerp(transform.localRotation, desiredRotation, Time.deltaTime * 5);
            transform.localRotation = Quaternion.Euler(
                                                  desiredRotation.eulerAngles.x,
                                                  desiredRotation.eulerAngles.y,
                                                  desiredRotation.eulerAngles.z
                                                 ) ;
            // Rotate camera
                // TODO (Maybe)

            // Add extra force to stick player to planet surface
            rb.AddForce((_gravityObject.position - transform.position) * gravityForce * 1 * Time.deltaTime);
        }
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