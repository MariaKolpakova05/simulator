using UnityEngine;

public class FlightProtection : MonoBehaviour
{
    
    [SerializeField] private FlightController _flightController;
    [SerializeField] private FlightStateLite _flightState;

    [Header("AoA Limiters")]


    [SerializeField]
    private float _aoaSoft = 14f;


    [SerializeField] private float _aoaHard = 18f;

    [Header("G Limiters")]
    [SerializeField]
    private float _gPos = 9;

    [SerializeField] private float _gNeg = -3f;
    [SerializeField] private float _gBlend = 1f;

                             
    public bool AoaWarn { get; private set; }
    public bool GWarn { get; private set; }
    public bool Stall { get; private set; }


    [SerializeField] private float _stallAoa = 17;
    [SerializeField] private float _stallFade = 3;

    [Header("Turbulence (optional)")]
    [SerializeField]
    private bool _useTurb = false;

    [SerializeField] private float _turbTorque = 8;
    [SerializeField] private float _turbForce = 150;
    [SerializeField] private float _turbFilter = 2;

    private Rigidbody _rigidbody;
    private Vector3 _turbTorqueState, _turbForceState;


    private void Awake() => _rigidbody = GetComponent<Rigidbody>();


    private float SoftGate(float soft, float hard, float value)
    {
        if (hard <= soft) return 0;
        if (value <= soft) return 1;
        if (value >= hard) return 0;

        float t = (value - soft) / (hard - soft);
        return 1 - (t * t * (3 - 2 * t));
    }

    public Vector3 ApplyLimiters(Vector3 cmdRateDeg)
    {
        float aoa = _flightState.AoAdeg;
        float nz = _flightState.Nz;


        float kAoa = SoftGate(_aoaSoft, _aoaHard, Mathf.Abs(aoa));
        AoaWarn = Mathf.Abs(aoa) > _aoaSoft;
        cmdRateDeg.x *= kAoa;


        float kG = 1;
        if (nz >= _gPos)
            kG = SoftGate(_gPos, _gPos + _gBlend, nz);
        else if (nz <= _gNeg)
            kG = SoftGate(-_gNeg, -(_gNeg + _gBlend), -nz);

        GWarn = (nz >= _gPos * 0.95f) || (nz <= _gNeg * 0.95f);
        cmdRateDeg.x *= kG;

        Stall = Mathf.Abs(aoa) > _stallAoa;

        return cmdRateDeg;
    }

    private void FixedUpdate()
    {
        if (_useTurb)
        {
            _turbTorqueState = LowPass(
                _turbTorqueState,
                Random.insideUnitSphere * _turbTorque,
                _turbFilter
            );
            _turbForceState = LowPass(
                _turbForceState,
                Random.insideUnitSphere * _turbForce,
                _turbFilter
            );
            _rigidbody.AddRelativeTorque(_turbTorqueState, ForceMode.Force);
            _rigidbody.AddForce(_turbForceState, ForceMode.Force);
        }
    }

    private Vector3 LowPass(Vector3 state, Vector3 target, float tau)
    {
        float dt = Time.fixedDeltaTime;
        float a = Mathf.Clamp01(dt / (tau + 1e-3f));
        return Vector3.Lerp(state, target, a);
    }
}
