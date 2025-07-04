using System;
using UnityEditor;
using UnityEngine;

public class CreateScene : MonoBehaviour
{
    [SerializeField] GameObject slots;
    [SerializeField] SpriteRenderer[] positions;
    [SerializeField] CreateScene_SpritesType[] objects;
    CreateScene()
    {
        EditorApplication.update += update;
    }
    void Start()
    {
        Config config = Config.Instance;
        foreach (SpriteRenderer position in positions)
        {
            bool min = true;
            foreach (CreateScene_SpritesType ob in objects)
            {
                if (!ob.min_create)
                {
                    min = false;
                    break;
                }
            }

            int rand = UnityEngine.Random.Range(0, objects.Length);
            foreach (CreateScene_SpritesType ob in objects)
            {
                Sprite sprite = objects[rand].OnCreate();
                if (sprite != null) { position.sprite = sprite; }
         }
        }
    }
    private void update()
    {
        if (slots != null)
        {
            positions = slots.GetComponentsInChildren<SpriteRenderer>();
        }
        if (slots != null)
        {
            //positions = slots.GetComponentsInChildren<GameObject>();
        }
        foreach (CreateScene_SpritesType ob in objects)
        {
            ob.Adjust();
            if (!EditorApplication.isPlaying) { ob.Reset(); }
        }
    }

}

[Serializable] //Permite que as variaveis possam ser visualizadas e alteradas no editor da unity conforme o seu nivel de proteção.
public class CreateScene_SpritesType
{
    public string name;
    [Range(0, 16)]
    [SerializeField]
    private int min_number;
    [Range(0, 16)]
    [SerializeField]
    private int max_number;
    [SerializeField]
    private int create;
    [SerializeField] private Sprite sprite;

    [SerializeField] public bool min_create = false;
    [SerializeField] public bool max_create = false;
    [SerializeField] public float max => max_number; //Ponteiro que diz que o valor da media é igual ao calculo apontado.

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
    public Sprite OnCreate()
    {
        create++;
        if (create == min_number) { min_create = true; }
        if (create == max_number) { max_create = true; }
        return sprite;
    }
}