using UnityEngine;

public enum ChessPieceType
{
    None = 0,
    Sup = 1,
    Tanque = 2,
    Dano = 3,
    Rei = 4,
	Flanco = 5
}

public class S_Pecas : MonoBehaviour
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
