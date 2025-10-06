using UnityEngine;
[RequireComponent (typeof(Rigidbody))]
public class Gilder : MonoBehaviour
{
    [SerializeField] private Transform _wingCP;
    [Header ("Плотность воздуха")]
    [SerializeField] private float _airDensity=1.225f;
    [Header ("Аэродинамические характеристики крыла")]
    [SerializeField] private float _wingArea=1.5f;//площадь
    [SerializeField] private float _wingAspect=8.0f;//удлинение

    [SerializeField] private float _wingCDO=0.02f;//паразитное сопротивление
    [SerializeField] private float _wingCLaplha=5.5f;

    private Rigidbody _rigidbody;

    private Vector3 _worldVelocity;
    private float _speedMS;
    private float _alfaRad;

    private float CL,CD,qDyn, Lmag, Dmag, qLideK;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        //скорость в точке крыла
        //_vPoint = _rigidbody.GetPointVelocity;

        _worldVelocity = _rigidbody.linearVelocity;
        _speedMS = _worldVelocity.magnitude;

        Vector3 xChord = _wingCP.forward;
        Vector3 zUP = _wingCP.up;

        Vector3 flowDir = _speedMS>0 ? -_worldVelocity.normalized: _wingCP.forward;

        float flowX = Vector3.Dot(flowDir, xChord);
        float flowZ = Vector3.Dot(flowDir, zUP);

        _alfaRad = Mathf.Atan2(flowZ, flowX);
    }
    /*
    private void OnGUI()
    {
        GUI.color = Color.black;
        GUILayout.Label(text: $'Скорость: {_speedMS:0,0} m/s');
        GUILayout.Label(text: $'Угол атаки: {_alphaRad * Mathf.Deg2Rad:0,0} ');
    }*/
}
