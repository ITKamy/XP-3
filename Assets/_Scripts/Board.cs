using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;
using static UnityEngine.Rendering.STP;
using Unity.VisualScripting;

public class Board : MonoBehaviour
{
    #region Atributos SerializeField

    [Header("Art Stuff")]
    // Materiais para as casas do tabuleiro
    [SerializeField] private Material tileMaterialLight;
    [SerializeField] private Material tileMaterialDark;
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private Material validMoveMaterial;

    // Tamanho das casas e ajustes de altura
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private float boardBaseHeight = 0.0f;

    [Header("Procedural Cursed Tiles")]
    // Chance de gerar uma casa amaldi�oada
    [Range(0, 1)]
    [SerializeField] private float cursedTileChance = 0.1f;
    [SerializeField] private Material cursedMaterial;
    [SerializeField] private float flashDuration = 0.3f;

    [SerializeField] private GameObject cursedEffectPrefab; // Prefab que ser� instanciado nas casas amaldi�oadas

    [Header("Game Logic")]
    // Qual time come�a jogando e qual time fica na parte de cima do tabuleiro
    [SerializeField] private int startingTeam = 0;
    [SerializeField] private int awayTeam = 1;
    [SerializeField] private string[] teamNames = new string[2] { "Roxo", "Laranja" };

    public bool WinTeam = false;
    public float Timer = 6f;

    [Header("Prefabs & Materiais")]
    // Prefabs das pe�as e materiais de cada time
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    [Header("Camera")]
    // Refer�ncia ao controlador da rota��o da c�mera
    [SerializeField] private CameraRotationController cameraRotator;

    [Header("UI Global")]
    // Refer�ncia � UI de detalhes da pe�a e texto de notifica��es
    [SerializeField] private PieceDetailsUI pieceDetailsUI;
   


    public NotificationPanelUI notificationPanelUI;

    #endregion

    // Vari�veis principais do tabuleiro
    private ChessPiece[,] chessPieces; // Matriz que guarda as pe�as no tabuleiro
    private ChessPiece currentlyDragging; // Pe�a que o jogador est� clicando
    private const int TILE_COUNT_X = 8; // N�mero de colunas do tabuleiro
    private const int TILE_COUNT_Y = 8; // N�mero de linhas do tabuleiro
    private GameObject[,] tiles; // Matriz de casas (tiles) do tabuleiro
    private Camera currentCamera; // Refer�ncia da c�mera usada no jogo
    private Vector2Int currentHover = -Vector2Int.one; // Posi��o da casa onde o mouse est� passando
    private Vector3 bounds; // Centro do tabuleiro (usado pra posicionar corretamente)
    private List<Vector2Int> highlightTiles = new List<Vector2Int>(); // Lista das casas v�lidas para mover
    private int timeDaVez; // Indica de qual time � o turno atual

    private void Awake()
    {
        // Primeiro, geramos todas as casas (tiles) do tabuleiro, passando o tamanho e a quantidade de colunas e linhas
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        // Depois, criamos e posicionamos todas as pe�as no tabuleiro para o in�cio da partida
        SpawnAllPieces();
        PositionAllPieces();

        // Se a UI que mostra detalhes da pe�a estiver configurada, escondemos ela no in�cio do jogo para n�o poluir a tela
        if (pieceDetailsUI != null)
            pieceDetailsUI.SetPanelVisibility(false);

        // --- REMOVIDO: sistema antigo de notifica��o com TextMeshProUGUI ---
        // if (notificationText != null)
        //     notificationText.alpha = 0;

        // Definimos qual time come�a jogando, baseado na vari�vel 'startingTeam' que configuramos no Inspector
        timeDaVez = startingTeam;

        // Pegamos o nome do time que come�a, para mostrar no log, evitando erros caso o �ndice n�o exista
        string startingTeamName = (timeDaVez < teamNames.Length) ? teamNames[timeDaVez] : $"Time {timeDaVez}";

        // Exibimos no console para debug qual time iniciou o jogo, com uma cor amarela para destacar a mensagem
        Debug.Log($"<color=yellow>IN�CIO DE JOGO:</color> � a vez do time {startingTeamName}.");
    }



    private void Update()
    {
        if (WinTeam)
        {
            if (Timer <= 0)
            {

                WinTeam = false; // Reseta a vari�vel para evitar loop infinito
                Config.Instance.ResetGame();      
                ChangeScene.ChangeTo("End");
            }
            else
            {
                Timer -= Time.deltaTime;
            }       
        }

        // Primeiro, verificamos se algu�m ganhou o jogo
        Win();

        // Se ainda n�o temos refer�ncia para a c�mera, tentamos pegar a principal da cena
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            if (!currentCamera)
            {
                Debug.LogError("C�mera n�o encontrada!");
                return; // Sai do Update para evitar erros
            }
        }

        // Criamos um raio a partir da posi��o do mouse na tela em dire��o ao mundo 3D
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit info;

        // Se estamos arrastando uma pe�a, atualizamos sua posi��o para seguir o mouse no plano horizontal
        if (currentlyDragging != null)
        {
            // Definimos um plano horizontal na altura do tabuleiro + offset para posicionar a pe�a
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * (transform.position.y + boardBaseHeight + yOffset));

            // Verificamos a interse��o do raio com esse plano para pegar o ponto correto no mundo
            if (horizontalPlane.Raycast(ray, out float distance))
            {
                // Atualiza a posi��o da pe�a que est� sendo arrastada para o ponto onde o mouse est� no plano
                currentlyDragging.SetPosition(ray.GetPoint(distance));
            }
        }

        // Detecta a casa (tile) que o mouse est� sobrevoando
        Vector2Int newHover = -Vector2Int.one; // Posi��o inv�lida padr�o

        // Dispara um raio para detectar casas (camadas "Tile" e "Hover")
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            // Se acertou algum tile, pega a posi��o (�ndice) daquela casa no tabuleiro
            newHover = LookupTileIndex(info.transform.gameObject);
        }

        // Se o tile que o mouse est� mudou em rela��o ao frame anterior
        if (currentHover != newHover)
        {
            // Se havia um tile anteriormente destacado (hover antigo)
            if (currentHover != -Vector2Int.one && tiles[currentHover.x, currentHover.y] != null)
            {
                TileInfo prevTileInfo = tiles[currentHover.x, currentHover.y].GetComponent<TileInfo>();

                // Se o tile antigo estava entre os tiles v�lidos para movimento, mantemos o material de movimento v�lido
                if (highlightTiles.Contains(currentHover))
                {
                    tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material = validMoveMaterial;
                }
                else
                {
                    // Caso contr�rio, volta o material original da casa e chama a fun��o de sa�da do hover
                    tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material = GetOriginalTileMaterial(currentHover.x, currentHover.y, prevTileInfo.type);
                    prevTileInfo.OnHoverExit();
                }
            }

            // Se o novo tile onde o mouse est� n�o for inv�lido
            if (newHover != -Vector2Int.one)
            {
                GameObject newHoveredTile = tiles[newHover.x, newHover.y];

                // Se esse tile novo n�o est� na lista de casas v�lidas para movimento
                if (!highlightTiles.Contains(newHover))
                {
                    TileInfo newTileInfo = newHoveredTile.GetComponent<TileInfo>();

                    // Se o tile � amaldi�oado, aplicamos um efeito de flash (s� se n�o estiver arrastando uma pe�a)
                    if (newTileInfo.type == TileType.Cursed)
                    {
                        if (currentlyDragging == null)
                            newTileInfo.Flash(flashDuration, 0.5f);
                    }
                    else
                    {
                        // Se n�o for amaldi�oado, trocamos o material para o de hover e garantimos que a transpar�ncia esteja ok
                        newHoveredTile.GetComponent<MeshRenderer>().material = hoverMaterial;
                        newTileInfo.SetAlpha(1f);
                    }
                }
            }
            // Atualizamos a vari�vel com o novo tile que est� sob o mouse
            currentHover = newHover;
        }

        // Se o bot�o esquerdo do mouse foi pressionado neste frame
        if (Input.GetMouseButtonDown(0))
        {
            // Criamos um raio vertical a partir do centro do tile sob o mouse para detectar pe�as nesse tile
            Ray localRay = new Ray(GetTileCenter(currentHover.x, currentHover.y), transform.up); // Dire��o para cima
            Debug.DrawRay(localRay.origin, localRay.direction, Color.red, 5f); // Debug visual do raio

            // Faz um raycast para detectar pe�a na camada "Piece"
            if (Physics.Raycast(localRay, out info, 100, LayerMask.GetMask("Piece")))
            {
                // Pega a pe�a clicada
                ChessPiece clickedPiece = info.transform.GetComponentInChildren<ChessPiece>();

                // Se a pe�a existe e � do time que est� jogando
                if (clickedPiece != null && clickedPiece.team == timeDaVez)
                {
                    // Limpa os destaques visuais antigos
                    ClearHighlights();

                    // Come�a a arrastar essa pe�a
                    currentlyDragging = clickedPiece;

                    // Pega os movimentos v�lidos daquela pe�a no estado atual do tabuleiro
                    highlightTiles = clickedPiece.GetAvailableMoves(chessPieces, tiles, TILE_COUNT_X, TILE_COUNT_Y);

                    // Destaca as casas v�lidas para movimento no tabuleiro
                    HighlightValidMoves();

                    // Atualiza a UI com os dados da pe�a selecionada (vida, dano, escudo, tipo)
                    if (pieceDetailsUI != null)
                    {
                        pieceDetailsUI.UpdatePieceDetails(
                            clickedPiece.type.ToString(),
                            clickedPiece.Health,
                            clickedPiece.maxHealth,
                            clickedPiece.Damage,
                            clickedPiece.Shield
                        );

                        // Mostra o painel com detalhes da pe�a
                        pieceDetailsUI.SetPanelVisibility(true);
                    }
                }
            }
        }

        // Se o bot�o esquerdo do mouse foi solto neste frame
        if (Input.GetMouseButtonUp(0))
        {
            // Se n�o estamos arrastando nenhuma pe�a, n�o faz nada
            if (currentlyDragging == null) return;

            bool validMove = false;

            // Se o tile onde o mouse est� � v�lido e est� entre os destaques de movimento
            if (currentHover != -Vector2Int.one && highlightTiles.Contains(currentHover))
            {
                // Tenta mover a pe�a para a posi��o do tile atual e armazena se foi v�lido
                validMove = MoveTo(currentlyDragging, currentHover.x, currentHover.y);
            }

            // Se o movimento n�o foi v�lido, reposiciona a pe�a na posi��o original
            if (!validMove)
            {
                PositionSinglePiece(currentlyDragging.currentX, currentlyDragging.currentY, true);
            }

            // Esconde o painel da UI de detalhes ao soltar a pe�a
            if (pieceDetailsUI != null) pieceDetailsUI.SetPanelVisibility(false);

            // Limpa os destaques visuais das casas
            ClearHighlights();

            // Para de arrastar a pe�a
            currentlyDragging = null;
        }
    }


    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (cp == null) return false;

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // Se houver uma pe�a inimiga no destino, atacar em vez de mover
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];
            if (ocp.team == cp.team) return false;

            AttackTo(cp, x, y);
            return false;
        }

        // Avisar o tile anterior que a pe�a saiu
        TileInfo oldTile = tiles[previousPosition.x, previousPosition.y].GetComponent<TileInfo>();
        oldTile?.OnPieceExit();

        // Avisar o novo tile que a pe�a entrou
        TileInfo newTile = tiles[x, y].GetComponent<TileInfo>();
        newTile?.OnPieceEnter(cp); // Passa a refer�ncia da pe�a

        // Aplica penalidade se for uma tile amaldi�oada
        if (newTile != null && newTile.type == TileType.Cursed)
        {
            int penaltyAmount = Random.Range(1, 6);
            int penaltyType = Random.Range(0, 3);

            // Armazena o tipo da penalidade para aplicar novamente nos pr�ximos turnos
            newTile.penaltyType = penaltyType;

            string teamName = (cp.team < teamNames.Length) ? teamNames[cp.team] : $"Time {cp.team}";
            string penaltyMessage = "";

            switch (penaltyType)
            {
                case 0:
                    cp.TakeDamage(penaltyAmount);
                    penaltyMessage = $"Time {teamName} pisou na maldi��o! Pe�a perdeu {penaltyAmount} de Vida.";
                    break;
                case 1:
                    cp.ReduceShield(penaltyAmount);
                    penaltyMessage = $"Time {teamName} pisou na maldi��o! Pe�a perdeu {penaltyAmount} de Escudo.";
                    break;
                case 2:
                    cp.ReduceDamage(penaltyAmount);
                    penaltyMessage = $"Time {teamName} pisou na maldi��o! Pe�a perdeu {penaltyAmount} de Dano.";
                    break;
            }

            if (notificationPanelUI != null)
                notificationPanelUI.ShowMessage(penaltyMessage);

        }

        // Atualiza a matriz do tabuleiro
        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        // Move visualmente a pe�a
        PositionSinglePiece(x, y, true);

        // Marca como movida se for do tipo Ataque
        if (cp.type == ChessPieceType.Ataque && !cp.hasMoved)
            cp.hasMoved = true;

        TrocarTurno(); // Passa o turno

        return true;
    }



    private void AttackTo(ChessPiece cp, int x, int y)
    {
        // Se a pe�a atacante for nula, n�o faz nada e retorna
        if (cp == null)
        {
            return;
        }

        // Cria um raio vertical a partir do centro do tile onde est� o mouse (posi��o do ataque)
        Ray localRay = new Ray(GetTileCenter(currentHover.x, currentHover.y), transform.up); // Dire��o para cima

        // Dispara um raio que retorna todas as colis�es na camada "Piece" dentro de 100 unidades, ou seja, pega todas as pe�as nesse tile
        RaycastHit[] hits = Physics.RaycastAll(localRay, 100, LayerMask.GetMask("Piece"));

        // Percorre todas as colis�es detectadas
        foreach (RaycastHit ataque in hits)
        {
            // Se o objeto atingido n�o for a pe�a que est� atacando (para evitar autoataque)
            if (ataque.transform.gameObject != cp.gameObject)
            {
                // Pega o componente ChessPiece da pe�a inimiga
                ChessPiece enemy = ataque.transform.gameObject.GetComponent<ChessPiece>();

                // Se o inimigo ainda tem escudo, reduz o escudo usando o dano da pe�a atacante
                if (enemy.Shield > 0)
                {
                    enemy.ReduceShield(cp.Damage);
                }
                else
                {
                    // Se n�o tem escudo, aplica dano diretamente na vida do inimigo
                    enemy.TakeDamage(cp.Damage);
                }

                // Ap�s o ataque, troca o turno para o pr�ximo jogador
                TrocarTurno();
            }
        }
    }


    private void Win()
    {
        // Vari�veis para indicar se cada time ainda possui pe�as no tabuleiro
        bool purpleteamWin = false;  // Indica se o time Roxo ainda tem pe�as
        bool orangeteamWin = false;  // Indica se o time Laranja ainda tem pe�as

        // Percorre todas as colunas do tabuleiro
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            // Percorre todas as linhas do tabuleiro
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                // Verifica se existe alguma pe�a na posi��o atual
                if (chessPieces[x, y] != null)
                {
                    // Verifica a qual time a pe�a pertence e marca que esse time ainda est� no jogo
                    switch (chessPieces[x, y].team)
                    {
                        case 0: purpleteamWin = true; break;  // Time Roxo tem pelo menos uma pe�a
                        case 1: orangeteamWin = true; break;  // Time Laranja tem pelo menos uma pe�a
                    }
                }

                // Se ambos os times ainda possuem pe�as, o jogo continua
                if (purpleteamWin && orangeteamWin)
                    return;
            }
        }

        // Se chegou aqui, � porque um dos times perdeu todas as pe�as

        if (purpleteamWin)
        {
            // Time Roxo venceu
            if (notificationPanelUI != null)
                notificationPanelUI.ShowMessage(" O TIME ROXO VENCEU!");
        }
        else
        {
            // Time Laranja venceu
            if (notificationPanelUI != null)
                notificationPanelUI.ShowMessage(" O TIME LARANJA VENCEU!");
        }

        WinTeam = true; // Marca que o jogo terminou    
        // Aqui voc� pode adicionar l�gica extra como encerrar o jogo, mostrar bot�o de rein�cio, etc.
    }



    // M�todo que troca o turno entre os dois times
    private void TrocarTurno()
    {
        // Aplica penalidade extra para quem ainda est� na maldi��o
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                TileInfo tile = tiles[x, y].GetComponent<TileInfo>();
                if (tile.type == TileType.Cursed && tile.occupyingPiece != null)
                {
                    ChessPiece p = tile.occupyingPiece;
                    string teamName = (p.team < teamNames.Length) ? teamNames[p.team] : $"Time {p.team}";
                    string message = "";
                    int repeatPenalty = Random.Range(1, 6); // Reaplica penalidade

                    switch (tile.penaltyType)
                    {
                        case 0:
                            p.TakeDamage(repeatPenalty);
                            message = $"Time {teamName} ainda est� na maldi��o!" +
                                $"" +
                                $" Perdeu {repeatPenalty} de Vida.";
                            break;
                        case 1:
                            p.ReduceShield(repeatPenalty);
                            message = $"Time {teamName} ainda est� na maldi��o!" +
                                $"" +
                                $" Perdeu {repeatPenalty} de Escudo.";
                            break;
                        case 2:
                            p.ReduceDamage(repeatPenalty);
                            message = $"Time {teamName} ainda est� na maldi��o! " +
                                $"" +
                                $"Perdeu {repeatPenalty} de Dano.";
                            break;
                    }

                    if (notificationPanelUI != null)
                        notificationPanelUI.ShowMessage(message);

                }
            }
        }

        // Troca o turno
        timeDaVez = 1 - timeDaVez;

        // Mostra no console de quem � a vez
        string currentTeamName = (timeDaVez < teamNames.Length) ? teamNames[timeDaVez] : $"Time {timeDaVez}";
        Debug.Log($"<color=yellow>TURNO MUDOU:</color> Agora � a vez do time {currentTeamName}.");

        // Gira a c�mera, se tiver
        if (cameraRotator != null)
        {
            cameraRotator.StartCameraTransition();
        }
    }


    #region Fun��es de Tabuleiro

    // Fun��o para destacar visualmente todas as casas v�lidas para movimento
    private void HighlightValidMoves()
    {
        foreach (Vector2Int pos in highlightTiles)
        {
            // Troca o material da casa para o material de movimento v�lido
            tiles[pos.x, pos.y].GetComponent<MeshRenderer>().material = validMoveMaterial;

            // Ajusta a transpar�ncia para garantir que o destaque fique vis�vel
            tiles[pos.x, pos.y].GetComponent<TileInfo>().SetAlpha(1f);
        }
    }

    // Fun��o para limpar todos os destaques do tabuleiro, voltando os materiais �s casas originais
    private void ClearHighlights()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (tiles[x, y] != null)
                {
                    // Restaura o material original da casa de acordo com seu tipo (normal ou amaldi�oada)
                    tiles[x, y].GetComponent<MeshRenderer>().material = GetOriginalTileMaterial(x, y, tiles[x, y].GetComponent<TileInfo>().type);

                    // Chama o m�todo para indicar que o mouse saiu do hover naquela casa
                    tiles[x, y].GetComponent<TileInfo>().OnHoverExit();
                }
            }
        }

        // Limpa a lista de casas destacadas para movimento
        highlightTiles.Clear();
    }

    // Retorna o material original da casa com base na posi��o e tipo dela
    private Material GetOriginalTileMaterial(int x, int y, TileType type)
    {
        // Se for uma casa amaldi�oada, retorna o material amaldi�oado
        if (type == TileType.Cursed)
            return cursedMaterial;

        // Caso contr�rio, usa o padr�o de tabuleiro xadrez (casas claras e escuras alternadas)
        return (x + y) % 2 == 0 ? tileMaterialLight : tileMaterialDark;
    }

    // Fun��o que gera todas as casas do tabuleiro proceduralmente
    private void GenerateAllTiles(float ts, int tileCountX, int tileCountY)
    {
        // Calcula o centro do tabuleiro para posicionar as casas corretamente na cena
        bounds = new Vector3((tileCountX * ts) / 2.0f, 0, (tileCountY * ts) / 2.0f);

        // Inicializa a matriz que armazenar� as casas do tabuleiro
        tiles = new GameObject[tileCountX, tileCountY];

        // Gera cada casa individualmente usando dois loops for para colunas e linhas
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                // Chama a fun��o que cria a casa �nica e armazena na matriz
                tiles[x, y] = GenerateSingleTile(ts, x, y);
            }
        }
    }

    // Fun��o que cria um tile (casa) �nico do tabuleiro
    private GameObject GenerateSingleTile(float ts, int x, int y)
    {
        // Cria o objeto Tile
        GameObject t = new GameObject($"Tile_{x}_{y}");
        t.transform.parent = transform;

        // Cria e configura a malha do tile
        Mesh m = new Mesh();
        t.AddComponent<MeshFilter>().mesh = m;
        t.AddComponent<MeshRenderer>();

        // Adiciona o TileInfo com posi��o
        TileInfo ti = t.AddComponent<TileInfo>();
        ti.x = x;
        ti.y = y;

        // Define se ser� uma casa amaldi�oada (fora das linhas iniciais)
        bool isCursed = (y > 1 && y < TILE_COUNT_Y - 2) && (Random.value < cursedTileChance);
        ti.type = isCursed ? TileType.Cursed : TileType.Normal;

        // Define o material base do tile
        Material mat = isCursed ? cursedMaterial : ((x + y) % 2 == 0 ? tileMaterialLight : tileMaterialDark);
        t.GetComponent<MeshRenderer>().material = new Material(mat); // instancia material novo
        ti.SetupTileVisual(tileMaterialLight.color, tileMaterialDark.color, cursedMaterial.color, isCursed);

        // Posiciona o tile no mundo
        t.transform.localPosition = new Vector3(x * ts, boardBaseHeight, y * ts) - bounds + new Vector3(ts / 2f, 0, ts / 2f);

        // Define os v�rtices e tri�ngulos da malha
        Vector3[] v = {
        new Vector3(-ts / 2, 0, -ts / 2),
        new Vector3(-ts / 2, 0, ts / 2),
        new Vector3(ts / 2, 0, -ts / 2),
        new Vector3(ts / 2, 0, ts / 2)
    };
        int[] tr = { 0, 1, 2, 1, 3, 2 };
        m.vertices = v;
        m.triangles = tr;
        m.RecalculateNormals();

        // Adiciona colisor e layer
        t.layer = LayerMask.NameToLayer("Tile");
        t.AddComponent<BoxCollider>().size = new Vector3(ts, 0.01f, ts);

        //  INSTANCIA O MODELO 3D DO EFEITO SE FOR AMALDI�OADO
        if (isCursed && cursedEffectPrefab != null)
        {
            GameObject effect = Instantiate(cursedEffectPrefab, t.transform);
            effect.transform.localPosition = Vector3.up * 0.01f; // Levemente acima do tile
            effect.SetActive(false); // Come�a desativado
            ti.cursedEffectAsset = effect;
        }

        return t;
    }



    private void PositionAllPieces()
    {
        // Percorre todas as posi��es do tabuleiro
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                // Se houver uma pe�a nessa posi��o, chama o m�todo para posicion�-la
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
    }

    // Posiciona uma �nica pe�a na posi��o x,y no tabuleiro
    // O par�metro 'force' indica se o posicionamento deve for�ar a atualiza��o da posi��o visual
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        // Se n�o existe pe�a na posi��o, n�o faz nada
        if (chessPieces[x, y] == null) return;

        ChessPiece cp = chessPieces[x, y];

        // Atualiza as coordenadas atuais da pe�a (�teis para l�gica do jogo)
        cp.currentX = x;
        cp.currentY = y;

        // Move a pe�a visualmente para o centro do tile correspondente
        cp.SetPosition(GetTileCenter(x, y), force);

        // Rotaciona a pe�a para "olhar" na dire��o certa dependendo do time dela
        if (cp.team == awayTeam)
        {
            // Time advers�rio fica rotacionado 180 graus no eixo Y (virado para baixo)
            cp.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else
        {
            // Time local fica na rota��o padr�o (olhando para cima)
            cp.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }

    // Retorna a posi��o central em world space do tile x,y no tabuleiro
    private Vector3 GetTileCenter(int x, int y)
    {
        // Calcula a posi��o somando o tamanho da casa, o offset de altura, e centralizando pelo bounds do tabuleiro
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2f, 0, tileSize / 2f);
    }

    // Dado um GameObject de uma casa (tile), retorna suas coordenadas no tabuleiro
    // Se n�o encontrar, retorna uma posi��o inv�lida (-1, -1)
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);
        return -Vector2Int.one; // Posi��o inv�lida para indicar "n�o encontrado"
    }

    private void SpawnAllPieces()
    {
        // Inicializa a matriz de pe�as com o tamanho do tabuleiro 8x8
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        // Refer�ncia para a configura��o global, que provavelmente controla regras do jogo e tipos de pe�as
        Config config = Config.Instance;

        // Loop para criar as pe�as da primeira linha (linha 0 para o time 0 e linha 7 para o time 1)
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            // Pega um tipo de pe�a aleat�rio para a posi��o atual (pode ser ajustado para ordem fixa)
            ChessPieceType type = config.GetRandomType();

            // Define a posi��o X para o time 1 e time 2
            int positionX_team1 = x;
            int positionX_team2 = x;

            // Se a configura��o do tabuleiro for espelhada, inverte a posi��o X do time 2
            if (config.mirrorBoard)
            {
                positionX_team2 = TILE_COUNT_X - (x + 1);
            }

            // Instancia uma pe�a do tipo escolhido para o time 0 na linha 0
            chessPieces[positionX_team1, 0] = SpawnSinglePiece(type, 0);

            // Instancia uma pe�a do tipo escolhido para o time 1 na �ltima linha do tabuleiro
            chessPieces[positionX_team2, TILE_COUNT_Y - 1] = SpawnSinglePiece(type, 1);

            // modelo antigo que usava uma lista fixa de tipos de pe�a para o time 0
            // chessPieces[x, 0] = SpawnSinglePiece(pieceOrder[x], 0);
        }

        // Loop para criar as pe�as da segunda linha (linha 1 para o time 0 e linha 6 para o time 1)
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            ChessPieceType type = config.GetRandomType();

            int positionX_team1 = x;
            int positionX_team2 = x;

            if (config.mirrorBoard)
            {
                positionX_team2 = TILE_COUNT_X - (x + 1);
            }

            // Instancia as pe�as da segunda linha para o time 0
            chessPieces[positionX_team1, 1] = SpawnSinglePiece(type, 0);

            // Instancia as pe�as da pen�ltima linha para o time 1
            chessPieces[positionX_team2, TILE_COUNT_Y - 2] = SpawnSinglePiece(type, 1);
        }
    }


    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        // Se o tipo da pe�a for 'None' (vazio), n�o cria nada e retorna null
        if (type == ChessPieceType.None) return null;

        // Calcula o �ndice do prefab baseado no tipo da pe�a (subtrai 1 pois enum come�a em 1)
        int pieceIndex = (int)type - 1;

        // Se o �ndice for inv�lido (fora do range dos prefabs), retorna null para evitar erro
        if (pieceIndex < 0 || pieceIndex >= prefabs.Length) return null;

        // Instancia o prefab da pe�a como filho do tabuleiro (this.transform)
        GameObject newPieceObject = Instantiate(prefabs[pieceIndex], transform);

        // Procura o componente ChessPiece dentro do prefab instanciado
        ChessPiece cp = newPieceObject.GetComponentInChildren<ChessPiece>();

        // Se n�o encontrar o script ChessPiece no prefab, exibe erro, destr�i o objeto e retorna null
        if (cp == null)
        {
            Debug.LogError($"FALHA CR�TICA: N�o encontrei o script 'ChessPiece' no prefab '{prefabs[pieceIndex].name}'.");
            Destroy(newPieceObject);
            return null;
        }

        // Configura o tipo e o time da pe�a criada
        cp.type = type;
        cp.team = team;

        // Inicializa os atributos da pe�a (Vida, Escudo, Dano) conforme seu tipo
        switch (type)
        {
            case ChessPieceType.Rei:
                cp.InitializeAttributes(35, 0, 0);
                break;

            case ChessPieceType.Ataque:
                cp.InitializeAttributes(50, 5, 10);
                break;

            case ChessPieceType.Flanco:
                cp.InitializeAttributes(25, 10, 5);
                break;

            case ChessPieceType.Sup:
                cp.InitializeAttributes(15, 0, 5);
                break;

            case ChessPieceType.Tanque:
                cp.InitializeAttributes(10, 10, 20);
                break;
        }

        // Busca o Renderer para aplicar o material correspondente ao time da pe�a
        Renderer pieceRenderer = cp.GetComponentInChildren<Renderer>();

        // Se encontrou o Renderer e existe material para esse time na lista, aplica
        if (pieceRenderer != null && team < teamMaterials.Length)
        {
            // Pega a lista atual de materiais (pode ter m�ltiplos)
            Material[] currentMaterials = pieceRenderer.materials;

            // Substitui todos os materiais pelo material do time da pe�a
            for (int i = 0; i < currentMaterials.Length; i++)
            {
                currentMaterials[i] = teamMaterials[team];
            }

            // Aplica os materiais atualizados ao Renderer
            pieceRenderer.materials = currentMaterials;
        }

        // Retorna o componente ChessPiece da pe�a criada para uso futuro
        return cp;
    }


    private void ShuffleList<T>(List<T> list) { for (int i = list.Count - 1; i > 0; i--) { int r = Random.Range(0, i + 1); T t = list[i]; list[i] = list[r]; list[r] = t; } }
 
    #endregion
    
}