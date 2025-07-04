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
    public bool hasHealedThisTurn = false;

    [Header("Atributos Atuais")]
    [SerializeField] private int _currentHealth;
    [SerializeField] private int _currentDamage;
    [SerializeField] private int _currentShield;
    public int maxHealth;

    public bool hasMoved = false;

    public int Health
    {
        get { return _currentHealth; }
        set { _currentHealth = Mathf.Clamp(value, 0, 20); }  // Limite máximo de 20 de vida
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
        hasHealedThisTurn = false;
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
        Health = Mathf.Min(Health + amount, maxHealth);
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

    // Função que tenta curar aliado adjacente se for do tipo SUP e ainda não curou no turno
    public void TryHealAlly(Board board)
    {
        if (type != ChessPieceType.Sup || hasHealedThisTurn)
            return;

        Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1)
        };

        foreach (var dir in directions)
        {
            int nx = currentX + dir.x;
            int ny = currentY + dir.y;

            if (nx >= 0 && nx < 8 && ny >= 0 && ny < 8)
            {
                ChessPiece target = board.GetPieceAt(nx, ny);
                if (target != null && target.team == this.team && target != this && target.Health < target.maxHealth)
                {
                    target.Heal(2); // Cura 2 de vida (balanceado)
                    hasHealedThisTurn = true;
                    board.ShowHealMessage(this, target);
                    return;
                }
            }
        }
    }

    public virtual List<Vector2Int> GetAvailableMoves(ChessPiece[,] board, GameObject[,] tiles, int boardSizeX, int boardSizeY)
    {
        List<Vector2Int> validMoves = new List<Vector2Int>();

        switch (type)
        {
            case ChessPieceType.Tanque:
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(0, 1), 2);
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(0, -1), 2);
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(1, 0), 2);
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(-1, 0), 2);
                break;

            case ChessPieceType.Ataque:
                int forwardDirection = (team == 0) ? 1 : -1;
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(0, forwardDirection), 2);
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(1, 0), 2);
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(-1, 0), 2);
                break;

            case ChessPieceType.Sup:
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(1, 1), 3);
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(1, -1), 3);
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(-1, 1), 3);
                GenerateMovesInLine(board, boardSizeX, boardSizeY, validMoves, new Vector2Int(-1, -1), 3);
                break;

            case ChessPieceType.Flanco:
                Vector2Int[] allDirections = {
                    new Vector2Int(0, 1), new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(-1, 0),
                    new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, 1), new Vector2Int(-1, -1)
                };

                foreach (Vector2Int dir in allDirections)
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        int targetX = currentX + dir.x * i;
                        int targetY = currentY + dir.y * i;

                        if (targetX >= 0 && targetX < boardSizeX && targetY >= 0 && targetY < boardSizeY)
                        {
                            ChessPiece targetPiece = board[targetX, targetY];
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

    private void GenerateMovesInLine(ChessPiece[,] board, int boardSizeX, int boardSizeY, List<Vector2Int> validMoves, Vector2Int direction, int maxSteps)
    {
        for (int i = 1; i <= maxSteps; i++)
        {
            int targetX = currentX + direction.x * i;
            int targetY = currentY + direction.y * i;

            if (targetX < 0 || targetX >= boardSizeX || targetY < 0 || targetY >= boardSizeY)
            {
                break;
            }

            ChessPiece targetPiece = board[targetX, targetY];
            if (targetPiece != null)
            {
                if (targetPiece.team != this.team)
                {
                    validMoves.Add(new Vector2Int(targetX, targetY));
                }
                break;
            }

            validMoves.Add(new Vector2Int(targetX, targetY));
        }
    }
}
