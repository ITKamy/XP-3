// CardUI.cs
using UnityEngine;
using UnityEngine.UI; // Para Image
using TMPro; // Se voc� estiver usando TextMeshPro

public class CardUI : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI cardNameText; // Ou public Text cardNameText;
    [SerializeField] private TextMeshProUGUI descriptionText; // Ou public Text descriptionText;
    [SerializeField] private TextMeshProUGUI costText; // Ou public Text costText;

    private CardData currentCardData; // Refer�ncia � CardData que esta UI representa

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
        else if (costText != null) // Se voc� n�o quiser mostrar o custo se for 0
            costText.gameObject.SetActive(data.cost > 0);
    }

    public CardData GetCardData()
    {
        return currentCardData;
    }

    // M�todos para intera��o, como OnClick (se for um Button)
    // public void OnCardClicked()
    // {
    //     Debug.Log($"Carta {currentCardData.cardName} clicada!");
    //     // Aqui voc� chamaria o sistema de jogo para tentar jogar esta carta
    //     // GameManager.Instance.PlayCard(this); // Exemplo
    // }
}