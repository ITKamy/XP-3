using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NotificationPanelUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject messagePrefab; // Prefab com um TextMeshProUGUI (ex: uma linha de texto)
    public Transform contentParent;  // Onde as mensagens serão adicionadas
    public int maxMessages = 10;     // Quantidade máxima de mensagens no painel

    private Queue<GameObject> messageQueue = new Queue<GameObject>();

    public void ShowMessage(string message)
    {
        // Cria uma nova linha de mensagem
        GameObject newMessage = Instantiate(messagePrefab, contentParent);
        TextMeshProUGUI text = newMessage.GetComponent<TextMeshProUGUI>();
        if (text != null)
            text.text = message;

        // Adiciona à fila
        messageQueue.Enqueue(newMessage);

        // Se exceder o limite, remove a mais antiga
        if (messageQueue.Count > maxMessages)
        {
            GameObject old = messageQueue.Dequeue();
            Destroy(old);
        }
    }

    public void ClearMessages()
    {
        foreach (var msg in messageQueue)
        {
            Destroy(msg);
        }
        messageQueue.Clear();
    }
}
