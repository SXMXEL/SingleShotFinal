using Photon.Pun;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviourPunCallbacks, IPunObservable
{
    #region Variables

    [HideInInspector] public int Health => _currentHealth;
    [HideInInspector] public ProfileData PlayerProfile;
    [HideInInspector] public bool AwayTeam;
    
    [SerializeField] private float _speed;
    [SerializeField] private float _slideModifier;
    [SerializeField] private float _speedModifier;
    [SerializeField] private float _crouchModifier;
    [SerializeField] private float _jumpForce;
    [SerializeField] private float _jetForce;
    [SerializeField] private float _jetWait;
    [SerializeField] private float _jetRecovery;
    [SerializeField] private float _lengthOfSlide;
    [SerializeField] private int _maxHealth;
    [SerializeField] private float _maxFuel;
    [SerializeField] private TextMeshPro _playerUsername;
    [SerializeField] private Camera _normalCam;
    [SerializeField] private Camera _weaponCam;
    [SerializeField] private GameObject _cameraParent;
    [SerializeField] private Transform _weaponParent;
    [SerializeField] private Transform _groundDetector;
    [SerializeField] private LayerMask _ground;
    [SerializeField] private float _slideAmount;
    [SerializeField] private float _crouchAmount;
    [SerializeField] private GameObject _mesh;
    [SerializeField] private GameObject _standingCollider;
    [SerializeField] private GameObject _crouchingCollider;
    [SerializeField] private Renderer[] _teamIndicators;
    [SerializeField] private Weapon weapon;

    private Transform ui_healthBar;
    private Transform ui_fuelBar;
    private TextMeshProUGUI ui_ammo;
    private TextMeshProUGUI ui_username;
    private TextMeshProUGUI ui_team;

    private Rigidbody _rig;

    private Vector3 _weaponParentOrigin;
    private Vector3 _targetWeaponBobPosition;
    private Vector3 _weaponParentCurrentPos;

    private float _movementCounter;
    private float _idleCounter;

    private float _baseFOV;
    private float _sprintFOVModifier = 1.5f;
    private Vector3 _origin;

    private int _currentHealth;
    private float _currentFuel;
    private float _currentRecovery;

    private Manager _manager;

    private bool _crouched;
    private bool _sliding;
    private float _slideTime;
    private Vector3 _slideDir;

    private bool _isAiming;
    private bool _canJet;
    
    private float _aimAngle;

    private Vector3 _normalCamTarget;
    private Vector3 _weaponCamTarget;

    private Animator _anim;
    private static readonly int Horizontal = Animator.StringToHash("Horizontal");
    private static readonly int Vertical = Animator.StringToHash("Vertical");
    private HeadsUpDisplay _display;

    #endregion

    #region Photon Callbacks

    public void OnPhotonSerializeView(PhotonStream p_stream, PhotonMessageInfo p_info)
    {
        if (p_stream.IsWriting)
        {
            p_stream.SendNext((int) (_weaponParent.transform.localEulerAngles.x * 100f));
        }
        else
        {
            _aimAngle = (int) p_stream.ReceiveNext() / 100f;
        }
    }

    #endregion

    #region Monobehaviour Callbacks

    private void Start()
    {
        _manager = GameObject.Find("Manager").GetComponent<Manager>();
        
        _currentHealth = _maxHealth;
        _currentFuel = _maxFuel;
        _cameraParent.SetActive(photonView.IsMine);

        if (!photonView.IsMine)
        {
            gameObject.layer = 11;
            _standingCollider.layer = 11;
            _crouchingCollider.layer = 11;
            ChangeLayersRecursively(_mesh.transform, 11);
        }

        _baseFOV = _normalCam.fieldOfView;
        _origin = _normalCam.transform.localPosition;
        

        _rig = GetComponent<Rigidbody>();
        _weaponParentOrigin = _weaponParent.localPosition;
        _weaponParentCurrentPos = _weaponParentOrigin;

        if (photonView.IsMine)
        {
            ui_healthBar = GameObject.Find("HUD/Health/Bar").transform;
            ui_fuelBar = GameObject.Find("HUD/Fuel/Bar").transform;
            ui_ammo = GameObject.Find("HUD/Ammo/Text").GetComponent<TextMeshProUGUI>();
            ui_username = GameObject.Find("HUD/Username/Name").GetComponent<TextMeshProUGUI>();
            ui_team = GameObject.Find("HUD/Team/Text").GetComponent<TextMeshProUGUI>();
            
            // var headsUpDisplay = GameObject.Find("Canvas/HUD").GetComponent<HeadsUpDisplay>();
            // ui_healthBar = headsUpDisplay.HealthBar;
            // ui_healthBar = headsUpDisplay.FuelBar;
            // ui_ammo = headsUpDisplay.Ammo;
            // ui_username = headsUpDisplay.Username;
            // ui_team = headsUpDisplay.Team;
            
            RefreshHealthBar();
            ui_username.text = Launcher.MyProfile.Username;
            photonView.RPC("SyncProfile", RpcTarget.All, 
                Launcher.MyProfile.Username, Launcher.MyProfile.Level, Launcher.MyProfile.XP);
            if (GameSettings.GameMode == GameMode.TDM)
            {
                photonView.RPC("SyncTeam", RpcTarget.All, GameSettings.IsAwayTeam);
                if (!ui_team.gameObject.activeSelf)
                {
                    ui_team.gameObject.SetActive(true);
                }
                
                if (GameSettings.IsAwayTeam)
                {
                    ui_team.text = "Team red";
                    ui_team.color = Color.red;
                }
                else
                {
                    ui_team.text = "Team blue";
                    ui_team.color = Color.blue;
                }
            }
            else
            {
                ui_team.gameObject.SetActive(false);
            }
            
            _anim = GetComponent<Animator>();
        }
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            RefreshMultiplayerState();
            return;
        }
        
        // Axles
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");

        if (transform.position.y < -500)
        {
            photonView.RPC("TakeDamage", RpcTarget.All, 100,-1);
        }
        
        //Controls
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool crouch = Input.GetKeyDown(KeyCode.C);
        bool pause = Input.GetKeyDown(KeyCode.Escape);
        

        //States
        bool isGrounded = Physics.Raycast(_groundDetector.position, Vector3.down, 0.15f, _ground);
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && t_vmove > 0 && !isJumping && isGrounded;
        bool isCrouching = crouch && !isSprinting && !isJumping && isGrounded;

        if (Input.GetKeyDown(KeyCode.U))
        {
            photonView.RPC("TakeDamage", RpcTarget.All, 100,-1);
        }
        
        //Pause
        if (pause)
        {
            GameObject.Find("Pause").GetComponent<Pause>().TogglePause();
        }

        if (Pause.IsPaused)
        {
            t_hmove = 0;
            t_vmove = 0;
            sprint = false;
            crouch = false;
            pause = false;
            isGrounded = false;
            isJumping = false;
            isSprinting = false;
            isCrouching = false;
        }
        
        //Crouching
        if (isCrouching)
        {
            photonView.RPC("SetCrouch", RpcTarget.All, !_crouched);
        }
        
        //Jumping
        if (isJumping)
        {
            if (_crouched)
            {
                photonView.RPC("SetCrouch", RpcTarget.All, !_crouched);
            }
            
            _rig.AddForce(Vector3.up * _jumpForce);
            _currentRecovery = 0f;
        }

        //Head Bob
        if (!isGrounded)
        {
            //Airborne
            HeadBob(_idleCounter, 0.01f, 0.01f);
            _idleCounter += 0;
            _weaponParent.localPosition =
                Vector3.MoveTowards(
                    _weaponParent.localPosition, _targetWeaponBobPosition, Time.deltaTime * 2f * 0.2f);
        }
        else if (_sliding)
        {
            //Sliding
            HeadBob(_movementCounter, 0.15f, 0.075f);
            _weaponParent.localPosition =
                Vector3.MoveTowards(
                    _weaponParent.localPosition, _targetWeaponBobPosition, Time.deltaTime * 10f * 0.2f);
        }
        else if (t_hmove == 0 & t_vmove == 0)
        {
            //Idling
            HeadBob(_idleCounter, 0.01f, 0.01f);
            _idleCounter += Time.deltaTime;
            _weaponParent.localPosition =
                Vector3.MoveTowards(
                    _weaponParent.localPosition, _targetWeaponBobPosition, Time.deltaTime * 2f * 0.2f);
        }
        else if (!isSprinting && !_crouched)
        {
            //Walking
            HeadBob(_movementCounter, 0.035f, 0.035f);
            _movementCounter += Time.deltaTime * 3f;
            _weaponParent.localPosition =
                Vector3.MoveTowards(
                    _weaponParent.localPosition, _targetWeaponBobPosition, Time.deltaTime * 6f * 0.2f);
        }
        else if (_crouched)
        {
            //Crouching
            HeadBob(_movementCounter, 0.02f, 0.02f);
            _movementCounter += Time.deltaTime * 1.75f;
            _weaponParent.localPosition =
                Vector3.MoveTowards(
                    _weaponParent.localPosition, _targetWeaponBobPosition, Time.deltaTime * 6f * 0.2f);
        }
        else
        {
            //Sprinting
            HeadBob(_movementCounter, 0.15f, 0.075f);
            _movementCounter += Time.deltaTime * 7f;
            _weaponParent.localPosition =
                Vector3.MoveTowards(
                    _weaponParent.localPosition, _targetWeaponBobPosition, Time.deltaTime * 10f);
        }

        //UI Refreshes
        RefreshHealthBar();
        weapon.RefreshAmmo(ui_ammo);
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine)
        {
            return;
        }
        
        // Axles
        float t_hmove = Input.GetAxisRaw("Horizontal");
        float t_vmove = Input.GetAxisRaw("Vertical");

        //Controls
        bool sprint = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool jump = Input.GetKeyDown(KeyCode.Space);
        bool slide = Input.GetKey(KeyCode.C);
        bool aim = Input.GetMouseButton(1);
        bool jet = Input.GetKey(KeyCode.Space);

        //States
        bool isGrounded = Physics.Raycast(_groundDetector.position, Vector3.down, 0.15f, _ground);
        bool isJumping = jump && isGrounded;
        bool isSprinting = sprint && t_vmove > 0 && !isJumping && isGrounded;
        bool isSliding = isSprinting && slide && !_sliding;
        _isAiming = aim && !isSliding && !isSprinting;
        
        if (Pause.IsPaused)
        {
            t_hmove = 0;
            t_vmove = 0;
            sprint = false;
            slide = false;
            jump = false;
            isGrounded = false;
            isJumping = false;
            isSprinting = false;
            isSliding = false;
            _isAiming = false;
        }
        
        //Movement
        Vector3 t_direction;
        float t_adjSpeed = _speed;
        if (!_sliding)
        {
            t_direction = new Vector3(t_hmove, 0, t_vmove);
            t_direction.Normalize();
            t_direction = transform.TransformDirection(t_direction);

            if (isSprinting)
            {
                if (_crouched)
                {
                    photonView.RPC("SetCrouch", RpcTarget.All, false);
                }
                
                t_adjSpeed *= _speedModifier;
            }
            else if(_crouched)
            {
                t_adjSpeed *= _crouchModifier;
            }
        }
        else
        {
            t_direction = _slideDir;
            t_adjSpeed *= _slideModifier;
            _slideTime -= Time.deltaTime;

            if (_slideTime <= 0)
            {
                _sliding = false;
                _weaponParentCurrentPos += Vector3.up * (_slideAmount - _crouchAmount);
            }
        }

        Vector3 t_targetVelocity = t_direction * t_adjSpeed * Time.deltaTime;
        t_targetVelocity.y = _rig.velocity.y;
        _rig.velocity = t_targetVelocity;

        //Sliding
        if (isSliding)
        {
            _sliding = true;
            _slideDir = t_direction;
            _slideTime = _lengthOfSlide;
            _weaponParentCurrentPos += Vector3.down * (_slideAmount - _crouchAmount);
            
            if (_crouched == false)
            {
                photonView.RPC("SetCrouch", RpcTarget.All, true);
            }
        }
        
        //Jetting
        if (jump && !isGrounded)
        {
            _canJet = true;
        }

        if (isGrounded)
        {
            _canJet = false;
        }

        if (_canJet && jet && _currentFuel > 0)
        {
            _rig.AddForce(Vector3.up * _jetForce * Time.fixedDeltaTime, ForceMode.Acceleration);
            _currentFuel = Mathf.Max(0, _currentFuel - Time.fixedDeltaTime);
        }

        if (isGrounded)
        {
            if (_currentRecovery < _jetWait)
            {
                _currentRecovery = Mathf.Min(_jetWait, _currentRecovery + Time.fixedDeltaTime);
            }
            else
            {
                _currentFuel = Mathf.Min(_maxFuel, _currentFuel + Time.fixedDeltaTime * _jetRecovery);
            }
        }
        
        ui_fuelBar.localScale = new Vector3(_currentFuel/_maxFuel,1,1);
        
        //Aiming
        _isAiming = weapon.Aim(_isAiming);

        //Camera stuff
        if (_sliding)
        {
            _normalCam.fieldOfView =
                Mathf.Lerp(
                    _normalCam.fieldOfView, 
                    _baseFOV * _sprintFOVModifier * 1.15f, 
                    Time.deltaTime * 8f);
            _weaponCam.fieldOfView =
                Mathf.Lerp(
                    _normalCam.fieldOfView, 
                    _baseFOV * _sprintFOVModifier * 1.15f, 
                    Time.deltaTime * 8f);
            
            _normalCamTarget =
                Vector3.MoveTowards(
                    _normalCam.transform.localPosition,
                    _origin + Vector3.down * _slideAmount, 
                    Time.deltaTime); 
            _weaponCamTarget =
                Vector3.MoveTowards(
                    _weaponCam.transform.localPosition,
                    _origin + Vector3.down * _slideAmount, 
                    Time.deltaTime);
        }
        else
        {
            if (isSprinting)
            {
                _normalCam.fieldOfView = Mathf.Lerp(_normalCam.fieldOfView, _baseFOV * _sprintFOVModifier, Time.deltaTime * 8f);
                _weaponCam.fieldOfView = Mathf.Lerp(_weaponCam.fieldOfView, _baseFOV * _sprintFOVModifier, Time.deltaTime * 8f);
            }
            else if(_isAiming)
            {
                _normalCam.fieldOfView = Mathf.Lerp(_normalCam.fieldOfView, _baseFOV * weapon.CurrentGunData.MainFOV, Time.deltaTime * 8f);
                _weaponCam.fieldOfView = Mathf.Lerp(_weaponCam.fieldOfView, _baseFOV * weapon.CurrentGunData.WeaponFOV, Time.deltaTime * 8f);
            }
            else
            {
                _normalCam.fieldOfView = Mathf.Lerp(_normalCam.fieldOfView, _baseFOV, Time.deltaTime * 8f);
                _weaponCam.fieldOfView = Mathf.Lerp(_weaponCam.fieldOfView, _baseFOV, Time.deltaTime * 8f);
            }

            if (_crouched)
            {
                _normalCamTarget = Vector3.MoveTowards(_normalCam.transform.localPosition, _origin + Vector3.down * _crouchAmount, Time.deltaTime);
                _weaponCamTarget = Vector3.MoveTowards(_weaponCam.transform.localPosition, _origin + Vector3.down * _crouchAmount, Time.deltaTime);
            }
            else
            {
                _normalCamTarget = Vector3.MoveTowards(_normalCam.transform.localPosition, _origin, Time.deltaTime);
                _weaponCamTarget = Vector3.MoveTowards(_weaponCam.transform.localPosition, _origin, Time.deltaTime);
            }
        }
        
        //Animations
        float t_anim_horizontal = 0f;
        float t_anim_vertical = 0f;

        if (isGrounded)
        {
            t_anim_horizontal = t_direction.x;
            t_anim_vertical = t_direction.z;
        }
        
        _anim.SetFloat(Horizontal, t_anim_horizontal);
        _anim.SetFloat(Vertical, t_anim_vertical);
    }

    private void LateUpdate()
    {
        _normalCam.transform.localPosition = _normalCamTarget;
        _weaponCam.transform.localPosition = _weaponCamTarget;
    }

    #endregion

    #region Pun RPC Calls

    [PunRPC]
    private void SyncProfile(string p_username, int p_level, int p_xp)
    {
        PlayerProfile = new ProfileData(p_username, p_level, p_xp);
        _playerUsername.text = PlayerProfile.Username;

    }

    [PunRPC]
    private void SyncTeam(bool p_awayTeam)
    {
        AwayTeam = p_awayTeam;

        ColorTeamIndicators(AwayTeam ? Color.red : Color.blue);
    }

    [PunRPC]
    private void SetCrouch(bool p_state)
    {
        if (_crouched == p_state)
        {
            return;
        }

        _crouched = p_state;

        if (_crouched)
        {
            _standingCollider.SetActive(false);
            _crouchingCollider.SetActive(true);
            _weaponParentCurrentPos += Vector3.down * _crouchAmount;
        }
        else
        {
            _standingCollider.SetActive(true);
            _crouchingCollider.SetActive(false);
            _weaponParentCurrentPos -= Vector3.down * _crouchAmount;
        }
    }

    #endregion
    
    #region Private Methods

    private void ColorTeamIndicators(Color p_color)
    {
        foreach (Renderer indicator in _teamIndicators)
        {
            indicator.material.color = p_color;
        }
    }

    
    private void ChangeLayersRecursively(Transform p_trans, int p_layer)
    {
        p_trans.gameObject.layer = p_layer;
        
        foreach (Transform t in p_trans)
        {
            ChangeLayersRecursively(t, p_layer);
        }
    }

    private void RefreshMultiplayerState()
    {
        float chacheEulY = _weaponParent.localEulerAngles.y;
        
        Quaternion targetRotation = Quaternion.identity * Quaternion.AngleAxis(_aimAngle, Vector3.right);
        _weaponParent.rotation = Quaternion.Slerp(_weaponParent.rotation, targetRotation, Time.deltaTime * 8f);
        Vector3 finalRotation = _weaponParent.localEulerAngles;
        finalRotation.y = chacheEulY;

        _weaponParent.localEulerAngles = finalRotation;
    }

    private void HeadBob(float p_z, float p_xIntensity, float p_yIntensity)
    {
        float t_aimAdjust = 1f;
        
        if (_isAiming)
        {
            t_aimAdjust = 0.1f;
        }
        
        _targetWeaponBobPosition = _weaponParentCurrentPos
                                   + new Vector3(
                                       Mathf.Cos(p_z) * p_xIntensity * t_aimAdjust,
                                       Mathf.Sin(p_z * 2) * p_yIntensity * t_aimAdjust,
                                       0);
    }

    private void RefreshHealthBar()
    {
        float t_healthRatio = (float) _currentHealth / (float) _maxHealth;
        ui_healthBar.localScale =
            Vector3.Lerp(ui_healthBar.localScale, new Vector3(t_healthRatio, 1, 1), Time.deltaTime * 8f);
    }

    

    #endregion

    #region Public Methods

    public void Init(HeadsUpDisplay display)
    {
        _display = display;
    }

    public void TakeDamage(int p_damage,int p_actor)
    {
        if (photonView.IsMine)
        {
            _currentHealth -= p_damage;
            RefreshHealthBar();

            if (_currentHealth <= 0)
            {
                //Die
                _manager.Spawn();
                _manager.ChangeStat_S(PhotonNetwork.LocalPlayer.ActorNumber, 1, 1);

                if (p_actor >= 0)
                {
                    _manager.ChangeStat_S(p_actor, 0,1);
                }
                
                PhotonNetwork.Destroy(gameObject);
            }
        }
    }

    public void TrySync()
    {
        if (!photonView.IsMine)
        {
            return;
        }
        
        photonView.RPC(
            "SyncProfile",
            RpcTarget.All,
            Launcher.MyProfile.Username, Launcher.MyProfile.Level, Launcher.MyProfile.XP);

        if (GameSettings.GameMode == GameMode.TDM)
        {
            photonView.RPC("SyncTeam", RpcTarget.All, GameSettings.IsAwayTeam);
        }
    }

    #endregion

   
}