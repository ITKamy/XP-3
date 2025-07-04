// CardData.cs
using UnityEngine;

// Define um novo tipo de asset no Unity Editor, em Create -> GameData -> Card
[CreateAssetMenu(fileName = "NewCardData", menuName = "GameData/Card")]
public class CardData : ScriptableObject
{
    [Header("Informa��es da Carta")]
    public string cardName = "Nova Carta";
    [TextArea(3, 5)] // Permite m�ltiplas linhas no Inspector para a descri��o
    public string description = "Descreva o efeito da carta aqui.";
    public Sprite icon; // Imagem para o �cone da carta
    public int cost = 0; // Custo para jogar a carta (pode ser mana, energia, etc.)

    [Header("Efeitos da Carta")]
    // Aqui voc� vai definir o tipo de efeito da carta.
    // Pode ser um enum, ou refer�ncias a outros ScriptableObjects/Scripts.
    // Por exemplo:
    public CardEffectType effectType; // Um enum para categorizar os efeitos (dano, cura, movimento, etc.)
    public int effectValue; // Um valor num�rico para o efeito (ex: +2 dano, -1 turno)

    // Mais tarde, podemos adicionar um m�todo para "executar" o efeito.
    // public void ExecuteEffect() { /* Implementa��o do efeito */ }
}

// Enum para categorizar os tipos de efeito (adicione mais conforme necess�rio)
public enum CardEffectType
{
    None,
    MovePiece,
    HealPiece,
    DamageEnemy,
    ClearCursedTile,
    BoostAttribute,
    DebuffAttribute // Para a discuss�o anterior de "diminuir atributo"
}