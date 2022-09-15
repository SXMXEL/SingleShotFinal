using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet
{
    public float Time;
    public Vector3 InitialPosition;
    public Vector3 InitialVelocity;
    public TrailRenderer Tracer;
}

public class WeaponStates : MonoBehaviour
{
    public int FireRate = 25;
    public float BulletSpeed = 1000f;
    public float BulletDrop = 0.0f;
    public bool IsFiring = false;
    public Transform Root;
    public Transform Aim;
    public Transform Hip;
    [SerializeField] private Transform _raycastOrigin;
    [SerializeField] private ParticleSystem _muzzleFlash;
    [SerializeField] private ParticleSystem _hitEffect;
    [SerializeField] private TrailRenderer _bulletTracer;

    private Ray _ray;
    private RaycastHit _hitInfo;
    private float _accumulatedTime;
    private List<Bullet> _bullets = new List<Bullet>();
    private float _maxLifetime = 3.0f;
    
    
    public void StartFiring(Transform raycastDestination)
    {
        IsFiring = true;
        _accumulatedTime = 0;
        FireBullet(raycastDestination);
    }

    public void UpdateFiring(float deltaTime, Transform raycastDestination)
    {
        _accumulatedTime += deltaTime;
        float fireInterval = 1.0f / FireRate;
        while (_accumulatedTime >= 0.0f)
        {
            FireBullet(raycastDestination);
            _accumulatedTime -= fireInterval;
        }
    }

    public void UpdateBullets(float deltaTime)
    {
        SimulateBullets(deltaTime);
        DestroyBullets();
    }

    public void StopFiring()
    {
        IsFiring = false;
    }

    private void SimulateBullets(float deltaTime)
    {
        _bullets.ForEach(bullet =>
        {
            Vector3 p0 = GetPosition(bullet);
            bullet.Time += deltaTime;
            Vector3 p1 = GetPosition(bullet);
            RaycastSegment(p0, p1, bullet);
        });
    }

    private void DestroyBullets()
    {
        _bullets.RemoveAll(bullet => bullet.Time >= _maxLifetime);
    }

    private void RaycastSegment(Vector3 startPoint, Vector3 endPoint, Bullet bullet)
    {
        Vector3 direction = endPoint - startPoint;
        float distance = direction.magnitude;
        _ray.origin = startPoint;
        _ray.direction = direction;
        if (Physics.Raycast(_ray, out _hitInfo, distance))
        {
            // Debug.DrawLine(_ray.origin, _hitInfo.point, Color.red, 1.0f);
            _hitEffect.transform.position = _hitInfo.point;
            _hitEffect.transform.forward = _hitInfo.normal;
            _hitEffect.Emit(1);
            bullet.Tracer.transform.position = _hitInfo.point;
            bullet.Time = _maxLifetime;
        }
        else
        {
            bullet.Tracer.transform.position = endPoint;
        }
    }

    private Vector3 GetPosition(Bullet bullet)
    {
        Vector3 gravity = Vector3.down * BulletDrop;
        return bullet.InitialPosition + bullet.InitialVelocity * bullet.Time +
               0.5f * gravity * bullet.Time * bullet.Time;
    }

    private Bullet CreateBullet(Vector3 position, Vector3 velocity)
    {
        Bullet bullet = new Bullet();
        bullet.InitialPosition = position;
        bullet.InitialVelocity = velocity;
        bullet.Time = 0.0f;
        bullet.Tracer = Instantiate(_bulletTracer, position, Quaternion.identity);
        bullet.Tracer.AddPosition(position);;
        return bullet;
    }

    private void FireBullet(Transform raycastDestination)
    {
        _muzzleFlash.Emit(1);

        Vector3 velocity = (raycastDestination.position - _raycastOrigin.position).normalized * BulletSpeed;
        var bullet = CreateBullet(_raycastOrigin.position, velocity);
        _bullets.Add(bullet);
        
        
    }

    
}