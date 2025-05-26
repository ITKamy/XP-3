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
    //Team = cor das pe�as (brancas/pretas)|(roxas/laranjas)
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    private Vector3 desiredPosition;

    //�nico momento de altera��o do tamanho da pe�a � quando elas morrem e v�o para o lado o tabuleiro;
    private Vector3 desiredScale;
}
