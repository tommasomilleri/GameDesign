using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

/// 
/// Carrellata di apertura su spline. Parte quando inizia il Lvl1,
/// percorre il path in 'duration' secondi, poi passa la regia al
/// CameraDirector. Skippabile con un click.
/// 
public class IntroDolly : MonoBehaviour
{
    [Header("Riferimenti")]
    public CinemachineCamera introCam;          // VC_Intro
    public CinemachineSplineDolly dolly;        // il Body di VC_Intro

    [Header("Impostazioni")]
    [Tooltip("Durata della carrellata in secondi")]
    public float duration = 12f;
    [Tooltip("Priorita' durante la carrellata (sopra tutte le altre)")]
    public int activePriority = 50;
    public int idlePriority = 5;

    bool playing = false;
    bool skipped = false;

    void Awake()
    {
        if (introCam != null && dolly == null)
        {
            // Versione a prova di bug visivo: non usa < e > 
            dolly = (CinemachineSplineDolly)introCam.GetComponent(typeof(CinemachineSplineDolly));
        }
    }

    /// Chiamato da StationActivator di GoST1 (una volta sola).
    public void PlayIntro()
    {
        if (playing || skipped) return;         // mai due volte
        if (introCam == null || dolly == null)
        {
            Debug.LogWarning("[IntroDolly] riferimenti mancanti: skip.");
            Finish();
            return;
        }
        StartCoroutine(DollyRoutine());
    }

    IEnumerator DollyRoutine()
    {
        playing = true;
        dolly.CameraPosition = 0f;
        introCam.Priority = activePriority;     // vince su tutte

        float t = 0f;
        while (t < duration)
        {
            // Skip con click (ma non durante pausa)
            if (Input.GetMouseButtonDown(0) &&
                (PauseMenuManager.Instance == null ||
                 !PauseMenuManager.Instance.isPaused))
                break;

            t += Time.deltaTime;
            // SmoothStep: parte piano, accelera, rallenta alla fine.
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / duration));
            dolly.CameraPosition = k;           // 0..1 lungo la spline
            yield return null;
        }

        Finish();
    }

    void Finish()
    {
        skipped = true;
        playing = false;
        if (introCam != null) introCam.Priority = idlePriority;
        // La regia torna alla state-driven camera: blend automatico
        if (CameraDirector.Instance != null)
            CameraDirector.Instance.GoTo(0);
    }
}