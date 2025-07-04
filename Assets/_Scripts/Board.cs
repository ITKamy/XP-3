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
    // Chance de gerar uma casa amaldiçoada
    [Range(0, 1)]
    [SerializeField] private float cursedTileChance = 0.1f;
    [SerializeField] private Material cursedMaterial;
    [SerializeField] private float flashDuration = 0.3f;

    [SerializeField] private GameObject cursedEffectPrefab; // Prefab que será instanciado nas casas amaldiçoadas

    [Header("Game Logic")]
    // Qual time começa jogando e qual time fica na parte de cima do tabuleiro
    [SerializeField] private int startingTeam = 0;
    [SerializeField] private int awayTeam = 1;
    [SerializeField] private string[] teamNames = new string[2] { "Roxo", "Laranja" };

    public bool WinTeam = false;
    public float Timer = 6f;

    [Header("Prefabs & Materiais")]
    // Prefabs das peças e materiais de cada time
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    [Header("Camera")]
    // Referência ao controlador da rotação da câmera
    [SerializeField] private CameraRotationController cameraRotator;

    [Header("UI Global")]
    // Referência à UI de detalhes da peça e texto de notificações
    [SerializeField] private PieceDetailsUI pieceDetailsUI;
   


    public NotificationPanelUI notificationPanelUI;

    #endregion

    // Variáveis principais do tabuleiro
    private ChessPiece[,] chessPieces; // Matriz que guarda as peças no tabuleiro
    private ChessPiece currentlyDragging; // Peça que o jogador está clicando
    private const int TILE_COUNT_X = 8; // Número de colunas do tabuleiro
    private const int TILE_COUNT_Y = 8; // Número de linhas do tabuleiro
    private GameObject[,] tiles; // Matriz de casas (tiles) do tabuleiro
    private Camera currentCamera; // Referência da câmera usada no jogo
    private Vector2Int currentHover = -Vector2Int.one; // Posição da casa onde o mouse está passando
    private Vector3 bounds; // Centro do tabuleiro (usado pra posicionar corretamente)
    private List<Vector2Int> highlightTiles = new List<Vector2Int>(); // Lista das casas válidas para mover
    private int timeDaVez; // Indica de qual time é o turno atual

    private void Awake()
    {
        // Primeiro, geramos todas as casas (tiles) do tabuleiro, passando o tamanho e a quantidade de colunas e linhas
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);

        // Depois, criamos e posicionamos todas as peças no tabuleiro para o início da partida
        SpawnAllPieces();
        PositionAllPieces();

        // Se a UI que mostra detalhes da peça estiver configurada, escondemos ela no início do jogo para não poluir a tela
        if (pieceDetailsUI != null)
            pieceDetailsUI.SetPanelVisibility(false);

        // --- REMOVIDO: sistema antigo de notificação com TextMeshProUGUI ---
        // if (notificationText != null)
        //     notificationText.alpha = 0;

        // Definimos qual time começa jogando, baseado na variável 'startingTeam' que configuramos no Inspector
        timeDaVez = startingTeam;

        // Pegamos o nome do time que começa, para mostrar no log, evitando erros caso o índice não exista
        string startingTeamName = (timeDaVez < teamNames.Length) ? teamNames[timeDaVez] : $"Time {timeDaVez}";

        // Exibimos no console para debug qual time iniciou o jogo, com uma cor amarela para destacar a mensagem
        Debug.Log($"<color=yellow>INÍCIO DE JOGO:</color> É a vez do time {startingTeamName}.");
    }



    private void Update()
    {
        if (WinTeam)
        {
            if (Timer <= 0)
            {

                WinTeam = false; // Reseta a variável para evitar loop infinito
                Config.Instance.ResetGame();      
                ChangeScene.ChangeTo("End");
            }
            else
            {
                Timer -= Time.deltaTime;
            }       
        }

        // Primeiro, verificamos se alguém ganhou o jogo
        Win();

        // Se ainda não temos referência para a câmera, tentamos pegar a principal da cena
        if (!currentCamera)
        {
            currentCamera = Camera.main;
            if (!currentCamera)
            {
                Debug.LogError("Câmera não encontrada!");
                return; // Sai do Update para evitar erros
            }
        }

        // Criamos um raio a partir da posição do mouse na tela em direção ao mundo 3D
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit info;

        // Se estamos arrastando uma peça, atualizamos sua posição para seguir o mouse no plano horizontal
        if (currentlyDragging != null)
        {
            // Definimos um plano horizontal na altura do tabuleiro + offset para posicionar a peça
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * (transform.position.y + boardBaseHeight + yOffset));

            // Verificamos a interseção do raio com esse plano para pegar o ponto correto no mundo
            if (horizontalPlane.Raycast(ray, out float distance))
            {
                // Atualiza a posição da peça que está sendo arrastada para o ponto onde o mouse está no plano
                currentlyDragging.SetPosition(ray.GetPoint(distance));
            }
        }

        // Detecta a casa (tile) que o mouse está sobrevoando
        Vector2Int newHover = -Vector2Int.one; // Posição inválida padrão

        // Dispara um raio para detectar casas (camadas "Tile" e "Hover")
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            // Se acertou algum tile, pega a posição (índice) daquela casa no tabuleiro
            newHover = LookupTileIndex(info.transform.gameObject);
        }

        // Se o tile que o mouse está mudou em relação ao frame anterior
        if (currentHover != newHover)
        {
            // Se havia um tile anteriormente destacado (hover antigo)
            if (currentHover != -Vector2Int.one && tiles[currentHover.x, currentHover.y] != null)
            {
                TileInfo prevTileInfo = tiles[currentHover.x, currentHover.y].GetComponent<TileInfo>();

                // Se o tile antigo estava entre os tiles válidos para movimento, mantemos o material de movimento válido
                if (highlightTiles.Contains(currentHover))
                {
                    tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material = validMoveMaterial;
                }
                else
                {
                    // Caso contrário, volta o material original da casa e chama a função de saída do hover
                    tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material = GetOriginalTileMaterial(currentHover.x, currentHover.y, prevTileInfo.type);
                    prevTileInfo.OnHoverExit();
                }
            }

            // Se o novo tile onde o mouse está não for inválido
            if (newHover != -Vector2Int.one)
            {
                GameObject newHoveredTile = tiles[newHover.x, newHover.y];

                // Se esse tile novo não está na lista de casas válidas para movimento
                if (!highlightTiles.Contains(newHover))
                {
                    TileInfo newTileInfo = newHoveredTile.GetComponent<TileInfo>();

                    // Se o tile é amaldiçoado, aplicamos um efeito de flash (só se não estiver arrastando uma peça)
                    if (newTileInfo.type == TileType.Cursed)
                    {
                        if (currentlyDragging == null)
                            newTileInfo.Flash(flashDuration, 0.5f);
                    }
                    else
                    {
                        // Se não for amaldiçoado, trocamos o material para o de hover e garantimos que a transparência esteja ok
                        newHoveredTile.GetComponent<MeshRenderer>().material = hoverMaterial;
                        newTileInfo.SetAlpha(1f);
                    }
                }
            }
            // Atualizamos a variável com o novo tile que está sob o mouse
            currentHover = newHover;
        }

        // Se o botão esquerdo do mouse foi pressionado neste frame
        if (Input.GetMouseButtonDown(0))
        {
            // Criamos um raio vertical a partir do centro do tile sob o mouse para detectar peças nesse tile
            Ray localRay = new Ray(GetTileCenter(currentHover.x, currentHover.y), transform.up); // Direção para cima
            Debug.DrawRay(localRay.origin, localRay.direction, Color.red, 5f); // Debug visual do raio

            // Faz um raycast para detectar peça na camada "Piece"
            if (Physics.Raycast(localRay, out info, 100, LayerMask.GetMask("Piece")))
            {
                // Pega a peça clicada
                ChessPiece clickedPiece = info.transform.GetComponentInChildren<ChessPiece>();

                // Se a peça existe e é do time que está jogando
                if (clickedPiece != null && clickedPiece.team == timeDaVez)
                {
                    // Limpa os destaques visuais antigos
                    ClearHighlights();

                    // Começa a arrastar essa peça
                    currentlyDragging = clickedPiece;

                    // Pega os movimentos válidos daquela peça no estado atual do tabuleiro
                    highlightTiles = clickedPiece.GetAvailableMoves(chessPieces, tiles, TILE_COUNT_X, TILE_COUNT_Y);

                    // Destaca as casas válidas para movimento no tabuleiro
                    HighlightValidMoves();

                    // Atualiza a UI com os dados da peça selecionada (vida, dano, escudo, tipo)
                    if (pieceDetailsUI != null)
                    {
                        pieceDetailsUI.UpdatePieceDetails(
                            clickedPiece.type.ToString(),
                            clickedPiece.Health,
                            clickedPiece.maxHealth,
                            clickedPiece.Damage,
                            clickedPiece.Shield
                        );

                        // Mostra o painel com detalhes da peça
                        pieceDetailsUI.SetPanelVisibility(true);
                    }
                }
            }
        }

        // Se o botão esquerdo do mouse foi solto neste frame
        if (Input.GetMouseButtonUp(0))
        {
            // Se não estamos arrastando nenhuma peça, não faz nada
            if (currentlyDragging == null) return;

            bool validMove = false;

            // Se o tile onde o mouse está é válido e está entre os destaques de movimento
            if (currentHover != -Vector2Int.one && highlightTiles.Contains(currentHover))
            {
                // Tenta mover a peça para a posição do tile atual e armazena se foi válido
                validMove = MoveTo(currentlyDragging, currentHover.x, currentHover.y);
            }

            // Se o movimento não foi válido, reposiciona a peça na posição original
            if (!validMove)
            {
                PositionSinglePiece(currentlyDragging.currentX, currentlyDragging.currentY, true);
            }

            // Esconde o painel da UI de detalhes ao soltar a peça
            if (pieceDetailsUI != null) pieceDetailsUI.SetPanelVisibility(false);

            // Limpa os destaques visuais das casas
            ClearHighlights();

            // Para de arrastar a peça
            currentlyDragging = null;
        }
    }


    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (cp == null) return false;

        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);

        // Se houver uma peça inimiga no destino, atacar em vez de mover
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];
            if (ocp.team == cp.team) return false;

            AttackTo(cp, x, y);
            return false;
        }

        // Avisar o tile anterior que a peça saiu
        TileInfo oldTile = tiles[previousPosition.x, previousPosition.y].GetComponent<TileInfo>();
        oldTile?.OnPieceExit();

        // Avisar o novo tile que a peça entrou
        TileInfo newTile = tiles[x, y].GetComponent<TileInfo>();
        newTile?.OnPieceEnter(cp); // Passa a referência da peça

        // Aplica penalidade se for uma tile amaldiçoada
        if (newTile != null && newTile.type == TileType.Cursed)
        {
            int penaltyAmount = Random.Range(1, 6);
            int penaltyType = Random.Range(0, 3);

            // Armazena o tipo da penalidade para aplicar novamente nos próximos turnos
            newTile.penaltyType = penaltyType;

            string teamName = (cp.team < teamNames.Length) ? teamNames[cp.team] : $"Time {cp.team}";
            string penaltyMessage = "";

            switch (penaltyType)
            {
                case 0:
                    cp.TakeDamage(penaltyAmount);
                    penaltyMessage = $"Time {teamName} pisou na maldição! Peça perdeu {penaltyAmount} de Vida.";
                    break;
                case 1:
                    cp.ReduceShield(penaltyAmount);
                    penaltyMessage = $"Time {teamName} pisou na maldição! Peça perdeu {penaltyAmount} de Escudo.";
                    break;
                case 2:
                    cp.ReduceDamage(penaltyAmount);
                    penaltyMessage = $"Time {teamName} pisou na maldição! Peça perdeu {penaltyAmount} de Dano.";
                    break;
            }

            if (notificationPanelUI != null)
                notificationPanelUI.ShowMessage(penaltyMessage);

        }

        // Atualiza a matriz do tabuleiro
        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;

        // Move visualmente a peça
        PositionSinglePiece(x, y, true);

        // Marca como movida se for do tipo Ataque
        if (cp.type == ChessPieceType.Ataque && !cp.hasMoved)
            cp.hasMoved = true;

        TrocarTurno(); // Passa o turno

        return true;
    }



    private void AttackTo(ChessPiece cp, int x, int y)
    {
        // Se a peça atacante for nula, não faz nada e retorna
        if (cp == null)
        {
            return;
        }

        // Cria um raio vertical a partir do centro do tile onde está o mouse (posição do ataque)
        Ray localRay = new Ray(GetTileCenter(currentHover.x, currentHover.y), transform.up); // Direção para cima

        // Dispara um raio que retorna todas as colisões na camada "Piece" dentro de 100 unidades, ou seja, pega todas as peças nesse tile
        RaycastHit[] hits = Physics.RaycastAll(localRay, 100, LayerMask.GetMask("Piece"));

        // Percorre todas as colisões detectadas
        foreach (RaycastHit ataque in hits)
        {
            // Se o objeto atingido não for a peça que está atacando (para evitar autoataque)
            if (ataque.transform.gameObject != cp.gameObject)
            {
                // Pega o componente ChessPiece da peça inimiga
                ChessPiece enemy = ataque.transform.gameObject.GetComponent<ChessPiece>();

                // Se o inimigo ainda tem escudo, reduz o escudo usando o dano da peça atacante
                if (enemy.Shield > 0)
                {
                    enemy.ReduceShield(cp.Damage);
                }
                else
                {
                    // Se não tem escudo, aplica dano diretamente na vida do inimigo
                    enemy.TakeDamage(cp.Damage);
                }

                // Após o ataque, troca o turno para o próximo jogador
                TrocarTurno();
            }
        }
    }


    private void Win()
    {
        // Variáveis para indicar se cada time ainda possui peças no tabuleiro
        bool purpleteamWin = false;  // Indica se o time Roxo ainda tem peças
        bool orangeteamWin = false;  // Indica se o time Laranja ainda tem peças

        // Percorre todas as colunas do tabuleiro
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            // Percorre todas as linhas do tabuleiro
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                // Verifica se existe alguma peça na posição atual
                if (chessPieces[x, y] != null)
                {
                    // Verifica a qual time a peça pertence e marca que esse time ainda está no jogo
                    switch (chessPieces[x, y].team)
                    {
                        case 0: purpleteamWin = true; break;  // Time Roxo tem pelo menos uma peça
                        case 1: orangeteamWin = true; break;  // Time Laranja tem pelo menos uma peça
                    }
                }

                // Se ambos os times ainda possuem peças, o jogo continua
                if (purpleteamWin && orangeteamWin)
                    return;
            }
        }

        // Se chegou aqui, é porque um dos times perdeu todas as peças

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
        // Aqui você pode adicionar lógica extra como encerrar o jogo, mostrar botão de reinício, etc.
    }



    // Método que troca o turno entre os dois times
    private void TrocarTurno()
    {
        // Aplica penalidade extra para quem ainda está na maldição
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
                            message = $"Time {teamName} ainda está na maldição!" +
                                $"" +
                                $" Perdeu {repeatPenalty} de Vida.";
                            break;
                        case 1:
                            p.ReduceShield(repeatPenalty);
                            message = $"Time {teamName} ainda está na maldição!" +
                                $"" +
                                $" Perdeu {repeatPenalty} de Escudo.";
                            break;
                        case 2:
                            p.ReduceDamage(repeatPenalty);
                            message = $"Time {teamName} ainda está na maldição! " +
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

        // Mostra no console de quem é a vez
        string currentTeamName = (timeDaVez < teamNames.Length) ? teamNames[timeDaVez] : $"Time {timeDaVez}";
        Debug.Log($"<color=yellow>TURNO MUDOU:</color> Agora é a vez do time {currentTeamName}.");

        // Gira a câmera, se tiver
        if (cameraRotator != null)
        {
            cameraRotator.StartCameraTransition();
        }
    }


    #region Funções de Tabuleiro

    // Função para destacar visualmente todas as casas válidas para movimento
    private void HighlightValidMoves()
    {
        foreach (Vector2Int pos in highlightTiles)
        {
            // Troca o material da casa para o material de movimento válido
            tiles[pos.x, pos.y].GetComponent<MeshRenderer>().material = validMoveMaterial;

            // Ajusta a transparência para garantir que o destaque fique visível
            tiles[pos.x, pos.y].GetComponent<TileInfo>().SetAlpha(1f);
        }
    }

    // Função para limpar todos os destaques do tabuleiro, voltando os materiais às casas originais
    private void ClearHighlights()
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (tiles[x, y] != null)
                {
                    // Restaura o material original da casa de acordo com seu tipo (normal ou amaldiçoada)
                    tiles[x, y].GetComponent<MeshRenderer>().material = GetOriginalTileMaterial(x, y, tiles[x, y].GetComponent<TileInfo>().type);

                    // Chama o método para indicar que o mouse saiu do hover naquela casa
                    tiles[x, y].GetComponent<TileInfo>().OnHoverExit();
                }
            }
        }

        // Limpa a lista de casas destacadas para movimento
        highlightTiles.Clear();
    }

    // Retorna o material original da casa com base na posição e tipo dela
    private Material GetOriginalTileMaterial(int x, int y, TileType type)
    {
        // Se for uma casa amaldiçoada, retorna o material amaldiçoado
        if (type == TileType.Cursed)
            return cursedMaterial;

        // Caso contrário, usa o padrão de tabuleiro xadrez (casas claras e escuras alternadas)
        return (x + y) % 2 == 0 ? tileMaterialLight : tileMaterialDark;
    }

    // Função que gera todas as casas do tabuleiro proceduralmente
    private void GenerateAllTiles(float ts, int tileCountX, int tileCountY)
    {
        // Calcula o centro do tabuleiro para posicionar as casas corretamente na cena
        bounds = new Vector3((tileCountX * ts) / 2.0f, 0, (tileCountY * ts) / 2.0f);

        // Inicializa a matriz que armazenará as casas do tabuleiro
        tiles = new GameObject[tileCountX, tileCountY];

        // Gera cada casa individualmente usando dois loops for para colunas e linhas
        for (int x = 0; x < tileCountX; x++)
        {
            for (int y = 0; y < tileCountY; y++)
            {
                // Chama a função que cria a casa única e armazena na matriz
                tiles[x, y] = GenerateSingleTile(ts, x, y);
            }
        }
    }

    // Função que cria um tile (casa) único do tabuleiro
    private GameObject GenerateSingleTile(float ts, int x, int y)
    {
        // Cria o objeto Tile
        GameObject t = new GameObject($"Tile_{x}_{y}");
        t.transform.parent = transform;

        // Cria e configura a malha do tile
        Mesh m = new Mesh();
        t.AddComponent<MeshFilter>().mesh = m;
        t.AddComponent<MeshRenderer>();

        // Adiciona o TileInfo com posição
        TileInfo ti = t.AddComponent<TileInfo>();
        ti.x = x;
        ti.y = y;

        // Define se será uma casa amaldiçoada (fora das linhas iniciais)
        bool isCursed = (y > 1 && y < TILE_COUNT_Y - 2) && (Random.value < cursedTileChance);
        ti.type = isCursed ? TileType.Cursed : TileType.Normal;

        // Define o material base do tile
        Material mat = isCursed ? cursedMaterial : ((x + y) % 2 == 0 ? tileMaterialLight : tileMaterialDark);
        t.GetComponent<MeshRenderer>().material = new Material(mat); // instancia material novo
        ti.SetupTileVisual(tileMaterialLight.color, tileMaterialDark.color, cursedMaterial.color, isCursed);

        // Posiciona o tile no mundo
        t.transform.localPosition = new Vector3(x * ts, boardBaseHeight, y * ts) - bounds + new Vector3(ts / 2f, 0, ts / 2f);

        // Define os vértices e triângulos da malha
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

        //  INSTANCIA O MODELO 3D DO EFEITO SE FOR AMALDIÇOADO
        if (isCursed && cursedEffectPrefab != null)
        {
            GameObject effect = Instantiate(cursedEffectPrefab, t.transform);
            effect.transform.localPosition = Vector3.up * 0.01f; // Levemente acima do tile
            effect.SetActive(false); // Começa desativado
            ti.cursedEffectAsset = effect;
        }

        return t;
    }



    private void PositionAllPieces()
    {
        // Percorre todas as posições do tabuleiro
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                // Se houver uma peça nessa posição, chama o método para posicioná-la
                if (chessPieces[x, y] != null)
                    PositionSinglePiece(x, y, true);
    }

    // Posiciona uma única peça na posição x,y no tabuleiro
    // O parâmetro 'force' indica se o posicionamento deve forçar a atualização da posição visual
    private void PositionSinglePiece(int x, int y, bool force = false)
    {
        // Se não existe peça na posição, não faz nada
        if (chessPieces[x, y] == null) return;

        ChessPiece cp = chessPieces[x, y];

        // Atualiza as coordenadas atuais da peça (úteis para lógica do jogo)
        cp.currentX = x;
        cp.currentY = y;

        // Move a peça visualmente para o centro do tile correspondente
        cp.SetPosition(GetTileCenter(x, y), force);

        // Rotaciona a peça para "olhar" na direção certa dependendo do time dela
        if (cp.team == awayTeam)
        {
            // Time adversário fica rotacionado 180 graus no eixo Y (virado para baixo)
            cp.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else
        {
            // Time local fica na rotação padrão (olhando para cima)
            cp.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }
    }

    // Retorna a posição central em world space do tile x,y no tabuleiro
    private Vector3 GetTileCenter(int x, int y)
    {
        // Calcula a posição somando o tamanho da casa, o offset de altura, e centralizando pelo bounds do tabuleiro
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2f, 0, tileSize / 2f);
    }

    // Dado um GameObject de uma casa (tile), retorna suas coordenadas no tabuleiro
    // Se não encontrar, retorna uma posição inválida (-1, -1)
    private Vector2Int LookupTileIndex(GameObject hitInfo)
    {
        for (int x = 0; x < TILE_COUNT_X; x++)
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (tiles[x, y] == hitInfo)
                    return new Vector2Int(x, y);
        return -Vector2Int.one; // Posição inválida para indicar "não encontrado"
    }

    private void SpawnAllPieces()
    {
        // Inicializa a matriz de peças com o tamanho do tabuleiro 8x8
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];

        // Referência para a configuração global, que provavelmente controla regras do jogo e tipos de peças
        Config config = Config.Instance;

        // Loop para criar as peças da primeira linha (linha 0 para o time 0 e linha 7 para o time 1)
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            // Pega um tipo de peça aleatório para a posição atual (pode ser ajustado para ordem fixa)
            ChessPieceType type = config.GetRandomType();

            // Define a posição X para o time 1 e time 2
            int positionX_team1 = x;
            int positionX_team2 = x;

            // Se a configuração do tabuleiro for espelhada, inverte a posição X do time 2
            if (config.mirrorBoard)
            {
                positionX_team2 = TILE_COUNT_X - (x + 1);
            }

            // Instancia uma peça do tipo escolhido para o time 0 na linha 0
            chessPieces[positionX_team1, 0] = SpawnSinglePiece(type, 0);

            // Instancia uma peça do tipo escolhido para o time 1 na última linha do tabuleiro
            chessPieces[positionX_team2, TILE_COUNT_Y - 1] = SpawnSinglePiece(type, 1);

            // modelo antigo que usava uma lista fixa de tipos de peça para o time 0
            // chessPieces[x, 0] = SpawnSinglePiece(pieceOrder[x], 0);
        }

        // Loop para criar as peças da segunda linha (linha 1 para o time 0 e linha 6 para o time 1)
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            ChessPieceType type = config.GetRandomType();

            int positionX_team1 = x;
            int positionX_team2 = x;

            if (config.mirrorBoard)
            {
                positionX_team2 = TILE_COUNT_X - (x + 1);
            }

            // Instancia as peças da segunda linha para o time 0
            chessPieces[positionX_team1, 1] = SpawnSinglePiece(type, 0);

            // Instancia as peças da penúltima linha para o time 1
            chessPieces[positionX_team2, TILE_COUNT_Y - 2] = SpawnSinglePiece(type, 1);
        }
    }


    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team)
    {
        // Se o tipo da peça for 'None' (vazio), não cria nada e retorna null
        if (type == ChessPieceType.None) return null;

        // Calcula o índice do prefab baseado no tipo da peça (subtrai 1 pois enum começa em 1)
        int pieceIndex = (int)type - 1;

        // Se o índice for inválido (fora do range dos prefabs), retorna null para evitar erro
        if (pieceIndex < 0 || pieceIndex >= prefabs.Length) return null;

        // Instancia o prefab da peça como filho do tabuleiro (this.transform)
        GameObject newPieceObject = Instantiate(prefabs[pieceIndex], transform);

        // Procura o componente ChessPiece dentro do prefab instanciado
        ChessPiece cp = newPieceObject.GetComponentInChildren<ChessPiece>();

        // Se não encontrar o script ChessPiece no prefab, exibe erro, destrói o objeto e retorna null
        if (cp == null)
        {
            Debug.LogError($"FALHA CRÍTICA: Não encontrei o script 'ChessPiece' no prefab '{prefabs[pieceIndex].name}'.");
            Destroy(newPieceObject);
            return null;
        }

        // Configura o tipo e o time da peça criada
        cp.type = type;
        cp.team = team;

        // Inicializa os atributos da peça (Vida, Escudo, Dano) conforme seu tipo
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

        // Busca o Renderer para aplicar o material correspondente ao time da peça
        Renderer pieceRenderer = cp.GetComponentInChildren<Renderer>();

        // Se encontrou o Renderer e existe material para esse time na lista, aplica
        if (pieceRenderer != null && team < teamMaterials.Length)
        {
            // Pega a lista atual de materiais (pode ter múltiplos)
            Material[] currentMaterials = pieceRenderer.materials;

            // Substitui todos os materiais pelo material do time da peça
            for (int i = 0; i < currentMaterials.Length; i++)
            {
                currentMaterials[i] = teamMaterials[team];
            }

            // Aplica os materiais atualizados ao Renderer
            pieceRenderer.materials = currentMaterials;
        }

        // Retorna o componente ChessPiece da peça criada para uso futuro
        return cp;
    }


    private void ShuffleList<T>(List<T> list) { for (int i = list.Count - 1; i > 0; i--) { int r = Random.Range(0, i + 1); T t = list[i]; list[i] = list[r]; list[r] = t; } }
 
    #endregion
    
}