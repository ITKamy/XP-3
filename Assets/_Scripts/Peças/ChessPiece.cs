using UnityEngine;
using System.Collections.Generic;

public enum ChessPieceType
{
    None = 0,
    Rei = 1,
    Ataque = 2,
    Flanco = 3,
    Sup = 4,
    Tanque = 5
}

public class ChessPiece : MonoBehaviour
{
    public int team;
    public int currentX;
    public int currentY;
    public ChessPieceType type;

    [Header("Atributos Atuais")]
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _currentDamage;
    [SerializeField] private int _currentShield;
    public int maxHealth;

    public bool hasMoved = false;

    public int Health
    {
        get { return _currentHealth; }
        set { _currentHealth = Mathf.Clamp(value, 0, 50); }
    }

    public int Damage
    {
        get { return _currentDamage; }
        set { _currentDamage = Mathf.Clamp(value, 0, 30); }
    }

    public int Shield
    {
        get { return _currentShield; }
        set { _currentShield = Mathf.Clamp(value, 0, 30); }
    }

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
        if (force)
            transform.localScale = desiredScale;
    }

    public void InitializeAttributes(int health, int damage, int shield)
    {
        maxHealth = health; // Guarda a vida inicial como vida máxima
        Health = health;
        Damage = damage;
        Shield = shield;
        hasMoved = false;
    }

    public void TakeDamage(int incomingDamage)
    {
        int damageAfterShield = Mathf.Max(0, incomingDamage - Shield);
        Health -= damageAfterShield;
        DiePiece();

    }

    public void DiePiece()
    {
        if (Health <= 0)
        {
            Destroy(gameObject); // Exemplo simples: destrói o objeto da peça
        }
    }


    public void ReduceShield(int amount)
    {
        Shield -= amount;
        if (Shield < 0) Shield = 0;
    }

    public void Heal(int amount)
    {
        Health += amount;
    }

    public void ApplyShieldBoost(int amount)
    {
        Shield += amount;
    }

    public void ApplyDamageBoost(int amount)
    {
        Damage += amount;
    }

    public void ReduceDamage(int amount)
    {
        Damage -= amount;
        if (Damage < 0) Damage = 0; // Garante que o dano não fique negativo
    }

    public virtual List<Vector2Int> GetAvailableMoves(ChessPiece[,] board, GameObject[,] tiles, int boardSizeX, int boardSizeY)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();

        switch (type)
        {
            // LÓGICA PARA O TANQUE: 2 casas em direções retas, sem pular peças.
            case ChessPieceType.Tanque:
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(0, 1), 2);  // Frente
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(0, -1), 2); // Trás
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(1, 0), 2);  // Direita
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(-1, 0), 2); // Esquerda
                break;

            // LÓGICA PARA O ATAQUE: 2 casas para frente, esquerda e direita, sem pular peças.
            case ChessPieceType.Ataque:
                int forwardDirection = (team == 0) ? 1 : -1;
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(0, forwardDirection), 2); // Frente
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(1, 0), 2);                 // Direita
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(-1, 0), 2);                // Esquerda
                break;

            // LÓGICA PARA O SUP: 3 casas nas diagonais, sem pular peças.
            case ChessPieceType.Sup:
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(1, 1), 3);   // Diagonal Superior Direita
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(1, -1), 3);  // Diagonal Inferior Direita
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(-1, 1), 3);  // Diagonal Superior Esquerda
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(-1, -1), 3); // Diagonal Inferior Esquerda
                break;

            // ##### LÓGICA ATUALIZADA PARA O FLANCO #####
            // Anda de 1 a 3 casas em todas as 8 direções, e pode pular peças.
            case ChessPieceType.Flanco:
                // Define todas as 8 direções (retas e diagonais)
                Vector2Int[] allDirections = {
                    new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
                    new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                };

                foreach (Vector2Int dir in allDirections)
                {
                    // Para cada direção, verifica as casas de 1 a 3 de distância
                    for (int i = 1; i <= 3; i++)
                    {
                        int targetX = currentX + dir.x * i;
                        int targetY = currentY + dir.y * i;

                        // Verifica se está dentro do tabuleiro
                        if (targetX >= 0 && targetX < boardSizeX && targetY >= 0 && targetY < boardSizeY)
                        {
                            // Como ele "pode passar por cima", não verificamos o caminho, apenas o destino final.
                            ChessPiece targetPiece = board[targetX, targetY];

                            // Se a casa de destino estiver vazia ou com um inimigo, o movimento é válido
                            if (targetPiece == null || targetPiece.team != this.team)
                            {
                                validMoves.Add(new Vector2Int(targetX, targetY));
                            }
                        }
                    }
                }
                break;
        }

        return validMoves;
    }

    // Função auxiliar para gerar movimentos em linha (para Tanque, Ataque e Sup)
    private void GenerateMovesInLine(ChessPiece[,] board, int boardSizeX, int boardSizeY, List<Vector2Int> validMoves, Vector2Int direction, int maxSteps)
    {
        for (int i = 1; i <= maxSteps; i++)
        {
            int targetX = currentX + direction.x * i;
            int targetY = currentY + direction.y * i;

            if (targetX < 0 || targetX >= boardSizeX || targetY < 0 || targetY >= boardSizeY)
            {
                break; // Saiu do tabuleiro
            }

            ChessPiece targetPiece = board[targetX, targetY];
            if (targetPiece != null)
            {
                // Se a peça for inimiga, adiciona como movimento válido (captura) e para.
                if (targetPiece.team != this.team)
                {
                    validMoves.Add(new Vector2Int(targetX, targetY));
                }
                // Se for amiga ou inimiga, o caminho está bloqueado. Para.
                break;
            }

            // Se a casa estiver vazia, adiciona como movimento válido.
            validMoves.Add(new Vector2Int(targetX, targetY));
        }
    }
}