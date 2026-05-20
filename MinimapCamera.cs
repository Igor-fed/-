using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Повесь на объект MinimapCamera.
/// Камера следует за игроком сверху вниз.
/// </summary>
public class MinimapCamera : MonoBehaviour
{
    [Header("Цель")]
    [SerializeField] private Transform target;          // Player
    [SerializeField] private float height = 50f;        // высота камеры над игроком

    [Header("Зум")]
    [SerializeField] private float zoomDefault = 30f;
    [SerializeField] private float zoomMin     = 10f;
    [SerializeField] private float zoomMax     = 80f;
    [SerializeField] private float zoomSpeed   = 5f;

    [Header("Стрелка игрока на карте (необязательно)")]
    [SerializeField] private RectTransform playerDot;   // Image-стрелка на RawImage

    private Camera minimapCam;

    private void Awake()
    {
        minimapCam = GetComponent<Camera>();
        minimapCam.orthographic     = true;
        minimapCam.orthographicSize = zoomDefault;

        // Если цель не назначена — ищем сами
        if (target == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Следуем за игроком по X и Z, Y фиксирован выше
        transform.position = new Vector3(
            target.position.x,
            target.position.y + height,
            target.position.z
        );

        // Всегда смотрим строго вниз
        transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        // Зум колесом (Shift + колесо чтобы не конфликтовать с хотбаром)
        if (Input.GetKey(KeyCode.LeftShift))
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.01f)
            {
                minimapCam.orthographicSize = Mathf.Clamp(
                    minimapCam.orthographicSize - scroll * zoomSpeed,
                    zoomMin, zoomMax
                );
            }
        }

        // Поворачиваем стрелку игрока вслед за его поворотом
        if (playerDot != null)
        {
            float yaw = target.eulerAngles.y;
            playerDot.localRotation = Quaternion.Euler(0f, 0f, -yaw);
        }
    }
}
