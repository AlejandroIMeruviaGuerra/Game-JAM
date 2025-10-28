using UnityEngine;

public class ReelMotion : MonoBehaviour
{
    [Header("Movimiento del carrete")]
    public float spacing = 3.2f;
    public float spinSpeed = 14f;
    public Sprite[] symbols;

    private SpriteRenderer[] renders;
    private float totalHeight;
    private bool isSpinning = false;
    private bool slowingDown = false;
    private bool aligning = false;
    private float currentSpeed = 0f;
    private float targetOffset = 0f;

    private float decel = 10f;
    private float smoothTime = 0.25f;
    private float alignTimer = 0f;
    private float yVelocity = 0f;

    void Start()
    {
        renders = GetComponentsInChildren<SpriteRenderer>();

        for (int i = 0; i < renders.Length; i++)
        {
            renders[i].sprite = symbols[Random.Range(0, symbols.Length)];
            renders[i].transform.localPosition = new Vector3(0, -i * spacing, 0);
        }

        totalHeight = spacing * renders.Length;
    }

    void Update()
    {
        if (isSpinning)
        {
            // Movimiento normal
            foreach (var r in renders)
            {
                Vector3 pos = r.transform.localPosition;
                pos.y -= currentSpeed * Time.deltaTime;

                // Reciclaje arriba
                if (pos.y <= -totalHeight)
                {
                    pos.y += totalHeight;
                    r.sprite = symbols[Random.Range(0, symbols.Length)];
                }

                r.transform.localPosition = pos;
            }

            // Desaceleración
            if (slowingDown)
            {
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, decel * Time.deltaTime);

                if (currentSpeed <= 0.2f && !aligning)
                {
                    aligning = true;
                    targetOffset = CalculateAlignOffset();
                    alignTimer = 0f;
                }

                if (aligning)
                {
                    alignTimer += Time.deltaTime;
                    float step = Mathf.SmoothStep(1f, 0f, alignTimer / 0.3f);

                    foreach (var r in renders)
                    {
                        Vector3 pos = r.transform.localPosition;
                        pos.y += targetOffset * Time.deltaTime * 5f * step;
                        r.transform.localPosition = pos;
                    }

                    // Cuando termina de alinearse
                    if (alignTimer >= 0.3f)
                    {
                        AlignSymbolsExactly();
                        StopReel();
                    }
                }
            }
        }
    }

    // --- Control externo ---
    public void StartSpin(float startSpeed)
    {
        spinSpeed = startSpeed;
        currentSpeed = startSpeed;
        isSpinning = true;
        slowingDown = false;
        aligning = false;
    }

    public void StartSlowDown()
    {
        if (isSpinning)
            slowingDown = true;
    }

    private void StopReel()
    {
        isSpinning = false;
        slowingDown = false;
        aligning = false;
        currentSpeed = 0f;
    }

    public bool IsStopped()
    {
        return !isSpinning && !slowingDown && !aligning;
    }

    // --- Alineación precisa ---
    private float CalculateAlignOffset()
    {
        float closestY = float.MaxValue;
        float offset = 0f;

        foreach (var r in renders)
        {
            float y = r.transform.localPosition.y;
            float dist = Mathf.Abs(y);
            if (dist < closestY)
            {
                closestY = dist;
                offset = -y; // Ajuste en dirección opuesta
            }
        }

        return offset;
    }

    private void AlignSymbolsExactly()
    {
        float offset = CalculateAlignOffset();

        foreach (var r in renders)
        {
            Vector3 pos = r.transform.localPosition;
            pos.y += offset;
            r.transform.localPosition = pos;
        }
    }

    public Sprite GetCenterSymbol()
    {
        SpriteRenderer closest = null;
        float minDist = float.MaxValue;

        foreach (var r in renders)
        {
            float dist = Mathf.Abs(r.transform.localPosition.y);
            if (dist < minDist)
            {
                minDist = dist;
                closest = r;
            }
        }

        return closest ? closest.sprite : null;
    }
}
