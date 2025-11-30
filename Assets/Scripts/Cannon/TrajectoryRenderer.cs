using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryRenderer : MonoBehaviour
{
    [Header("Отрисовка")]
    [SerializeField] private int _pointsCount = 30;
    [SerializeField] private float _timeStep = 0.1f;
    [SerializeField] private float _widthLine = 0.15f;
    private LineRenderer _line;
    
    [Header("Физика воздуха")]
    [SerializeField] private float _mass = 1f;             // кг
    [SerializeField] private float _radius = 0.1f;         // м
    [SerializeField] private float _dragCoefficient = 0.47f;// сфера
    [SerializeField] private float _airDensity = 1.225f;   // кг/м^3
    [SerializeField] private Vector3 _wind = Vector3.zero; // м/с
    
    private float _area; // π r^2

    private void Awake()
    {
        _line = GetComponent<LineRenderer>();
        _line.useWorldSpace = true;
        _line.startWidth = _widthLine;
        _line.endWidth = _widthLine;
        _line.material = new Material(Shader.Find("Sprites/Default"));
    }

    public void SetPreviewParams(float mass, float radius, float dragCoefficient, float airDensity, Vector3 wind)
    {
        _mass = mass;
        _radius = radius;
        _dragCoefficient = dragCoefficient;
        _airDensity = airDensity;
        _wind = wind;
        _area = _radius * _radius * Mathf.PI;
    }


    public void DrawVacuum(Vector3 startPosition, Vector3 startVelocity)
    {
        if (_pointsCount < 2) _pointsCount = 2;

        _line.positionCount = _pointsCount;
        for (int i = 0; i < _pointsCount; i++)
        {
            float t = i * _timeStep;
            Vector3 newPosition = startPosition + startVelocity * t + Physics.gravity * (t * t) / 2;
          
            _line.SetPosition(i, newPosition);
        }
    }
    
    public void DrawWithAirEuler(Vector3 startPosition, Vector3 startVelocity)
    {
        Vector3 p = startPosition;
        Vector3 v = startVelocity;
        _line.positionCount = _pointsCount;

        for (int i = 0; i < _pointsCount; i++)
        {
            _line.SetPosition(i, p);

            // ускорение: g + Fd/m, Fd = -0.5*rho*Cd*A*|v_rel|*v_rel
            Vector3 vRel = v - _wind;
            float speed = vRel.magnitude;
            Vector3 drag = speed > 1e-6f ? (-0.5f * _airDensity * _dragCoefficient * _area * speed) * vRel : Vector3.zero;
            Vector3 a = Physics.gravity + drag / _mass;

            v += a * _timeStep;  // шаг по скорости
            p += v * _timeStep;  // шаг по позиции
        }
    }
    
    private Gradient CreateTrajectoryGradient()
    {
        Gradient gradient = new Gradient();
        gradient.colorKeys = new GradientColorKey[]
        {
            new GradientColorKey(Color.white, 0f),
            new GradientColorKey(Color.yellow, 0.5f),
            new GradientColorKey(Color.red, 1f)
        };
        gradient.alphaKeys = new GradientAlphaKey[]
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(0.8f, 1f)
        };
        return gradient;
    }
}
