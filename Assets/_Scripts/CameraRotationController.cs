// CameraRotationController.cs
using UnityEngine;
using System.Collections;

public class CameraRotationController : MonoBehaviour
{
    [Header("Configurações de Transição da Câmera")]
    [SerializeField] private float transitionDuration = 1.0f; // Duração da transição em segundos
    [SerializeField] private AnimationCurve transitionCurve; // Curva de animação para suavizar (opcional)

    [Header("Posições das Câmeras por Time")]
    // Estes Transforms serão os GameObjects VAZIOS que você posiciona no Editor.
    // Arraste o GameObject 'CameraView_PurpleTeam' para o slot roxo, e 'CameraView_OrangeTeam' para o slot laranja.
    [SerializeField] private Transform purpleTeamViewPoint;
    [SerializeField] private Transform orangeTeamViewPoint;

    private bool isTransitioning = false; // Usado para evitar múltiplas transições simultâneas
    private bool isPurpleTeamViewActive = true; // Controla qual visão está ativa (true = Roxo, false = Laranja)

    private void Awake()
    {
        // Garante que os pontos de visão foram atribuídos no Inspector
        if (purpleTeamViewPoint == null || orangeTeamViewPoint == null)
        {
            Debug.LogError("CameraRotationController: Pontos de visão dos times não atribuídos! A câmera não funcionará corretamente. Por favor, arraste os GameObjects de referência para os slots no Inspector.");
            enabled = false; // Desabilita o script se não houver referências
            return;
        }

        // Posiciona e rotaciona a câmera instantaneamente na visão do time Roxo ao iniciar
        transform.position = purpleTeamViewPoint.position;
        transform.rotation = purpleTeamViewPoint.rotation;
        isPurpleTeamViewActive = true; // Define o estado inicial como visão do time roxo
    }

    // Este método é chamado pelo Board para iniciar a transição
    public void StartCameraTransition()
    {
        if (!isTransitioning) // Só inicia a transição se não estiver já em uma
        {
            StartCoroutine(TransitionCameraCoroutine());
        }
    }

    private IEnumerator TransitionCameraCoroutine()
    {
        isTransitioning = true;
        float elapsedTime = 0f;

        Vector3 startPos = transform.position; // Posição atual da câmera
        Quaternion startRot = transform.rotation; // Rotação atual da câmera

        Vector3 endPos;
        Quaternion endRot;

        // Define o alvo de posição e rotação com base no turno atual
        if (isPurpleTeamViewActive) // Se a visão atual é do time Roxo, o alvo é o time Laranja
        {
            endPos = orangeTeamViewPoint.position;
            endRot = orangeTeamViewPoint.rotation;
        }
        else // Se a visão atual é do time Laranja, o alvo é o time Roxo
        {
            endPos = purpleTeamViewPoint.position;
            endRot = purpleTeamViewPoint.rotation;
        }

        // Loop de interpolação
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float percentage = elapsedTime / transitionDuration;

            if (transitionCurve != null)
            {
                percentage = transitionCurve.Evaluate(percentage);
            }

            // Interpola tanto a posição quanto a rotação
            transform.position = Vector3.Lerp(startPos, endPos, percentage);
            transform.rotation = Quaternion.Slerp(startRot, endRot, percentage);

            yield return null; // Espera o próximo frame
        }

        // Garante que a câmera termine exatamente na posição e rotação alvo
        transform.position = endPos;
        transform.rotation = endRot;

        isPurpleTeamViewActive = !isPurpleTeamViewActive; // Inverte a flag para o próximo turno
        isTransitioning = false;
    }
}