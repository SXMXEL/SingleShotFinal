using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using ScriptableObjectGens;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class Weapon : MonoBehaviourPunCallbacks
{
    #region Variables

    public bool IsAiming;

    [HideInInspector] public Gun CurrentGunData;
    [SerializeField] private List<Gun> _loadOut;
    [SerializeField] private Transform _weaponParent;
    [SerializeField] private GameObject _bulletHolePrefab;
    [SerializeField] private LayerMask _canBeShot;
    [SerializeField] private AudioSource _sfx;
    [SerializeField] private AudioClip _hitMarkerSound;
    [SerializeField] private Transform _raycastDestination;

    private float _currentCooldown;
    private int _currentIndex;
    private GameObject _currentWeapon;
    private WeaponStates _weaponState;

    private Image _hitMarkerImage;
    private float _hitMarkerWait;
    private readonly Color CLEAR_WHITE = new Color(1, 1, 1, 0);

    private bool _isReloading;

    private const string RELOAD = "ReloadRPC";

    #endregion

    #region Monobehaviour Callbacks

    private void Start()
    {
        foreach (var gun in _loadOut)
        {
            gun.Initialize();
        }

        _hitMarkerImage = GameObject.Find("HUD/Hitmarker/Image").GetComponent<Image>();
        _hitMarkerImage.color = CLEAR_WHITE;

        photonView.RPC("Equip", RpcTarget.All, 0);
    }

    private void Update()
    {
        if (Pause.IsPaused && photonView.IsMine)
        {
            return;
        }

        if (photonView.IsMine)
        {
            if (Input.GetKey(KeyCode.Alpha1))
            {
                photonView.RPC("Equip", RpcTarget.All, 0);
            }
            else if (Input.GetKey(KeyCode.Alpha2))
            {
                if (_loadOut.Count > 1)
                {
                    photonView.RPC("Equip", RpcTarget.All, 1);
                }
            }
        }

        if (_currentWeapon != null)
        {
            if (photonView.IsMine)
            {
                if (_loadOut[_currentIndex].Burst != 1)
                {
                    if (Input.GetMouseButtonDown(0) && _currentCooldown <= 0)
                    {
                        if (_loadOut[_currentIndex].FireBullet())
                        {
                            photonView.RPC("Shoot", RpcTarget.All);
                        }
                        else
                        {
                            photonView.RPC(RELOAD, RpcTarget.All);
                        }
                    }
                }
                else
                {
                    if (Input.GetMouseButton(0) && _currentCooldown <= 0)
                    {
                        if (_loadOut[_currentIndex].FireBullet())
                        {
                            photonView.RPC("Shoot", RpcTarget.All);
                        }
                        else
                        {
                            photonView.RPC(RELOAD, RpcTarget.All);
                        }
                    }
                }

                if (Input.GetKeyDown(KeyCode.R))
                {
                    photonView.RPC(RELOAD, RpcTarget.All);
                }

                //Cooldown
                if (_currentCooldown > 0)
                {
                    _currentCooldown -= Time.deltaTime;
                }
            }

            //Weapon position elasticity
            _currentWeapon.transform.localPosition =
                Vector3.Lerp(_currentWeapon.transform.localPosition, Vector3.zero, Time.deltaTime * 4f);
        }

        //HitMarker

        if (photonView.IsMine)
        {
            if (_hitMarkerWait > 0)
            {
                _hitMarkerWait -= Time.deltaTime;
            }
            else if (_hitMarkerImage.color.a > 0)
            {
                _hitMarkerImage.color = Color.Lerp(_hitMarkerImage.color, CLEAR_WHITE, Time.deltaTime * 2f);
            }
        }
    }

    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            if (_isReloading)
            {
                if (_weaponState.IsFiring)
                {
                    _weaponState.StopFiring();
                }
            }

            if (Input.GetMouseButtonDown(0) && _isReloading == false)
            {
                if (_loadOut[_currentIndex].Burst != 1)
                {
                    _weaponState.StartFiring(_raycastDestination);
                    _weaponState.StopFiring();
                }
                else
                {
                    _weaponState.StartFiring(_raycastDestination);
                }
            }
            else if (Input.GetMouseButtonUp(0))
            {
                if (_weaponState.IsFiring)
                {
                    _weaponState.StopFiring();
                }
            }

            _weaponState.UpdateBullets(Time.deltaTime);

            if (_weaponState.IsFiring)
            {
                _weaponState.UpdateFiring(Time.deltaTime, _raycastDestination);
            }
        }
    }

    #endregion

    #region Private Methods

    [PunRPC]
    private void ReloadRPC()
    {
        if (_isReloading)
        {
            return;
        }

        if (IsAiming)
        {
            return;
        }

        StartCoroutine(Reload(_loadOut[_currentIndex].ReloadTime));
    }

    private IEnumerator Reload(float p_wait)
    {
        _isReloading = true;
        if (_currentWeapon.GetComponent<Animator>())
        {
            _currentWeapon.GetComponent<Animator>().Play("Reload", 0, 0);
        }
        else
        {
            _currentWeapon.SetActive(false);
        }

        yield return new WaitForSeconds(p_wait);
        _loadOut[_currentIndex].Reload();
        if (_currentWeapon.activeSelf == false)
        {
            _currentWeapon.SetActive(true);
        }

        _isReloading = false;
    }

    [PunRPC]
    private void Equip(int p_ind)
    {
        if (_currentWeapon != null)
        {
            if (_isReloading)
            {
                StopCoroutine($"Reload");
            }

            Destroy(_currentWeapon);
        }

        _currentIndex = p_ind;

        GameObject t_newWeapon =
            Instantiate(
                _loadOut[p_ind].Prefab,
                _weaponParent.position,
                _weaponParent.rotation,
                _weaponParent);
        t_newWeapon.transform.localPosition = Vector3.zero;
        t_newWeapon.transform.localEulerAngles = Vector3.zero;
        t_newWeapon.GetComponent<Sway>().IsMine = photonView.IsMine;

        if (photonView.IsMine)
        {
            ChangeLayersRecursively(t_newWeapon, 10);
        }
        else
        {
            ChangeLayersRecursively(t_newWeapon, 0);
        }

        t_newWeapon.GetComponent<Animator>().Play("Equip", 0, 0);

        _currentWeapon = t_newWeapon;
        _weaponState = _currentWeapon.GetComponent<WeaponStates>();
        CurrentGunData = _loadOut[p_ind];
    }

    [PunRPC]
    private void PickUpWeapon(string gunName)
    {
        Gun newGun = GunLibrary.FindGun(gunName);
        newGun.Initialize();

        if (_loadOut.Count >= 2)
        {
            _loadOut[_currentIndex] = newGun;
            Equip(_currentIndex);
        }
        else
        {
            _loadOut.Add(newGun);
            Equip(_loadOut.Count - 1);
        }
    }

    private void ChangeLayersRecursively(GameObject p_target, int p_layer)
    {
        p_target.layer = p_layer;

        foreach (Transform a in p_target.transform)
        {
            ChangeLayersRecursively(a.gameObject, p_layer);
        }
    }


    [PunRPC]
    private void Shoot()
    {
        Transform t_spawn = transform.Find("Cameras/NormalCamera");

        //Cooldown
        _currentCooldown = _loadOut[_currentIndex].FireRate;

        for (int i = 0; i < Mathf.Max(1, CurrentGunData.Pellets); i++)
        {
            //Bloom
            var position = t_spawn.position;
            Vector3 t_bloom = position + t_spawn.forward * 1000f;
            t_bloom += Random.Range(-_loadOut[_currentIndex].Bloom, _loadOut[_currentIndex].Bloom) * t_spawn.up;
            t_bloom += Random.Range(-_loadOut[_currentIndex].Bloom, _loadOut[_currentIndex].Bloom) * t_spawn.right;
            t_bloom -= position;
            t_bloom.Normalize();

            //Raycast
            RaycastHit t_hit = new RaycastHit();
            if (Physics.Raycast(t_spawn.position, t_bloom, out t_hit, 1000f, _canBeShot))
            {
                //TODO remove
                // GameObject t_newHole =
                //     Instantiate(
                //         _bulletHolePrefab,
                //         t_hit.point + t_hit.normal * 0.01f,
                //         Quaternion.identity) as GameObject;
                //
                // t_newHole.transform.LookAt(t_hit.point + t_hit.normal);
                // Destroy(t_newHole, 5f);

                if (photonView.IsMine)
                {
                    // Shooting another player
                    if (t_hit.collider.gameObject.layer == 11)
                    {
                        bool applyDamage = false;

                        if (GameSettings.GameMode == GameMode.FFA)
                        {
                            applyDamage = true;
                        }
                        else if (GameSettings.GameMode == GameMode.TDM)
                        {
                            if (t_hit.collider.transform.root.gameObject.GetComponent<Player>().AwayTeam !=
                                GameSettings.IsAwayTeam)
                            {
                                applyDamage = true;
                            }
                        }

                        if (applyDamage)
                        {
                            //RPC call to damage them
                            t_hit.collider.transform.root.gameObject.GetPhotonView()
                                .RPC("TakeDamage", RpcTarget.All, _loadOut[_currentIndex].Damage,
                                    PhotonNetwork.LocalPlayer.ActorNumber);

                            //Show hitMarker
                            if (t_hit.collider.transform.root.gameObject.GetComponent<Player>().Health <= 0)
                            {
                                _hitMarkerImage.color = Color.red;
                            }
                            else
                            {
                                _hitMarkerImage.color = Color.white;
                            }

                            _sfx.PlayOneShot(_hitMarkerSound);
                            _hitMarkerWait = 1f;
                        }
                    }

                    if (t_hit.collider.gameObject.layer == 12)
                    {
                        Destroy(t_hit.collider.gameObject);
                        //Show hitMarker
                        _hitMarkerImage.color = Color.white;
                        _sfx.PlayOneShot(_hitMarkerSound);
                        _hitMarkerWait = 1f;
                    }
                }
            }
        }

        if (photonView.IsMine)
        {
            //Sound
            _sfx.Stop();
            _sfx.clip = CurrentGunData.GunshotSound;
            _sfx.volume = CurrentGunData.ShotVolume;
            _sfx.pitch = 1 - CurrentGunData.PitchRandomization +
                         Random.Range(-CurrentGunData.PitchRandomization, CurrentGunData.PitchRandomization);
            _sfx.Play();

            //Gun fx
            _currentWeapon.transform.Rotate(-_loadOut[_currentIndex].Recoil, 0, 0);
            _currentWeapon.transform.position -= _currentWeapon.transform.forward * _loadOut[_currentIndex].KickBack;

            if (CurrentGunData.Recovery)
            {
                _currentWeapon.GetComponent<Animator>().Play("Recovery", 0, 0);
            }
        }
    }

    [PunRPC]
    private void TakeDamage(int p_damage, int p_actor)
    {
        GetComponent<Player>().TakeDamage(p_damage, p_actor);
    }

    #endregion

    #region Public Methods

    public bool Aim(bool p_isAiming)
    {
        if (!_currentWeapon)
        {
            return false;
        }

        if (_isReloading)
        {
            p_isAiming = false;
        }

        IsAiming = p_isAiming;

        var state = _currentWeapon.transform.GetComponent<WeaponStates>();

        Transform t_anchor = state.Root;
        Transform t_stateAim = state.Aim;
        Transform t_stateHip = state.Hip;

        if (p_isAiming)
        {
            //Aim
            t_anchor.position = Vector3.Lerp(
                t_anchor.position,
                t_stateAim.position,
                Time.deltaTime * _loadOut[_currentIndex].AimSpeed);
        }
        else
        {
            //Hip
            t_anchor.position = Vector3.Lerp(
                t_anchor.position,
                t_stateHip.position,
                Time.deltaTime * _loadOut[_currentIndex].AimSpeed);
        }

        return p_isAiming;
    }

    public void RefreshAmmo(TextMeshProUGUI p_ammo)
    {
        float t_clip = _loadOut[_currentIndex].GetClip();
        float t_stash = _loadOut[_currentIndex].GetStash();

        p_ammo.text = t_clip + " / " + t_stash;
    }

    #endregion
}