using UnityEngine;
using UnityEngine.InputSystem;

public class CannonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float muzzleRotationSpeed = 90f;

    [Header("Shooting Settings")]
    [SerializeField] private Transform muzzle; //точка, откуда будет происходить выстрел

    private Vector2 movementInput;
    private float muzzleRotationInput;

    private void Update()
    {
        HandleInput();
        MoveCannon();
        RotateMuzzle();
    }

    private void HandleInput()
    {
        //W/S - движение вперед/назад
        movementInput.y = 0f;
        if (Keyboard.current.wKey.isPressed) movementInput.y += 1f;
        if (Keyboard.current.sKey.isPressed) movementInput.y -= 1f;

        //A/D - движение влево/вправо
        movementInput.x = 0f;
        if (Keyboard.current.dKey.isPressed) movementInput.x += 1f;
        if (Keyboard.current.aKey.isPressed) movementInput.x -= 1f;

        muzzleRotationInput = 0f;
        if (Keyboard.current.eKey.isPressed) muzzleRotationInput += 1f;
        if (Keyboard.current.qKey.isPressed) muzzleRotationInput -= 1f;
    }

    
    private void MoveCannon()
    {
        Vector3 movement = new Vector3(movementInput.x, 0, movementInput.y) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);
    }

    private void RotateMuzzle()
    {
        float muzzleRotation = muzzleRotationInput * muzzleRotationSpeed * Time.deltaTime;
        //поворот дула вокруг локальной оси X
        muzzle.Rotate(muzzleRotation, 0, 0);
    }
}
