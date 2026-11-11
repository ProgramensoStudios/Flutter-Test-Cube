using System.Globalization;
using UnityEngine;
using UnityEngine.SceneManagement;

// Adjuntar a un GameObject llamado EXACTAMENTE "Cube" (Flutter lo busca por nombre).
public class CubeController : MonoBehaviour
{
    [Header("Límites en X")]
    [SerializeField] float minX = -3f;
    [SerializeField] float maxX = 3f;

    [Header("Movimiento")]
    [SerializeField] float speed = 2f;

    [Header("Envío de posición a Flutter")]
    [SerializeField] float positionSendInterval = 0.1f;

    float _direction;          // -1 = izquierda, 0 = quieto, 1 = derecha
    float _timeSinceLastSend;

    [System.Serializable]
    class OutMessage
    {
        public string type;
        public float x;
    }

    // ---------- Aviso de que la escena cargó ----------

    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Send("scene_loaded");
    }

    // ---------- Movimiento ----------

    void Update()
    {
        if (_direction == 0f) return;

        Vector3 pos = transform.position;
        pos.x += _direction * speed * Time.deltaTime;   // solo cambia X

        string limitHit = null;
        if (pos.x >= maxX) { pos.x = maxX; limitHit = "limit_right"; }
        else if (pos.x <= minX) { pos.x = minX; limitHit = "limit_left"; }

        transform.position = pos;

        if (limitHit != null)
        {
            _direction = 0f;
            Send(limitHit);
            Send("position");
            return;
        }

        _timeSinceLastSend += Time.deltaTime;
        if (_timeSinceLastSend >= positionSendInterval)
        {
            _timeSinceLastSend = 0f;
            Send("position");
        }
    }

    // ---------- Métodos llamados desde Flutter (un solo parámetro string) ----------

    public void MoveLeft(string _)
    {
        _direction = -1f;
    }

    public void MoveRight(string _)
    {
        _direction = 1f;
    }

    public void StopMove(string _)
    {
        _direction = 0f;
        Send("position");
    }

    public void SetSpeed(string value)
    {
        // Siempre InvariantCulture para evitar problemas con coma/punto decimal
        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float s))
        {
            speed = Mathf.Clamp(s, 0.1f, 10f);
        }
    }

    public void ResetPosition(string _)
    {
        _direction = 0f;
        Vector3 pos = transform.position;
        pos.x = 0f;
        transform.position = pos;
        Send("position");
    }

    // ---------- Envío a Flutter (JSON) ----------

    void Send(string type)
    {
        var msg = new OutMessage { type = type, x = transform.position.x };
        SendToFlutter.Send(JsonUtility.ToJson(msg));
    }
}