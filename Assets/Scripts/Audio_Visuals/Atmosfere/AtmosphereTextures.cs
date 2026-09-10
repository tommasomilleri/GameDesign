
using UnityEngine;
using UnityEngine.Rendering;

namespace Atmosphere
{
    /// <summary>
    /// Fabbrica di texture e materiali procedurali condivisi.
    ///
    /// Ogni risorsa viene generata UNA VOLTA SOLA per sessione e riusata da
    /// tutti i volumi della scena. Le texture sono piccole (32-128 px) perche'
    /// vengono sempre viste sfocate e molto ingrandite: risoluzioni maggiori
    /// allungherebbero solo il tempo di avvio senza guadagno visibile.
    /// </summary>
    public static class AtmosphereTextures
    {
        // ---- cache ----------------------------------------------------------
        static Texture2D _mote, _star, _puff, _wisp;
        static Material _matMoteAdd, _matStarAdd;

        // I campi statici sopravvivono tra le sessioni di Play quando
        // "Enter Play Mode Options" ha il domain reload disattivato, ma le
        // Texture2D e i Material sono gia' stati distrutti da Unity. Senza
        // questo reset, al secondo Play si prende MissingReferenceException.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _mote = _star = _puff = _wisp = null;
            _matMoteAdd = _matStarAdd = null;
        }

        // ====================================================================
        //  TEXTURE
        // ====================================================================

        /// <summary>
        /// Granello di polvere: nucleo compatto piu' alone tenue.
        ///
        /// Non e' un semplice disco sfocato. Un granello reale colto di taglio
        /// dalla luce e' un punto NETTO circondato da un debole alone di
        /// diffrazione. La differenza si nota: col disco sfocato il pulviscolo
        /// sembra sporco sull'obiettivo, col nucleo netto sembra materia
        /// sospesa nell'aria.
        /// </summary>
        public static Texture2D Mote()
        {
            if (_mote != null) return _mote;

            const int S = 32;
            Color[] px = new Color[S * S];

            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float d = Radius(x, y, S);

                    float core = Mathf.Clamp01(1f - d * 2.6f);
                    core *= core;

                    float halo = Mathf.Clamp01(1f - d);
                    halo = halo * halo * halo * halo * 0.30f;

                    px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(core + halo));
                }

            _mote = Make(S, "AtmoMote", px, TextureWrapMode.Clamp);
            return _mote;
        }

        /// <summary>
        /// Granello con le quattro punte della diffrazione.
        /// Dettaglio che l'occhio non registra consapevolmente ma che separa
        /// un granello credibile da un puntino generico. Da usare con
        /// parsimonia: su centinaia di particelle diventa kitsch.
        /// </summary>
        public static Texture2D Star()
        {
            if (_star != null) return _star;

            const int S = 32;
            Color[] px = new Color[S * S];

            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f) / S - 0.5f;
                    float dy = (y + 0.5f) / S - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;

                    float core = Mathf.Clamp01(1f - d * 3.2f);
                    core *= core;

                    float ax = Mathf.Clamp01(1f - Mathf.Abs(dx) * 26f);
                    float ay = Mathf.Clamp01(1f - Mathf.Abs(dy) * 26f);
                    float len = Mathf.Clamp01(1f - d * 1.15f);
                    float spikes = (ax + ay) * len * len * 0.42f;

                    px[y * S + x] = new Color(1f, 1f, 1f, Mathf.Clamp01(core + spikes));
                }

            _star = Make(S, "AtmoStar", px, TextureWrapMode.Clamp);
            return _star;
        }

        /// <summary>
        /// Banco di nebbia tondeggiante: rumore frattale sotto maschera
        /// radiale. Il rumore impedisce che i banchi sembrino palle sfocate
        /// identiche; la maschera elimina ogni bordo visibile del quad.
        /// </summary>
        public static Texture2D Puff()
        {
            if (_puff != null) return _puff;

            const int S = 128;
            Color[] px = new Color[S * S];

            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S;
                    float v = (y + 0.5f) / S;

                    float n = Fbm(u, v, 3f, 4, 11.3f);

                    float mask = Smooth(Mathf.Clamp01(1f - Radius(x, y, S)));
                    mask *= mask;

                    px[y * S + x] = new Color(1f, 1f, 1f, mask * Mathf.Lerp(0.45f, 1f, n));
                }

            _puff = Make(S, "AtmoPuff", px, TextureWrapMode.Clamp);
            return _puff;
        }

        /// <summary>
        /// Banco allungato e filamentoso per la nebbia bassa.
        ///
        /// La nebbia che striscia al suolo non e' fatta di sbuffi tondi: e'
        /// fatta di lingue stirate dal movimento dell'aria. Per questo il
        /// rumore qui e' anisotropo, con frequenza molto piu' alta sull'asse
        /// verticale, e la maschera e' ellittica.
        /// </summary>
        public static Texture2D Wisp()
        {
            if (_wisp != null) return _wisp;

            const int S = 128;
            Color[] px = new Color[S * S];

            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    float u = (x + 0.5f) / S;
                    float v = (y + 0.5f) / S;

                    float n = 0f, amp = 0.5f, fx = 1.6f, fy = 5.5f, sd = 47.9f;
                    for (int o = 0; o < 4; o++)
                    {
                        n += Mathf.PerlinNoise(u * fx + sd, v * fy + sd) * amp;
                        amp *= 0.5f; fx *= 2.05f; fy *= 2.05f; sd += 19.1f;
                    }
                    n = Mathf.SmoothStep(0.25f, 0.80f, Mathf.Clamp01(n));

                    float dx = (u - 0.5f) * 2f;
                    float dy = (v - 0.5f) * 2f * 2.1f;      // ellisse schiacciata
                    float mask = Smooth(Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy)));
                    mask *= mask;

                    px[y * S + x] = new Color(1f, 1f, 1f, mask * n);
                }

            _wisp = Make(S, "AtmoWisp", px, TextureWrapMode.Clamp);
            return _wisp;
        }

        // ====================================================================
        //  MATERIALI CONDIVISI
        // ====================================================================

        /// <summary>
        /// Materiale additivo per il pulviscolo, condiviso da tutti i volumi.
        ///
        /// Puo' essere condiviso perche' il colore del pulviscolo viene
        /// pilotato dallo startColor del ParticleSystem, non dal materiale.
        /// Un solo materiale significa che tutti i volumi di pulviscolo della
        /// scena finiscono in un unico batch.
        /// </summary>
        public static Material MoteMaterial(bool starShaped)
        {
            if (starShaped)
            {
                if (_matStarAdd == null)
                    _matStarAdd = BuildMaterial(Star(), true, 0f, "AtmoStarAdd");
                return _matStarAdd;
            }

            if (_matMoteAdd == null)
                _matMoteAdd = BuildMaterial(Mote(), true, 0f, "AtmoMoteAdd");
            return _matMoteAdd;
        }

        /// <summary>
        /// Materiale per la nebbia. NON e' condiviso: la distanza di soft fade
        /// e' una proprieta' del materiale e cambia da volume a volume, quindi
        /// ognuno deve avere il suo. Il chiamante e' responsabile di
        /// distruggerlo in OnDestroy.
        /// </summary>
        public static Material CreateMistMaterial(bool filament, bool additive,
                                                  float softFadeDistance)
        {
            return BuildMaterial(filament ? Wisp() : Puff(), additive,
                                 softFadeDistance, "AtmoMist");
        }

        // ====================================================================
        //  COSTRUZIONE MATERIALE
        // ====================================================================

        /// <summary>
        /// Costruisce un materiale particellare corretto su Built-in e URP.
        ///
        /// ATTENZIONE BUILD: Shader.Find risolve lo shader in editor, ma nella
        /// build lo shader viene STRIPPATO se nessun asset del progetto lo
        /// referenzia, e le particelle diventano magenta. Rimedi:
        ///   - Project Settings > Graphics > Always Included Shaders
        ///   - oppure assegnare un materiale a mano nel campo dell'ispettore.
        /// </summary>
        static Material BuildMaterial(Texture2D tex, bool additive,
                                      float softFadeDistance, string matName)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            bool urp = s != null;

            if (s == null) s = Shader.Find("Particles/Standard Unlit");
            if (s == null) s = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
            if (s == null) s = Shader.Find("Sprites/Default");

            if (s == null)
            {
                Debug.LogError("[Atmosphere] Nessuno shader particellare trovato. " +
                               "Le particelle saranno magenta. Assegna un materiale " +
                               "a mano nel componente.");
                Shader err = Shader.Find("Hidden/InternalErrorShader");
                return new Material(err != null ? err : Shader.Find("Sprites/Default"));
            }

            Material m = new Material(s);
            m.name = matName + "_Runtime";
            m.hideFlags = HideFlags.HideAndDontSave;

            // Built-in usa _MainTex, URP usa _BaseMap. Impostiamo entrambi
            // quando esistono: assegnare solo mainTexture non basta su URP.
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);

            m.SetOverrideTag("RenderType", "Transparent");

            if (m.HasProperty("_SrcBlend"))
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend"))
                m.SetFloat("_DstBlend", (float)(additive
                    ? BlendMode.One : BlendMode.OneMinusSrcAlpha));
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", (float)CullMode.Off);

            if (urp)
            {
                // Su URP il rendering guarda le KEYWORD. I float _Surface e
                // _Blend li legge solo la GUI dell'inspector: impostarli senza
                // le keyword non cambia nulla a schermo.
                if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f);
                if (m.HasProperty("_Blend")) m.SetFloat("_Blend", additive ? 1f : 0f);

                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.DisableKeyword("_ALPHATEST_ON");

                if (additive) { m.EnableKeyword("_ALPHAMODULATE_ON"); m.DisableKeyword("_ALPHABLEND_ON"); }
                else { m.DisableKeyword("_ALPHAMODULATE_ON"); m.EnableKeyword("_ALPHABLEND_ON"); }

                m.SetShaderPassEnabled("ShadowCaster", false);
            }
            else
            {
                // _Mode: 0 Opaque, 1 Cutout, 2 Fade, 3 Transparent, 4 Additive
                if (m.HasProperty("_Mode")) m.SetFloat("_Mode", additive ? 4f : 2f);
                if (additive) { m.EnableKeyword("_ALPHAMODULATE_ON"); m.DisableKeyword("_ALPHABLEND_ON"); }
                else { m.DisableKeyword("_ALPHAMODULATE_ON"); m.EnableKeyword("_ALPHABLEND_ON"); }
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            // ---- SOFT PARTICLES ---------------------------------------------
            // Senza questo i banchi tagliano pavimento e muri con una linea
            // dritta e l'illusione crolla in mezzo secondo. Richiede la Depth
            // Texture attiva. Sul pulviscolo non serve: e' troppo piccolo.
            if (softFadeDistance > 0f && m.HasProperty("_SoftParticlesEnabled"))
            {
                m.SetFloat("_SoftParticlesEnabled", 1f);
                m.EnableKeyword("_SOFTPARTICLES_ON");

                float far = Mathf.Max(0.01f, softFadeDistance);
                if (m.HasProperty("_SoftParticleFadeParams"))
                    m.SetVector("_SoftParticleFadeParams", new Vector4(0f, 1f / far, 0f, 0f));
                if (m.HasProperty("_SoftParticlesNearFadeDistance"))
                    m.SetFloat("_SoftParticlesNearFadeDistance", 0f);
                if (m.HasProperty("_SoftParticlesFarFadeDistance"))
                    m.SetFloat("_SoftParticlesFarFadeDistance", far);
            }

            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
        }

        // ====================================================================
        //  HELPER
        // ====================================================================

        static Texture2D Make(int size, string texName, Color[] px, TextureWrapMode wrap)
        {
            Texture2D t = new Texture2D(size, size, TextureFormat.RGBA32, true, true);
            t.name = texName + "_Runtime";
            t.wrapMode = wrap;
            t.filterMode = FilterMode.Bilinear;
            t.anisoLevel = 1;
            t.hideFlags = HideFlags.HideAndDontSave;
            t.SetPixels(px);
            t.Apply(true, true);        // genera mipmap e libera la copia in RAM
            return t;
        }

        static float Radius(int x, int y, int size)
        {
            float dx = (x + 0.5f) / size - 0.5f;
            float dy = (y + 0.5f) / size - 0.5f;
            return Mathf.Sqrt(dx * dx + dy * dy) * 2f;
        }

        static float Smooth(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        static float Fbm(float u, float v, float freq, int octaves, float seed)
        {
            float n = 0f, amp = 0.5f;
            for (int o = 0; o < octaves; o++)
            {
                n += Mathf.PerlinNoise(u * freq + seed, v * freq + seed) * amp;
                amp *= 0.5f; freq *= 2.1f; seed += 13.7f;
            }
            return Mathf.Clamp01(n);
        }
    }
}
