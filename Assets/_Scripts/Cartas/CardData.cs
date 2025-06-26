// CardData.cs
using UnityEngine;

// Define um novo tipo de asset no Unity Editor, em Create -> GameData -> Card
[CreateAssetMenu(fileName = "NewCardData", menuName = "GameData/Card")]
public class CardData : ScriptableObject
{
    [Header("Informações da Carta")]
    public string cardName = "Nova Carta";
    [TextArea(3, 5)] // Permite múltiplas linhas no Inspector para a descrição
    public string description = "Descreva o efeito da carta aqui.";
    public Sprite icon; // Imagem para o ícone da carta
    public int cost = 0; // Custo para jogar a carta (pode ser mana, energia, etc.)

    [Header("Efeitos da Carta")]
    // Aqui você vai definir o tipo de efeito da carta.
    // Pode ser um enum, ou referências a outros ScriptableObjects/Scripts.
    // Por exemplo:
    public CardEffectType effectType; // Um enum para categorizar os efeitos (dano, cura, movimento, etc.)
    public int effectValue; // Um valor numérico para o efeito (ex: +2 dano, -1 turno)

    // Mais tarde, podemos adicionar um método para "executar" o efeito.
    // public void ExecuteEffect() { /* Implementação do efeito */ }
}

// Enum para categorizar os tipos de efeito (adicione mais conforme necessário)
public enum CardEffectType
{
    None,
    MovePiece,
    HealPiece,
    DamageEnemy,
    ClearCursedTile,
    BoostAttribute,
    DebuffAttribute // Para a discussão anterior de "diminuir atributo"
}