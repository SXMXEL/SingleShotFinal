using Photon.Pun;
using UnityEngine;
using Cursor = UnityEngine.Cursor;

public class Look : MonoBehaviourPunCallbacks
{
    #region Variables

    [SerializeField] private Transform _player;
    [SerializeField] private Transform _normalCam;
    [SerializeField] private Transform _weaponCam;
    [SerializeField] private Transform _weapon;
    [SerializeField] private float _xSensitivity;
    [SerializeField] private float _ySensitivity;
    [SerializeField] private float _maxAngle;

    private static bool _cursorLocked = true;
    private Quaternion _camsCenter;

    #endregion

    #region Monobehaviour Callbacks

    private void Start()
    {
        if (!photonView.IsMine)
        {
            return;
        }
        _camsCenter = _normalCam.localRotation;
    }

    private void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }

        if (Pause.IsPaused)
        {
            return;
        }
        
        SetY();
        
        SetX();
        
        UpdateCursorLock();

        // _weaponCam.localRotation = _normalCam.localRotation;
        _weaponCam.rotation = _normalCam.rotation;
    }
    
    #endregion

    #region Private Methods

    private void SetY()
    {
        float t_input = Input.GetAxis("Mouse Y") * _ySensitivity * Time.deltaTime;
        Quaternion t_adj = Quaternion.AngleAxis(t_input, -Vector3.right);
        Quaternion t_delta = _normalCam.localRotation * t_adj;

        if (Quaternion.Angle(_camsCenter, t_delta) < _maxAngle)
        {
            _normalCam.localRotation = t_delta;
        }

        _weapon.rotation = _normalCam.rotation;
    }

    private void SetX()
    {
        float t_input = Input.GetAxis("Mouse X") * _xSensitivity * Time.deltaTime;
        Quaternion t_adj = Quaternion.AngleAxis(t_input, Vector3.up);
        Quaternion t_delta = _player.localRotation * t_adj;
        _player.localRotation = t_delta;
    }

    private void UpdateCursorLock()
    {
        if (_cursorLocked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _cursorLocked = false;
            }
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _cursorLocked = true;
            }
        }
    }

    #endregion
    
}