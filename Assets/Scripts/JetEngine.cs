using UnityEngine;
[RequireComponent (typeof(Rigidbody))]
public class JetEngine : MonoBehaviour
{
    /*
    [SerializeField] private Transform _nozzle;//сопло двигателя

    [Header("Тяга")]
    [SerializeField] private float _thrustDrySL = 79000f;//сухой режим
    [SerializeField] private float _thrustABSL = 129000f;//форсаж режим

    //скорость изменения РУД(рычаг управления двигателя)
    [SerializeField] private float _throttleRate = 1.0f;
    [SerializeField] private float _throttleStep = 0.05f;//шаг изменения по X/Z

    private Rigidbody _rigidbody;

    //текущее состояние двигателя 
    private float _throttle01;
    private bool _afterBurner;


    private float _speedMS;
    private float _lastAppliedThrust;
    //input
    private inputAction _throttleUpHold;//shift
    private inputAction _throttleDownHold;//LCtrl
    private inputAction _throttleStepUp;//шаг по X
    private inputAction _throttleStepDown;//шаг по Y
    private inputAction _toggleAB; //Lalt

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        _throttle01 = 0.0f;
        _afterBurner = false;
    }

    private void FixedUpdate()
    {
        _speedMS = _rigidbody.linearVelocity.magnitude;
        
        //плавное изменение по удержанию
        float dt = Time.fixedDeltaTime;

        if (_throttleUpHold.IsPressed())
            _throttle01 = Mathf.Clamp01(_throttle01 + _throttleRate * dt);

        if (_throttleDownHold.IsPressed())
            _throttle01 = Mathf.Clamp01(_throttle01 -_throttleRate * dt);
    }*/
}
