using System;
using System.Collections;
using System.Collections.Generic;
using ScriptableObjectGens;
using UnityEngine;

public class GunLibrary : MonoBehaviour
{
    public static Gun[] Guns;
    
    [SerializeField] private Gun[] _allGuns;

    private void Awake()
    {
        Guns = _allGuns;
    }

    public static Gun FindGun(string gunName)
    {
        foreach (var gun in Guns)
        {
            if (gun.name.Equals(gunName))
            {
                return gun;
            }
        }

        return Guns[0];
    }
}