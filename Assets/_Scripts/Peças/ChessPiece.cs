using UnityEngine;

public enum ChessPieceType
{
    None = 0,
    Rei = 1,
    Dano = 2,
    Flanco = 3,
    Sup = 4,
    Tanque = 5
}

public class ChessPiece : MonoBehaviour
{
    //Team = cor das peças (brancas/pretas)|(roxas/laranjas)
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    private Vector3 desiredPosition;

    //Único momento de alteração do tamanho da peça é quando elas morrem e vão para o lado o tabuleiro;
    private Vector3 desiredScale;
}
