
using UnityEngine;
using UnityEngine.Rendering;

namespace RealisticFire
{
    /// <summary>
    /// Costruisce e pilota tutti i sistemi particellari del fuoco.
    /// Viene creato automaticamente da FireSource: NON aggiungerlo a mano.
    /// </summary>
    [DisallowMultipleComponent]
    public class FireVFX : MonoBehaviour
    {
        FireProfile p;
        FireKind kind;
        bool built;

        ParticleSystem psFlame, psSpark, psEmber, psSmoke, psBed;
        ParticleSystem.EmissionModule emFlame, emSpark, emEmber, emSmoke, emBed;
        ParticleSystem.ForceOverLifetimeModule fFlame, fSpark, fEmber, fSmoke;

        // materiali creati a runtime: vanno distrutti da noi
        Material ownedAdditive, ownedAlpha;

        float sizeK = 1f;
        bool sparkCollision = true;

        // FIX 8: la texture statica non sopravvive al reload del dominio
        // disattivato. Va azzerata a ogni entrata in Play.
        static Texture2D _dot;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _dot = null;
        }

        // --------------------------------------------------------------------
        //  COSTRUZIONE
        // --------------------------------------------------------------------

        /// <param name="additiveOverride">
        /// Materiale additivo assegnato dall'ispettore. Se null se ne crea uno
        /// a runtime, ma in build lo shader potrebbe essere strippato.
        /// </param>
        public void Build(FireKind k, FireProfile profile,
                          Material additiveOverride, Material alphaOverride,
                          bool enableSparkCollision)
        {
            if (built)
            {
                Debug.LogWarning("[FireVFX] Build chiamato due volte. Ignorato.", this);
                return;
            }
            built = true;

            kind = k;
            p = profile;
            sparkCollision = enableSparkCollision;

            sizeK = Mathf.Clamp(p.flameHeight / 0.30f, 0.35f, 3.0f);

            // FIX 3: i materiali dall'ispettore hanno la precedenza. Solo se
            // mancano si ripiega sulla generazione a runtime.
            Material matAdditive = additiveOverride;
            if (matAdditive == null)
            {
                ownedAdditive = MakeMat(true);
                matAdditive = ownedAdditive;
            }

            Material matAlpha = alphaOverride;
            if (matAlpha == null)
            {
                ownedAlpha = MakeMat(false);
                matAlpha = ownedAlpha;
            }

            psFlame = BuildFlame(matAdditive);
            psSmoke = BuildSmoke(matAlpha);

            if (p.sparkRate > 0f || p.burstSparkCount > 0)
                psSpark = BuildSparks(matAdditive);
            if (p.emberRate > 0f)
                psEmber = BuildEmbers(matAdditive);
            if (p.hasEmbersBed)
                psBed = BuildEmberBed(matAdditive);

            emFlame = psFlame.emission; fFlame = psFlame.forceOverLifetime;
            emSmoke = psSmoke.emission; fSmoke = psSmoke.forceOverLifetime;
            if (psSpark != null) { emSpark = psSpark.emission; fSpark = psSpark.forceOverLifetime; }
            if (psEmber != null) { emEmber = psEmber.emission; fEmber = psEmber.forceOverLifetime; }
            if (psBed != null) { emBed = psBed.emission; }
        }

        void OnDestroy()
        {
            // FIX: distruggiamo SOLO i materiali che abbiamo creato noi, mai
            // quelli assegnati dall'ispettore (sarebbero asset di progetto).
            if (ownedAdditive != null) DestroyMat(ownedAdditive);
            if (ownedAlpha != null) DestroyMat(ownedAlpha);
        }

        static void DestroyMat(Material m)
        {
            if (Application.isPlaying) Destroy(m);
            else DestroyImmediate(m);
        }

        // --------------------------------------------------------------------
        //  FIAMMA - nucleo visibile, additivo, sale e si assottiglia a lacrima
        // --------------------------------------------------------------------
        ParticleSystem BuildFlame(Material mat)
        {
            ParticleSystem ps = NewPS("Flame");

            // Velocita' e durata sono calcolate in modo che la particella
            // percorra esattamente flameHeight prima di spegnersi.
            float riseTime = 0.30f + p.flameHeight * 0.35f;
            float riseSpd = p.flameHeight / riseTime;

            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(riseTime * 0.80f, riseTime * 1.50f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(riseSpd * 0.75f, riseSpd * 1.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(p.flameWidth * 1.1f, p.flameWidth * 2.0f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -0.05f;                 // convezione: sale
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 400;

            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = (kind == FireKind.Candle) ? 2f : 14f;
            sh.radius = p.flameWidth * 0.45f;

            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = p.flameRate;

            // colore: cuore caldo alla nascita, poi arancio, poi si spegne
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = Grad(
                new Color[] { p.cCore, p.cBody, p.cEmber },
                new float[] { 0f, 0.35f, 1f },
                new float[] { 0f, 1f, 0.9f, 0f },
                new float[] { 0f, 0.08f, 0.55f, 1f });

            // forma a lacrima: larga in basso, sottile in cima
            ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, Curve(
                0f, 0.35f, 0.18f, 1f, 0.6f, 0.7f, 1f, 0.05f));

            // turbolenza interna: la lingua di fuoco ondeggia
            ParticleSystem.NoiseModule no = ps.noise;
            no.enabled = true;
            no.strength = p.flameHeight * ((kind == FireKind.Candle) ? 0.35f : 1.1f);
            no.frequency = (kind == FireKind.Candle) ? 1.2f : 0.6f;
            no.scrollSpeed = 0.7f;
            no.damping = true;
            no.octaveCount = 2;

            ParticleSystem.ForceOverLifetimeModule fol = ps.forceOverLifetime;
            fol.enabled = true;
            fol.space = ParticleSystemSimulationSpace.World;

            ParticleSystemRenderer rn = ps.GetComponent<ParticleSystemRenderer>();
            rn.renderMode = ParticleSystemRenderMode.Billboard;
            rn.sharedMaterial = mat;                       // FIX: sharedMaterial
            rn.sortMode = ParticleSystemSortMode.YoungestInFront;
            rn.alignment = ParticleSystemRenderSpace.View;
            rn.shadowCastingMode = ShadowCastingMode.Off;  // FIX: mai ombre
            rn.receiveShadows = false;

            return ps;
        }

        // --------------------------------------------------------------------
        //  SCINTILLE - veloci, minuscole, gravita' reale, render allungato
        // --------------------------------------------------------------------
        ParticleSystem BuildSparks(Material mat)
        {
            ParticleSystem ps = NewPS("Sparks");

            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(
                                       Mathf.Max(0.6f, p.flameHeight * 3f),
                                       Mathf.Max(2.0f, p.flameHeight * 9f));
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f * sizeK, 0.020f * sizeK);
            main.gravityModifier = 0.30f;                  // ricadono
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;

            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 22f;
            sh.radius = p.flameWidth * 0.5f;

            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = p.sparkRate;

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = Grad(
                new Color[] {
                    new Color(1f, 0.95f, 0.75f),
                    new Color(1f, 0.55f, 0.12f),
                    new Color(0.7f, 0.12f, 0f) },
                new float[] { 0f, 0.4f, 1f },
                new float[] { 1f, 1f, 0f },
                new float[] { 0f, 0.6f, 1f });

            // ogni scintilla pulsa mentre brucia
            ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, Curve(
                0f, 1f, 0.3f, 0.75f, 0.5f, 1f, 0.75f, 0.5f, 1f, 0f));

            ParticleSystem.NoiseModule no = ps.noise;
            no.enabled = true;
            no.strength = 0.5f;
            no.frequency = 1.4f;
            no.scrollSpeed = 1.2f;

            ParticleSystem.ForceOverLifetimeModule fol = ps.forceOverLifetime;
            fol.enabled = true;
            fol.space = ParticleSystemSimulationSpace.World;

            // FIX 9: la qualita' Medium/Low usa il depth buffer, quindi le
            // scintille collidono solo con cio' che e' visibile a schermo e il
            // risultato cambia col punto di vista. High usa raycast veri.
            // Il flag e' esposto in FireSource: su mobile disattivalo.
            ParticleSystem.CollisionModule co = ps.collision;
            co.enabled = sparkCollision;
            if (sparkCollision)
            {
                co.type = ParticleSystemCollisionType.World;
                co.mode = ParticleSystemCollisionMode.Collision3D;
                co.dampen = 0.6f;
                co.bounce = 0.25f;
                co.lifetimeLoss = 0.4f;
                co.quality = ParticleSystemCollisionQuality.High;
                co.maxCollisionShapes = 64;
            }

            ParticleSystemRenderer rn = ps.GetComponent<ParticleSystemRenderer>();
            rn.renderMode = ParticleSystemRenderMode.Stretch;
            rn.velocityScale = 0.06f;
            rn.lengthScale = 2.5f;
            rn.sharedMaterial = mat;
            rn.shadowCastingMode = ShadowCastingMode.Off;
            rn.receiveShadows = false;

            return ps;
        }

        // --------------------------------------------------------------------
        //  BRACI - lente, ascendenti, molto deviate dal vento
        // --------------------------------------------------------------------
        ParticleSystem BuildEmbers(Material mat)
        {
            ParticleSystem ps = NewPS("Embers");

            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.010f * sizeK, 0.028f * sizeK);
            main.gravityModifier = -0.08f;                 // la convezione le porta su
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 150;

            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = 18f;
            sh.radius = p.flameWidth * 0.8f;

            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = p.emberRate;

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = Grad(
                new Color[] {
                    new Color(1f, 0.70f, 0.25f),
                    new Color(1f, 0.35f, 0.05f),
                    new Color(0.35f, 0.05f, 0f) },
                new float[] { 0f, 0.5f, 1f },
                new float[] { 0f, 1f, 0.8f, 0f },
                new float[] { 0f, 0.1f, 0.7f, 1f });

            ParticleSystem.NoiseModule no = ps.noise;
            no.enabled = true;
            no.strength = 0.9f;
            no.frequency = 0.35f;
            no.scrollSpeed = 0.4f;
            no.octaveCount = 2;

            ParticleSystem.ForceOverLifetimeModule fol = ps.forceOverLifetime;
            fol.enabled = true;
            fol.space = ParticleSystemSimulationSpace.World;

            ParticleSystemRenderer rn = ps.GetComponent<ParticleSystemRenderer>();
            rn.renderMode = ParticleSystemRenderMode.Billboard;
            rn.sharedMaterial = mat;
            rn.shadowCastingMode = ShadowCastingMode.Off;
            rn.receiveShadows = false;

            return ps;
        }

        // --------------------------------------------------------------------
        //  FUMO - alpha blended, si espande, rallenta, svanisce
        // --------------------------------------------------------------------
        ParticleSystem BuildSmoke(Material mat)
        {
            ParticleSystem ps = NewPS("Smoke");

            // smokeRise e' una VELOCITA' in metri al secondo, non una durata.
            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 3.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(p.smokeRise * 0.55f,
                                                                  p.smokeRise * 1.05f);
            main.startSize = new ParticleSystem.MinMaxCurve(p.flameWidth * 1.5f,
                                                                  p.flameWidth * 3.0f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = -0.02f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;

            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Cone;
            sh.angle = (kind == FireKind.Candle) ? 3f : 10f;
            sh.radius = p.flameWidth * 0.4f;

            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = p.smokeRate;

            // nasce ancora caldo (rossastro), poi grigio, poi sparisce
            float dark = Mathf.Lerp(0.55f, 0.16f, p.fuelRichness);
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = Grad(
                new Color[] {
                    new Color(0.45f, 0.25f, 0.15f),
                    new Color(dark, dark, dark),
                    new Color(dark, dark, dark) },
                new float[] { 0f, 0.25f, 1f },
                new float[] { 0f, Mathf.Lerp(0.12f, 0.42f, p.fuelRichness), 0f },
                new float[] { 0f, 0.22f, 1f });

            ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, Curve(0f, 0.4f, 1f, 3.2f));

            ParticleSystem.RotationOverLifetimeModule rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-0.6f, 0.6f);

            // rallenta salendo: attrito con l'aria
            ParticleSystem.LimitVelocityOverLifetimeModule vel = ps.limitVelocityOverLifetime;
            vel.enabled = true;
            vel.limit = new ParticleSystem.MinMaxCurve(Mathf.Max(0.05f, p.smokeRise));
            vel.dampen = 0.12f;

            ParticleSystem.NoiseModule no = ps.noise;
            no.enabled = true;
            no.strength = p.smokeRise * 0.8f;
            no.frequency = 0.25f;
            no.scrollSpeed = 0.3f;
            no.octaveCount = 2;
            no.damping = true;

            ParticleSystem.ForceOverLifetimeModule fol = ps.forceOverLifetime;
            fol.enabled = true;
            fol.space = ParticleSystemSimulationSpace.World;

            ParticleSystemRenderer rn = ps.GetComponent<ParticleSystemRenderer>();
            rn.renderMode = ParticleSystemRenderMode.Billboard;
            rn.sharedMaterial = mat;
            rn.sortMode = ParticleSystemSortMode.OldestInFront;
            rn.shadowCastingMode = ShadowCastingMode.Off;
            rn.receiveShadows = false;

            return ps;
        }

        // --------------------------------------------------------------------
        //  LETTO DI BRACE - solo camino, pulsa a terra sotto la fiamma
        // --------------------------------------------------------------------
        ParticleSystem BuildEmberBed(Material mat)
        {
            ParticleSystem ps = NewPS("EmberBed");
            ps.transform.localPosition = new Vector3(0f, -p.flameHeight * 0.45f, 0f);

            ParticleSystem.MainModule main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 4f);
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f * sizeK, 0.075f * sizeK);
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 120;

            ParticleSystem.ShapeModule sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Box;
            sh.scale = new Vector3(p.flameWidth * 2.2f, 0.02f, p.flameWidth * 1.4f);

            ParticleSystem.EmissionModule em = ps.emission;
            em.rateOverTime = p.bedRate;

            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            col.color = Grad(
                new Color[] {
                    new Color(1f, 0.30f, 0.04f),
                    new Color(1f, 0.55f, 0.12f),
                    new Color(0.30f, 0.03f, 0f) },
                new float[] { 0f, 0.45f, 1f },
                new float[] { 0f, 1f, 0.85f, 0f },
                new float[] { 0f, 0.15f, 0.6f, 1f });

            // ogni brace pulsa in modo indipendente: il letto respira
            ParticleSystem.SizeOverLifetimeModule sz = ps.sizeOverLifetime;
            sz.enabled = true;
            sz.size = new ParticleSystem.MinMaxCurve(1f, Curve(
                0f, 0.6f, 0.25f, 1f, 0.5f, 0.65f, 0.75f, 1f, 1f, 0.4f));

            ParticleSystemRenderer rn = ps.GetComponent<ParticleSystemRenderer>();
            rn.renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            rn.sharedMaterial = mat;
            rn.shadowCastingMode = ShadowCastingMode.Off;
            rn.receiveShadows = false;

            return ps;
        }

        // --------------------------------------------------------------------
        //  RUNTIME - il VFX reagisce a intensita', vento e scoppiettii
        // --------------------------------------------------------------------

        /// <param name="heat">0..1, quanto la fiamma e' viva in questo istante.</param>
        /// <param name="wind">Vettore vento in world space.</param>
        public void Tick(float heat, Vector3 wind)
        {
            if (!built) return;

            heat = Mathf.Clamp01(heat);
            float h = Mathf.Lerp(0.55f, 1.35f, heat);

            emFlame.rateOverTime = p.flameRate * h;
            // fuma di piu' quando la fiamma cala: combustione incompleta
            emSmoke.rateOverTime = p.smokeRate * Mathf.Lerp(1.4f, 0.7f, heat);
            if (psSpark != null) emSpark.rateOverTime = p.sparkRate * h;
            if (psEmber != null) emEmber.rateOverTime = p.emberRate * h;

            // FIX 10: il letto di brace ora respira anche lui, ma in
            // controfase attenuata: quando la fiamma cala la brace resta.
            if (psBed != null)
                emBed.rateOverTime = p.bedRate * Mathf.Lerp(0.75f, 1.15f, heat);

            // il vento piega fiamma, scintille, braci e fumo con inerzie diverse
            Vector3 w = wind * p.windSensitivity;
            SetForce(fFlame, w * p.flameHeight * 9f);
            SetForce(fSmoke, w * 3.5f);
            if (psSpark != null) SetForce(fSpark, w * 4.5f);
            if (psEmber != null) SetForce(fEmber, w * 5.5f);
        }

        /// <summary>Scoppiettio: sputa un ventaglio di scintille.</summary>
        public void Pop(float power)
        {
            if (!built) return;
            if (psSpark == null || p.burstSparkCount <= 0) return;
            if (!psSpark.gameObject.activeInHierarchy) return;

            int n = Mathf.RoundToInt(p.burstSparkCount
                                     * Mathf.Clamp01(power)
                                     * Random.Range(0.6f, 1.4f));
            if (n > 0) psSpark.Emit(n);
        }

        /// <summary>Ferma l'emissione lasciando finire le particelle gia' vive.</summary>
        public void StopEmitting()
        {
            StopIf(psFlame); StopIf(psSmoke); StopIf(psSpark);
            StopIf(psEmber); StopIf(psBed);
        }

        /// <summary>Riprende l'emissione.</summary>
        public void Resume()
        {
            PlayIf(psFlame); PlayIf(psSmoke); PlayIf(psSpark);
            PlayIf(psEmber); PlayIf(psBed);
        }

        static void StopIf(ParticleSystem ps)
        {
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        static void PlayIf(ParticleSystem ps)
        {
            if (ps != null && !ps.isPlaying) ps.Play(true);
        }

        static void SetForce(ParticleSystem.ForceOverLifetimeModule m, Vector3 f)
        {
            m.x = new ParticleSystem.MinMaxCurve(f.x);
            m.y = new ParticleSystem.MinMaxCurve(f.y);
            m.z = new ParticleSystem.MinMaxCurve(f.z);
        }

        // --------------------------------------------------------------------
        //  HELPER
        // --------------------------------------------------------------------
        ParticleSystem NewPS(string psName)
        {
            GameObject go = new GameObject(psName);
            go.transform.SetParent(transform, false);

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.playOnAwake = true;
            main.loop = true;
            // FIX 7: Hierarchy moltiplicava la scala del transform per quella
            // gia' applicata al profilo, quindi scalare il GameObject in scena
            // scalava il fuoco DUE volte. Local ignora i genitori.
            main.scalingMode = ParticleSystemScalingMode.Local;
            return ps;
        }

        /// <summary>
        /// Costruisce una AnimationCurve da coppie tempo/valore in sequenza.
        /// Uso: Curve(0f, 0.3f, 0.5f, 1f, 1f, 0f)
        /// FIX 11: le tangenti ora sono automatiche (interpolazione morbida
        /// reale). SmoothTangents(i, 0f) le appiattiva tutte, producendo curve
        /// a gradini invece che fluide.
        /// </summary>
        static AnimationCurve Curve(params float[] pairs)
        {
            if (pairs == null || pairs.Length < 4)
                return AnimationCurve.Linear(0f, 1f, 1f, 1f);

            int n = pairs.Length / 2;
            Keyframe[] keys = new Keyframe[n];
            for (int i = 0; i < n; i++)
                keys[i] = new Keyframe(pairs[i * 2], pairs[i * 2 + 1]);

            AnimationCurve c = new AnimationCurve(keys);
            for (int i = 0; i < c.length; i++)
            {
#if UNITY_EDITOR
                UnityEditor.AnimationUtility.SetKeyLeftTangentMode(
                    c, i, UnityEditor.AnimationUtility.TangentMode.ClampedAuto);
                UnityEditor.AnimationUtility.SetKeyRightTangentMode(
                    c, i, UnityEditor.AnimationUtility.TangentMode.ClampedAuto);
#endif
                c.SmoothTangents(i, 1f);   // 1 = tangente auto, non piatta
            }
            return c;
        }

        static ParticleSystem.MinMaxGradient Grad(Color[] cols, float[] cTimes,
                                                  float[] alphas, float[] aTimes)
        {
            Gradient g = new Gradient();

            int nC = Mathf.Min(cols.Length, cTimes.Length, 8);
            GradientColorKey[] ck = new GradientColorKey[nC];
            for (int i = 0; i < nC; i++)
                ck[i] = new GradientColorKey(cols[i], Mathf.Clamp01(cTimes[i]));

            int nA = Mathf.Min(alphas.Length, aTimes.Length, 8);
            GradientAlphaKey[] ak = new GradientAlphaKey[nA];
            for (int i = 0; i < nA; i++)
                ak[i] = new GradientAlphaKey(Mathf.Clamp01(alphas[i]), Mathf.Clamp01(aTimes[i]));

            g.SetKeys(ck, ak);
            return new ParticleSystem.MinMaxGradient(g);
        }

        // --------------------------------------------------------------------
        //  MATERIALI
        // --------------------------------------------------------------------

        /// <summary>
        /// FIX 3 e 4: costruzione materiale corretta per Built-in e URP.
        ///
        /// ATTENZIONE: Shader.Find funziona in editor ma in BUILD lo shader
        /// viene strippato se nessun asset lo referenzia. Metti lo shader in
        /// Project Settings > Graphics > Always Included Shaders, oppure
        /// assegna i materiali a mano nell'ispettore di FireSource.
        ///
        /// Su URP non basta scrivere _Blend e _Surface: quei float li legge
        /// solo la GUI dell'inspector. Il rendering vero usa le keyword e i
        /// blend state, che qui impostiamo esplicitamente.
        /// </summary>
        static Material MakeMat(bool additive)
        {
            bool urp = false;
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s != null) urp = true;

            if (s == null) s = Shader.Find("Particles/Standard Unlit");
            if (s == null) s = Shader.Find("Legacy Shaders/Particles/Additive");
            if (s == null) s = Shader.Find("Mobile/Particles/Additive");
            if (s == null) s = Shader.Find("Sprites/Default");

            if (s == null)
            {
                Debug.LogError("[FireVFX] Nessuno shader particellare trovato. " +
                               "Le particelle saranno magenta. Assegna i materiali " +
                               "a mano nei campi 'Flame Material' e 'Smoke Material' " +
                               "di FireSource.");
                Shader err = Shader.Find("Hidden/InternalErrorShader");
                return new Material(err != null ? err : Shader.Find("Sprites/Default"));
            }

            Material m = new Material(s);
            m.name = additive ? "FireAdditive_Runtime" : "FireAlpha_Runtime";
            m.hideFlags = HideFlags.HideAndDontSave;

            Texture2D tex = SoftDot();

            // Built-in usa _MainTex, URP usa _BaseMap. Impostiamo entrambi
            // quando esistono: mainTexture da sola non basta su URP.
            if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", Color.white);
            if (m.HasProperty("_Color")) m.SetColor("_Color", Color.white);

            m.SetOverrideTag("RenderType", "Transparent");

            // ---- blend state esplicito, valido su tutte le pipeline ---------
            if (m.HasProperty("_SrcBlend"))
                m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend"))
                m.SetFloat("_DstBlend", (float)(additive
                    ? BlendMode.One
                    : BlendMode.OneMinusSrcAlpha));
            if (m.HasProperty("_ZWrite")) m.SetFloat("_ZWrite", 0f);
            if (m.HasProperty("_Cull")) m.SetFloat("_Cull", (float)CullMode.Off);

            if (urp)
            {
                // ---- URP: contano le KEYWORD, non i float della GUI ---------
                if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1f); // Transparent
                if (m.HasProperty("_Blend")) m.SetFloat("_Blend", additive ? 1f : 0f);

                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                m.DisableKeyword("_ALPHATEST_ON");

                if (additive)
                {
                    // su Particles Unlit l'additivo si ottiene modulando alpha
                    m.EnableKeyword("_ALPHAMODULATE_ON");
                    m.DisableKeyword("_ALPHABLEND_ON");
                }
                else
                {
                    m.DisableKeyword("_ALPHAMODULATE_ON");
                    m.EnableKeyword("_ALPHABLEND_ON");
                }
                m.SetShaderPassEnabled("ShadowCaster", false);
            }
            else
            {
                // ---- Built-in "Particles/Standard Unlit" --------------------
                // _Mode: 0 Opaque, 1 Cutout, 2 Fade, 3 Transparent, 4 Additive
                if (m.HasProperty("_Mode")) m.SetFloat("_Mode", additive ? 4f : 2f);

                if (additive)
                {
                    m.EnableKeyword("_ALPHAMODULATE_ON");
                    m.DisableKeyword("_ALPHABLEND_ON");
                }
                else
                {
                    m.DisableKeyword("_ALPHAMODULATE_ON");
                    m.EnableKeyword("_ALPHABLEND_ON");
                }
                m.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            m.renderQueue = (int)RenderQueue.Transparent;   // 3000
            return m;
        }

        /// <summary>Texture radiale morbida generata a runtime: niente asset.</summary>
        static Texture2D SoftDot()
        {
            if (_dot != null) return _dot;

            const int S = 64;
            Texture2D t = new Texture2D(S, S, TextureFormat.RGBA32, true, true);
            t.name = "FireSoftDot_Runtime";
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Bilinear;
            t.hideFlags = HideFlags.HideAndDontSave;

            Color[] px = new Color[S * S];
            for (int y = 0; y < S; y++)
            {
                for (int x = 0; x < S; x++)
                {
                    float dx = (x + 0.5f) / S - 0.5f;
                    float dy = (y + 0.5f) / S - 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                    float a = Mathf.Clamp01(1f - d);
                    a = a * a * a;                          // falloff morbido
                    px[y * S + x] = new Color(1f, 1f, 1f, a);
                }
            }

            t.SetPixels(px);
            t.Apply(true, true);   // makeNoLongerReadable: libera la copia CPU
            _dot = t;
            return _dot;
        }
    }
}
