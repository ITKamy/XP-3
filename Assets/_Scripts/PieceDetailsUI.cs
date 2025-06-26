using UnityEngine;
using TMPro; // Necessário para TextMeshProUGUI

public class PieceDetailsUI : MonoBehaviour
{
    [SerializeField] private GameObject panelContainer; // O GameObject do painel que será ativado/desativado
    [SerializeField] private TextMeshProUGUI pieceNameText;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private TextMeshProUGUI shieldText;

    private void Awake()
    {
        // Garante que o painel começa desativado
        if (panelContainer != null)
        {
            panelContainer.SetActive(false);
        }
    }

    // Método para atualizar as informações do painel
    public void UpdatePieceDetails(string pieceName, int currentHealth, int maxHealth, int damage, int shield)
    {
        if (pieceNameText != null)
        {
            pieceNameText.text = pieceName;
        }
        if (healthText != null)
        {
            healthText.text = $"Vida: {currentHealth}/{maxHealth}";
        }
        if (damageText != null)
        {
            damageText.text = $"Dano: {damage}";
        }
        if (shieldText != null)
        {
            shieldText.text = $"Escudo: {shield}";
        }
    }

    // Método para mostrar/esconder o painel
    public void SetPanelVisibility(bool isVisible)
    {
        if (panelContainer != null)
        {
            panelContainer.SetActive(isVisible);
        }
    }
}