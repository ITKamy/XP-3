using UnityEngine;
using System.Collections;


public class TileInfo : MonoBehaviour
{
    public int x;
    public int y;
    public TileType type = TileType.Normal;

    private MeshRenderer meshRenderer;
    private Color _originalLightColor;
    private Color _originalDarkColor;
    private Color _cursedTileBaseColor;
    private Coroutine currentEffectCoroutine = null;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void SetupTileVisual(Color lightColor, Color darkColor, Color cursedColor, bool isCursedType)
    {
        _originalLightColor = lightColor;
        _originalDarkColor = darkColor;
        _cursedTileBaseColor = cursedColor;
        type = isCursedType ? TileType.Cursed : TileType.Normal;

        if (type == TileType.Cursed)
        {
            SetAlpha(1f); // Inicia invisível
        }
        else
        {
            SetAlpha(1f); // Inicia visível
        }
    }

    // OnHoverExit pode ser removido, pois a lógica de hover foi centralizada no Board.
    // Ou pode ser mantido para outros usos futuros, mas não é mais chamado pela lógica principal de hover.
    public void OnHoverExit()
    {
        // Se este método for chamado de forma externa, ele ainda pode restaurar o estado visual.
        // A lógica de hover principal está em Board.HandleTileHover().
        if (type == TileType.Cursed)
        {
            if (meshRenderer != null && meshRenderer.material != null && currentEffectCoroutine == null)
            {
                SetAlpha(1f); // Garante que volte a ser invisível se não estiver piscando
            }
        }
        else
        {
            if (meshRenderer != null && meshRenderer.material != null)
            {
                meshRenderer.material.color = (x + y) % 2 == 0 ? _originalLightColor : _originalDarkColor;
                SetAlpha(1f);
            }
        }
    }

    public void SetAlpha(float alpha)
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            Color c = meshRenderer.material.color;
            c.a = alpha;
            meshRenderer.material.color = c;
        }
    }

    public void Flash(float duration, float maxAlpha)
    {
        if (type != TileType.Cursed) return;
        if (currentEffectCoroutine != null) StopCoroutine(currentEffectCoroutine); // Para qualquer flash anterior
        currentEffectCoroutine = StartCoroutine(FlashCoroutineInternal(duration, maxAlpha));
    }

    private IEnumerator FlashCoroutineInternal(float duration, float maxAlpha)
    {
        if (meshRenderer == null) yield break;
        meshRenderer.material.color = _cursedTileBaseColor; // Garante que a cor base é a da cursed tile

        float timer = 0f;
        while (timer < duration)
        {
            float alpha = Mathf.Lerp(1f, maxAlpha, Mathf.PingPong(timer / (duration / 2f), 1f)); // PingPong para um efeito de pulsação
            SetAlpha(alpha);
            timer += Time.deltaTime;
            yield return null;
        }
        SetAlpha(1); // Volta a ser invisível após o flash
        currentEffectCoroutine = null;
    }
}