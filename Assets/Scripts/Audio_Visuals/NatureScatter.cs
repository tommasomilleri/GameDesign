using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Scatter procedurale di vegetazione su terreno.
/// Genera in editor: gli oggetti restano collegati ai prefab.
/// </summary>
public class NatureScatter : MonoBehaviour
{
    [Header("Cosa Generare")]
    [Tooltip("Trascina qui i prefab _Wind. Il PIVOT deve stare ALLA BASE " +
             "del modello, non al centro, altrimenti meta' pianta finisce " +
             "sottoterra.")]
    public GameObject[] prefabs;

    [Min(0)]
    public int amount = 100;

    [Header("Area e Terreno")]
    [Tooltip("Larghezza del prato (asse rosso).")]
    public float areaX = 50f;

    [Tooltip("Profondita' del prato (asse blu).")]
    public float areaZ = 50f;

    [Tooltip("Layer del terreno. NON lasciarlo vuoto e NON metterci il layer " +
             "della vegetazione, altrimenti i raggi colpiscono le piante gia' " +
             "generate e le impilano.")]
    public LayerMask groundLayer = 1;      // "Default"

    [Min(1f)]
    [Tooltip("Quanto sopra il pivot parte il raggio.")]
    public float rayHeight = 50f;

    [Min(1f)]
    [Tooltip("Lunghezza totale del raggio. Deve superare rayHeight, " +
             "altrimenti non raggiunge nemmeno il livello del pivot.")]
    public float rayDistance = 200f;

    [Header("Orientamento")]
    [Range(0f, 1f)]
    [Tooltip("0 = sempre verticale (giusto per erba e alberi: crescono dritti " +
             "anche in pendenza). 1 = perpendicolare al suolo (giusto per " +
             "rocce e detriti).")]
    public float alignToSurface = 0f;

    [Range(0f, 90f)]
    [Tooltip("Pendenza massima su cui puo' nascere qualcosa.")]
    public float maxSlope = 45f;

    [Range(0f, 30f)]
    [Tooltip("Inclinazione casuale: toglie l'aria di 'piantato col righello'.")]
    public float randomTilt = 4f;

    [Tooltip("Affonda l'oggetto nel terreno. Nasconde la base della mesh.")]
    public float sinkDepth = 0.02f;

    [Header("Regole di Spazio (Anti-Incastro)")]
    [Min(0f)]
    [Tooltip("Distanza minima ORIZZONTALE tra due oggetti, in metri. " +
             "Non usa la fisica: confronta le posizioni gia' piazzate, quindi " +
             "funziona anche con prefab privi di Collider.")]
    public float minDistance = 0.5f;

    [Header("Randomizzazione")]
    public float minScale = 0.8f;
    public float maxScale = 1.5f;

    [Tooltip("Stesso seme = stesso risultato. Rende la generazione " +
             "riproducibile e versionabile.")]
    public int seed = 12345;

    public bool randomizeSeed = false;

#if UNITY_EDITOR

    // I punti gia' piazzati. Usiamo una griglia sparsa invece del confronto
    // di tutti-con-tutti: quest'ultimo e' quadratico e a 5000 oggetti
    // richiederebbe minuti.
    readonly Dictionary<Vector2Int, List<Vector2>> grid =
        new Dictionary<Vector2Int, List<Vector2>>();

    static readonly RaycastHit[] hitBuffer = new RaycastHit[16];

    [ContextMenu("🌿 Genera Prato 🌿")]
    public void Generate()
    {
        // ---- controlli preventivi con messaggi utili ---------------------
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogError("[NatureScatter] Nessun prefab assegnato.", this);
            return;
        }

        bool anyPrefab = false;
        foreach (var p in prefabs)
        {
            if (p == null) continue;
            anyPrefab = true;

            if (p.GetComponentInChildren<Renderer>(true) == null)
                Debug.LogWarning("[NatureScatter] Il prefab '" + p.name +
                                 "' non ha Renderer: sara' invisibile.", this);

            if (p.GetComponentInChildren<NatureScatter>(true) != null)
            {
                Debug.LogError("[NatureScatter] Il prefab '" + p.name +
                               "' contiene a sua volta un NatureScatter. " +
                               "Ricorsione bloccata.", this);
                return;
            }
        }

        if (!anyPrefab)
        {
            Debug.LogError("[NatureScatter] L'array contiene solo slot vuoti.", this);
            return;
        }

        if (groundLayer.value == 0)
        {
            Debug.LogError("[NatureScatter] 'Ground Layer' e' su Nothing: " +
                           "nessun raycast colpira' mai niente. Selezionalo.", this);
            return;
        }

        if (rayDistance <= rayHeight)
        {
            Debug.LogError("[NatureScatter] 'Ray Distance' (" + rayDistance +
                           ") deve essere maggiore di 'Ray Height' (" + rayHeight +
                           "), altrimenti il raggio non arriva al suolo.", this);
            return;
        }

        Clear();

        if (randomizeSeed) seed = Random.Range(1, int.MaxValue);

        // Salviamo lo stato globale del generatore: senza, ogni click
        // altererebbe la sequenza casuale di tutto il progetto.
        Random.State saved = Random.state;
        Random.InitState(seed);

        // Con autoSyncTransforms disattivato (default moderno) un terreno
        // spostato di recente darebbe hit sulla posizione vecchia.
        Physics.SyncTransforms();

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Genera Prato");

        int spawned = 0;
        int failedRay = 0, failedSlope = 0, failedSpace = 0;

        try
        {
            grid.Clear();

            int maxAttempts = Mathf.Max(amount * 12, 1000);

            for (int i = 0; i < maxAttempts && spawned < amount; i++)
            {
                if ((i & 255) == 0 &&
                    EditorUtility.DisplayCancelableProgressBar(
                        "Nature Scatter",
                        spawned + " / " + amount,
                        (float)spawned / Mathf.Max(1, amount)))
                    break;

                float rx = transform.position.x + Random.Range(-areaX * 0.5f, areaX * 0.5f);
                float rz = transform.position.z + Random.Range(-areaZ * 0.5f, areaZ * 0.5f);
                var origin = new Vector3(rx, transform.position.y + rayHeight, rz);

                Vector3 point, normal;
                if (!GroundRaycast(origin, out point, out normal)) { failedRay++; continue; }

                if (Vector3.Angle(normal, Vector3.up) > maxSlope) { failedSlope++; continue; }

                // ANTI-INCASTRO senza fisica: funziona anche su prefab
                // privi di Collider, ed e' molto piu' veloce di CheckSphere.
                if (minDistance > 0f && HasNeighbor(point, minDistance))
                {
                    failedSpace++;
                    continue;
                }

                GameObject prefab = prefabs[Random.Range(0, prefabs.Length)];
                if (prefab == null) continue;

                // InstantiatePrefab mantiene il collegamento: modificando il
                // prefab si aggiornano tutte le copie gia' in scena.
                var clone = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
                if (clone == null) clone = Instantiate(prefab, transform);

                // ---- rotazione ------------------------------------------
                // Lo yaw va moltiplicato a SINISTRA dell'allineamento,
                // altrimenti ruota nello spazio gia' inclinato e produce
                // orientamenti imprevedibili.
                Quaternion yaw = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                Quaternion rot = yaw;

                if (alignToSurface > 0f && normal.sqrMagnitude > 1e-6f)
                {
                    Quaternion aligned = Quaternion.FromToRotation(Vector3.up, normal) * yaw;
                    rot = Quaternion.Slerp(yaw, aligned, alignToSurface);
                }

                if (randomTilt > 0f)
                {
                    // Random.insideUnitCircle.normalized puo' valere (0,0):
                    // AngleAxis con asse nullo genera un quaternione NaN che
                    // corrompe l'intera gerarchia. Campionare l'angolo evita
                    // il caso degenere ed e' pure piu' veloce.
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    var axis = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    rot = Quaternion.AngleAxis(Random.Range(-randomTilt, randomTilt), axis) * rot;
                }

                clone.transform.rotation = rot;
                clone.transform.position = point - clone.transform.up * sinkDepth;

                float s = Random.Range(minScale, maxScale);
                clone.transform.localScale = new Vector3(s, s, s);

                AddPoint(point);
                spawned++;
            }
        }
        finally
        {
            Random.state = saved;
            EditorUtility.ClearProgressBar();
            // SetCurrentGroupName da solo NON collassa il gruppo: senza
            // questa chiamata un Ctrl+Z cancellerebbe un solo ciuffo.
            Undo.CollapseUndoOperations(undoGroup);
        }

        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);

        if (spawned == 0)
        {
            Debug.LogError("[NatureScatter] Nessun oggetto generato.\n" +
                "Raggi a vuoto: " + failedRay + " (il terreno ha un Collider? " +
                "e' sul layer giusto?)\n" +
                "Scartati per pendenza: " + failedSlope + "\n" +
                "Scartati per spazio: " + failedSpace + " (abbassa Min Distance)", this);
        }
        else
        {
            Debug.Log("[NatureScatter] Prato generato: " + spawned + " elementi. " +
                      "(raggi a vuoto " + failedRay + ", pendenza " + failedSlope +
                      ", spazio " + failedSpace + ")", this);
        }
    }

    /// <summary>
    /// Raycast che scarta gli hit appartenenti a questo scatter: senza,
    /// con groundLayer troppo permissivo i raggi colpirebbero la vegetazione
    /// gia' generata e la impilerebbero.
    /// </summary>
    bool GroundRaycast(Vector3 origin, out Vector3 point, out Vector3 normal)
    {
        point = origin;
        normal = Vector3.up;

        int n = Physics.RaycastNonAlloc(origin, Vector3.down, hitBuffer,
                                        rayDistance, groundLayer,
                                        QueryTriggerInteraction.Ignore);
        if (n <= 0) return false;

        float best = float.MaxValue;
        bool found = false;

        for (int i = 0; i < n; i++)
        {
            Transform ht = hitBuffer[i].collider.transform;
            if (ht == transform || ht.IsChildOf(transform)) continue;

            if (hitBuffer[i].distance < best)
            {
                best = hitBuffer[i].distance;
                point = hitBuffer[i].point;
                normal = hitBuffer[i].normal;
                found = true;
            }
        }

        return found;
    }

    // ---- griglia sparsa per la distanza minima ---------------------------
    // E' 2D (piano XZ) perche' lo spacing della vegetazione e' una distanza
    // orizzontale: in 3D, due piante sovrapposte a vista su una parete
    // ripida risulterebbero "lontane".

    Vector2Int Cell(Vector3 p)
    {
        float c = Mathf.Max(0.1f, minDistance);
        return new Vector2Int(Mathf.FloorToInt(p.x / c), Mathf.FloorToInt(p.z / c));
    }

    void AddPoint(Vector3 p)
    {
        Vector2Int k = Cell(p);
        List<Vector2> list;
        if (!grid.TryGetValue(k, out list))
        {
            list = new List<Vector2>(4);
            grid[k] = list;
        }
        list.Add(new Vector2(p.x, p.z));
    }

    bool HasNeighbor(Vector3 p, float radius)
    {
        Vector2Int k = Cell(p);
        float r2 = radius * radius;

        for (int x = -1; x <= 1; x++)
        for (int z = -1; z <= 1; z++)
        {
            List<Vector2> list;
            if (!grid.TryGetValue(new Vector2Int(k.x + x, k.y + z), out list)) continue;

            for (int i = 0; i < list.Count; i++)
            {
                float dx = list[i].x - p.x;
                float dz = list[i].y - p.z;
                if (dx * dx + dz * dz < r2) return true;
            }
        }
        return false;
    }

    [ContextMenu("🗑 Cancella")]
    public void Clear()
    {
        int undoGroup = Undo.GetCurrentGroup();

        // All'indietro e' obbligatorio: distruggendo un figlio la lista si
        // riordina e un ciclo in avanti salterebbe elementi.
        for (int i = transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(transform.GetChild(i).gameObject);

        Undo.CollapseUndoOperations(undoGroup);
        grid.Clear();
    }

    void OnValidate()
    {
        areaX = Mathf.Max(0.1f, areaX);
        areaZ = Mathf.Max(0.1f, areaZ);
        if (maxScale < minScale) maxScale = minScale;
        if (minScale < 0.01f) minScale = 0.01f;
        rayDistance = Mathf.Max(rayDistance, rayHeight + 1f);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 c = transform.position;

        Gizmos.color = new Color(0.2f, 0.8f, 0.2f, 0.9f);
        Gizmos.DrawWireCube(c, new Vector3(areaX, 0.05f, areaZ));

        // volume effettivamente coperto dai raggi
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.25f);
        Vector3 top = c + Vector3.up * rayHeight;
        Vector3 bottom = top + Vector3.down * rayDistance;
        Gizmos.DrawWireCube((top + bottom) * 0.5f,
                            new Vector3(areaX, rayDistance, areaZ));

        Gizmos.color = new Color(1f, 0.4f, 0.2f, 0.8f);
        Gizmos.DrawLine(top, bottom);
    }

#endif // UNITY_EDITOR
}