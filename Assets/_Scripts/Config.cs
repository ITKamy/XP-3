using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "Config", menuName = "Game/Config")]
[ExecuteInEditMode]
public class Config : ScriptableObject
{
    public static Config Instance;

    [Header("Spawn Variables")]
    public ChessPiece_SpawnRate[] spawn_rate;
    public float media;
    public bool reset = false;

    [Header("Chain Variables")]
    [Tooltip("Change Position: Define se as peças vão trocar de posição após o fim do 'chain_attack' ou se ao atacar a atacante ira invadir a celula da outra.")]
    public bool change_position;
    [Tooltip("First Strike: Define se a peça dara 1 ataque antes de iniciar o chain attack ou não.")]
    public bool firsStrike;
    [Tooltip("Mirror Board: Define se os tabuleiros serão espelhados ou não.")]
    public bool mirrorBoard;


    Config()
    {
        Instance = this;
        EditorApplication.update += Update; //Adiciona a função update aos updates da aplição
                                            //permitindo que ela seja chamada tanto no editor quanto no jogo.
    }

    public void Update() //Função que deve ser chamada a todo tempo.
    {
        SpawnRateUpdate();
    }

    public void ResetGame()
    {
        foreach (ChessPiece_SpawnRate spawnRate in spawn_rate)
        {
            spawnRate.Reset();
        }
    }

    public void SpawnRateUpdate() //Faz todos os testes referentes ao SpawnRate.
    {
        if (reset)
        {
            foreach (ChessPiece_SpawnRate spawnRate in spawn_rate)
            {
                spawnRate.ResetAll();
            }
            reset = false;
        }

        media = 0;

        foreach (ChessPiece_SpawnRate spawnRate in spawn_rate)
        {
            spawnRate.Adjust();
            if (!EditorApplication.isPlaying) { spawnRate.Reset(); } //Verifica se o jogo esta no editor ou play mode e chama a função.
            media += spawnRate.media;
        }
    }

    public ChessPieceType GetRandomType() //Função criada para devolver um Type aleatorio
                                          //considerando o min que cada pessa tem que ter em campo
                                          //E o maximo.
    {
        if (media < 16)
        {
            throw new Exception("Media inferior a 16.");
        }
        bool min_create = true;
        foreach (ChessPiece_SpawnRate spawnRate in spawn_rate)
        {
            if (!spawnRate.min_create)
            {
                min_create = false;
                break;
            }
        }

        ChessPieceType pieceType = ChessPieceType.None;

        bool breakWhile = false;

        while (true)
        {
            Debug.Log("None piece");
            int rand = UnityEngine.Random.Range(0, spawn_rate.Length);

            if (!min_create)
            {
                while (true)
                {
                    if (!spawn_rate[rand].min_create)
                    {
                        pieceType = spawn_rate[rand].pieceType;
                        breakWhile = true;
                        break;
                    }
                    else
                    {
                        rand++;
                        if (rand >= spawn_rate.Length)
                        {
                            rand = 0;
                        }
                    }
                }
            }
            else
            {
                while (true)
                {
                    if (!spawn_rate[rand].max_create)
                    {
                        pieceType = spawn_rate[rand].pieceType;
                        breakWhile = true;
                        break;
                    }
                    else
                    {
                        rand++;
                        if (rand >= spawn_rate.Length)
                        {
                            rand = 0;
                        }
                    }
                }
            }

            if (breakWhile == true) { break; }
        }

        foreach (ChessPiece_SpawnRate spawn_rate in spawn_rate)
        {
            if (spawn_rate.pieceType == pieceType)
            {
                spawn_rate.OnCreate();
            }
        }
        //Debug.Log(pieceType.ToString());
        return pieceType;
    }
}

[Serializable] //Permite que as variaveis possam ser visualizadas e alteradas no editor da unity conforme o seu nivel de proteção.
public class ChessPiece_SpawnRate
{
    [HideInInspector] //Esconde a variavel public do editor, não permitindo altera-la por lá mas mantendo a sua função.
    public string name; //Cria uma variavel nome para alterar o nome na lista.
    public ChessPieceType pieceType;
    [Range(0, 16)]
    [SerializeField]
    private int min_number;
    [Range(0, 16)]
    [SerializeField]
    private int max_number;
    [SerializeField]
    private int create;

    [SerializeField] public bool min_create = false;
    [SerializeField] public bool max_create = false;
    [SerializeField] public float media => (min_number + max_number) / 2; //Ponteiro que diz que o valor da media é igual ao calculo apontado.

    public int GetMin()
    {
        return min_number;
    }
    public int GetMax()
    {
        return max_number;
    }
    public void Adjust()
    {
        name = pieceType.ToString();
        if (pieceType == ChessPieceType.Rei)
        {
            max_number = min_number;
        }

        if (max_number < min_number)
        {
            max_number = min_number;
        }
    }
    public void ResetAll()
    {
        min_number = 0;
        max_number = 0;
        Reset();
    }
    public void Reset()
    {
        create = 0;
        min_create = false;
        max_create = false;
    }
    public void OnCreate()
    {
        create++;
        if (create == min_number) { min_create = true; }
        if (create == max_number) { max_create = true; }
    }
}