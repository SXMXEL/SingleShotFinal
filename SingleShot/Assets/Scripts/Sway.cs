using System;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

public class Sway : MonoBehaviour
{
    #region Variables

    public bool IsMine;
    
    [SerializeField] private float _intensity;
    [SerializeField] private float _smooth;

    private Quaternion _originRotation;

    #endregion

    #region Monobehaviour Callbacks

    private void Start()
    {
        _originRotation = transform.localRotation;
    }

    private void Update()
    {
        UpdateSway();
    }

    #endregion

    #region Private Methods

    private void UpdateSway()
    {
        //controls
        float t_xMouse = Input.GetAxis("Mouse X");
        float t_yMouse = Input.GetAxis("Mouse Y");

        if (!IsMine)
        {
            t_xMouse = 0;
            t_yMouse = 0;
        }
        //calculate target rotation
        Quaternion t_xAdj = Quaternion.AngleAxis(_intensity * -t_xMouse, Vector3.up);
        Quaternion t_yAdj = Quaternion.AngleAxis(_intensity * t_yMouse, Vector3.right);
        Quaternion t_targetRotation = _originRotation * t_xAdj * t_yAdj;
        
        //Rotate towards target rotation
        transform.localRotation = Quaternion.Lerp(transform.localRotation, t_targetRotation, Time.deltaTime * _smooth);
    }


    #endregion
}
