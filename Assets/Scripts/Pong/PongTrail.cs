using UnityEngine;

[RequireComponent(typeof(TrailRenderer))]
public class PongTrail : MonoBehaviour
{
    [SerializeField] private float startWidth = 0.3f;
    [SerializeField] private float time = 0.35f;

    private TrailRenderer trail;

    private void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        trail.time = time;
        trail.widthMultiplier = startWidth;
    }

    /// <summary>Llamar cuando cambie el color base o el status effect.</summary>
    public void SetColor(Color color)
    {
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(0.8f, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;
    }

    /// <summary>Llamar al reutilizar el pong (pooling) o al teletransportarlo, para que no dibuje una línea entre posiciones.</summary>
    public void ResetTrail() => trail.Clear();
}