
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

    // Stats
    public int currentKills = 0;
    public int currentDeaths = 0;

    // Input
    private Vector2 moveDirection;

    // Movement variables
    private readonly int moveSpeed = 4500;
    public LayerMask whatIsGround;
    public bool isGrounded;

    // Ground Check
    public Transform groundCheck;
    public float groundDistance = 1;

    // Gravity variables (Disable use gravity for rigid body)
    public LayerMask whatIsGravityObject;
    public float gravityMaxDistance = 500;
    public float gravityForce = 4500;
    public float maxDistanceFromOrigin = 600, forceBackToOrigin = 4500f;
    public Quaternion lastOrientationRotation;

    // JetPack 
    public float jetPackForce = 20;
    public bool isJetPackRecoveryActive = true;
    public float maxJetPackPower = 2.1f;
    public float currentJetPackPower;
    public float jetPackBurstCost = .5f;
    public float jetPackRecoveryIncrementor = .1f;

    // Grappling Variables ==================
    // Components
    private SpringJoint joint;
    public LayerMask whatIsGrapple;

    // Numerical variables
    private float maxGrappleDistance = 10000f, minGrappleDistance = 5f;
    public float maxGrappleTime = 1000f, grappleRecoveryIncrement = 50f;
    public float timeLeftToGrapple, grappleTimeLimiter;

    public bool IsGrappleRecoveryInProgress { get; set; } = false;
    public bool IsGrappling { get; private set; }
    public Vector3 GrapplePoint { get; private set; }

    // Magnetize
    public int magnetizeForce = 100;

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
        currentJetPackPower = maxJetPackPower;
        timeLeftToGrapple = maxGrappleTime;
        grappleTimeLimiter = maxGrappleTime / 4;
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
            accuaracyOffset = 0,
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
        GravityController();

        if (isGrounded)
            Movement();
        else if (moveDirection.x > 0)
            JetPackThrust(orientation.right);
        else if (moveDirection.x < 0)
            JetPackThrust(-orientation.right);
        else if (moveDirection.y > 0)
            JetPackThrust(orientation.forward);
        else if (moveDirection.y < 0)
            JetPackThrust(-orientation.forward);
        if (IsGrappling)
        {
            if (timeLeftToGrapple > 0)
                ContinueGrapple();
            else
                StopGrapple();
        }

        if (currentJetPackPower < maxJetPackPower)
        {
            currentJetPackPower += jetPackRecoveryIncrementor;
            ServerSend.PlayerContinueJetPack(id, currentJetPackPower);
        }
    }

    private void Update()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, whatIsGround);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.collider.CompareTag("GravityObject"))
        {
            rb.velocity /= 2;
        }
    }


    /// <summary>Updates the player input with newly received input.</summary>
    /// <param name="_inputs">The new key inputs.</param>
    /// <param name="_rotation">The new rotation.</param>
    public void SetMovementInput(Vector2 _moveDirection, Quaternion _rotation)
    {
        moveDirection = _moveDirection;

        orientation.localRotation = _rotation;
    }

    public void SetActionInput(bool _isAnimInProgress)
    {
        isAnimInProgress = _isAnimInProgress;
    }

    #region Movement

    /// <summary>Calculates the player's desired movement direction and moves him.</summary>
    /// <param name="_inputDirection"></param>
    private void Movement()
    {
        rb.AddForce(orientation.forward * moveDirection.y * moveSpeed * Time.deltaTime);
        rb.AddForce(orientation.right * moveDirection.x * moveSpeed * Time.deltaTime);

        // Stick to ground object
        Collider[] _groundCollider = Physics.OverlapSphere(transform.position, groundDistance * 2, whatIsGround);
        rb.AddForce((_groundCollider[0].transform.position - transform.position) * gravityForce * 3 * Time.deltaTime);


        SendPlayerData();
    }

    #endregion

    #region JetPack

    public void JetPackThrust(Vector3 _direction)
    {
        if (currentJetPackPower < jetPackBurstCost) return;
        currentJetPackPower -= jetPackBurstCost;

        rb.AddForce(_direction * jetPackForce * 50 * Time.deltaTime, ForceMode.Impulse);
        SendPlayerData();
    }

    public void JetPackMovement(Vector3 _direction)
    {
        if(isGrounded)
            rb.AddForce(_direction * jetPackForce * 5 * Time.deltaTime, ForceMode.Impulse);
        //ServerSend.PlayerContinueJetPack(id, currentJetPackTime);
        else 
            rb.AddForce(_direction * jetPackForce * Time.deltaTime, ForceMode.Impulse);
        SendPlayerData();
    }
    #endregion

    #region Magnetize 
    
    public void PlayerMagnetize()
    {
        rb.velocity = Vector3.zero;
        Vector3 _desiredPosition = FindNearestGravityObjectPosition();

        rb.AddForce((_desiredPosition - transform.position) * magnetizeForce * Time.deltaTime, ForceMode.Impulse);
    }

    public Vector3 FindNearestGravityObjectPosition()
    {
        float _checkingDistance = 100, _errorCatcher = 0;
        Collider[] _gravityObjectColiiders;
        do
        {
            _gravityObjectColiiders = Physics.OverlapSphere(transform.position, _checkingDistance, whatIsGravityObject);
            _checkingDistance += 500;
            _errorCatcher++;
            if (_errorCatcher > 10)
                return Vector3.zero; // Return to sender (Origin) (This prevents infinite loop )
        } while (_gravityObjectColiiders.Length == 0);

        Transform _nearestGravityObject = _gravityObjectColiiders[0].transform;
        float _lastDistance = 100000;   // garbage value
        // Find closest gravity object
        foreach (Collider _gravityObject in _gravityObjectColiiders)
        {
            if (Vector3.Distance(_gravityObject.transform.position, transform.position) < _lastDistance)
            {
                _lastDistance = Vector3.Distance(_gravityObject.transform.position, transform.position);
                _nearestGravityObject = _gravityObject.transform;
            }
        }
        return _nearestGravityObject.transform.position;
    }

    #endregion

    #region Artificial Gravity

    public void GravityController()
    {
        if (transform.position.magnitude > maxDistanceFromOrigin)
        {
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

    public void UpdateShootDirection(Vector3 _firePoint, Vector3 _fireDirection)
    {
        firePoint = _firePoint;
        fireDirection = _fireDirection;
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
            if (Physics.Raycast(ray, out RaycastHit _hit, currentGun.range, whatIsShootable))
            {
                ServerSend.PlayerShotLanded(id, _hit.point);
                if (_hit.collider.CompareTag("Player"))
                    _hit.collider.GetComponent<Player>().TakeDamage(id, currentGun.damage);
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
                if (Physics.Raycast(ray, out RaycastHit _hit, currentGun.range, whatIsShootable))
                {
                    ServerSend.PlayerShotLanded(id, _hit.point);
                    if (_hit.collider.CompareTag("Player"))
                        _hit.collider.GetComponent<Player>().TakeDamage(id, currentGun.damage);
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
        if (Physics.Raycast(ray, out RaycastHit _hit, currentGun.range, whatIsShootable))
        {
            ServerSend.PlayerShotLanded(id, _hit.point);
            if (_hit.collider.CompareTag("Player"))
                _hit.collider.GetComponent<Player>().TakeDamage(id, currentGun.damage);
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
        foreach(Client _client in Server.clients.Values)
        {
            if (_client.player != null)
            {
                if (_client.id != id)
                {
                    ServerSend.OtherPlayerSwitchedWeapon(id, _client.id, currentGun.name);
                }
            }
        }
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
        if (timeLeftToGrapple < grappleTimeLimiter)
            return;
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
            if (joint != null)
                Destroy(joint);
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
        SendPlayerData();
        timeLeftToGrapple -= Time.deltaTime;
        ServerSend.PlayerContinueGrapple(id, timeLeftToGrapple);
        if (timeLeftToGrapple < 0)
            StopGrapple();
    
        // Pull player to grapple point
        Vector3 direction = (GrapplePoint - transform.position).normalized;
        rb.AddForce(direction * 50 * Time.deltaTime, ForceMode.Impulse);

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
        ServerSend.PlayerContinueGrapple(id, timeLeftToGrapple);
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
            currentDeaths++;
            // Teleport to random spawnpoint
            transform.position = EnvironmentGenerator.spawnPoints[
                                 Random.Range(0, EnvironmentGenerator.spawnPoints.Count)];
            ServerSend.PlayerPosition(this);
            StartCoroutine(Respawn());
        }

        ServerSend.PlayerHealth(this);
    }

    public void TakeDamage(int _fromId, float _damage)
    {
        if (health <= 0)
            return;

        health -= _damage;

        if (health <= 0)
        {
            health = 0;
            currentDeaths++;
            ServerSend.UpdatePlayerDeathStats(id, currentDeaths);
            Server.clients[_fromId].player.currentKills++;
            ServerSend.UpdatePlayerKillStats(_fromId, Server.clients[_fromId].player.currentKills);
            // Teleport to random spawnpoint
            transform.position = EnvironmentGenerator.spawnPoints[
                                 Random.Range(0, EnvironmentGenerator.spawnPoints.Count)];
            ServerSend.PlayerPosition(this);
            StartCoroutine(Respawn());
        }

        ServerSend.PlayerHealth(this);
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(5f);

        health = maxHealth;
        ServerSend.PlayerRespawned(this);
    }

    #endregion

    public void SendPlayerData()
    {
        ServerSend.PlayerPosition(this);
        ServerSend.PlayerRotation(this, orientation.localRotation);
    }
}