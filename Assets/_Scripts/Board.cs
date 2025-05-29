using System;
using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Art Stuff")]
    [SerializeField] private Material tileMaterial; // Material padrão do tile
    [SerializeField] private Material hoverMaterial; // Material aplicado ao tile quando o mouse está em cima
    [SerializeField] private float tileSize = 1.0f; // Tamanho de cada tile
    [SerializeField] private float yOffset = 0.2f; // Elevação do tile
    [SerializeField] private Vector3 boardCenter = Vector3.zero; // Ponto central do tabuleiro

    [Header("Prefabs & Materiais")]
    [SerializeField] private GameObject[] prefabs; // Prefabs das peças
    [SerializeField] private Material[] teamMaterials; // Materiais para cada time

    private ChessPiece[,] chessPieces; // Matriz de peças
    private ChessPiece currentlyDragging; // Peça sendo arrastada
    private const int TILE_COUNT_X = 8; // Quantidade de colunas
    private const int TILE_COUNT_Y = 8; // Quantidade de linhas
    private GameObject[,] tiles; // Matriz de tiles
    private Camera currentCamera; // Câmera principal
    private Vector2Int currentHover; // Tile atualmente sob o cursor
    private GameObject previousHoveredTile; // Tile anteriormente sob o cursor
    private Vector3 bounds; // Limites do tabuleiro para centralização

    private void Awake()
    {
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y); // Gera todos os tiles
        SpawnAllPieces(); // Spawna as peças
        PositionAllPieces(); // Posiciona as peças no tabuleiro
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main; // Busca a câmera principal
            return;
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition); // Cria um ray a partir do cursor

        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover"))) // Testa colisão com tiles
        {
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject); // Descobre o índice do tile atingido

            if (currentHover != hitPosition)
            {
                if (previousHoveredTile != null)
                {
                    previousHoveredTile.GetComponent<MeshRenderer>().material = tileMaterial; // Volta ao material padrão
                    previousHoveredTile.layer = LayerMask.NameToLayer("Tile");
                }

                previousHoveredTile = tiles[hitPosition.x, hitPosition.y];
                previousHoveredTile.GetComponent<MeshRenderer>().material = hoverMaterial; // Aplica material de hover
                previousHoveredTile.layer = LayerMask.NameToLayer("Hover"); // Muda layer

                currentHover = hitPosition;
            }

            if (Input.GetMouseButtonDown(0)) // Clique do mouse
            {
                if (chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    currentlyDragging = chessPieces[hitPosition.x, hitPosition.y]; // Começa a arrastar a peça
                }
            }

            if (currentlyDragging != null && Input.GetMouseButtonUp(0)) // Solta o mouse
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                bool validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y); // Move peça
                if (validMove)
                {
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y));
                    currentlyDragging = null; // Para de arrastar
                }
            }
        }
        else
        {
            if (currentHover != -Vector2Int.one)
            {
                if (previousHoveredTile != null)
                {
                    previousHoveredTile.GetComponent<MeshRenderer>().material = tileMaterial; // Reseta material
                    previousHoveredTile.layer = LayerMask.NameToLayer("Tile");
                }
                currentHover = -Vector2Int.one;
                previousHoveredTile = null;
            }
        }
    }

    private void GenerateAllTiles(float tileSize, int tileCountX, int tileCountY)
    {
        yOffset += transform.position.y; // Adiciona elevação do tabuleiro
        bounds = new Vector3((tileCountX * tileSize) / 2, 0, (tileCountY * tileSize) / 2) - boardCenter; // Centraliza o tabuleiro

        tiles = new GameObject[tileCountX, tileCountY];

        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(tileSize, x, y);
            }
        }
    }

    private GameObject GenerateSingleTile(float tileSize, int x, int y)
    {
        GameObject tileObject = new GameObject($"x{x}, y{y}"); // Cria GameObject
        tileObject.transform.parent = transform;

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        float halfTile = tileSize / 2;
        Vector3 center = new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(halfTile, 0, halfTile);

        Vector3[] vertices = new Vector3[4];
        vertices[0] = center + new Vector3(-halfTile, 0, -halfTile);
        vertices[1] = center + new Vector3(-halfTile, 0, halfTile);
        vertices[2] = center + new Vector3(halfTile, 0, -halfTile);
        vertices[3] = center + new Vector3(halfTile, 0, halfTile);

        int[] tris = new int[] { 0, 1, 2, 1, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        tileObject.layer = LayerMask.NameToLayer("Tile");
        tileObject.AddComponent<BoxCollider>();

        return tileObject;
    }

    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int purpleTeam = 0;
        int orangeTeam = 1;

        // Time Roxo
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Tanque, purpleTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Dano, purpleTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Sup, purpleTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Flanco, purpleTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.Rei, purpleTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Flanco, purpleTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Sup, purpleTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Dano, purpleTeam);

        // Time Laranja
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Tanque, orangeTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Dano, orangeTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Sup, orangeTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Flanco, orangeTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.Rei, orangeTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Flanco, orangeTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Sup, orangeTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Dano, orangeTeam);
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>();
        cp.type = type;
        cp.team = team;
        cp.GetComponent<MeshRenderer>().material = teamMaterials[team];
        return cp;
    }

    private void PositionAllPieces()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    PositionSinglePiece(x, y);
                }
            }
        }
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y),force);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2, 0, tileSize / 2);
    }

    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // Tem alguma peça nessa posição?
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];
            if (cp.team == ocp.team)
                return false;
        }
        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;
        PositionSinglePiece(x, y);
        return true;
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (tiles[x, y] == hitInfo)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return -Vector2Int.one; // Tile inválido
    }
}
