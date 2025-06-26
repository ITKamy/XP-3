// CameraRotationController.cs
using UnityEngine;
using System.Collections;

public class CameraRotationController : MonoBehaviour
{
    [Header("Configura��es de Transi��o da C�mera")]
    [SerializeField] private float transitionDuration = 1.0f; // Dura��o da transi��o em segundos
    [SerializeField] private AnimationCurve transitionCurve; // Curva de anima��o para suavizar (opcional)

    [Header("Posi��es das C�meras por Time")]
    // Estes Transforms ser�o os GameObjects VAZIOS que voc� posiciona no Editor.
    // Arraste o GameObject 'CameraView_PurpleTeam' para o slot roxo, e 'CameraView_OrangeTeam' para o slot laranja.
    [SerializeField] private Transform purpleTeamViewPoint;
    [SerializeField] private Transform orangeTeamViewPoint;

    private bool isTransitioning = false; // Usado para evitar m�ltiplas transi��es simult�neas
    private bool isPurpleTeamViewActive = true; // Controla qual vis�o est� ativa (true = Roxo, false = Laranja)

    private void Awake()
    {
        // Garante que os pontos de vis�o foram atribu�dos no Inspector
        if (purpleTeamViewPoint == null || orangeTeamViewPoint == null)
        {
            Debug.LogError("CameraRotationController: Pontos de vis�o dos times n�o atribu�dos! A c�mera n�o funcionar� corretamente. Por favor, arraste os GameObjects de refer�ncia para os slots no Inspector.");
            enabled = false; // Desabilita o script se n�o houver refer�ncias
            return;
        }

        // Posiciona e rotaciona a c�mera instantaneamente na vis�o do time Roxo ao iniciar
        transform.position = purpleTeamViewPoint.position;
        transform.rotation = purpleTeamViewPoint.rotation;
        isPurpleTeamViewActive = true; // Define o estado inicial como vis�o do time roxo
    }

    // Este m�todo � chamado pelo Board para iniciar a transi��o
    public void StartCameraTransition()
    {
        if (!isTransitioning) // S� inicia a transi��o se n�o estiver j� em uma
        {
            StartCoroutine(TransitionCameraCoroutine());
        }
    }

    private IEnumerator TransitionCameraCoroutine()
    {
        isTransitioning = true;
        float elapsedTime = 0f;

        Vector3 startPos = transform.position; // Posi��o atual da c�mera
        Quaternion startRot = transform.rotation; // Rota��o atual da c�mera

        Vector3 endPos;
        Quaternion endRot;

        // Define o alvo de posi��o e rota��o com base no turno atual
        if (isPurpleTeamViewActive) // Se a vis�o atual � do time Roxo, o alvo � o time Laranja
        {
            endPos = orangeTeamViewPoint.position;
            endRot = orangeTeamViewPoint.rotation;
        }
        else // Se a vis�o atual � do time Laranja, o alvo � o time Roxo
        {
            endPos = purpleTeamViewPoint.position;
            endRot = purpleTeamViewPoint.rotation;
        }

        // Loop de interpola��o
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float percentage = elapsedTime / transitionDuration;

            if (transitionCurve != null)
            {
                percentage = transitionCurve.Evaluate(percentage);
            }

            // Interpola tanto a posi��o quanto a rota��o
            transform.position = Vector3.Lerp(startPos, endPos, percentage);
            transform.rotation = Quaternion.Slerp(startRot, endRot, percentage);

            yield return null; // Espera o pr�ximo frame
        }

        // Garante que a c�mera termine exatamente na posi��o e rota��o alvo
        transform.position = endPos;
        transform.rotation = endRot;

        isPurpleTeamViewActive = !isPurpleTeamViewActive; // Inverte a flag para o pr�ximo turno
        isTransitioning = false;
    }
}