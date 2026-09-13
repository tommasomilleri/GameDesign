

using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Nature
{
    public class NatureMaterialSetup : EditorWindow
    {
        // ====================================================================
        //  PRESET
        // ====================================================================

        public enum PlantPreset { Auto, Grass, Bush, TreeCanopy, TreeTrunk }

        struct PresetData
        {
            public float stiffness;      // esponente maschera: rigidita del fusto
            public float strength;       // moltiplicatore locale sul vento globale
            public float flutter;        // ampiezza tremolio foglie
            public float flutterSpeed;
            public float phaseVariation; // sfasamento tra istanze
            public float translucency;   // luce che passa attraverso la foglia
            public float colorVariation;
            public bool doubleSided;
            public bool alphaClip;
            public float cutoff;

            public static PresetData Get(PlantPreset p)
            {
                PresetData d = new PresetData();
                d.cutoff = 0.4f;
                d.alphaClip = true;
                d.doubleSided = true;

                switch (p)
                {
                    // Un filo d'erba si curva per tutta la lunghezza: rigidita
                    // bassa. Il tremolio serve poco, e gia tutto flessibile.
                    case PlantPreset.Grass:
                        d.stiffness = 1.2f; d.strength = 1.4f;
                        d.flutter = 0.20f; d.flutterSpeed = 8f;
                        d.phaseVariation = 1.6f;
                        d.translucency = 0.90f;
                        d.colorVariation = 0.30f;
                        break;

                    case PlantPreset.Bush:
                        d.stiffness = 2.0f; d.strength = 1.0f;
                        d.flutter = 0.45f; d.flutterSpeed = 7f;
                        d.phaseVariation = 1.2f;
                        d.translucency = 0.80f;
                        d.colorVariation = 0.22f;
                        break;

                    // La chioma di un albero si muove molto in punta e quasi
                    // nulla vicino al tronco: rigidita alta. Il tremolio delle
                    // foglie invece e marcato.
                    case PlantPreset.TreeCanopy:
                        d.stiffness = 3.5f; d.strength = 0.50f;
                        d.flutter = 0.50f; d.flutterSpeed = 5f;
                        d.phaseVariation = 1.0f;
                        d.translucency = 0.70f;
                        d.colorVariation = 0.15f;
                        break;

                    // Il tronco e quasi immobile, opaco e a faccia singola.
                    // Deve pero muoversi UN PO, altrimenti la chioma ondeggia
                    // staccata da un palo rigido e si vede.
                    case PlantPreset.TreeTrunk:
                        d.stiffness = 5.0f; d.strength = 0.22f;
                        d.flutter = 0f; d.flutterSpeed = 4f;
                        d.phaseVariation = 1.0f;
                        d.translucency = 0.05f;
                        d.colorVariation = 0.06f;
                        d.doubleSided = false;
                        d.alphaClip = false;
                        break;

                    default:
                        d.stiffness = 2f; d.strength = 1f;
                        d.flutter = 0.35f; d.flutterSpeed = 7f;
                        d.phaseVariation = 1.2f;
                        d.translucency = 0.7f;
                        d.colorVariation = 0.2f;
                        break;
                }
                return d;
            }
        }

        // ====================================================================
        //  STATO DELLA FINESTRA
        // ====================================================================

        PlantPreset preset = PlantPreset.Auto;
        string outputFolder = "Assets/Materials/Vegetation";
        bool createVariants = true;
        bool enableInstancing = true;
        bool overrideHeight = false;
        float manualHeight = 1f;
        Vector2 scroll;
        string report = "";

        const string SHADER_NAME = "Nature/Wind Vegetation";

        [MenuItem("Tools/Nature/Setup Wind Materials")]
        static void Open()
        {
            NatureMaterialSetup w = GetWindow<NatureMaterialSetup>("Nature Wind Setup");
            w.minSize = new Vector2(430f, 520f);
        }

        void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Conversione vegetazione importata",
                                       EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "Seleziona nel Project uno o piu prefab (o file FBX/GLB) e premi " +
                "Converti.\n\n" +
                "I modelli importati sono di sola lettura: verranno creati dei " +
                "Prefab Variant modificabili, lasciando intatti i file originali.",
                MessageType.Info);

            EditorGUILayout.Space();

            preset = (PlantPreset)EditorGUILayout.EnumPopup(
                new GUIContent("Tipo di pianta",
                    "Auto riconosce tronco e chioma dai nomi di materiali e mesh."),
                preset);

            outputFolder = EditorGUILayout.TextField(
                new GUIContent("Cartella materiali"), outputFolder);

            createVariants = EditorGUILayout.Toggle(
                new GUIContent("Crea Prefab Variant",
                    "Obbligatorio per i modelli importati, che non sono modificabili."),
                createVariants);

            enableInstancing = EditorGUILayout.Toggle(
                new GUIContent("GPU Instancing",
                    "Tienilo SEMPRE attivo. E cio che permette migliaia di " +
                    "piante mantenendo il vento indipendente per ognuna."),
                enableInstancing);

            EditorGUILayout.Space();

            overrideHeight = EditorGUILayout.Toggle(
                new GUIContent("Altezza manuale",
                    "Normalmente l'altezza viene calcolata dalla mesh. " +
                    "Attiva solo se il risultato non ti convince."),
                overrideHeight);

            EditorGUI.BeginDisabledGroup(!overrideHeight);
            manualHeight = EditorGUILayout.FloatField("   Altezza (metri)", manualHeight);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            int selected = (Selection.gameObjects != null) ? Selection.gameObjects.Length : 0;
            EditorGUILayout.LabelField("Selezionati: " + selected + " oggetti");

            EditorGUI.BeginDisabledGroup(selected == 0);

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Analizza", GUILayout.Height(28)))
                Analyze();

            GUI.backgroundColor = new Color(0.6f, 1f, 0.6f);
            if (GUILayout.Button("Converti", GUILayout.Height(28)))
                ConvertSelection();
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            EditorGUI.EndDisabledGroup();

            EditorGUILayout.Space();

            if (!string.IsNullOrEmpty(report))
            {
                EditorGUILayout.LabelField("Report", EditorStyles.boldLabel);
                scroll = EditorGUILayout.BeginScrollView(scroll);
                EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
                EditorGUILayout.EndScrollView();
            }
        }

        // ====================================================================
        //  ANALISI
        // ====================================================================

        void Analyze()
        {
            StringBuilder sb = new StringBuilder();

            GameObject[] selection = Selection.gameObjects;

            for (int s = 0; s < selection.Length; s++)
            {
                GameObject go = selection[s];
                if (go == null) continue;

                sb.AppendLine("=== " + go.name + " ===");

                PrefabAssetType type = PrefabUtility.GetPrefabAssetType(go);
                sb.AppendLine("  Tipo: " + type);

                Renderer[] rends = go.GetComponentsInChildren<Renderer>(true);
                sb.AppendLine("  Renderer: " + rends.Length);

                if (rends.Length == 0)
                {
                    sb.AppendLine("  ATTENZIONE: nessun Renderer. Non e un modello valido.");
                    sb.AppendLine();
                    continue;
                }

                Bounds b;
                if (ComputeBounds(go, out b))
                {
                    float h = b.max.y - b.min.y;
                    sb.AppendLine("  Altezza: " + h.ToString("0.00") + " m");
                    sb.AppendLine("  Base Y:  " + b.min.y.ToString("0.000"));

                    if (Mathf.Abs(b.min.y) > 0.02f)
                        sb.AppendLine("  NOTA: il pivot NON e alla base. Verra compensato " +
                                      "con _PlantBaseY, ma per lo scatter e meglio " +
                                      "un pivot a terra.");

                    if (h < 0.02f)
                        sb.AppendLine("  ATTENZIONE: altezza quasi nulla. Probabile " +
                                      "problema di scala nell'import (Blender esporta " +
                                      "spesso a 0.01).");

                    if (h > 60f)
                        sb.AppendLine("  ATTENZIONE: altezza enorme. Controlla lo Scale " +
                                      "Factor nelle impostazioni di import.");
                }

                // ---- vertex color e triangoli --------------------------------
                bool anyVC = false;
                int tris = 0;

                MeshFilter[] mfs = go.GetComponentsInChildren<MeshFilter>(true);
                for (int i = 0; i < mfs.Length; i++)
                {
                    Mesh mesh = mfs[i].sharedMesh;
                    if (mesh == null) continue;

                    tris += mesh.triangles.Length / 3;

                    Color32[] cols = mesh.colors32;
                    if (cols != null && cols.Length > 0) anyVC = true;
                }

                sb.AppendLine("  Triangoli: " + tris);
                sb.AppendLine("  Vertex color: " + (anyVC ? "presenti" : "assenti"));

                if (!anyVC)
                    sb.AppendLine("    Nessun problema: verra usata la maschera per " +
                                  "altezza, che non richiede preparazione del modello.");

                if (tris > 3000)
                    sb.AppendLine("  NOTA: " + tris + " triangoli sono tanti per un " +
                                  "elemento replicato migliaia di volte. Valuta una LOD.");

                // ---- materiali -----------------------------------------------
                HashSet<Material> mats = new HashSet<Material>();
                for (int i = 0; i < rends.Length; i++)
                {
                    Material[] shared = rends[i].sharedMaterials;
                    for (int k = 0; k < shared.Length; k++)
                        if (shared[k] != null) mats.Add(shared[k]);
                }

                sb.AppendLine("  Materiali: " + mats.Count);

                foreach (Material m in mats)
                {
                    PlantPreset guessed = GuessPreset(m.name, "");
                    Texture t = FindBaseTexture(m);
                    string shaderName = (m.shader != null) ? m.shader.name : "NULL";

                    sb.AppendLine("    - " + m.name +
                                  "  [shader: " + shaderName + "]" +
                                  "  -> " + guessed +
                                  ((t == null) ? "  SENZA TEXTURE" : ""));
                }

                // ---- LOD ------------------------------------------------------
                LODGroup lod = go.GetComponentInChildren<LODGroup>();
                if (lod != null)
                    sb.AppendLine("  LODGroup con " + lod.lodCount + " livelli. " +
                                  "Verranno convertiti tutti.");

                // ---- scala ----------------------------------------------------
                if (go.transform.localScale != Vector3.one)
                    sb.AppendLine("  NOTA: scala del prefab " + go.transform.localScale +
                                  ". Meglio normalizzarla nell'importer.");

                sb.AppendLine();
            }

            report = sb.ToString();
            Repaint();
        }

        // ====================================================================
        //  CONVERSIONE
        // ====================================================================

        void ConvertSelection()
        {
            Shader windShader = Shader.Find(SHADER_NAME);
            if (windShader == null)
            {
                EditorUtility.DisplayDialog("Shader mancante",
                    "Non trovo lo shader '" + SHADER_NAME + "'.\n\n" +
                    "Verifica che NatureWind.shader sia nel progetto e che " +
                    "non abbia errori di compilazione.", "Ok");
                return;
            }

            if (!EnsureFolder(outputFolder))
            {
                EditorUtility.DisplayDialog("Cartella non valida",
                    "Il percorso '" + outputFolder + "' non e valido.\n\n" +
                    "Deve iniziare con 'Assets/'.", "Ok");
                return;
            }

            StringBuilder sb = new StringBuilder();
            int converted = 0;

            // La cache impedisce di creare dieci materiali identici quando lo
            // stesso materiale sorgente e condiviso da piu prefab.
            Dictionary<Material, Material> cache = new Dictionary<Material, Material>();

            GameObject[] selection = Selection.gameObjects;

            AssetDatabase.StartAssetEditing();

            try
            {
                for (int i = 0; i < selection.Length; i++)
                {
                    GameObject src = selection[i];
                    if (src == null) continue;

                    EditorUtility.DisplayProgressBar("Nature Wind Setup",
                        src.name, (float)i / Mathf.Max(1, selection.Length));

                    string result = ConvertOne(src, windShader, cache);
                    sb.AppendLine(result);

                    if (!result.StartsWith("SALTATO")) converted++;
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            sb.AppendLine();
            sb.AppendLine("Completato: " + converted + " prefab, " +
                          cache.Count + " materiali creati.");
            sb.AppendLine();
            sb.AppendLine("PROSSIMI PASSI");
            sb.AppendLine("  1. Usa i prefab _Wind nella lista di NatureScatter.");
            sb.AppendLine("  2. In NatureScatter DISATTIVA 'Mark Static'.");
            sb.AppendLine("     Il static batching fonde le mesh e azzera la matrice");
            sb.AppendLine("     per istanza: tutte le piante ondeggerebbero in sincrono.");
            sb.AppendLine("  3. Metti un NatureWind in scena se non c'e gia.");

            report = sb.ToString();
            Repaint();
        }

        string ConvertOne(GameObject src, Shader windShader,
                          Dictionary<Material, Material> cache)
        {
            PrefabAssetType type = PrefabUtility.GetPrefabAssetType(src);

            if (type == PrefabAssetType.NotAPrefab)
                return "SALTATO " + src.name + ": non e un prefab. Trascinalo " +
                       "prima nel Project.";

            string srcPath = AssetDatabase.GetAssetPath(src);
            if (string.IsNullOrEmpty(srcPath))
                return "SALTATO " + src.name + ": nessun percorso asset.";

            // ---- calcolo altezza e base --------------------------------------
            Bounds bounds;
            if (!ComputeBounds(src, out bounds))
                return "SALTATO " + src.name + ": nessuna mesh valida.";

            float height = overrideHeight ? manualHeight : (bounds.max.y - bounds.min.y);
            float baseY = overrideHeight ? 0f : bounds.min.y;

            if (height < 0.001f)
                return "SALTATO " + src.name + ": altezza nulla, controlla lo Scale Factor.";

            // ---- creazione del target modificabile ----------------------------
            // I model prefab (FBX, GLB) sono generati dall'importer e non si
            // possono modificare. Ne creiamo una variante: eredita tutto ma
            // permette override, e se domani reimporti il modello aggiornato
            // la variante lo segue automaticamente.
            string targetPath;
            bool needVariant = (type == PrefabAssetType.Model) || createVariants;

            if (needVariant)
            {
                string dir = System.IO.Path.GetDirectoryName(srcPath);
                dir = dir.Replace('\\', '/');

                targetPath = AssetDatabase.GenerateUniqueAssetPath(
                    dir + "/" + src.name + "_Wind.prefab");

                GameObject temp = PrefabUtility.InstantiatePrefab(src) as GameObject;
                if (temp == null)
                    return "SALTATO " + src.name + ": instanziazione fallita.";

                GameObject variant = PrefabUtility.SaveAsPrefabAsset(temp, targetPath);
                UnityEngine.Object.DestroyImmediate(temp);

                if (variant == null)
                    return "SALTATO " + src.name + ": variante non creata.";
            }
            else
            {
                targetPath = srcPath;
            }

            // ---- modifica del contenuto del prefab -----------------------------
            GameObject contents = PrefabUtility.LoadPrefabContents(targetPath);
            if (contents == null)
                return "SALTATO " + src.name + ": impossibile aprire il prefab.";

            int matCount = 0;

            try
            {
                Renderer[] rends = contents.GetComponentsInChildren<Renderer>(true);

                for (int i = 0; i < rends.Length; i++)
                {
                    Renderer r = rends[i];

                    // I BillboardRenderer di SpeedTree usano una pipeline
                    // dedicata e non accettano materiali custom.
                    if (r is BillboardRenderer) continue;

                    Material[] mats = r.sharedMaterials;
                    bool changed = false;

                    for (int m = 0; m < mats.Length; m++)
                    {
                        Material orig = mats[m];
                        if (orig == null) continue;

                        // gia convertito
                        if (orig.shader != null && orig.shader.name == SHADER_NAME)
                            continue;

                        Material convertedMat;
                        if (!cache.TryGetValue(orig, out convertedMat))
                        {
                            PlantPreset p = (preset == PlantPreset.Auto)
                                ? GuessPreset(orig.name, r.gameObject.name)
                                : preset;

                            convertedMat = BuildMaterial(orig, windShader, p, height, baseY);
                            cache[orig] = convertedMat;
                            matCount++;
                        }

                        mats[m] = convertedMat;
                        changed = true;
                    }

                    if (changed) r.sharedMaterials = mats;

                    // Il vento sposta i vertici: i bounds calcolati da Unity
                    // non lo sanno e la pianta puo sparire ai bordi dello
                    // schermo mentre e piegata. Allarghiamo i bounds del
                    // renderer per compensare.
                    MeshRenderer mr = r as MeshRenderer;
                    if (mr != null)
                    {
                        Bounds lb = mr.localBounds;
                        lb.Expand(height * 0.5f);
                        mr.localBounds = lb;
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(contents, targetPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return "OK " + System.IO.Path.GetFileName(targetPath) +
                   "  (h " + height.ToString("0.00") + " m, " +
                   matCount + " materiali nuovi)";
        }

        // ====================================================================
        //  COSTRUZIONE MATERIALE
        // ====================================================================

        Material BuildMaterial(Material src, Shader windShader, PlantPreset p,
                               float height, float baseY)
        {
            PresetData d = PresetData.Get(p);

            Material m = new Material(windShader);
            m.name = src.name + "_Wind";

            // ---- texture e colore, dal materiale originale --------------------
            Texture baseTex = FindBaseTexture(src);
            if (baseTex != null)
            {
                m.SetTexture("_BaseMap", baseTex);
                m.SetTextureScale("_BaseMap", FindTextureScale(src));
                m.SetTextureOffset("_BaseMap", FindTextureOffset(src));
            }
            else
            {
                Debug.LogWarning("[NatureSetup] Il materiale '" + src.name +
                                 "' non ha una texture riconoscibile. " +
                                 "Assegnala a mano nel materiale generato.");
            }

            m.SetColor("_BaseColor", FindBaseColor(src));

            // ---- alpha clip ----------------------------------------------------
            // Il fogliame e fatto di card rettangolari con la sagoma della
            // foglia nell'alpha. Senza clip vedresti i rettangoli.
            if (d.alphaClip)
            {
                m.SetFloat("_AlphaClip", 1f);
                m.EnableKeyword("_ALPHATEST_ON");

                float cutoff = d.cutoff;
                if (src.HasProperty("_Cutoff"))
                {
                    float srcCutoff = src.GetFloat("_Cutoff");
                    if (srcCutoff > 0.01f) cutoff = srcCutoff;
                }
                m.SetFloat("_Cutoff", Mathf.Clamp01(cutoff));
            }
            else
            {
                m.SetFloat("_AlphaClip", 0f);
                m.DisableKeyword("_ALPHATEST_ON");
            }

            // ---- doppia faccia --------------------------------------------------
            // Le card di fogliame sono piani senza spessore: viste da dietro
            // sparirebbero. Il tronco invece e un solido e va cullato.
            m.SetFloat("_Cull", d.doubleSided ? 0f : 2f);

            // ---- parametri di vento ---------------------------------------------
            m.SetFloat("_PlantHeight", height);
            m.SetFloat("_PlantBaseY", baseY);
            m.SetFloat("_WindStiffness", d.stiffness);
            m.SetFloat("_WindStrength", d.strength);
            m.SetFloat("_FlutterAmount", d.flutter);
            m.SetFloat("_FlutterSpeed", d.flutterSpeed);
            m.SetFloat("_PhaseVariation", d.phaseVariation);

            // ---- aspetto ---------------------------------------------------------
            m.SetFloat("_Translucency", d.translucency);
            m.SetFloat("_ColorVariation", d.colorVariation);
            m.SetColor("_TintA", new Color(0.88f, 1f, 0.80f));
            m.SetColor("_TintB", new Color(1f, 0.96f, 0.74f));

            // La maschera per altezza funziona su qualsiasi mesh senza
            // preparazione. Lasciamo spento il vertex color: molti esportatori
            // scrivono bianco pieno, che darebbe maschera 1 ovunque e farebbe
            // TRASLARE la pianta invece di piegarla, staccandola dal terreno.
            m.DisableKeyword("_USE_VERTEX_COLOR_MASK");
            m.SetFloat("_UseVCMask", 0f);

            m.enableInstancing = enableInstancing;

            string path = AssetDatabase.GenerateUniqueAssetPath(
                outputFolder + "/" + SanitizeFileName(m.name) + ".mat");
            AssetDatabase.CreateAsset(m, path);

            return m;
        }

        // ====================================================================
        //  RICONOSCIMENTO AUTOMATICO
        // ====================================================================

        /// <summary>
        /// Indovina il tipo di pianta dai nomi. Gli asset pack seguono
        /// convenzioni abbastanza stabili: bark, trunk, wood per il legno;
        /// leaf, foliage, branch per la chioma.
        /// </summary>
        static PlantPreset GuessPreset(string materialName, string objectName)
        {
            string s = ((materialName == null ? "" : materialName) + " " +
                        (objectName == null ? "" : objectName)).ToLowerInvariant();

            if (Contains(s, "bark", "trunk", "stem", "wood", "log", "branch_base"))
                return PlantPreset.TreeTrunk;

            if (Contains(s, "grass", "erba", "blade", "weed", "wheat", "reed", "fern"))
                return PlantPreset.Grass;

            if (Contains(s, "bush", "shrub", "cespuglio", "hedge"))
                return PlantPreset.Bush;

            if (Contains(s, "leaf", "leaves", "foliage", "canopy", "needle",
                            "frond", "tree", "albero", "pine", "oak", "birch"))
                return PlantPreset.TreeCanopy;

            return PlantPreset.Bush;
        }

        static bool Contains(string s, params string[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
                if (s.Contains(keys[i])) return true;
            return false;
        }

        // ====================================================================
        //  ESTRAZIONE PROPRIETA
        // ====================================================================

        // I nomi delle proprieta cambiano tra URP/Lit, Standard e i vari
        // importer glTF. Le proviamo tutte, in ordine di probabilita.
        static readonly string[] TEX_NAMES = {
            "_BaseMap", "_MainTex", "_BaseColorTexture", "baseColorTexture",
            "_AlbedoMap", "_Albedo"
        };

        static readonly string[] COL_NAMES = {
            "_BaseColor", "_Color", "baseColorFactor", "_BaseColorFactor"
        };

        static Texture FindBaseTexture(Material m)
        {
            if (m == null) return null;

            for (int i = 0; i < TEX_NAMES.Length; i++)
            {
                if (!m.HasProperty(TEX_NAMES[i])) continue;
                Texture t = m.GetTexture(TEX_NAMES[i]);
                if (t != null) return t;
            }
            return m.mainTexture;
        }

        static Vector2 FindTextureScale(Material m)
        {
            if (m == null) return Vector2.one;

            for (int i = 0; i < TEX_NAMES.Length; i++)
                if (m.HasProperty(TEX_NAMES[i]))
                    return m.GetTextureScale(TEX_NAMES[i]);

            return Vector2.one;
        }

        static Vector2 FindTextureOffset(Material m)
        {
            if (m == null) return Vector2.zero;

            for (int i = 0; i < TEX_NAMES.Length; i++)
                if (m.HasProperty(TEX_NAMES[i]))
                    return m.GetTextureOffset(TEX_NAMES[i]);

            return Vector2.zero;
        }

        static Color FindBaseColor(Material m)
        {
            if (m == null) return Color.white;

            for (int i = 0; i < COL_NAMES.Length; i++)
            {
                if (!m.HasProperty(COL_NAMES[i])) continue;

                Color c = m.GetColor(COL_NAMES[i]);
                // molti importer scrivono nero: sarebbe una pianta invisibile
                if (c.maxColorComponent > 0.02f) return c;
            }
            return Color.white;
        }

        // ====================================================================
        //  UTILITY
        // ====================================================================

        /// <summary>
        /// Bounds combinati di tutte le mesh, espressi nello spazio della
        /// radice del prefab. Non usiamo Renderer.bounds perche richiede che
        /// l'oggetto sia istanziato in scena: qui lavoriamo su asset.
        /// </summary>
        static bool ComputeBounds(GameObject root, out Bounds result)
        {
            result = new Bounds();
            bool any = false;

            Matrix4x4 rootInv = root.transform.worldToLocalMatrix;

            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                Mesh mesh = filters[i].sharedMesh;
                if (mesh == null) continue;

                Matrix4x4 mtx = rootInv * filters[i].transform.localToWorldMatrix;
                Bounds mb = mesh.bounds;

                // trasformiamo tutti e otto gli spigoli: trasformare solo
                // centro ed estensione darebbe risultati sbagliati con
                // qualsiasi rotazione nella gerarchia
                for (int c = 0; c < 8; c++)
                {
                    Vector3 corner = new Vector3(
                        ((c & 1) == 0) ? mb.min.x : mb.max.x,
                        ((c & 2) == 0) ? mb.min.y : mb.max.y,
                        ((c & 4) == 0) ? mb.min.z : mb.max.z);

                    Vector3 p = mtx.MultiplyPoint3x4(corner);

                    if (!any)
                    {
                        result = new Bounds(p, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        result.Encapsulate(p);
                    }
                }
            }

            SkinnedMeshRenderer[] skins =
                root.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            for (int i = 0; i < skins.Length; i++)
            {
                Mesh mesh = skins[i].sharedMesh;
                if (mesh == null) continue;

                Bounds mb = mesh.bounds;
                Matrix4x4 mtx = rootInv * skins[i].transform.localToWorldMatrix;
                Vector3 p = mtx.MultiplyPoint3x4(mb.center);

                Bounds wb = new Bounds(p, mb.size);

                if (!any)
                {
                    result = wb;
                    any = true;
                }
                else
                {
                    result.Encapsulate(wb);
                }
            }

            return any;
        }

        static bool EnsureFolder(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;

            path = path.Replace('\\', '/').TrimEnd('/');

            if (!path.StartsWith("Assets")) return false;
            if (AssetDatabase.IsValidFolder(path)) return true;

            string[] parts = path.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i])) continue;

                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);

                current = next;
            }

            return AssetDatabase.IsValidFolder(current);
        }

        /// <summary>
        /// Rimuove dai nomi i caratteri non validi nei percorsi. Alcuni
        /// esportatori generano nomi di materiale con due punti o barre, che
        /// farebbero fallire silenziosamente CreateAsset.
        /// </summary>
        static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Material";

            char[] invalid = System.IO.Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder(name.Length);

            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                bool bad = false;

                for (int k = 0; k < invalid.Length; k++)
                {
                    if (c == invalid[k]) { bad = true; break; }
                }

                sb.Append(bad ? '_' : c);
            }

            return sb.ToString();
        }
    }
}