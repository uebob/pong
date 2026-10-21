using UnityEngine;

public class PongTargetGlow : MonoBehaviour
{
    [SerializeField] private PongProjectile pong;
    [SerializeField] private Renderer glowRenderer;
    [SerializeField] private Color color = new Color(1f, 0.25f, 0.2f, 1f);
    [SerializeField] private float maxIntensity = 1.2f;
    [SerializeField] private float fadeSpeed = 8f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField, Range(0f, 0.5f)] private float pulseAmount = 0.2f;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private MaterialPropertyBlock block;
    private Transform cam;
    private float current;      // 0..1
    private bool targetingPlayer;

    private void Awake()
    {
        if (pong == null) pong = GetComponentInParent<PongProjectile>();
        block = new MaterialPropertyBlock();
        cam = Camera.main.transform;
        Apply(0f);
    }
    private void OnEnable()
    {
        if (pong == null) return;
        pong.TargetChanged += OnTargetChanged;
        // Sincroniza por si el target ya estaba fijado antes de activarse
        targetingPlayer = IsPlayer(pong.Target);
    }

    private void OnDisable()
    {
        if (pong != null) pong.TargetChanged -= OnTargetChanged;
        ResetGlow();
    }


    /// <summary>Llamar desde tu lógica de homing cuando cambie el objetivo.</summary>
    public void SetTargetingPlayer(bool value) => targetingPlayer = value;

    /// <summary>Para pooling / relanzamiento.</summary>
    public void ResetGlow() { current = 0f; targetingPlayer = false; Apply(0f); }

    private void LateUpdate()
    {
        float goal = targetingPlayer ? 1f : 0f;
        current = Mathf.MoveTowards(current, goal, fadeSpeed * Time.deltaTime);

        if (current <= 0f)
        {
            if (glowRenderer.enabled) glowRenderer.enabled = false;
            return;
        }
        glowRenderer.enabled = true;

        // Billboard
        glowRenderer.transform.rotation = cam.rotation;

        float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        Apply(current * pulse);
    }

    private void Apply(float intensity)
    {
        var c = color * (intensity * maxIntensity);
        c.a = Mathf.Clamp01(intensity);
        glowRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        glowRenderer.SetPropertyBlock(block);
    }

    private void OnTargetChanged(PongProjectile p, Transform newTarget)
    {
        targetingPlayer = IsPlayer(newTarget);
    }

    private static bool IsPlayer(Transform t) =>
    t != null && t == PongTargeting.FindPlayer();
}