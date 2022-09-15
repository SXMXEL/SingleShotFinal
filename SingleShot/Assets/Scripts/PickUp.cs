using System.Collections.Generic;
using Photon.Pun;
using ScriptableObjectGens;
using UnityEngine;

public class PickUp : MonoBehaviourPunCallbacks
{
    [SerializeField] private Gun _gun;
    [SerializeField] private float _coolDown;
    [SerializeField] private Transform _gunDisplay;
    [SerializeField] private List<GameObject> _targets;

    private bool _isDisabled;
    private float _wait;

    private void Start()
    {
        foreach (Transform t in _gunDisplay)
        {
            Destroy(t.gameObject);
        }
        
        GameObject newDisplay = 
            Instantiate(_gun.Display, _gunDisplay.position, _gunDisplay.rotation);
        newDisplay.transform.SetParent(_gunDisplay);
    }

    private void Update()
    {
        if (_isDisabled)
        {
            if (_wait > 0)
            {
                _wait -= Time.deltaTime;
            }
            else
            {
                photonView.RPC("Enable", RpcTarget.All);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == null)
        {
            return;
        }
        
        if (other.attachedRigidbody.gameObject.tag.Equals("Player"))
        {
            Weapon weaponController = other.attachedRigidbody.gameObject.GetComponent<Weapon>();
            weaponController.photonView.RPC("PickUpWeapon", RpcTarget.All, _gun.name);
            photonView.RPC("Disable", RpcTarget.All);
        }
    }
    
    [PunRPC]
    private void Disable()
    {
        _isDisabled = true;
        _wait = _coolDown;
        foreach (var target in _targets)
        {
            target.SetActive(false);
        }
    }

    [PunRPC]
    private void Enable()
    {
        _isDisabled = false;
        _wait = 0;
        foreach (var target in _targets)
        {
            target.SetActive(true);
        }
    }
}