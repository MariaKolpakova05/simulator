using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;


public class FlightStateLite : MonoBehaviour
{
    [SerializeField] private Transform _wingChord;

    private const float MinValueForAngeleAttack = 1e-3f;
    public float IAS {  get; private set; }

    public float AoAdeg {  get; private set; }

    public float Nz {  get; private set; }

    public Rigidbody _rigidbody;
    private Vector3 _vPrev;
    private float _tPrev;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _vPrev = _rigidbody.linearVelocity;
        _tPrev=Time.time;
    }

    private void FixedUpdate()
    {
        Vector3 currentVelocity = _rigidbody.linearVelocity;
        IAS = currentVelocity.magnitude;

        if (IAS > MinValueForAngeleAttack)
        {
            Vector3 flow = (-currentVelocity).normalized;
            float flowX = Vector3.Dot(lhs: flow, rhs: _wingChord.forward);
            float FlowZ = Vector3.Dot(lhs: flow, rhs: _wingChord.up);

            AoAdeg = Mathf.Deg2Rad * Mathf.Atan2(y: flowX, FlowZ);
        }
        else 
        {
            AoAdeg = 0;
        }


        float currentTime = Time.time;
        float dt =Mathf.Max(MinValueForAngeleAttack, currentTime - _tPrev);
        Vector3 aWorld =(currentVelocity - _vPrev)/dt;
        float aVert = Vector3.Dot(lhs: aWorld+Physics.gravity, rhs: transform.up);

        Nz =1f+(aVert/Mathf.Abs(Physics.gravity.y));
        _vPrev = currentVelocity;
        _tPrev = currentTime;
    }
}
