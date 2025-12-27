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

    [Header("Steering")]
    [SerializeField] private float _maxSteeringAngle = 30f; //запоминаем позицию передних колес
    
    [Header("Friction")]
    [SerializeField] private float _frictionCoefficient = 1.0f; //коэффициент трения
    [SerializeField] private float _lateralStiffnes = 80f; //жёсткость шины по углу скольжения
    //какую боковую силу создает шина при малом боковом скольжении
    [SerializeField] private float _rolingResistance = 0.5f;
    
    [Header("Handbrake")]
    [SerializeField] private float _handbrakeDragMultiplier = 5f;
    [SerializeField] private float _handbrakeReleaseSmoothness = 5f;
    
    // Телеметрия
    private Vector3 _velocity;
    private float _speedKmh;
    private float _speedMs;
    private float _totalFxRear;
    private float _totalFyFront;
    private float _frontLeftVLat;
    private float _frontRightVLat;
    private float _rearLeftVLat;
    private float _rearRightVLat;
    
    // Ссылки на другие компоненты для телеметрии
    private CarSuspension _carSuspension;
    private KartAero _kartAero;
    
    // Данные подвески для телеметрии
    private float[] _suspensionForces = new float[4]; // FL, FR, RL, RR
    private float[] _wheelToGroundDistances = new float[4];
    private float[] _suspensionCompressions = new float[4];
    private float _centerOfMassHeight;
    
    // Handbrake
    private float _currentRearLateralStiffness;
    private float _currentRearRollingResistance;
    private bool _isHandbrakeActive;
    
    private Quaternion _frontLeftInitialLocalRot;
    private Quaternion _frontRightInitialLocalRot;
    private Rigidbody _rigidbody;

    private InputAction _moveAction;
    private InputAction _handbrakeAction;
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
        _handbrakeAction = map.FindAction("Handbrake");

        _frontLeftInitialLocalRot = _frontLeftWheel.localRotation;
        _frontRightInitialLocalRot = _frontRightWheel.localRotation;

        //инициализация параметров ручного тормоза
        _currentRearLateralStiffness = _lateralStiffnes;
        _currentRearRollingResistance = _rolingResistance;
        
        // Получаем ссылки на другие компоненты
        _carSuspension = GetComponent<CarSuspension>();
        _kartAero = GetComponent<KartAero>();
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
        _steepInput = Mathf.Clamp(move.x, -1, 1);
        _throttleInput = Mathf.Clamp(move.y, -1, 1);
        
        //чтение ручного тормоза
        _isHandbrakeActive = _handbrakeAction.ReadValue<float>() > 0.5f;
        
        //обновление параметров задних колес при ручном тормозе
        if (_isHandbrakeActive)
        {
            _currentRearLateralStiffness = 0f; //убираем боковую жесткость
            _currentRearRollingResistance = _rolingResistance * _handbrakeDragMultiplier;
        }
        else
        {
            //плавное восстановление параметров
            _currentRearLateralStiffness = Mathf.Lerp(
                _currentRearLateralStiffness, 
                _lateralStiffnes, 
                Time.deltaTime * _handbrakeReleaseSmoothness
            );
            _currentRearRollingResistance = Mathf.Lerp(
                _currentRearRollingResistance, 
                _rolingResistance, 
                Time.deltaTime * _handbrakeReleaseSmoothness
            );
        }
    }

    private void Update()
    {
        ReadInput();
        RotateFrontWheel();
        
        //обновление телеметрии
        UpdateTelemetry();
        
        // Обновление данных подвески
        UpdateSuspensionData();
    }

    private void UpdateSuspensionData()
    {
        // Получаем данные о высоте центра масс
        _centerOfMassHeight = transform.position.y;
        
        // Обновляем данные подвески через Raycast
        UpdateWheelSuspensionData(_frontLeftWheel, 0, -_frontLeftWheel.up);
        UpdateWheelSuspensionData(_frontRightWheel, 1, -_frontRightWheel.up);
        UpdateWheelSuspensionData(_rearLeftWheel, 2, -_rearLeftWheel.up);
        UpdateWheelSuspensionData(_rearRightWheel, 3, -_rearRightWheel.up);
    }
    
    private void UpdateWheelSuspensionData(Transform wheel, int index, Vector3 direction)
    {
        // Логика получения данных о подвеске аналогична CarSuspension.SimulateWheel
        float restLength = 0.4f; // Должно совпадать с CarSuspension
        float springTravel = 0.2f;
        float wheelRadius = 0.35f;
        
        Vector3 origin = wheel.position;
        float maxDist = restLength + springTravel + wheelRadius;
        
        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDist))
        {
            float currentLength = hit.distance - wheelRadius;
            currentLength = Mathf.Clamp(currentLength, restLength - springTravel, restLength + springTravel);
            
            // Сохраняем расстояние до земли
            _wheelToGroundDistances[index] = hit.distance;
            
            // Сохраняем сжатие подвески
            _suspensionCompressions[index] = restLength - currentLength;
        }
        else
        {
            _wheelToGroundDistances[index] = float.MaxValue;
            _suspensionCompressions[index] = 0f;
        }
    }

    private void RotateFrontWheel()
    {
        float steerAngle = _maxSteeringAngle * _steepInput;
        Quaternion steerRotation = Quaternion.Euler(0, steerAngle, 0);

        _frontLeftWheel.localRotation = _frontLeftInitialLocalRot * steerRotation;
        _frontRightWheel.localRotation = _frontRightInitialLocalRot * steerRotation;
    }

    private void ComputeStaticWheelLoad()
    {
        float mass = _rigidbody.mass;
        float totalWeight = mass * Mathf.Abs(Physics.gravity.y);

        float frontWeight = totalWeight * _fontAxisShare;
        float rearWeight = totalWeight * (1 - _fontAxisShare);

        _frontRightNormalForce = frontWeight * 0.5f;
        _frontLeftNormalForce = frontWeight * 0.5f;
        _rearRightNormalForce = rearWeight * 0.5f;
        _rearLeftNormalForce = rearWeight * 0.5f;
    }

    private void FixedUpdate()
    {
        //обнуляем суммарные силы для телеметрии
        _totalFxRear = 0f;
        _totalFyFront = 0f;
        
        ApplyEngineForced();
        ApplyWheelForce(_frontLeftWheel, _frontLeftNormalForce, true, false, _lateralStiffnes, _rolingResistance);
        ApplyWheelForce(_frontRightWheel, _frontRightNormalForce, true, false, _lateralStiffnes, _rolingResistance);
        ApplyWheelForce(_rearLeftWheel, _rearLeftNormalForce, false, true, _currentRearLateralStiffness, _currentRearRollingResistance);
        ApplyWheelForce(_rearRightWheel, _rearRightNormalForce, false, true, _currentRearLateralStiffness, _currentRearRollingResistance);
    }

    private void ApplyWheelForce(Transform wheel, float normalForce, bool isSteer, bool isDriven, float lateralStiffness, float rollingResistance)
    {
        Vector3 wheelPos = wheel.position;
        Vector3 wheelForward = wheel.forward;
        Vector3 wheelRight = wheel.right;

        //скорость точки колеса в мировых координатах
        Vector3 velocity = _rigidbody.GetPointVelocity(wheelPos);

        float vLong = Vector3.Dot(velocity, wheelForward);
        float vLat = Vector3.Dot(velocity, wheelRight);
        
        //сохранение vLat для телеметрии
        if (wheel == _frontLeftWheel) _frontLeftVLat = vLat;
        if (wheel == _frontRightWheel) _frontRightVLat = vLat;
        if (wheel == _rearLeftWheel) _rearLeftVLat = vLat;
        if (wheel == _rearRightWheel) _rearRightVLat = vLat;

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
                float wheelTorque = totalWheelTorque * 0.5f;
                Fx += wheelTorque / _wheelRadius;
            }

            float rolling = -rollingResistance * vLong;
            Fx += rolling;
        }
        else
        {
            //на неведущих колесах тоже может быть сопротивление качению
            float rolling = -rollingResistance * vLong;
            Fx += rolling;
        }
    
        //боковая сила шин
        float FyRaw = -lateralStiffness * vLat;
        Fy += FyRaw;
        
        //суммирование сил для телеметрии
        if (isDriven) //задние колеса
            _totalFxRear += Fx;
        else //передние колеса
            _totalFyFront += Fy;

        float frictionLimit = _frictionCoefficient * normalForce;
        float forceLength = Mathf.Sqrt(Fx * Fx + Fy * Fy);

        if (forceLength > frictionLimit)
        {
            float scale = frictionLimit / forceLength;
            Fy *= scale;
            Fx *= scale;
        }

        Vector3 totalForce = wheelForward * Fx + wheelRight * Fy;
        _rigidbody.AddForceAtPosition(totalForce, wheelPos, ForceMode.Force);
        
        // Сохраняем силу подвески для телеметрии
        if (wheel == _frontLeftWheel) _suspensionForces[0] = Mathf.Abs(Fy) + Mathf.Abs(normalForce);
        else if (wheel == _frontRightWheel) _suspensionForces[1] = Mathf.Abs(Fy) + Mathf.Abs(normalForce);
        else if (wheel == _rearLeftWheel) _suspensionForces[2] = Mathf.Abs(Fy) + Mathf.Abs(normalForce);
        else if (wheel == _rearRightWheel) _suspensionForces[3] = Mathf.Abs(Fy) + Mathf.Abs(normalForce);
    }

    private void ApplyEngineForced()
    {
        Vector3 forward = transform.forward;
        float speedAlongForward = Vector3.Dot(_rigidbody.linearVelocity, forward);

        if (_throttleInput > 0 && speedAlongForward > _maxSpeed)
            return;

        if (_throttleInput < 0 && speedAlongForward < -_maxSpeed * 0.5f) //задний ход медленнее
            return;

        float driveTorque = _engine.Simulate(
            _throttleInput,
            speedAlongForward,
            Time.fixedDeltaTime
        );

        float driveForcePerWheel = driveTorque / _wheelRadius / 2f;

        Vector3 forceRearLeft = forward * driveForcePerWheel;
        Vector3 forceRearRight = forward * driveForcePerWheel;

        _rigidbody.AddForceAtPosition(forceRearLeft, _rearLeftWheel.position, ForceMode.Force);
        _rigidbody.AddForceAtPosition(forceRearRight, _rearRightWheel.position, ForceMode.Force);
    }
    
    private void UpdateTelemetry()
    {
        _velocity = _rigidbody.linearVelocity;
        _speedMs = _velocity.magnitude;
        _speedKmh = _speedMs * 3.6f;
    }
    
    // Метод для расчета силы аэродинамического сопротивления (Drag Force)
    private float CalculateDragForce()
    {
        float speed = _rigidbody.linearVelocity.magnitude;
        if (speed < 0.01f) return 0f;
        
        // Используем параметры из KartAero
        float airDensity = 1.225f;
        float dragCoefficient = 0.9f;
        float frontalArea = 0.6f;
        
        return 0.5f * airDensity * dragCoefficient * frontalArea * speed * speed;
    }
    
    // Метод для расчета силы прижима крыла (Downforce)
    private float CalculateDownforce()
    {
        float speed = _rigidbody.linearVelocity.magnitude;
        if (speed < 0.01f) return 0f;
        
        // Используем параметры из KartAero
        float airDensity = 1.225f;
        float wingAngleDeg = 10f;
        float wingArea = 0.4f;
        float liftCoefficientSlope = 0.05f;
        
        float alphaRad = wingAngleDeg * Mathf.Deg2Rad;
        float Cl = liftCoefficientSlope * alphaRad;
        
        return 0.5f * airDensity * Cl * wingArea * speed * speed;
    }
    
    private void OnGUI()
    {
        GUI.Box(new Rect(10, 10, 300, 400), "Kart Telemetry");
    
        GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = 12;
        labelStyle.normal.textColor = Color.white;
    
        int yPos = 35;
        int lineHeight = 20;
    
        // 1. Скорость автомобиля
        GUI.Label(new Rect(20, yPos, 280, 20), $"Speed: {_speedMs:F2} m/s ({_speedKmh:F1} km/h)", labelStyle);
        yPos += lineHeight;
    
        // 2. Обороты двигателя
        GUI.Label(new Rect(20, yPos, 280, 20), $"RPM: {_engine.CurrentRpm:F0}", labelStyle);
        yPos += lineHeight;
    
        // 3. Сила аэродинамического сопротивления
        float dragForce = CalculateDragForce();
        GUI.Label(new Rect(20, yPos, 280, 20), $"Drag: {dragForce:F0} N", labelStyle);
        yPos += lineHeight;
    
        // 4. Сила прижима крыла
        float downforce = CalculateDownforce();
        GUI.Label(new Rect(20, yPos, 280, 20), $"Downforce: {downforce:F0} N", labelStyle);
        yPos += lineHeight;
    
        // 5. Силы подвески на каждом колесе
        // FL Suspension Force
        GUI.Label(new Rect(20, yPos, 280, 20), $"FL Suspension: {_suspensionForces[0]:F0} N", labelStyle);
        yPos += lineHeight;
    
        // FR Suspension Force
        GUI.Label(new Rect(20, yPos, 280, 20), $"FR Suspension: {_suspensionForces[1]:F0} N", labelStyle);
        yPos += lineHeight;
    
        // RL Suspension Force
        GUI.Label(new Rect(20, yPos, 280, 20), $"RL Suspension: {_suspensionForces[2]:F0} N", labelStyle);
        yPos += lineHeight;
    
        // RR Suspension Force
        GUI.Label(new Rect(20, yPos, 280, 20), $"RR Suspension: {_suspensionForces[3]:F0} N", labelStyle);
        yPos += lineHeight;
    
        // 6. Расстояние от каждого колеса до земли
        GUI.Label(new Rect(20, yPos, 280, 20), $"FL Distance: {_wheelToGroundDistances[0]:F3} m", labelStyle);
        yPos += lineHeight;
    
        GUI.Label(new Rect(20, yPos, 280, 20), $"FR Distance: {_wheelToGroundDistances[1]:F3} m", labelStyle);
        yPos += lineHeight;
    
        GUI.Label(new Rect(20, yPos, 280, 20), $"RL Distance: {_wheelToGroundDistances[2]:F3} m", labelStyle);
        yPos += lineHeight;
    
        GUI.Label(new Rect(20, yPos, 280, 20), $"RR Distance: {_wheelToGroundDistances[3]:F3} m", labelStyle);
        yPos += lineHeight;
    
        // 7. Степень сжатия подвески каждого колеса
        GUI.Label(new Rect(20, yPos, 280, 20), $"FL Compression: {_suspensionCompressions[0]:F4} m", labelStyle);
        yPos += lineHeight;
    
        GUI.Label(new Rect(20, yPos, 280, 20), $"FR Compression: {_suspensionCompressions[1]:F4} m", labelStyle);
        yPos += lineHeight;
    
        GUI.Label(new Rect(20, yPos, 280, 20), $"RL Compression: {_suspensionCompressions[2]:F4} m", labelStyle);
        yPos += lineHeight;
    
        GUI.Label(new Rect(20, yPos, 280, 20), $"RR Compression: {_suspensionCompressions[3]:F4} m", labelStyle);
        yPos += lineHeight;
    
        // 8. Высота центра масс автомобиля
        GUI.Label(new Rect(20, yPos, 280, 20), $"COM Height: {_centerOfMassHeight:F3} m", labelStyle);
        yPos += lineHeight;
    
        // Дополнительная информация (если нужно оставить)
        GUI.Label(new Rect(20, yPos, 280, 20), $"Handbrake: {(_isHandbrakeActive ? "ACTIVE" : "inactive")}", 
            new GUIStyle(labelStyle) { 
                normal = { textColor = _isHandbrakeActive ? Color.red : Color.green } 
            });
    }
}