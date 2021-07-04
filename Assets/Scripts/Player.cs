﻿
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    #region Variables
    // Generic variables
    public int id;
    public string username;
    public float health;
    public float maxHealth = 100;
    public Rigidbody rb;
    public Transform orientation;

    // Input
    private Vector2 moveDirection;

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

    // Gun Variables

    public class GunInformation
    {
        public string name;
        public GameObject gunContainer;
        public ParticleSystem bullet;
        public ParticleSystem gun;
        public float originalGunRadius;
        public int magSize;
        public int ammoIncrementor;
        public int reserveAmmo;
        public int currentAmmo;
        public float damage;
        public float fireRate;
        public float accuaracyOffset;
        public float reloadTime;
        public float range;
        public float rightHandPosition;
        public float leftHandPosition;
        public bool isAutomatic;
    }

    public Dictionary<string, GunInformation> allGunInformation { get; private set; } = new Dictionary<string, GunInformation>();

    //public float timeSinceLastShoot = 5f;       // Garbage value

    // Guns
    public GunInformation currentGun;
    public GunInformation secondaryGun;

    public string[] gunNames;

    public LayerMask whatIsShootable;
    public bool isShooting = false;

    private Vector3 firePoint;
    private Vector3 fireDirection;

    public bool isAnimInProgress;

    #endregion

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

        SetGunInformation();
    }

    public void SetGunInformation()
    {
        allGunInformation["Pistol"] = new GunInformation
        {
            name = "Pistol",
            magSize = 6,
            ammoIncrementor = 60,
            reserveAmmo = 60,
            currentAmmo = 6,
            damage = 30,
            fireRate = .7f,
            accuaracyOffset = .001f,
            reloadTime = 1f,
            range = 1000f,
            rightHandPosition = -.3f,
            leftHandPosition = -1.5f,
            isAutomatic = false,
        };

        allGunInformation["SMG"] = new GunInformation
        {
            name = "SMG",
            magSize = 30,
            ammoIncrementor = 300,
            reserveAmmo = 300,
            currentAmmo = 30,
            damage = 10,
            fireRate = .1f,
            accuaracyOffset = .025f,
            reloadTime = 1f,
            range = 1000f,
            rightHandPosition = -.3f,
            leftHandPosition = -1.5f,
            isAutomatic = true,
        };

        allGunInformation["AR"] = new GunInformation
        {
            name = "AR",
            magSize = 20,
            ammoIncrementor = 260,
            reserveAmmo = 260,
            currentAmmo = 20,
            damage = 20,
            fireRate = .2f,
            accuaracyOffset = .02f,
            reloadTime = 1f,
            range = 1000f,
            rightHandPosition = -.3f,
            leftHandPosition = -1.5f,
            isAutomatic = true,
        };

        allGunInformation["Shotgun"] = new GunInformation
        {
            name = "Shotgun",
            magSize = 8,
            ammoIncrementor = 80,
            reserveAmmo = 80,
            currentAmmo = 8,
            damage = 10,
            fireRate = .7f,
            accuaracyOffset = .1f,
            reloadTime = 1f,
            range = 75f,
            rightHandPosition = -.3f,
            leftHandPosition = -.3f,
            isAutomatic = false,
        };

        int index = 0;
        gunNames = new string[allGunInformation.Count];
        foreach (string str in allGunInformation.Keys)
        {
            gunNames[index] = str;
            index++;
        }

        do
        {
            currentGun = allGunInformation[gunNames[Random.Range(0, gunNames.Length)]];
            secondaryGun = allGunInformation[gunNames[Random.Range(0, gunNames.Length)]];
        } while (currentGun.name.Equals(secondaryGun.name));
    }


    /// <summary>Processes player input and moves the player.</summary>
    public void FixedUpdate()
    {
        if (health <= 0) return;
        GetInput();
        GravityController();
        if (IsGrappling)
        {
            if (timeLeftToGrapple > maxGrappleTime / 4)
                ContinueGrapple();
            else
                StopGrapple();
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
    public void SetInput(Vector2 _moveDirection, Quaternion _rotation, bool _isAnimInProgress)
    {
        moveDirection = _moveDirection;

        orientation.localRotation = _rotation;
        isAnimInProgress = _isAnimInProgress;
    }

    private void GetInput()
    {
        if (isGrounded)
            Movement(moveDirection);
        else if (moveDirection.x != 0 || moveDirection.y != 0)
            JetPackMovement(moveDirection);

        // JetPack
            // TODO
    }

    #region Movement

    /// <summary>Calculates the player's desired movement direction and moves him.</summary>
    /// <param name="_inputDirection"></param>
    private void Movement(Vector2 _inputDirection)
    {
        rb.AddForce(orientation.forward * _inputDirection.y * moveSpeed * Time.deltaTime);
        rb.AddForce(orientation.right * _inputDirection.x * moveSpeed * Time.deltaTime);

        SendPlayerData();
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

        SendPlayerData();
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

        SendPlayerData();
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
        
        SendPlayerData();
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
            SendPlayerData();
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
        SendPlayerData();
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
            transform.localRotation = desiredRotation;
            // Add extra force to stick player to planet surface
            rb.AddForce((_gravityObject.position - transform.position) * gravityForce * 1 * Time.deltaTime);
        }
        lastOrientationRotation = orientation.localRotation;
    }


    #endregion

    #region Weapons

    public void ShootController(Vector3 _firePoint, Vector3 _fireDirection)
    {
        if (health <= 0) return;
        if (isAnimInProgress) return;

        firePoint = _firePoint;
        fireDirection = _fireDirection;

        if(!isShooting)
        {
            if (currentGun.isAutomatic)
                StartAutomaticFire();
            else
                SingleFireShoot();
        }
    }

    public void StopShootContoller()
    {
        StopAutomaticShoot();
    }

    public void SingleFireShoot()
    {
        isAnimInProgress = true;
        currentGun.currentAmmo--;
        ServerSend.PlayerSingleFire(id, currentGun.currentAmmo, currentGun.reserveAmmo);
        // Reduce accuracy by a certain value 
        Vector3 reduceAccuracy = fireDirection + new Vector3(Random.Range(-currentGun.accuaracyOffset, currentGun.accuaracyOffset),
                                                                Random.Range(-currentGun.accuaracyOffset, currentGun.accuaracyOffset));

        // Reload if current ammo is zero
        if (currentGun.currentAmmo <= 0)
        {
            if (currentGun.reserveAmmo > 0)
            {
                Reload();
            }
        }

        if (currentGun.name == "Pistol")
        {
            Ray ray = new Ray(firePoint, reduceAccuracy);
            if (Physics.Raycast(ray, out RaycastHit hit, currentGun.range, whatIsShootable))
            {
                // TODO Inflict damage
            }
        }
        // shotgun
        else
        {
            Vector3 trajectory;
            for (int i = 0; i < 10; i++)
            {
                trajectory = fireDirection + new Vector3(Random.Range(-currentGun.accuaracyOffset, currentGun.accuaracyOffset),
                                                                Random.Range(-currentGun.accuaracyOffset, currentGun.accuaracyOffset),
                                                                Random.Range(-currentGun.accuaracyOffset, currentGun.accuaracyOffset));
                Ray ray = new Ray(firePoint, trajectory);
                if (Physics.Raycast(ray, out RaycastHit hit, currentGun.range, whatIsShootable))
                {
                    // TODO Inflict damage
                }
            }
        }
        // Reload if current ammo is zero
        if (currentGun.currentAmmo <= 0)
        {
            if (currentGun.reserveAmmo > 0)
            {
                Reload();
            }
        }
    }

    public void StartAutomaticFire()
    {
        isShooting = true;
        ServerSend.PlayerStartAutomaticFire(id, currentGun.currentAmmo, currentGun.reserveAmmo);
        InvokeRepeating("AutomaticShoot", 0f, currentGun.fireRate);
    }

    public void AutomaticShoot()
    {
        // Reduce accuracy by a certain value 
        currentGun.currentAmmo--;
        ServerSend.PlayerContinueAutomaticFire(id, currentGun.currentAmmo, currentGun.reserveAmmo);
        Vector3 reduceAccuracy = fireDirection + new Vector3(Random.Range(-currentGun.accuaracyOffset, currentGun.accuaracyOffset),
                                                                Random.Range(-currentGun.accuaracyOffset, currentGun.accuaracyOffset));


        // Reload if current ammo is zero
        if (currentGun.currentAmmo <= 0)
        {
            StopAutomaticShoot();
            if (currentGun.reserveAmmo > 0)
                Reload();
        }


        Ray ray = new Ray(firePoint, reduceAccuracy);
        if (Physics.Raycast(ray, out RaycastHit hit, currentGun.range, whatIsShootable))
        {
            // TODO Inflict damage
        }
    }

    public void StopAutomaticShoot()
    {
        ServerSend.PlayerStopAutomaticFire(id);
        CancelInvoke("AutomaticShoot");
        isShooting = false;
    }

    public void Reload()
    {
        if (isShooting && currentGun.isAutomatic)
            StopAutomaticShoot();

        // Reload gun
        if (currentGun.reserveAmmo > currentGun.magSize)
        {
            currentGun.reserveAmmo += -currentGun.magSize + currentGun.currentAmmo;
            currentGun.currentAmmo = currentGun.magSize;
        }
        else
        {
            if (currentGun.magSize - currentGun.currentAmmo <= currentGun.reserveAmmo)
            {
                currentGun.reserveAmmo -= currentGun.magSize - currentGun.currentAmmo;
                currentGun.currentAmmo = currentGun.magSize;
            }
            else
            {
                currentGun.currentAmmo += currentGun.reserveAmmo;
                currentGun.reserveAmmo = 0;
            }
        }
        ServerSend.PlayerReload(id, currentGun.currentAmmo, currentGun.reserveAmmo);
    }

    public void SwitchWeapon()
    {
        StopAutomaticShoot();
        isAnimInProgress = true;

        GunInformation temp = currentGun;
        currentGun = secondaryGun;
        secondaryGun = temp;

        ServerSend.PlayerSwitchWeapon(id, currentGun.name, currentGun.currentAmmo, currentGun.reserveAmmo);
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
        Ray ray = new Ray(transform.position, _direction);
        if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, whatIsGrapple))
        {
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
    }

    /// <summary>
    /// Call every frame while the player is grappling
    /// </summary>
    public void ContinueGrapple()
    {
        timeLeftToGrapple -= Time.deltaTime;
        if (timeLeftToGrapple < 0)
            StopGrapple();
    
        // Pull player to grapple point
        Vector3 direction = (GrapplePoint - transform.position).normalized;
        rb.AddForce(direction * 100 * Time.deltaTime, ForceMode.Impulse);

        // Prevent grapple from phasing through/into objectsz
        // (Game objects such as buildings must have a rotation for this section to work)
        if (Physics.Raycast(GrapplePoint, (transform.position - GrapplePoint), Vector3.Distance(GrapplePoint, transform.position) - 5, whatIsGrapple))
            StopGrapple();
        SendPlayerData();
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

    public void SendPlayerData()
    {
        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this, orientation.localRotation);
    }
}