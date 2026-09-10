
using UnityEngine;
using UnityEngine.Rendering;

namespace Atmosphere
{
    /// <summary>
    /// Classe base per tutti i volumi atmosferici.
    ///
    /// Gestisce il ciclo di vita del ParticleSystem, il volume di emissione,
    /// l'inseguimento della camera, il LOD e il budget di particelle.
    /// Le sottoclassi definiscono solo l'aspetto e il comportamento specifico.
    ///
    /// NON aggiungere questo componente direttamente: usa AmbientDust o
    /// AmbientMist.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class AtmosphereVolume : MonoBehaviour
    {
        // ====================================================================
        //  MODALITA' DI RIEMPIMENTO
        // ====================================================================
        public enum FillMode
        {
            /// <summary>
            /// Volume fisso in world space. Le particelle nascono ovunque nella
            /// scatola. Adatto a stanze e ambienti chiusi fino a circa 20 metri
            /// di lato: oltre, la maggior parte delle particelle finisce fuori
            /// dal campo visivo e il costo e' sprecato.
            /// </summary>
            FixedBox,

            /// <summary>
            /// Il volume segue la camera. La densita' percepita resta costante
            /// a costo costante, indipendentemente da quanto e' grande
            /// l'ambiente. E' la modalita' giusta per spazi ampi o aperti.
            /// Il volume viene comunque limitato dai Bounds se sono definiti.
            /// </summary>
            CameraFollow
        }

        [Header("Volume")]
        [Tooltip("FixedBox per stanze chiuse. CameraFollow per ambienti grandi " +
                 "o aperti: densita' costante a costo costante.")]
        public FillMode fillMode = FillMode.FixedBox;

        [Tooltip("Dimensioni della scatola di emissione in metri. " +
                 "In CameraFollow rappresenta il volume che segue la camera.")]
        public Vector3 volumeSize = new Vector3(12f, 4f, 12f);

        [Tooltip("Centro del volume rispetto al pivot dell'oggetto.")]
        public Vector3 volumeOffset = Vector3.zero;

        [Tooltip("Se ON, prova a dimensionare il volume automaticamente sui " +
                 "Renderer figli di questo oggetto. Comodo per agganciarlo " +
                 "al mesh di una stanza.")]
        public bool autoFitToChildren = false;

        [Header("Confini (solo CameraFollow)")]
        [Tooltip("Se ON, l'atmosfera esiste solo dentro la scatola definita da " +
                 "boundsSize. Fuori si spegne. Utile per non avere nebbia negli " +
                 "interni quando il volume e' pensato per l'esterno.")]
        public bool useBounds = false;

        public Vector3 boundsSize = new Vector3(60f, 20f, 60f);
        public Vector3 boundsCenter = Vector3.zero;

        [Header("Densita'")]
        [Range(0, 600)]
        [Tooltip("Particelle vive contemporaneamente PRIMA del moltiplicatore " +
                 "globale di AtmosphereQuality.")]
        public int count = 60;

        [Min(0.5f)]
        [Tooltip("Durata di vita media in secondi. Valori alti significano " +
                 "meno nascite al secondo a parita' di popolazione, quindi " +
                 "meno lavoro per la CPU.")]
        public float lifetime = 14f;

        [Header("Movimento")]
        [Tooltip("Deriva costante in metri al secondo, world space.")]
        public Vector3 drift = new Vector3(0.02f, 0.01f, 0f);

        [Range(0f, 2f)]
        [Tooltip("Turbolenza interna. Impedisce che le particelle vadano dritte.")]
        public float turbulence = 0.4f;

        [Min(0.005f)]
        [Tooltip("Scala spaziale della turbolenza. Bassa = strutture ampie e " +
                 "lente. Alta = agitazione fine.")]
        public float turbulenceScale = 0.15f;

        [Header("Performance")]
        [Min(0f)]
        [Tooltip("Oltre questa distanza dalla camera il volume si spegne. " +
                 "0 = sempre attivo. Ignorato in CameraFollow.")]
        public float cullDistance = 0f;

        [Tooltip("Se ON il sistema si mette in pausa quando non e' visibile da " +
                 "nessuna camera. Fa risparmiare CPU in scene con molti volumi.")]
        public bool pauseWhenInvisible = true;

        [Header("Materiale")]
        [Tooltip("Lascia vuoto per generarlo a runtime. In BUILD e' piu' sicuro " +
                 "assegnare un materiale creato nel progetto: lo shader trovato " +
                 "con Shader.Find puo' essere strippato.")]
        public Material materialOverride;

        // ---- protetti, accessibili alle sottoclassi -------------------------
        protected ParticleSystem ps;
        protected ParticleSystem.MainModule main;
        protected ParticleSystem.EmissionModule emission;
        protected ParticleSystem.ShapeModule shape;
        protected ParticleSystem.ForceOverLifetimeModule force;
        protected ParticleSystemRenderer psr;
        protected Transform volumeT;
        protected float seed;

        // ---- privati --------------------------------------------------------
        Material ownedMaterial;
        Camera cam;
        float camSearchTimer;
        bool built;
        bool active = true;
        Vector3 lastFollowPos;
        int appliedCount = -1;

        // ====================================================================
        //  CICLO DI VITA
        // ====================================================================
        protected virtual void Awake()
        {
            seed = Random.Range(0f, 1000f);
            Build();
        }

        protected virtual void OnEnable()
        {
            AtmosphereQuality.Changed += OnQualityChanged;
            if (built && ps != null && !ps.isPlaying) ps.Play(true);
        }

        protected virtual void OnDisable()
        {
            AtmosphereQuality.Changed -= OnQualityChanged;
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        protected virtual void OnDestroy()
        {
            // distruggiamo SOLO il materiale creato da noi. Quello assegnato
            // nell'ispettore e' un asset di progetto e non va toccato.
            if (ownedMaterial != null)
            {
                if (Application.isPlaying) Destroy(ownedMaterial);
                else DestroyImmediate(ownedMaterial);
            }
        }

        void OnQualityChanged()
        {
            if (!built) return;
            ApplyDensity();
            RefreshMaterial();
        }

        // ====================================================================
        //  COSTRUZIONE
        // ====================================================================
        void Build()
        {
            if (built) return;
            built = true;

            if (autoFitToChildren) AutoFit();

            GameObject go = new GameObject(GetType().Name + "_PS");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = volumeOffset;
            volumeT = go.transform;

            ps = go.AddComponent<ParticleSystem>();
            psr = go.GetComponent<ParticleSystemRenderer>();

            // ---- MAIN --------------------------------------------------------
            main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startSpeed = 0f;      // il moto viene tutto dalle forze
            main.gravityModifier = 0f;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            // World space: le particelle NON devono seguire il volume quando
            // questo si sposta dietro la camera, altrimenti sembrano incollate
            // alla vista e il movimento del giocatore non le attraversa.
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.scalingMode = ParticleSystemScalingMode.Local;

            // Senza prewarm la scena parte limpida e si riempie lentamente nei
            // primi secondi. Con lifetime lunghi il riempimento richiederebbe
            // anche mezzo minuto, ben visibile all'ingresso in una stanza.
            main.prewarm = true;

            // ---- SHAPE -------------------------------------------------------
            shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = volumeSize;

            emission = ps.emission;

            // ---- FORZE -------------------------------------------------------
            force = ps.forceOverLifetime;
            force.enabled = true;
            force.space = ParticleSystemSimulationSpace.World;

            // ---- TURBOLENZA --------------------------------------------------
            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = turbulence > 0f;
            noise.strength = turbulence;
            noise.frequency = turbulenceScale;
            noise.scrollSpeed = turbulenceScale * 0.6f;
            noise.octaveCount = 2;
            noise.octaveMultiplier = 0.42f;
            noise.damping = true;
            noise.quality = ParticleSystemNoiseQuality.Low;

            // ---- RENDERER ----------------------------------------------------
            psr.shadowCastingMode = ShadowCastingMode.Off;
            psr.receiveShadows = false;
            psr.lightProbeUsage = LightProbeUsage.Off;
            psr.reflectionProbeUsage = ReflectionProbeUsage.Off;
            psr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            psr.allowOcclusionWhenDynamic = false;
            psr.renderMode = ParticleSystemRenderMode.Billboard;

            // lasciamo configurare il resto alla sottoclasse
            Configure();

            ApplyDensity();
            RefreshMaterial();

            cam = Camera.main;
            lastFollowPos = transform.position;
        }

        /// <summary>
        /// Le sottoclassi configurano qui aspetto, colore, dimensioni e curve.
        /// A questo punto ps, main, shape, emission, force e psr sono pronti.
        /// </summary>
        protected abstract void Configure();

        /// <summary>Texture e blending scelti dalla sottoclasse.</summary>
        protected abstract Material CreateMaterial();

        /// <summary>
        /// True se la sottoclasse usa un materiale condiviso e statico che NON
        /// deve essere distrutto in OnDestroy.
        /// </summary>
        protected virtual bool UsesSharedMaterial { get { return false; } }

        void RefreshMaterial()
        {
            if (psr == null) return;

            if (materialOverride != null)
            {
                psr.sharedMaterial = materialOverride;
                return;
            }

            if (ownedMaterial != null)
            {
                if (Application.isPlaying) Destroy(ownedMaterial);
                else DestroyImmediate(ownedMaterial);
                ownedMaterial = null;
            }

            Material m = CreateMaterial();
            if (!UsesSharedMaterial) ownedMaterial = m;
            psr.sharedMaterial = m;
        }

        // ====================================================================
        //  DENSITA'
        // ====================================================================
        protected void ApplyDensity()
        {
            if (ps == null) return;

            int n = Mathf.Max(0, Mathf.RoundToInt(count * AtmosphereQuality.DensityScale));
            float life = Mathf.Max(0.5f, lifetime);

            main.maxParticles = Mathf.Max(1, n);
            main.startLifetime = new ParticleSystem.MinMaxCurve(life * 0.65f, life * 1.35f);

            // Il rate e' popolazione diviso vita media: cosi' il numero di
            // particelle vive si stabilizza esattamente su n, invece di
            // saturare contro maxParticles con emissioni sprecate.
            emission.rateOverTime = (n <= 0) ? 0f : n / life;

            appliedCount = n;

            if (n <= 0 && ps.isPlaying) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            else if (n > 0 && !ps.isPlaying && isActiveAndEnabled) ps.Play(true);
        }

        // ====================================================================
        //  AGGIORNAMENTO
        // ====================================================================
        protected virtual void LateUpdate()
        {
            if (!built) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            AcquireCamera(dt);
            UpdatePlacement();
            UpdateActivation();

            if (!active) return;

            // la densita' e' modificabile a runtime dall'ispettore
            int target = Mathf.RoundToInt(count * AtmosphereQuality.DensityScale);
            if (target != appliedCount) ApplyDensity();

            ApplyForces();
            Tick(dt);
        }

        /// <summary>Aggiornamento specifico della sottoclasse.</summary>
        protected virtual void Tick(float dt) { }

        void AcquireCamera(float dt)
        {
            if (cam != null) return;

            // la camera puo' comparire dopo: scene additive, spawn del player
            camSearchTimer -= dt;
            if (camSearchTimer <= 0f)
            {
                cam = Camera.main;
                camSearchTimer = 1f;
            }
        }

        void UpdatePlacement()
        {
            if (fillMode != FillMode.CameraFollow || cam == null || volumeT == null)
                return;

            Vector3 target = cam.transform.position;

            // Il volume viene proiettato leggermente in avanti: cosi' le
            // particelle nascono soprattutto dove il giocatore sta guardando,
            // invece che meta' dietro la sua testa dove non le vedra' mai.
            Vector3 fwd = cam.transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude > 0.0001f)
                target += fwd.normalized * volumeSize.z * 0.22f;

            if (useBounds)
            {
                Vector3 c = transform.TransformPoint(boundsCenter);
                Vector3 h = boundsSize * 0.5f;
                target.x = Mathf.Clamp(target.x, c.x - h.x, c.x + h.x);
                target.y = Mathf.Clamp(target.y, c.y - h.y, c.y + h.y);
                target.z = Mathf.Clamp(target.z, c.z - h.z, c.z + h.z);
            }

            // Se la camera fa un salto enorme (teletrasporto, cambio scena) le
            // particelle vecchie resterebbero visibili come una scia. Meglio
            // azzerare e lasciare che il prewarm ricostruisca il volume.
            float jump = (target - lastFollowPos).sqrMagnitude;
            float limit = volumeSize.magnitude * 3f;
            if (jump > limit * limit)
            {
                volumeT.position = target;
                ps.Clear(true);
                ps.Simulate(lifetime * 0.6f, true, false, false);
                ps.Play(true);
            }
            else
            {
                volumeT.position = target;
            }

            lastFollowPos = target;
        }

        void UpdateActivation()
        {
            bool shouldBeActive = true;

            if (fillMode == FillMode.FixedBox && cullDistance > 0f && cam != null)
            {
                // distanza dalla superficie della scatola, non dal centro:
                // stando dentro un volume grande la distanza dal centro
                // potrebbe superare cullDistance e spegnere tutto.
                Vector3 c = transform.TransformPoint(volumeOffset);
                Vector3 d = cam.transform.position - c;
                Vector3 h = volumeSize * 0.5f;
                Vector3 outside = new Vector3(
                    Mathf.Max(0f, Mathf.Abs(d.x) - h.x),
                    Mathf.Max(0f, Mathf.Abs(d.y) - h.y),
                    Mathf.Max(0f, Mathf.Abs(d.z) - h.z));
                shouldBeActive = outside.sqrMagnitude <= cullDistance * cullDistance;
            }

            if (useBounds && fillMode == FillMode.CameraFollow && cam != null)
            {
                Vector3 c = transform.TransformPoint(boundsCenter);
                Vector3 d = cam.transform.position - c;
                Vector3 h = boundsSize * 0.5f;
                if (Mathf.Abs(d.x) > h.x || Mathf.Abs(d.y) > h.y || Mathf.Abs(d.z) > h.z)
                    shouldBeActive = false;
            }

            if (shouldBeActive == active) return;
            active = shouldBeActive;

            if (active)
            {
                ps.Play(true);
            }
            else
            {
                // StopEmitting invece di StopEmittingAndClear: le particelle
                // vive finiscono la loro dissolvenza e spariscono dolcemente
                // invece di scomparire tutte insieme in un frame.
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        protected virtual void ApplyForces()
        {
            Vector3 f = drift;
            force.x = new ParticleSystem.MinMaxCurve(f.x);
            force.y = new ParticleSystem.MinMaxCurve(f.y);
            force.z = new ParticleSystem.MinMaxCurve(f.z);
        }

        // ====================================================================
        //  API PUBBLICA
        // ====================================================================

        /// <summary>
        /// Riapplica dimensione del volume, densita' e materiale. Chiamalo se
        /// modifichi i campi da script durante il gioco.
        /// </summary>
        public void Refresh()
        {
            if (!built) return;
            if (volumeT != null) volumeT.localPosition =
                (fillMode == FillMode.FixedBox) ? volumeOffset : volumeT.localPosition;
            shape.scale = volumeSize;
            ApplyDensity();
            RefreshMaterial();
        }

        /// <summary>Svuota istantaneamente il volume e lo ricostruisce.</summary>
        public void ResetVolume()
        {
            if (!built || ps == null) return;
            ps.Clear(true);
            ps.Simulate(lifetime * 0.6f, true, false, false);
            ps.Play(true);
        }

        // ====================================================================
        //  UTILITY
        // ====================================================================
        void AutoFit()
        {
            Renderer[] rs = GetComponentsInChildren<Renderer>();
            if (rs == null || rs.Length == 0) return;

            Bounds b = rs[0].bounds;
            for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);

            // margine negativo: l'atmosfera deve stare DENTRO le pareti,
            // altrimenti si vedono banchi che spuntano attraverso i muri
            Vector3 size = b.size * 0.92f;
            volumeSize = size;
            volumeOffset = transform.InverseTransformPoint(b.center);
        }

        /// <summary>Gradiente di dissolvenza lenta ai due capi della vita.</summary>
        protected static ParticleSystem.MinMaxGradient FadeGradient(float inT, float outT)
        {
            inT = Mathf.Clamp(inT, 0.01f, 0.45f);
            outT = Mathf.Clamp(outT, 0.55f, 0.99f);

            Gradient g = new Gradient();
            g.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(Color.white, 0f),
                    new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(1f, inT),
                    new GradientAlphaKey(1f, outT),
                    new GradientAlphaKey(0f, 1f) });
            return new ParticleSystem.MinMaxGradient(g);
        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            volumeSize = new Vector3(
                Mathf.Max(0.1f, volumeSize.x),
                Mathf.Max(0.1f, volumeSize.y),
                Mathf.Max(0.1f, volumeSize.z));
            if (lifetime < 0.5f) lifetime = 0.5f;
            if (cullDistance < 0f) cullDistance = 0f;

            if (Application.isPlaying && built) Refresh();
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Matrix4x4 old = Gizmos.matrix;

            Gizmos.color = new Color(0.5f, 0.8f, 1f, 0.35f);
            Gizmos.matrix = Matrix4x4.TRS(
                transform.TransformPoint(volumeOffset), transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, volumeSize);

            if (useBounds)
            {
                Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
                Gizmos.matrix = Matrix4x4.TRS(
                    transform.TransformPoint(boundsCenter), transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, boundsSize);
            }

            Gizmos.matrix = old;
        }
#endif
    }
}
