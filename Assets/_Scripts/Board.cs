using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Art Stuff")]
    [SerializeField] private Material tileMaterial; // Material padrão do tile
    [SerializeField] private Material hoverMaterial; // Material aplicado ao tile quando o mouse está em cima
    [SerializeField] private float tileSize = 1.0f; // Tamanho de cada tile
    [SerializeField] private float yOffset = 0.2f; // Elevação do tile/peça em relação ao Y do tabuleiro
    [SerializeField] private Vector3 boardCenter = Vector3.zero; // Ponto central do tabuleiro
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;


    [Header("Prefabs & Materiais")]
    [SerializeField] private GameObject[] prefabs; // Prefabs das peças
    [SerializeField] private Material[] teamMaterials; // Materiais para cada time

    private ChessPiece[,] chessPieces; // Matriz de peças
    private ChessPiece currentlyDragging; // Peça sendo arrastada
    private List<ChessPiece> deadpurples = new List<ChessPiece>();
    private List<ChessPiece> deadoranges = new List<ChessPiece>();

    private const int TILE_COUNT_X = 8; // Quantidade de colunas
    private const int TILE_COUNT_Y = 8; // Quantidade de linhas
    private GameObject[,] tiles; // Matriz de tiles
    private Camera currentCamera; // Câmera principal
    private Vector2Int currentHover = -Vector2Int.one; // Tile atualmente sob o cursor, inicializado como inválido
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
            if (!currentCamera) // Ainda não encontrou a câmera
            {
                Debug.LogError("Câmera principal não encontrada!");
                return;
            }
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition); // Cria um ray a partir do cursor

        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover"))) // Testa colisão com tiles
        {
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            // Se o hitPosition for válido (LookupTileIndex não retornou -Vector2Int.one)
            if (hitPosition != -Vector2Int.one)
            {
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
            }
            else // O raycast atingiu algo, mas LookupTileIndex não encontrou o tile (deve ser raro se o raycast só pega tiles)
            {
                if (previousHoveredTile != null && currentHover != -Vector2Int.one) // currentHover != -Vector2Int.one para evitar reset desnecessário
                {
                    previousHoveredTile.GetComponent<MeshRenderer>().material = tileMaterial;
                    previousHoveredTile.layer = LayerMask.NameToLayer("Tile");
                    previousHoveredTile = null;
                }
                currentHover = -Vector2Int.one;
            }


            if (Input.GetMouseButtonDown(0)) // Clique do mouse
            {
                if (hitPosition != -Vector2Int.one && chessPieces[hitPosition.x, hitPosition.y] != null)
                {
                    // TODO: Adicionar verificação de turno aqui, se necessário
                    currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                }
            }

            if (currentlyDragging != null && Input.GetMouseButtonUp(0)) // Solta o mouse
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                // Tenta mover a peça para a nova posição (hitPosition)
                // Só tenta mover se o hitPosition for um tile válido
                bool validMove = false;
                if (hitPosition != -Vector2Int.one)
                {
                    validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                }


                if (!validMove) // Se o movimento NÃO foi válido (ou se soltou fora do tabuleiro)
                {
                    // Retorna a peça para sua posição original visualmente
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y), true); // true para forçar, caso SetPosition tenha animação
                }
                // Se validMove for true, a peça já foi movida e posicionada corretamente
                // pela chamada a PositionSinglePiece dentro de MoveTo.

                currentlyDragging = null; // Para de arrastar, independentemente se o movimento foi válido ou não
            }
        }
        else // Raycast não atingiu nenhum tile
        {
            if (currentHover != -Vector2Int.one) // Se antes estava sobre um tile
            {
                if (previousHoveredTile != null)
                {
                    previousHoveredTile.GetComponent<MeshRenderer>().material = tileMaterial; // Reseta material
                    previousHoveredTile.layer = LayerMask.NameToLayer("Tile");
                }
                currentHover = -Vector2Int.one;
                previousHoveredTile = null;
            }

            // Se estava arrastando uma peça e soltou o mouse fora do tabuleiro
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                // Retorna a peça para sua posição original
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y), true);
                currentlyDragging = null;
            }
        }
    }

    private void GenerateAllTiles(float ts, int tileCountX, int tileCountY) // Renomeado tileSize para ts para evitar conflito com o campo da classe
    {
        // bounds agora é calculado para centralizar o tabuleiro na origem do GameObject 'Board'
        // Se boardCenter for (0,0,0), o centro geométrico do tabuleiro coincide com a posição do GameObject 'Board'.
        // Se boardCenter for, por exemplo, ( (tileCountX*ts)/2, 0, (tileCountY*ts)/2 ),
        // o canto inferior esquerdo do tabuleiro coincidirá com a posição do GameObject 'Board'.
        bounds = new Vector3((tileCountX * ts) / 2.0f, 0, (tileCountY * ts) / 2.0f) - boardCenter;

        tiles = new GameObject[tileCountX, tileCountY];

        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                tiles[x, y] = GenerateSingleTile(ts, x, y);
            }
        }
    }

    private GameObject GenerateSingleTile(float ts, int x, int y) // Renomeado tileSize para ts
    {
        GameObject tileObject = new GameObject($"Tile_{x}_{y}");
        tileObject.transform.parent = transform; // Os tiles são filhos do objeto Board

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        // Calcula a posição do centro do tile no espaço local do objeto Board
        // A elevação yOffset é adicionada aqui, mas a posição Y final também dependerá do transform.position.y do Board
        float tileActualY = this.yOffset; // yOffset é relativo ao Y do tabuleiro
        Vector3 localTileCenter = new Vector3(x * ts, tileActualY, y * ts) - bounds + new Vector3(ts / 2.0f, 0, ts / 2.0f);

        // Define a posição local do tile. A posição global será transform.TransformPoint(localTileCenter) ou similar.
        // Para simplificar, como os tiles são filhos diretos e não há rotações complexas iniciais,
        // podemos definir a posição local e deixar a hierarquia cuidar da posição global.
        // Se o 'Board' estiver em (0,0,0) e sem rotação, a posição local é a global.
        // Ajustando para que a posição do tile seja relativa ao pai (o objeto Board)
        tileObject.transform.localPosition = new Vector3(localTileCenter.x, tileActualY, localTileCenter.z); // O yOffset já está no localTileCenter.y se calculado como acima

        // Os vértices da mesh do tile são relativos ao centro do tileObject (seu próprio transform)
        float halfTile = ts / 2.0f;
        Vector3[] vertices = new Vector3[4];
        // Definindo os vértices no plano XZ local do tile, com Y = 0 (pois o yOffset já está na posição do tileObject)
        vertices[0] = new Vector3(-halfTile, 0, -halfTile); // Bottom-Left
        vertices[1] = new Vector3(-halfTile, 0, halfTile);  // Top-Left
        vertices[2] = new Vector3(halfTile, 0, -halfTile); // Bottom-Right
        vertices[3] = new Vector3(halfTile, 0, halfTile);  // Top-Right

        // Ordem anti-horária (CCW) para faces frontais (vistas de cima)
        // Triângulo 1: BL, TL, TR (0, 1, 3)
        // Triângulo 2: BL, TR, BR (0, 3, 2)
        int[] tris = new int[] { 0, 1, 3, 0, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals(); // Essencial para iluminação correta

        tileObject.layer = LayerMask.NameToLayer("Tile");
        // O BoxCollider será automaticamente centralizado e dimensionado para a mesh se adicionado sem parâmetros.
        // Se precisar de ajuste fino, defina center e size.
        BoxCollider collider = tileObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(ts, 0.01f, ts); // Colisor fino no Y para evitar problemas de raycast com peças

        return tileObject;
    }

    private void SpawnAllPieces()
    {
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        int purpleTeam = 0;
        int orangeTeam = 1;

        // Time Roxo (Linha de baixo, y=0)
        chessPieces[0, 0] = SpawnSinglePiece(ChessPieceType.Tanque, purpleTeam);
        chessPieces[1, 0] = SpawnSinglePiece(ChessPieceType.Dano, purpleTeam);
        chessPieces[2, 0] = SpawnSinglePiece(ChessPieceType.Sup, purpleTeam);
        chessPieces[3, 0] = SpawnSinglePiece(ChessPieceType.Flanco, purpleTeam);
        chessPieces[4, 0] = SpawnSinglePiece(ChessPieceType.Rei, purpleTeam);
        chessPieces[5, 0] = SpawnSinglePiece(ChessPieceType.Flanco, purpleTeam);
        chessPieces[6, 0] = SpawnSinglePiece(ChessPieceType.Sup, purpleTeam);
        chessPieces[7, 0] = SpawnSinglePiece(ChessPieceType.Dano, purpleTeam);
        // TODO: Adicionar peões (pawns) ou segunda linha se for xadrez tradicional

        // Time Laranja (Linha de cima, y=7)
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Tanque, orangeTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Dano, orangeTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Sup, orangeTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Flanco, orangeTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.Rei, orangeTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Flanco, orangeTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Sup, orangeTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Dano, orangeTeam);
        // TODO: Adicionar peões (pawns) ou segunda linha se for xadrez tradicional
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        // ATENÇÃO: Verifique se o seu enum ChessPieceType começa em 0 ou 1.
        // Se começar em 0, use: Instantiate(prefabs[(int)type], ...)
        // Se começar em 1 (como Tanque=1, Dano=2,...), então (int)type - 1 está correto.
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>(); // Peças são filhas do Board
        cp.type = type;
        cp.team = team;
        // Garanta que o prefab da peça tenha um MeshRenderer
        Renderer pieceRenderer = cp.GetComponent<Renderer>(); // Use Renderer para cobrir MeshRenderer e SkinnedMeshRenderer
        if (pieceRenderer != null)
        {
            pieceRenderer.material = teamMaterials[team];
        }
        else
        {
            Debug.LogError($"Peça {type} não tem um Renderer para aplicar o material do time.");
        }
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
                    PositionSinglePiece(x, y, true); // true para forçar posição inicial sem animação
                }
            }
        }
    }

    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        if (chessPieces[x, y] == null) return;

        chessPieces[x, y].currentX = x;
        chessPieces[x, y].currentY = y;
        chessPieces[x, y].SetPosition(GetTileCenter(x, y), force);
    }

    private Vector3 GetTileCenter(int x, int y)
    {
        // Calcula o centro do tile no espaço do MUNDO
        // this.yOffset é o deslocamento vertical da peça/tile em relação ao Y do transform do Board.
        // transform.position é a posição do objeto Board no mundo.

        // Posição local do centro do tile (sem considerar o yOffset ainda, ele é aplicado na altura final)
        Vector3 localBasePos = new Vector3(x * tileSize, 0, y * tileSize) - bounds + new Vector3(tileSize / 2.0f, 0, tileSize / 2.0f);

        // Adiciona o yOffset à componente Y da posição local base
        Vector3 localPosWithOffset = localBasePos + new Vector3(0, this.yOffset, 0);

        // Converte a posição local calculada para coordenadas do mundo,
        // considerando a posição, rotação e escala do objeto 'Board'.
        return transform.TransformPoint(localPosWithOffset);
    }

    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (cp == null) return false;

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // TODO: Implementar regras de movimento específicas da peça (cp.CanMoveTo(x,y, chessPieces))
        // Por enquanto, qualquer movimento para uma casa vazia ou inimiga é "válido"
        // Exemplo de verificação básica (não é regra de xadrez, só para demonstração):
        // if (Mathf.Abs(x - previousPosition.x) > 1 || Mathf.Abs(y - previousPosition.y) > 1)
        // {
        //    if(cp.type != ChessPieceType.AlgumTipoQuePodeMoverLonge) return false; // Movimento muito longo
        // }


        // Tem alguma peça nessa posição de destino?
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y]; // Outra peça
            if (cp.team == ocp.team) // Não pode mover para uma casa ocupada por uma peça do mesmo time
                return false;

            // Se for uma peça inimiga, ela seria capturada (aqui apenas a removemos da lógica)
            if(ocp.team == 0)
            {
                deadpurples.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
                ocp.SetPosition(new Vector3(8 * tileSize, yOffset - 1 * (tileSize / -2))
                    - bounds + new Vector3(tileSize / 2, 0, tileSize / 2)
                    + (Vector3.forward * deathSpacing) * deadpurples.Count);
            }
            else
            {
                deadoranges.Add(ocp);
                ocp.SetScale(Vector3.one * deathSize);
            }


            // TODO: Implementar lógica de captura (ex: mover peça capturada para fora do tabuleiro, pontuação, etc.)
            // Destroy(ocp.gameObject); // Exemplo: destruir a peça capturada
        }

        // Atualiza a matriz lógica
        chessPieces[x, y] = cp;
        if (chessPieces[previousPosition.x, previousPosition.y] == cp) // Garante que só anula se a peça ainda estiver lá
        {
            chessPieces[previousPosition.x, previousPosition.y] = null;
        }

        // Atualiza a posição visual e interna da peça
        PositionSinglePiece(x, y); // O 'force' aqui é false por padrão, permitindo animação se houver

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
        Debug.LogWarning($"Tile não encontrado no lookup: {hitInfo.name}");
        return -Vector2Int.one; // Tile inválido ou não encontrado
    }
}