using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Board : MonoBehaviour
{
    [Header("Art Stuff")]
    [SerializeField] private Material tileMaterial; // Material padr�o do tile
    [SerializeField] private Material hoverMaterial; // Material aplicado ao tile quando o mouse est� em cima
    [SerializeField] private float tileSize = 1.0f; // Tamanho de cada tile
    [SerializeField] private float yOffset = 0.2f; // Eleva��o do tile/pe�a em rela��o ao Y do tabuleiro
    [SerializeField] private Vector3 boardCenter = Vector3.zero; // Ponto central do tabuleiro
    [SerializeField] private float deathSize = 0.3f;
    [SerializeField] private float deathSpacing = 0.3f;


    [Header("Prefabs & Materiais")]
    [SerializeField] private GameObject[] prefabs; // Prefabs das pe�as
    [SerializeField] private Material[] teamMaterials; // Materiais para cada time

    private ChessPiece[,] chessPieces; // Matriz de pe�as
    private ChessPiece currentlyDragging; // Pe�a sendo arrastada
    private List<ChessPiece> deadpurples = new List<ChessPiece>();
    private List<ChessPiece> deadoranges = new List<ChessPiece>();

    private const int TILE_COUNT_X = 8; // Quantidade de colunas
    private const int TILE_COUNT_Y = 8; // Quantidade de linhas
    private GameObject[,] tiles; // Matriz de tiles
    private Camera currentCamera; // C�mera principal
    private Vector2Int currentHover = -Vector2Int.one; // Tile atualmente sob o cursor, inicializado como inv�lido
    private GameObject previousHoveredTile; // Tile anteriormente sob o cursor
    private Vector3 bounds; // Limites do tabuleiro para centraliza��o

    private void Awake()
    {
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y); // Gera todos os tiles
        SpawnAllPieces(); // Spawna as pe�as
        PositionAllPieces(); // Posiciona as pe�as no tabuleiro
    }

    private void Update()
    {
        if (!currentCamera)
        {
            currentCamera = Camera.main; // Busca a c�mera principal
            if (!currentCamera) // Ainda n�o encontrou a c�mera
            {
                Debug.LogError("C�mera principal n�o encontrada!");
                return;
            }
        }

        RaycastHit info;
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition); // Cria um ray a partir do cursor

        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover"))) // Testa colis�o com tiles
        {
            Vector2Int hitPosition = LookupTileIndex(info.transform.gameObject);

            // Se o hitPosition for v�lido (LookupTileIndex n�o retornou -Vector2Int.one)
            if (hitPosition != -Vector2Int.one)
            {
                if (currentHover != hitPosition)
                {
                    if (previousHoveredTile != null)
                    {
                        previousHoveredTile.GetComponent<MeshRenderer>().material = tileMaterial; // Volta ao material padr�o
                        previousHoveredTile.layer = LayerMask.NameToLayer("Tile");
                    }

                    previousHoveredTile = tiles[hitPosition.x, hitPosition.y];
                    previousHoveredTile.GetComponent<MeshRenderer>().material = hoverMaterial; // Aplica material de hover
                    previousHoveredTile.layer = LayerMask.NameToLayer("Hover"); // Muda layer

                    currentHover = hitPosition;
                }
            }
            else // O raycast atingiu algo, mas LookupTileIndex n�o encontrou o tile (deve ser raro se o raycast s� pega tiles)
            {
                if (previousHoveredTile != null && currentHover != -Vector2Int.one) // currentHover != -Vector2Int.one para evitar reset desnecess�rio
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
                    // TODO: Adicionar verifica��o de turno aqui, se necess�rio
                    currentlyDragging = chessPieces[hitPosition.x, hitPosition.y];
                }
            }

            if (currentlyDragging != null && Input.GetMouseButtonUp(0)) // Solta o mouse
            {
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);

                // Tenta mover a pe�a para a nova posi��o (hitPosition)
                // S� tenta mover se o hitPosition for um tile v�lido
                bool validMove = false;
                if (hitPosition != -Vector2Int.one)
                {
                    validMove = MoveTo(currentlyDragging, hitPosition.x, hitPosition.y);
                }


                if (!validMove) // Se o movimento N�O foi v�lido (ou se soltou fora do tabuleiro)
                {
                    // Retorna a pe�a para sua posi��o original visualmente
                    currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y), true); // true para for�ar, caso SetPosition tenha anima��o
                }
                // Se validMove for true, a pe�a j� foi movida e posicionada corretamente
                // pela chamada a PositionSinglePiece dentro de MoveTo.

                currentlyDragging = null; // Para de arrastar, independentemente se o movimento foi v�lido ou n�o
            }
        }
        else // Raycast n�o atingiu nenhum tile
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

            // Se estava arrastando uma pe�a e soltou o mouse fora do tabuleiro
            if (currentlyDragging != null && Input.GetMouseButtonUp(0))
            {
                // Retorna a pe�a para sua posi��o original
                Vector2Int previousPosition = new Vector2Int(currentlyDragging.currentX, currentlyDragging.currentY);
                currentlyDragging.SetPosition(GetTileCenter(previousPosition.x, previousPosition.y), true);
                currentlyDragging = null;
            }
        }
    }

    private void GenerateAllTiles(float ts, int tileCountX, int tileCountY) // Renomeado tileSize para ts para evitar conflito com o campo da classe
    {
        // bounds agora � calculado para centralizar o tabuleiro na origem do GameObject 'Board'
        // Se boardCenter for (0,0,0), o centro geom�trico do tabuleiro coincide com a posi��o do GameObject 'Board'.
        // Se boardCenter for, por exemplo, ( (tileCountX*ts)/2, 0, (tileCountY*ts)/2 ),
        // o canto inferior esquerdo do tabuleiro coincidir� com a posi��o do GameObject 'Board'.
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
        tileObject.transform.parent = transform; // Os tiles s�o filhos do objeto Board

        Mesh mesh = new Mesh();
        tileObject.AddComponent<MeshFilter>().mesh = mesh;
        tileObject.AddComponent<MeshRenderer>().material = tileMaterial;

        // Calcula a posi��o do centro do tile no espa�o local do objeto Board
        // A eleva��o yOffset � adicionada aqui, mas a posi��o Y final tamb�m depender� do transform.position.y do Board
        float tileActualY = this.yOffset; // yOffset � relativo ao Y do tabuleiro
        Vector3 localTileCenter = new Vector3(x * ts, tileActualY, y * ts) - bounds + new Vector3(ts / 2.0f, 0, ts / 2.0f);

        // Define a posi��o local do tile. A posi��o global ser� transform.TransformPoint(localTileCenter) ou similar.
        // Para simplificar, como os tiles s�o filhos diretos e n�o h� rota��es complexas iniciais,
        // podemos definir a posi��o local e deixar a hierarquia cuidar da posi��o global.
        // Se o 'Board' estiver em (0,0,0) e sem rota��o, a posi��o local � a global.
        // Ajustando para que a posi��o do tile seja relativa ao pai (o objeto Board)
        tileObject.transform.localPosition = new Vector3(localTileCenter.x, tileActualY, localTileCenter.z); // O yOffset j� est� no localTileCenter.y se calculado como acima

        // Os v�rtices da mesh do tile s�o relativos ao centro do tileObject (seu pr�prio transform)
        float halfTile = ts / 2.0f;
        Vector3[] vertices = new Vector3[4];
        // Definindo os v�rtices no plano XZ local do tile, com Y = 0 (pois o yOffset j� est� na posi��o do tileObject)
        vertices[0] = new Vector3(-halfTile, 0, -halfTile); // Bottom-Left
        vertices[1] = new Vector3(-halfTile, 0, halfTile);  // Top-Left
        vertices[2] = new Vector3(halfTile, 0, -halfTile); // Bottom-Right
        vertices[3] = new Vector3(halfTile, 0, halfTile);  // Top-Right

        // Ordem anti-hor�ria (CCW) para faces frontais (vistas de cima)
        // Tri�ngulo 1: BL, TL, TR (0, 1, 3)
        // Tri�ngulo 2: BL, TR, BR (0, 3, 2)
        int[] tris = new int[] { 0, 1, 3, 0, 3, 2 };

        mesh.vertices = vertices;
        mesh.triangles = tris;
        mesh.RecalculateNormals(); // Essencial para ilumina��o correta

        tileObject.layer = LayerMask.NameToLayer("Tile");
        // O BoxCollider ser� automaticamente centralizado e dimensionado para a mesh se adicionado sem par�metros.
        // Se precisar de ajuste fino, defina center e size.
        BoxCollider collider = tileObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(ts, 0.01f, ts); // Colisor fino no Y para evitar problemas de raycast com pe�as

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
        // TODO: Adicionar pe�es (pawns) ou segunda linha se for xadrez tradicional

        // Time Laranja (Linha de cima, y=7)
        chessPieces[0, 7] = SpawnSinglePiece(ChessPieceType.Tanque, orangeTeam);
        chessPieces[1, 7] = SpawnSinglePiece(ChessPieceType.Dano, orangeTeam);
        chessPieces[2, 7] = SpawnSinglePiece(ChessPieceType.Sup, orangeTeam);
        chessPieces[3, 7] = SpawnSinglePiece(ChessPieceType.Flanco, orangeTeam);
        chessPieces[4, 7] = SpawnSinglePiece(ChessPieceType.Rei, orangeTeam);
        chessPieces[5, 7] = SpawnSinglePiece(ChessPieceType.Flanco, orangeTeam);
        chessPieces[6, 7] = SpawnSinglePiece(ChessPieceType.Sup, orangeTeam);
        chessPieces[7, 7] = SpawnSinglePiece(ChessPieceType.Dano, orangeTeam);
        // TODO: Adicionar pe�es (pawns) ou segunda linha se for xadrez tradicional
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        // ATEN��O: Verifique se o seu enum ChessPieceType come�a em 0 ou 1.
        // Se come�ar em 0, use: Instantiate(prefabs[(int)type], ...)
        // Se come�ar em 1 (como Tanque=1, Dano=2,...), ent�o (int)type - 1 est� correto.
        ChessPiece cp = Instantiate(prefabs[(int)type - 1], transform).GetComponent<ChessPiece>(); // Pe�as s�o filhas do Board
        cp.type = type;
        cp.team = team;
        // Garanta que o prefab da pe�a tenha um MeshRenderer
        Renderer pieceRenderer = cp.GetComponent<Renderer>(); // Use Renderer para cobrir MeshRenderer e SkinnedMeshRenderer
        if (pieceRenderer != null)
        {
            pieceRenderer.material = teamMaterials[team];
        }
        else
        {
            Debug.LogError($"Pe�a {type} n�o tem um Renderer para aplicar o material do time.");
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
                    PositionSinglePiece(x, y, true); // true para for�ar posi��o inicial sem anima��o
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
        // Calcula o centro do tile no espa�o do MUNDO
        // this.yOffset � o deslocamento vertical da pe�a/tile em rela��o ao Y do transform do Board.
        // transform.position � a posi��o do objeto Board no mundo.

        // Posi��o local do centro do tile (sem considerar o yOffset ainda, ele � aplicado na altura final)
        Vector3 localBasePos = new Vector3(x * tileSize, 0, y * tileSize) - bounds + new Vector3(tileSize / 2.0f, 0, tileSize / 2.0f);

        // Adiciona o yOffset � componente Y da posi��o local base
        Vector3 localPosWithOffset = localBasePos + new Vector3(0, this.yOffset, 0);

        // Converte a posi��o local calculada para coordenadas do mundo,
        // considerando a posi��o, rota��o e escala do objeto 'Board'.
        return transform.TransformPoint(localPosWithOffset);
    }

    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (cp == null) return false;

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // TODO: Implementar regras de movimento espec�ficas da pe�a (cp.CanMoveTo(x,y, chessPieces))
        // Por enquanto, qualquer movimento para uma casa vazia ou inimiga � "v�lido"
        // Exemplo de verifica��o b�sica (n�o � regra de xadrez, s� para demonstra��o):
        // if (Mathf.Abs(x - previousPosition.x) > 1 || Mathf.Abs(y - previousPosition.y) > 1)
        // {
        //    if(cp.type != ChessPieceType.AlgumTipoQuePodeMoverLonge) return false; // Movimento muito longo
        // }


        // Tem alguma pe�a nessa posi��o de destino?
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y]; // Outra pe�a
            if (cp.team == ocp.team) // N�o pode mover para uma casa ocupada por uma pe�a do mesmo time
                return false;

            // Se for uma pe�a inimiga, ela seria capturada (aqui apenas a removemos da l�gica)
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


            // TODO: Implementar l�gica de captura (ex: mover pe�a capturada para fora do tabuleiro, pontua��o, etc.)
            // Destroy(ocp.gameObject); // Exemplo: destruir a pe�a capturada
        }

        // Atualiza a matriz l�gica
        chessPieces[x, y] = cp;
        if (chessPieces[previousPosition.x, previousPosition.y] == cp) // Garante que s� anula se a pe�a ainda estiver l�
        {
            chessPieces[previousPosition.x, previousPosition.y] = null;
        }

        // Atualiza a posi��o visual e interna da pe�a
        PositionSinglePiece(x, y); // O 'force' aqui � false por padr�o, permitindo anima��o se houver

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
        Debug.LogWarning($"Tile n�o encontrado no lookup: {hitInfo.name}");
        return -Vector2Int.one; // Tile inv�lido ou n�o encontrado
    }
}