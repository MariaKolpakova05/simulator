using UnityEngine;
using System;

[RequireComponent (typeof(Rigidbody))]
public class Glider : MonoBehaviour
{
    [SerializeField] private Transform _wingCP;

    private Rigidbody _rigidBody;
    private float _speedMS;

    private float _alphaRad;

    private Vector3 _worldVelocity;

    [SerializeField] private float _airDencity=1.225f;

    [SerializeField] float _wingArea = 1.5f;
    [SerializeField] float _wingAspect = 8f;

    [SerializeField] float _wingCDO = 0.02f;
    [SerializeField] float _wingLapLha = 5.5f;

    private float _Cl, _CD, _qDyn, _Lmag, _Dmag, _qLideK;

    void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {


        Vector3 _vPoint = _rigidBody.GetPointVelocity(_wingCP.position);
        _speedMS = _vPoint.magnitude;
        
        Vector3 flowDir = (-_vPoint).normalized;
        Vector3 xChord = _wingCP.forward;
        Vector3 zUp = _wingCP.up;
        Vector3 ySpan = _wingCP.right;


        float flowX = Vector3.Dot(flowDir, xChord);

        float flowZ = Vector3.Dot(flowDir, zUp);

        _alphaRad = Mathf.Atan2(flowZ, flowX);



        _Cl = _wingLapLha * _alphaRad;
        _CD = _wingCDO + _Cl * _Cl / (Mathf.PI * _wingAspect * 0.85f);


        _qDyn = 0.5f * _airDencity * _speedMS * _speedMS;
        _Lmag = _qDyn * _wingArea * _Cl;
        _Dmag = _qDyn * _wingArea * _CD;

        Vector3 Ddir = -flowDir;

        Vector3 liftDir = Vector3.Cross(flowDir, ySpan);
        liftDir.Normalize();

        Vector3 L = _Lmag * liftDir;
        Vector3 D = _Dmag * Ddir;

        _rigidBody.AddForceAtPosition(L + D, _wingCP.position, ForceMode.Force);
    }

    void Planning()
    {
        _worldVelocity = _rigidBody.linearVelocity;
        _speedMS = _worldVelocity.magnitude;

        Vector3 xChord = _wingCP.forward;
        Vector3 zUp = _wingCP.up;

        Vector3 flowDir = _speedMS > 0 ? -_worldVelocity.normalized : _wingCP.forward;


        float flowX = Vector3.Dot(flowDir, xChord);

        float flowZ = Vector3.Dot(flowDir, zUp);

        _alphaRad = Mathf.Atan2(flowZ, flowX);
    }

    void OnGUI()
    {
        GUI.color = Color.black;
        GUILayout.Label($"Speed: {_speedMS:0.0} m/s");
        GUILayout.Label($"AoA: {_alphaRad*Mathf.Deg2Rad:0.0}");
    }
}
