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
    [SerializeField] private Material tileMaterialLight;
    [SerializeField] private Material tileMaterialDark;
    [SerializeField] private Material hoverMaterial;
    [SerializeField] private Material validMoveMaterial;
    [SerializeField] private float tileSize = 1.0f;
    [SerializeField] private float yOffset = 0.2f;
    [SerializeField] private float boardBaseHeight = 0.0f;

    [Header("Procedural Cursed Tiles")]
    [Range(0, 1)]
    [SerializeField] private float cursedTileChance = 0.1f;
    [SerializeField] private Material cursedMaterial;
    [SerializeField] private float flashDuration = 0.3f;

    [Header("Game Logic")]
    [Tooltip("Qual time começa o jogo? (0 ou 1)")]
    [SerializeField] private int startingTeam = 0;
    [Tooltip("Qual time fica na parte de cima do tabuleiro e precisa ser girado? (0 ou 1)")]
    [SerializeField] private int awayTeam = 1;
    [Tooltip("Defina os nomes dos times. A ordem deve bater com os materiais e a lógica de spawn.")]
    [SerializeField] private string[] teamNames = new string[2] { "Roxo", "Laranja" };


    [Header("Prefabs & Materiais")]
    [SerializeField] private GameObject[] prefabs;
    [SerializeField] private Material[] teamMaterials;

    [Header("Camera")]
    [SerializeField] private CameraRotationController cameraRotator;

    [Header("UI Global")]
    [SerializeField] private PieceDetailsUI pieceDetailsUI; // Referência para o seu script
    [SerializeField] private TextMeshProUGUI notificationText;
    #endregion

    private ChessPiece[,] chessPieces;
    private ChessPiece currentlyDragging;
    private const int TILE_COUNT_X = 8;
    private const int TILE_COUNT_Y = 8;
    private GameObject[,] tiles;
    private Camera currentCamera;
    private Vector2Int currentHover = -Vector2Int.one;
    private Vector3 bounds;
    private List<Vector2Int> highlightTiles = new List<Vector2Int>();
    private int timeDaVez;

    private void Awake()
    {
        GenerateAllTiles(tileSize, TILE_COUNT_X, TILE_COUNT_Y);
        SpawnAllPieces();
        PositionAllPieces();

        // Usando a função do seu script para esconder o painel
        if (pieceDetailsUI != null) pieceDetailsUI.SetPanelVisibility(false);
        if (notificationText != null) notificationText.alpha = 0;

        timeDaVez = startingTeam;
        string startingTeamName = (timeDaVez < teamNames.Length) ? teamNames[timeDaVez] : $"Time {timeDaVez}";
        Debug.Log($"<color=yellow>INÍCIO DE JOGO:</color> É a vez do time {startingTeamName}.");
    }

    private void Update()
    {
        Win();

        if (!currentCamera) { currentCamera = Camera.main; if (!currentCamera) { Debug.LogError("Câmera não encontrada!"); return; } }
        Ray ray = currentCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit info;

        if (currentlyDragging != null)
        {
            Plane horizontalPlane = new Plane(Vector3.up, Vector3.up * (transform.position.y + boardBaseHeight + yOffset));
            if (horizontalPlane.Raycast(ray, out float distance))
            {
                currentlyDragging.SetPosition(ray.GetPoint(distance));
            }
        }

        Vector2Int newHover = -Vector2Int.one;
        if (Physics.Raycast(ray, out info, 100, LayerMask.GetMask("Tile", "Hover")))
        {
            newHover = LookupTileIndex(info.transform.gameObject);
        }

        if (currentHover != newHover)
        {
            if (currentHover != -Vector2Int.one && tiles[currentHover.x, currentHover.y] != null)
            {
                TileInfo prevTileInfo = tiles[currentHover.x, currentHover.y].GetComponent<TileInfo>();
                if (highlightTiles.Contains(currentHover)) { tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material = validMoveMaterial; }
                else { tiles[currentHover.x, currentHover.y].GetComponent<MeshRenderer>().material = GetOriginalTileMaterial(currentHover.x, currentHover.y, prevTileInfo.type); prevTileInfo.OnHoverExit(); }
            }
            if (newHover != -Vector2Int.one)
            {
                GameObject newHoveredTile = tiles[newHover.x, newHover.y];
                if (!highlightTiles.Contains(newHover))
                {
                    TileInfo newTileInfo = newHoveredTile.GetComponent<TileInfo>();
                    if (newTileInfo.type == TileType.Cursed) { if (currentlyDragging == null) newTileInfo.Flash(flashDuration, 0.5f); }
                    else { newHoveredTile.GetComponent<MeshRenderer>().material = hoverMaterial; newTileInfo.SetAlpha(1f); }
                }
            }
            currentHover = newHover;
        }

        if (Input.GetMouseButtonDown(0))
        {
            Ray localRay = new Ray(GetTileCenter(currentHover.x, currentHover.y), transform.up); // Pega a possição para cima
            Debug.DrawRay(localRay.origin, localRay.direction, Color.red, 5f);

            if (Physics.Raycast(localRay, out info, 100, LayerMask.GetMask("Piece")))
            {
                ChessPiece clickedPiece = info.transform.GetComponentInChildren<ChessPiece>();
                if (clickedPiece != null && clickedPiece.team == timeDaVez)
                {
                    ClearHighlights();
                    currentlyDragging = clickedPiece;
                    highlightTiles = clickedPiece.GetAvailableMoves(chessPieces, tiles, TILE_COUNT_X, TILE_COUNT_Y);
                    HighlightValidMoves();

                    // ===== LÓGICA DA UI USANDO SUAS FUNÇÕES =====
                    if (pieceDetailsUI != null)
                    {
                        // Chama a sua função de update com os parâmetros corretos
                        pieceDetailsUI.UpdatePieceDetails(
                            clickedPiece.type.ToString(),
                            clickedPiece.Health,
                            clickedPiece.maxHealth, // Usando a nova variável
                            clickedPiece.Damage,
                            clickedPiece.Shield
                        );
                        // Chama a sua função para mostrar o painel
                        pieceDetailsUI.SetPanelVisibility(true);
                    }
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (currentlyDragging == null) return;
            bool validMove = false;
            if (currentHover != -Vector2Int.one && highlightTiles.Contains(currentHover))
            {
                validMove = MoveTo(currentlyDragging, currentHover.x, currentHover.y);
            }
            if (!validMove)
            {
                PositionSinglePiece(currentlyDragging.currentX, currentlyDragging.currentY, true);
            }

            // Usando sua função para esconder o painel ao soltar a peça
            if (pieceDetailsUI != null) pieceDetailsUI.SetPanelVisibility(false);

            ClearHighlights();
            currentlyDragging = null;
        }
    }

    private bool MoveTo(ChessPiece cp, int x, int y)
    {
        if (cp == null) return false;
        Vector2Int previousPosition = new Vector2Int(cp.currentX, cp.currentY);
        if (chessPieces[x, y] != null)
        {
            ChessPiece ocp = chessPieces[x, y];
            if (ocp.team == cp.team) return false;
            else
            {
                AttackTo(cp, x, y);
                return false;
            }

        }
        TileInfo targetTileInfo = tiles[x, y].GetComponent<TileInfo>();
        if (targetTileInfo != null && targetTileInfo.type == TileType.Cursed)
        {
            int penaltyAmount = Random.Range(1, 6);
            int penaltyType = Random.Range(0, 3);
            string teamName = (cp.team < teamNames.Length) ? teamNames[cp.team] : $"Time {cp.team}";
            string penaltyMessage = "";
            switch (penaltyType)
            {
                case 0: cp.TakeDamage(penaltyAmount); penaltyMessage = $"Time {teamName} pisou na maldição! Peça perdeu {penaltyAmount} de Vida."; break;
                case 1: cp.ReduceShield(penaltyAmount); penaltyMessage = $"Time {teamName} pisou na maldição! Peça perdeu {penaltyAmount} de Escudo."; break;
                case 2: cp.ReduceDamage(penaltyAmount); penaltyMessage = $"Time {teamName} pisou na maldição! Peça perdeu {penaltyAmount} de Dano."; break;
            }
            if (notificationText != null)
            {
                StartCoroutine(ShowNotificationCoroutine(penaltyMessage, 3f));
            }
        }
        chessPieces[x, y] = cp;
        chessPieces[previousPosition.x, previousPosition.y] = null;
        PositionSinglePiece(x, y, true);
        if (cp.type == ChessPieceType.Ataque && !cp.hasMoved) cp.hasMoved = true;
        TrocarTurno();
        return true;
    }

    private void AttackTo(ChessPiece cp, int x, int y)
    {
        if(cp == null)
        {
            return;
        }
        Ray localRay = new Ray(GetTileCenter(currentHover.x, currentHover.y), transform.up); // Pega a posição para cima

        RaycastHit[] hits = Physics.RaycastAll(localRay, 100, LayerMask.GetMask("Piece")); // lista de colisão
        foreach (RaycastHit ataque in hits) {
            if (ataque.transform.gameObject != cp.gameObject)
            {
                ChessPiece enemy = ataque.transform.gameObject.GetComponent<ChessPiece>();
                if (enemy.Shield > 0) { enemy.ReduceShield(cp.Damage); }
                else {             
                    enemy.TakeDamage(cp.Damage);
                }
                    TrocarTurno(); // Troca de turno após o ataque
                           
            }
        }
    }

    private void Win()
    {
        bool purpleteamWin = false;
        bool orangeteamWin = false;

        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            for (int y = 0; y < TILE_COUNT_Y; y++)
            {
                if (chessPieces[x, y] != null)
                {
                    switch(chessPieces[x, y].team)
                    {
                        case 0: purpleteamWin = true; break;
                        case 1: orangeteamWin = true; break;
                    }
                }
                if (purpleteamWin && orangeteamWin) return; // Se ambos os times ainda tiverem peças, não há vencedor                  
            }
        }
        if (purpleteamWin)
        {
           if(notificationText.text != null)
            {
                StartCoroutine(ShowNotificationCoroutine("O time Roxo venceu!", 5f));
            }
        }
        else
        {
            if (notificationText.text != null)
            {
                StartCoroutine(ShowNotificationCoroutine("O time Laranja venceu!", 5f));
            }
        }
    }

    private void TrocarTurno()
    {
        timeDaVez = 1 - timeDaVez;
        string currentTeamName = (timeDaVez < teamNames.Length) ? teamNames[timeDaVez] : $"Time {timeDaVez}";
        Debug.Log($"<color=yellow>TURNO MUDOU:</color> Agora é a vez do time {currentTeamName}.");
        if (cameraRotator != null)
        {
            cameraRotator.StartCameraTransition();
        }
    }

    #region Funções de Tabuleiro
    private void HighlightValidMoves(){
        foreach (Vector2Int pos in highlightTiles) {
            tiles[pos.x, pos.y].GetComponent<MeshRenderer>().material = validMoveMaterial; tiles[pos.x, pos.y].GetComponent<TileInfo>().SetAlpha(1f);
        }
    }

    private void ClearHighlights() { 
        for (int x = 0; x < TILE_COUNT_X; x++) {
            for (int y = 0; y < TILE_COUNT_Y; y++) 
            { 
                if (tiles[x, y] != null) 
                { 
                    tiles[x, y].GetComponent<MeshRenderer>().material = GetOriginalTileMaterial(x, y, tiles[x, y].GetComponent<TileInfo>().type); 
                    tiles[x, y].GetComponent<TileInfo>().OnHoverExit(); 
                } 
            } 
        } highlightTiles.Clear(); }

    private Material GetOriginalTileMaterial(int x, int y, TileType type) {
        if (type == TileType.Cursed) return cursedMaterial; return (x + y) % 2 == 0 ? tileMaterialLight : tileMaterialDark; 
    
    }
    private void GenerateAllTiles(float ts, int tileCountX, int tileCountY) {
        bounds = new Vector3((tileCountX * ts) / 2.0f, 0, (tileCountY * ts) / 2.0f);
        tiles = new GameObject[tileCountX, tileCountY];
        for (int x = 0; x < tileCountX; x++) 
            for (int y = 0; y < tileCountY; y++) tiles[x, y] = GenerateSingleTile(ts, x, y); 
    }

    private GameObject GenerateSingleTile(float ts, int x, int y) { 
        GameObject t = new GameObject($"Tile_{x}_{y}"); 
        t.transform.parent = transform; Mesh m = new Mesh();
        t.AddComponent<MeshFilter>().mesh = m;
        t.AddComponent<MeshRenderer>();
        TileInfo ti = t.AddComponent<TileInfo>(); 
        ti.x = x; ti.y = y;
        bool isCursed = (y > 0 && y < TILE_COUNT_Y - 1) && (Random.value < cursedTileChance);
        Material mat = isCursed ? cursedMaterial : ((x + y) % 2 == 0 ? tileMaterialLight : tileMaterialDark);
        t.GetComponent<MeshRenderer>().material = new Material(mat); 
        ti.SetupTileVisual(tileMaterialLight.color, tileMaterialDark.color, cursedMaterial.color, isCursed); 
        t.transform.localPosition = new Vector3(x * ts, boardBaseHeight, y * ts) - bounds + new Vector3(ts / 2f, 0, ts / 2f); 
        Vector3[] v = { new Vector3(-ts / 2, 0, -ts / 2), new Vector3(-ts / 2, 0, ts / 2), new Vector3(ts / 2, 0, -ts / 2), new Vector3(ts / 2, 0, ts / 2) };
        int[] tr = { 0, 1, 2, 1, 3, 2 };
        m.vertices = v; m.triangles = tr;
        m.RecalculateNormals(); 
        t.layer = LayerMask.NameToLayer("Tile"); 
        t.AddComponent<BoxCollider>().size = new Vector3(ts, 0.01f, ts); return t;
    }

    private void PositionAllPieces() {
        for (int x = 0; x < TILE_COUNT_X; x++) 
            for (int y = 0; y < TILE_COUNT_Y; y++)
                if (chessPieces[x, y] != null) PositionSinglePiece(x, y, true); 
    }

    private void PositionSinglePiece(int x, int y, bool force = false) 
    { 
        if (chessPieces[x, y] == null) return;
        ChessPiece cp = chessPieces[x, y];
        cp.currentX = x; cp.currentY = y; 
        cp.SetPosition(GetTileCenter(x, y), force); 
        if (cp.team == awayTeam) { cp.transform.rotation = Quaternion.Euler(0f, 180f, 0f); 
        } 
        else { cp.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        } 
    }
    private Vector3 GetTileCenter(int x, int y) { 
        return new Vector3(x * tileSize, yOffset, y * tileSize) - bounds + new Vector3(tileSize / 2f, 0, tileSize / 2f); 
    }

    private Vector2Int LookupTileIndex(GameObject hitInfo) { 
        for (int x = 0; x < TILE_COUNT_X; x++) 
            for (int y = 0; y < TILE_COUNT_Y; y++) 
                if (tiles[x, y] == hitInfo) return new Vector2Int(x, y);
        return -Vector2Int.one;
    }

    private void SpawnAllPieces() { 
        chessPieces = new ChessPiece[TILE_COUNT_X, TILE_COUNT_Y];
        //List<ChessPieceType> pieceOrder = new List<ChessPieceType>;
        Config config = Config.Instance;
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            ChessPieceType type = config.GetRandomType();
            int positionX_team1 = x;
            int positionX_team2 = x;
            if (config.mirrorBoard) { positionX_team2 = TILE_COUNT_X - (x + 1); }

            chessPieces[positionX_team1, 0] = SpawnSinglePiece(type, 0);
            chessPieces[positionX_team2, TILE_COUNT_Y - 1] = SpawnSinglePiece(type, 1);
            //chessPieces[x, 0] = SpawnSinglePiece(pieceOrder[x], 0); //Tirar do "//" caso queira retornar ao modelo antigo.
        }
        for (int x = 0; x < TILE_COUNT_X; x++)
        {
            ChessPieceType type = config.GetRandomType();
            int positionX_team1 = x;
            int positionX_team2 = x;
            if (config.mirrorBoard) { positionX_team2 = TILE_COUNT_X - (x + 1); }

            chessPieces[positionX_team1, 1] = SpawnSinglePiece(type, 0);
            chessPieces[positionX_team2, TILE_COUNT_Y - 2] = SpawnSinglePiece(type, 1);
        }
    }

    private ChessPiece SpawnSinglePiece(ChessPieceType type, int team) {
        if (type == ChessPieceType.None) return null;
        int pieceIndex = (int)type - 1; 
        if (pieceIndex < 0 || pieceIndex >= prefabs.Length) return null; 
        GameObject newPieceObject = Instantiate(prefabs[pieceIndex], transform);
        ChessPiece cp = newPieceObject.GetComponentInChildren<ChessPiece>(); 
        if (cp == null) { Debug.LogError($"FALHA CRÍTICA: Não encontrei o script 'ChessPiece' no prefab '{prefabs[pieceIndex].name}'."); 
            Destroy(newPieceObject); return null; 
        }
        cp.type = type; cp.team = team; switch (type) { 
            case ChessPieceType.Rei: cp.InitializeAttributes(35, 0, 0);
                break; case ChessPieceType.Ataque: cp.InitializeAttributes(50, 5, 10); break; case ChessPieceType.Flanco: cp.InitializeAttributes(25, 10, 5); break; case ChessPieceType.Sup: cp.InitializeAttributes(15, 0, 5); break; case ChessPieceType.Tanque: cp.InitializeAttributes(10, 10, 20); break; } Renderer pieceRenderer = cp.GetComponentInChildren<Renderer>(); if (pieceRenderer != null && team < teamMaterials.Length) { Material[] currentMaterials = pieceRenderer.materials; for (int i = 0; i < currentMaterials.Length; i++) { currentMaterials[i] = teamMaterials[team]; } pieceRenderer.materials = currentMaterials; } return cp; }
    private void ShuffleList<T>(List<T> list) { for (int i = list.Count - 1; i > 0; i--) { int r = Random.Range(0, i + 1); T t = list[i]; list[i] = list[r]; list[r] = t; } }
    private IEnumerator ShowNotificationCoroutine(string message, float duration) { if (notificationText == null) yield break; notificationText.text = message; float fadeTime = 0.5f; for (float t = 0; t < fadeTime; t += Time.deltaTime) { notificationText.alpha = Mathf.Lerp(0, 1, t / fadeTime); yield return null; } notificationText.alpha = 1; yield return new WaitForSeconds(duration); for (float t = 0; t < fadeTime; t += Time.deltaTime) { notificationText.alpha = Mathf.Lerp(1, 0, t / fadeTime); yield return null; } notificationText.alpha = 0; }
    #endregion
}