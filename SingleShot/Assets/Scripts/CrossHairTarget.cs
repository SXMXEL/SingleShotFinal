using Photon.Pun;
using UnityEngine;

public class CrossHairTarget : MonoBehaviourPunCallbacks
{
    [SerializeField] private Camera _mainCamera;
    private Ray _ray;
    private RaycastHit _hitInfo;
    

    private void Update()
    {
        if (photonView.IsMine)
        {
            _ray.origin = _mainCamera.transform.position;
            _ray.direction = _mainCamera.transform.forward;
            Physics.Raycast(_ray, out  _hitInfo);
            transform.position = _hitInfo.point;
        }
    }
}
