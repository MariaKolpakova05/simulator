using UnityEngine;
[RequireComponent (typeof(Rigidbody))]

public class QuadriicDrag : MonoBehaviour
{
    private float _mass;
    private float _radius;
    private float _dragCoef;
    private float _airDenisty;
    private Vector3 _wind;

    [SerializeField]private Rigidbody _rb;
    private float _area;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        //масштабируем объект в соответствии с радиусом
        transform.localScale = Vector3.one * (_radius * 2f);
    }

    private void FixedUpdate()
    {
        Vector3 vReal=_rb.linearVelocity-_wind;
        float _speed=vReal.magnitude;
        Vector3 drag=-0.5f* _airDenisty*_dragCoef*_area*_speed*vReal;
        _rb.AddForce (drag,ForceMode.Force);
        

    }
    public void SetPhisicalParams(float mass, float radius, float dragCoef, float airDensity, Vector3 wind, Vector3 initialVelosity)
    {
        _radius = radius;
        _mass = mass;
        _dragCoef = dragCoef;
        _airDenisty = airDensity;
        _wind = wind;

        _rb.mass = _mass;
        _rb.useGravity = true;
        _rb.linearVelocity = initialVelosity;

        _area= _radius* _radius*Mathf.PI;

        transform.localScale = Vector3.one * (_radius * 2f);
    }
}
