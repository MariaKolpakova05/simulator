using UnityEngine;
using System;
using UnityEngine.InputSystem;
[RequireComponent(typeof(TrajectoryRenderer))]

public class BalisticCalc : MonoBehaviour
{
    [SerializeField] private Transform _lanchpoint;
    [SerializeField] private float _muzzleVelosity=20;
    [SerializeField, Range(0,85)] private float _muzzleVeAngle = 20;
    [Space]
    [SerializeField] private QuadriicDrag _shootRound;

    [Header("GEn Parr")]
    [SerializeField] private float _minMass = 0.5f;
    [SerializeField] private float _maxMass = 3f;
    [SerializeField] private float _minRadius = 0.05f;
    [SerializeField] private float _maxRadius = 0.3f;

    [SerializeField] private float _dragCoef=0.47f;
    [SerializeField] private float _airDenisty=1.225f;
    [SerializeField] private Vector3 _wind=Vector3.zero;
    private TrajectoryRenderer _traectoryRender;
    private float _currentMass;
    private float _currentRadius;

    private void Start()
    {
        _traectoryRender = GetComponent<TrajectoryRenderer>();
        GenerateRandomProjectileParams();
        UpdateTrajectoryPreview();

    }
    private void Update()
    {
        if(_lanchpoint==null) return;
        UpdateTrajectoryPreview();
        //Vector3 v0= CalculateVilosityVector(_muzzleVeAngle);
        //_traectoryRender.DrawVacuum(_lanchpoint.position, startVelocity: v0);
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Vector3 v0= CalculateVilosityVector(_muzzleVeAngle);
            Fire(v0);
        }
            

    }

    private void GenerateRandomProjectileParams()
    {
        _currentMass = UnityEngine.Random.Range(_minMass, _maxMass);
        _currentRadius = UnityEngine.Random.Range(_minRadius, _maxRadius);
        
        //Debug.Log($"New Projectile Params - Mass: {_currentMass:F2}, Radius: {_currentRadius:F2}");
    }

    private void UpdateTrajectoryPreview()
    {
        Vector3 v0 = CalculateVilosityVector(_muzzleVeAngle);
        
        //обновление параметров превью траектории
        _traectoryRender.SetPreviewParams(_currentMass, _currentRadius, _dragCoef, _airDenisty, _wind);
        _traectoryRender.DrawWithAirEuler(_lanchpoint.position, v0);
    }


    private void Fire(Vector3 initialVelosity)
    {
        if(_shootRound==null) return;
        GameObject nevhShoot= Instantiate(_shootRound.gameObject, _lanchpoint.position, Quaternion.identity);

        QuadriicDrag quadriicDrag= nevhShoot.GetComponent<QuadriicDrag>();
        quadriicDrag.SetPhisicalParams(_currentMass, _currentRadius, _dragCoef, _airDenisty,_wind,initialVelosity);
    }
    private Vector3 CalculateVilosityVector(float angle)
    {
        float vx= _muzzleVelosity * Mathf.Cos(f:angle*Mathf.Deg2Rad);
        float vy = _muzzleVelosity * Mathf.Sin(f:angle * Mathf.Deg2Rad);
        return _lanchpoint.forward*vx + _lanchpoint.up*vy;
    }

}
