using UnityEngine;

namespace ScriptableObjectGens
{
    [CreateAssetMenu(fileName = "New gun", menuName = "Gun")]
    public class Gun : ScriptableObject
    {
        public string Name => _name;
        public int Damage => _damage;
        public int Ammo => _ammo;
        public int Burst => _burst; // 0 semi | 1 auto | 2+ burst fire
        public int Pellets => _pellets;
        public int ClipSize => _clipSize;
        public float Bloom => _bloom;
        public float Recoil => _recoil;
        public float FireRate => _fireRate;
        public float KickBack => _kickBack;
        public float AimSpeed => _aimSpeed;
        public float ReloadTime => _reloadTime;
        public float MainFOV => _mainFOV;
        public float WeaponFOV => _weaponFOV;
        public AudioClip GunshotSound => _gunshotSound;
        public float PitchRandomization => _pitchRandomization;
        public float ShotVolume => _shotVolume;
        public GameObject Prefab => _prefab;
        public GameObject Display => _display;
        public bool Recovery => _recovery;

        [SerializeField] private string _name;
        [SerializeField] private int _damage;
        [SerializeField] private int _ammo;
        [SerializeField] private int _burst;
        [SerializeField] private int _pellets;
        [SerializeField] private int _clipSize;
        [SerializeField] private float _bloom;
        [SerializeField] private float _recoil;
        [SerializeField] private float _fireRate;
        [SerializeField] private float _kickBack;
        [SerializeField] private float _aimSpeed;
        [SerializeField] private float _reloadTime;
        [SerializeField] [Range(0, 1)] private float _mainFOV;
        [SerializeField] [Range(0, 1)] private float _weaponFOV;
        [SerializeField] private AudioClip _gunshotSound;
        [SerializeField] private float _pitchRandomization;
        [SerializeField] private float _shotVolume;
        [SerializeField] private GameObject _prefab;
        [SerializeField] private GameObject _display;
        [SerializeField] private bool _recovery;

        private int _stash;
        private int _clip;

        public void Initialize()
        {
            _stash = _ammo;
            _clip = _clipSize;
        }

        public bool FireBullet()
        {
            if (_clip > 0)
            {
                _clip -= 1;
                return true;
            }
            else
            {
                return false;
            }
        }

        public void Reload()
        {
            _stash += _clip;
            _clip = Mathf.Min(_clipSize, _stash);
            _stash -= _clip;
        }

        public int GetStash()
        {
            return _stash;
        }

        public int GetClip()
        {
            return _clip;
        }
    }
}