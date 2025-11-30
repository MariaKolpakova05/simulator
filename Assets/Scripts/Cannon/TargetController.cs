using UnityEngine;

public class TargetController : MonoBehaviour
{
    private TargetSpawner spawner;
    private Rigidbody rb;
    private float bounceForce;
    private bool useTrigger;

    public void Initialize(TargetSpawner targetSpawner, float bounceForceMultiplier, bool triggerMode)
    {
        spawner = targetSpawner;
        rb = GetComponent<Rigidbody>();
        bounceForce = bounceForceMultiplier;
        useTrigger = triggerMode;
    }

    private void HandleHit(Collision collision)
    {
        //получение точки столкновения и нормали
        Vector3 hitPoint = collision.contacts[0].point;
        Vector3 hitNormal = collision.contacts[0].normal;

        //рассчитывание силы отскока
        Vector3 bounceDirection = Vector3.Reflect(rb.linearVelocity.normalized, hitNormal);
        float incomingSpeed = collision.relativeVelocity.magnitude;

        //применение силы отскока
        rb.linearVelocity = bounceDirection * incomingSpeed * bounceForce;
    }

    private void HandleHit(Collider other)
    {
        //для триггера рассчитываем направление от центра мишени к снаряду
        Vector3 hitDirection = (other.transform.position - transform.position).normalized;
        
        //отскакиваем в противоположном направлении
        Vector3 bounceDirection = -hitDirection;
        
        //применяем силу отскока
        float projectileSpeed = other.GetComponent<Rigidbody>()?.linearVelocity.magnitude ?? 5f;
        rb.linearVelocity = bounceDirection * projectileSpeed * bounceForce;
    }

    void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.OnTargetDestroyed();
        }
    }
}
