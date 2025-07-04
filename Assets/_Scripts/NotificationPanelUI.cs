using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class NotificationPanelUI : MonoBehaviour
{
    public GameObject notificationTemplate; // Prefab desativado
    public Transform notificationContainer; // O painel que segura os textos
    public float displayTime = 6f;          // Tempo que a notificação fica visível

    private Queue<GameObject> activeMessages = new Queue<GameObject>();

    public void ShowMessage(string message)
    {
        // Cria uma nova notificação baseada no template
        GameObject newNotification = Instantiate(notificationTemplate, notificationContainer);
        newNotification.SetActive(true);
        TMP_Text textComponent = newNotification.GetComponent<TMP_Text>();
        textComponent.text = message;

        activeMessages.Enqueue(newNotification);
        StartCoroutine(RemoveAfterTime(newNotification, displayTime));
    }

    private IEnumerator RemoveAfterTime(GameObject notification, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (notification != null)
        {
            activeMessages.Dequeue();
            Destroy(notification);
        }
    }
}
