// CardUI.cs
using UnityEngine;
using UnityEngine.UI; // Para Image
using TMPro; // Se você estiver usando TextMeshPro

public class CardUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI cardNameText; // Ou public Text cardNameText;
    [SerializeField] private TextMeshProUGUI descriptionText; // Ou public Text descriptionText;
    [SerializeField] private TextMeshProUGUI costText; // Ou public Text costText;

    private CardData currentCardData; // Referência à CardData que esta UI representa

    public void SetupCard(CardData data)
    {
        currentCardData = data;

        if (iconImage != null)
            iconImage.sprite = data.icon;
        if (cardNameText != null)
            cardNameText.text = data.cardName;
        if (descriptionText != null)
            descriptionText.text = data.description;
        if (costText != null)
            costText.text = "Custo: " + data.cost.ToString();
        else if (costText != null) // Se você não quiser mostrar o custo se for 0
            costText.gameObject.SetActive(data.cost > 0);
    }

    public CardData GetCardData()
    {
        return currentCardData;
    }

    // Métodos para interação, como OnClick (se for um Button)
    // public void OnCardClicked()
    // {
    //     Debug.Log($"Carta {currentCardData.cardName} clicada!");
    //     // Aqui você chamaria o sistema de jogo para tentar jogar esta carta
    //     // GameManager.Instance.PlayCard(this); // Exemplo
    // }
}