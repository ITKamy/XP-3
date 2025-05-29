using UnityEngine;

//�nico momento de altera��o do tamanho da pe�a � quando elas morrem e v�o para o lado o tabuleiro;
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
    private Vector3 desiredScale = Vector3.one;

    private void Update()
    {
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * 10);
        transform.localScale = Vector3.Lerp(transform.localScale, desiredScale, Time.deltaTime * 10);
    }

    public virtual void SetPosition(Vector3 position, bool force = false)
    {
        desiredPosition = position;
        if (force)
            transform.position = desiredPosition;
    }
    public virtual void SetScale(Vector3 scale, bool force = false)
    {
        desiredScale = scale;
        if(force)
            transform.position = desiredScale;   
    }
}
