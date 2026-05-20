using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Вешается на CameraHolder (дочерний объект Player).
/// Main Camera внутри CameraHolder: localPosition (0, 1.6, -5), localRotation (0,0,0)
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    [SerializeField] private float mouseSensitivity = 2f;

    [Header("Vertical Look Limits")]
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Invert")]
    [SerializeField] private bool invertY = false;

    private float pitch = 0f;
    private float yaw = 0f;
    private Transform playerBody;

    private void Awake()
    {
        playerBody = transform.parent;
        if (playerBody == null)
        {
            Debug.LogError("CameraHolder должен быть дочерним объектом Player!");
            return;
        }

        // Блокируем курсор в центре экрана
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Начальные углы
        yaw = playerBody.eulerAngles.y;
        pitch = transform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f; // нормализуем в диапазон [-180, 180]
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void Update()
    {
        // Получаем движение мыши за этот кадр
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // Применяем чувствительность (без Time.deltaTime, т.к. дельта мыши уже приращение)
        float deltaX = mouseDelta.x * mouseSensitivity;
        float deltaY = mouseDelta.y * mouseSensitivity * (invertY ? -1f : 1f);

        // Обновляем углы
        yaw += deltaX;
        pitch -= deltaY;  // минус – чтобы движение мыши вверх поднимало камеру

        // Ограничиваем угол наклона
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        // Поворачиваем тело игрока по горизонтали (Yaw)
        playerBody.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Поворачиваем камеру по вертикали (Pitch) – локально
        transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}