using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class KartController : MonoBehaviour
{
    [SerializeField] private InputActionAsset _playerInput;
    [Header("Wheels")]
    [SerializeField] private Transform _frontLeftWheel;
    [SerializeField] private Transform _frontRightWheel;
    [SerializeField] private Transform _rearLeftWheel;
    [SerializeField] private Transform _rearRightWheel;

    [SerializeField, Range(0,1)] private float _fontAxisShare = 0.5f;

    [Header("Engine")]
    [SerializeField] private KartEngine _engine;
    [SerializeField] private float _gearRatio = 8f;
    [SerializeField] private float _drivetrainEfficiency = 0.9f;

    [SerializeField] private float _wheelRadius = 0.3f;
    [SerializeField] private float _maxSpeed = 20;

    [Header("Stering")]
    [SerializeField] private float _maxSteeringAngle = 30f;
    //запоминаем позицию передних колес

    [Header("friction")]
    [SerializeField] private float _frictionCoefficient = 1.0f; //коэффициент трения
    [SerializeField] private float _lateralStiffnes  = 80f; //жёсткость шины по углу скольжения
    //какую боковую силу создает шина при малом боковом скольжении
    [SerializeField] private float _rolingResistance = 0.5f;
    private Quaternion _frontLeftInitialLocalRot;
    private Quaternion _frontRightInitialLocalRot;
    private Rigidbody _rigidbody;

    private InputAction _moveAction;
    private float _throttleInput;
    private float _steepInput;

    private float _frontLeftNormalForce;
    private float _frontRightNormalForce;
    private float _rearLeftNormalForce;
    private float _rearRightNormalForce;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        var map = _playerInput.FindActionMap("Kart");
        _moveAction = map.FindAction("Move");

        _frontLeftInitialLocalRot = _frontLeftWheel.localRotation;
        _frontRightInitialLocalRot = _frontRightWheel.localRotation;

    }

    private void Start()
    {
        ComputeStaticWheelLoad();
    }

    private void OnEnable()
    {
        _playerInput.Enable();
        
    }

    private void OnDisable()
    {
        _playerInput.Disable();
        
    }

    private void ReadInput()
    {
        Vector2 move = _moveAction.ReadValue<Vector2>();
        _steepInput = Mathf.Clamp(move.x,-1,1);
        _throttleInput = Mathf.Clamp(move.y,-1,1);
        
    }

    private void Update()
    {
        ReadInput();
        RotateFrontWheel();
    }

    private void RotateFrontWheel()
    {
        float steerAngle = _maxSteeringAngle*_steepInput;
        Quaternion steerRotation = Quaternion.Euler(0,steerAngle,0);

        _frontLeftWheel.localRotation = _frontLeftInitialLocalRot * steerRotation;
        _frontRightWheel.localRotation = _frontRightInitialLocalRot * steerRotation;
    }

    private void ComputeStaticWheelLoad()
    {
        float mass = _rigidbody.mass;
        float totalWeight = mass*Mathf.Abs(Physics.gravity.y);

        float frontWeight = totalWeight*_fontAxisShare;
        float rearWeight = totalWeight*(1-_fontAxisShare);

        _frontRightNormalForce = frontWeight * 0.5f;
        _frontLeftNormalForce = frontWeight * 0.5f;
        _rearRightNormalForce = rearWeight * 0.5f;
        _rearLeftNormalForce = rearWeight * 0.5f;
    }

    private void FixedUpdate()
    {
        ApplyEngineForced();
        ApplyWheelForce(_frontLeftWheel,_frontLeftNormalForce,
        isSteer: true, isDriven:false);
        ApplyWheelForce(_frontRightWheel,_frontRightNormalForce,
        isSteer: true, isDriven:false);
        ApplyWheelForce(_rearLeftWheel,_rearLeftNormalForce,
        isSteer: false, isDriven:true);
        ApplyWheelForce(_rearRightWheel,_rearRightNormalForce,
        isSteer: false, isDriven:true);

        
    }

    private void ApplyWheelForce(Transform wheel, float normalForce, bool isSteer, bool isDriven)
    {
        Vector3 wheelPos = wheel.position;
        Vector3 wheelForward = wheel.forward;
        Vector3 wheelRight = wheel.right;
    
        //скорость точки колеса в мировых координатах
        Vector3 velocity = _rigidbody.GetPointVelocity(wheelPos);

        float vLong = Vector3.Dot(velocity, wheelForward);
        float vLat = Vector3.Dot(velocity, wheelRight);

        float Fx = 0f;
        float Fy = 0f;

        if (isDriven)
        {
            Vector3 bodyForward = transform.forward;
            float speedAlongForward = Vector3.Dot(_rigidbody.linearVelocity, bodyForward);
        
            //ограничение скорости вперед
            if (!(_throttleInput > 0 && speedAlongForward > _maxSpeed))
            {
                float engineTorque = _engine.Simulate(
                    _throttleInput,
                    speedAlongForward,
                    Time.fixedDeltaTime
                );

                float totalWheelTorque = engineTorque * _gearRatio * _drivetrainEfficiency;
                float wheelTorque = totalWheelTorque * 0.5f; // два задних колеса
                Fx += wheelTorque / _wheelRadius;
            }

            float rolling = -_rolingResistance * vLong;
            Fx += rolling;
        }
        else
        {
            //на неведущих колесах тоже может быть сопротивление качению
            float rolling = -_rolingResistance * vLong;
            Fx += rolling;
        }
    
        //боковая сила шин
        float FyRaw = -_lateralStiffnes * vLat; // ИСПРАВЛЕНО: должно быть vLat, а не vLong
        Fy += FyRaw;

        float frictionLimit = _frictionCoefficient * normalForce;
        float forceLength = Mathf.Sqrt(Fx * Fx + Fy * Fy); // ИСПРАВЛЕНО: Fx*Fx + Fy*Fy

        if (forceLength > frictionLimit)
        {
            float scale = frictionLimit / forceLength;
            Fy *= scale;
            Fx *= scale;
        }

        
        Vector3 totalForce = wheelForward * Fx + wheelRight * Fy;
        _rigidbody.AddForceAtPosition(totalForce, wheelPos, ForceMode.Force);

    }

    private void ApplyEngineForced()
    {
        Vector3 forward = transform.forward;
        float speedAlongForward = Vector3.Dot(_rigidbody.linearVelocity, forward);

        if (_throttleInput>0 && speedAlongForward> _maxSpeed)
            return;

        if (_throttleInput < 0 && speedAlongForward < -_maxSpeed * 0.5f) // задний ход медленнее
        return;

        float driveTorque = _engine.Simulate(
                _throttleInput,
                speedAlongForward,
                Time.fixedDeltaTime
                );

        float driveForcePerWheel = driveTorque/_wheelRadius/2f;

        Vector3 forceRearLeft = forward * driveForcePerWheel;
        Vector3 forceRearRight = forward * driveForcePerWheel;

        _rigidbody.AddForceAtPosition(forceRearLeft, _rearLeftWheel.position, ForceMode.Force);
        _rigidbody.AddForceAtPosition(forceRearRight, _rearRightWheel.position, ForceMode.Force);

    }


}
