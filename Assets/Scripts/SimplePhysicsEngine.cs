using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections.Generic;

[RequireComponent(typeof(ForceVisualizer))]
public class SimplePhysicsEngine : MonoBehaviour
{
    [Header("Физические параметры")]

    [SerializeField] private float Mass;
    [SerializeField] private Vector3 Velocity = Vector3.zero;
    [SerializeField] private bool UseGravity = true;
    [SerializeField] private float DragCoefficient = 0.1f;

    [Header("Управление")]
    [SerializeField] private float thrustForce = 10f;

    [Header("Постоянные силы")]
    [SerializeField] private Vector3 windForce ;
    [SerializeField] private Vector3 magneticForce;

    private Vector3 netForce;
    private ForceVisualizer visualizer;
    private Vector3 previousPosition;

    public enum IntegrationMethod { Euler, Verlet }
    public IntegrationMethod Method = IntegrationMethod.Euler;

    private void Start()
    {
        visualizer = GetComponent<ForceVisualizer>();
        previousPosition = transform.position;
    }

    private void ApplyForce(Vector3 force, Color color, string name)
    {
        netForce += force;
        visualizer.AddForce(force, color, name);
    }

    private void IntegrateMotion(Vector3 acceleration)
    {
        switch (Method)
        {
            case IntegrationMethod.Euler:
                Velocity += acceleration * Time.fixedDeltaTime;
                transform.position += Velocity * Time.fixedDeltaTime;
                break;

            case IntegrationMethod.Verlet:
                Vector3 newPos = 2 * transform.position - previousPosition + acceleration * (Time.fixedDeltaTime * Time.fixedDeltaTime);
                previousPosition = transform.position;
                transform.position = newPos;
                Velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;
                break;
        }
    }

    private void FixedUpdate()
    {
        netForce = Vector3.zero;
        visualizer.Forces.Clear();

        // 1. Гравитация
        if (UseGravity)
        {
            Vector3 gravity = Vector3.down * (9.81f * Mass);
            ApplyForce(gravity, Color.yellow, "Gravity");
        }

        // 2. Сопротивление воздуха
        if (Velocity.magnitude > 0.01f)
        {
            Vector3 drag = -Velocity.normalized * (DragCoefficient * Velocity.sqrMagnitude);
            ApplyForce(drag, Color.cyan, "Drag");
        }

        // 3. Постоянные силы
        ApplyForce(windForce, Color.blue, "Wind");
        ApplyForce(magneticForce, Color.magenta, "Magnetic");

        // 4. Ускорение
        Vector3 acceleration = netForce / Mass;

        // 5. Интегрирование
        IntegrateMotion(acceleration);

        // 6. Визуализация скорости
        Debug.DrawRay(transform.position, Velocity, Color.green);

        // Итоговая сила
        visualizer.AddForce(netForce, Color.green, "NET FORCE");
    }

    private void Update()
    {
        // Управление WASD + пробел
        if (Keyboard.current.wKey.isPressed) ApplyForce(Vector3.forward * thrustForce, Color.red, "Thrust_Forward");
        if (Keyboard.current.sKey.isPressed) ApplyForce(Vector3.back * thrustForce, Color.red, "Thrust_Back");
        if (Keyboard.current.aKey.isPressed) ApplyForce(Vector3.left * thrustForce, Color.red, "Thrust_Left");
        if (Keyboard.current.dKey.isPressed) ApplyForce(Vector3.right * thrustForce, Color.red, "Thrust_Right");
        if (Keyboard.current.spaceKey.isPressed) ApplyForce(Vector3.up * (thrustForce * 2), Color.magenta, "Jump");
    }
}


