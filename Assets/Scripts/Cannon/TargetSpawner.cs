using UnityEngine;
using System.Collections;

public class TargetSpawner : MonoBehaviour
{
    [Header("Спавн мишеней")]
    [SerializeField] private GameObject targetPrefab;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private int maxTargets = 10;
    
    [Header("Диапазон массы")]
    [SerializeField] private float minMass = 0.5f;
    [SerializeField] private float maxMass = 3f;
    
    [Header("Диапазон радиуса")]
    [SerializeField] private float minRadius = 0.5f;
    [SerializeField] private float maxRadius = 2f;
    
    [Header("Начальная скорость")]
    [SerializeField] private float minHorizontalSpeed = 2f;
    [SerializeField] private float maxHorizontalSpeed = 8f;
    
    [Header("Параметры отскока")]
    [SerializeField] private float bounceForceMultiplier = 2f;
    [SerializeField] private bool useTrigger = false;
    
    [Header("Область спавна")]
    [SerializeField] private Vector3 spawnAreaSize = new Vector3(10f, 5f, 10f);
    
    private int currentTargetCount = 0;
    private Coroutine spawnCoroutine;

    void Start()
    {
        StartSpawning();
    }

    public void StartSpawning()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        
        spawnCoroutine = StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }
    }

    private IEnumerator SpawnRoutine()
    {
        while (true)
        {
            if (currentTargetCount < maxTargets)
            {
                SpawnTarget();
            }
            
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnTarget()
    {
        if (targetPrefab == null)
        {
            Debug.LogError("Target prefab is not assigned!");
            return;
        }

        //случайная позиция в области спавна
        Vector3 spawnPosition = transform.position + new Vector3(
            Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2),
            Random.Range(-spawnAreaSize.y / 2, spawnAreaSize.y / 2),
            Random.Range(-spawnAreaSize.z / 2, spawnAreaSize.z / 2)
        );

        //создание мишени
        GameObject target = Instantiate(targetPrefab, spawnPosition, Quaternion.identity);
        currentTargetCount++;

        //настройка мишени
        SetupTarget(target);
    }

    private void SetupTarget(GameObject target)
    {
        //случайные параметры
        float mass = Random.Range(minMass, maxMass);
        float radius = Random.Range(minRadius, maxRadius);
        float horizontalSpeed = Random.Range(minHorizontalSpeed, maxHorizontalSpeed);

        //направление движения (случайное горизонтальное направление)
        Vector3 randomDirection = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized;

        Vector3 initialVelocity = randomDirection * horizontalSpeed;

        //добавление и настройка Rigidbody
        Rigidbody rb = target.GetComponent<Rigidbody>();
        if (rb == null)
            rb = target.AddComponent<Rigidbody>();

        rb.mass = mass;
        rb.linearVelocity = initialVelocity;
        rb.useGravity = false;

        //настройка масштаба (радиуса)
        target.transform.localScale = Vector3.one * radius * 2;

        //добавление сферического коллайдера
        SphereCollider collider = target.GetComponent<SphereCollider>();
        if (collider == null)
            collider = target.AddComponent<SphereCollider>();

        collider.radius = 0.5f; //базовый радиус 0.5, масштаб применяется через transform
        collider.isTrigger = useTrigger; //настройка режима триггера

        //добавление компонента для управления мишенью
        TargetController targetController = target.GetComponent<TargetController>();
        if (targetController == null)
            targetController = target.AddComponent<TargetController>();

        targetController.Initialize(this, bounceForceMultiplier, useTrigger);
    }

    public void OnTargetDestroyed()
    {
        currentTargetCount--;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawCube(transform.position, spawnAreaSize);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, spawnAreaSize);
    }
}
