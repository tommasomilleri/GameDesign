using UnityEngine;

namespace RealisticFire
{
    /// <summary>I tre tipi di fuoco supportati.</summary>
    public enum FireKind
    {
        Candle,     // fiammella pulita, laminare, ipersensibile all'aria
        Torch,      // pece o stoffa: ricca, nervosa, sputa scintille
        Fireplace   // legna: lenta, profonda, letto di brace
    }

    /// <summary>
    /// Tutti i parametri fisici ed estetici che distinguono i tre fuochi.
    /// Centralizzati qui, cosi' luce, VFX e audio restano coerenti tra loro.
    /// </summary>
    public struct FireProfile
    {
        // ---- scala fisica ----
        public float flameHeight;      // metri, altezza visibile della fiamma
        public float flameWidth;       // metri, larghezza alla base
        public float fuelRichness;     // 0..1, quanto e' sporca la combustione

        // ---- luce ----
        public float intensity, range;
        public float speed;            // frequenza base del rumore (Hz circa)
        public float flickerAmount;    // ampiezza variazione intensita' 0..1
        public float rangeAmount;      // ampiezza variazione raggio
        public float wobbleRadius;     // spostamento fisico della luce, metri
        public float gustChance;       // eventi al secondo (guizzi o cali)
        public float gustPower;        // ampiezza degli eventi
        public float saturationSwing;  // respiro cromatico
        public float windSensitivity;  // 0..1, quanto il vento la disturba
        public Color cEmber, cBody, cCore, cBase;
        public float baseBlueAmount;   // quanto affiora la base blu
        public float kelvinCold, kelvinHot;

        // ---- particelle ----
        public float flameRate;        // particelle al secondo, fiamma
        public float sparkRate;        // scintille continue al secondo
        public int burstSparkCount;  // scintille per scoppiettio
        public float emberRate;        // braci ascendenti al secondo
        public float smokeRate;
        public float smokeRise;        // metri al secondo circa
        public bool hasEmbersBed;     // letto di brace a terra, solo camino
        public float bedRate;          // FIX: rate del letto di brace, ora nel profilo

        // ---- audio ----
        public float loopVolume, loopPitch, crackleVolume;
        public float audioRange;

        public static FireProfile Get(FireKind k)
        {
            FireProfile p = new FireProfile();

            switch (k)
            {
                // ------------------------------------------------------------
                // CANDELA
                // Combustione pulita e laminare. Poca aria, poco fumo, NESSUNA
                // scintilla. Massima sensibilita' a spostamenti d'aria.
                // ------------------------------------------------------------
                case FireKind.Candle:
                    p.flameHeight = 0.045f;
                    p.flameWidth = 0.014f;
                    p.fuelRichness = 0.10f;

                    p.intensity = 0.9f;
                    p.range = 2.6f;
                    p.speed = 6.5f;
                    p.flickerAmount = 0.42f;
                    p.rangeAmount = 0.12f;
                    p.wobbleRadius = 0.010f;
                    p.gustChance = 0.30f;
                    p.gustPower = 0.60f;
                    p.saturationSwing = 0.30f;
                    p.windSensitivity = 1.00f;              // massima

                    p.cEmber = Hex("6E1A05");
                    p.cBody = Hex("FF6A14");
                    p.cCore = Hex("FFE1A0");
                    p.cBase = Hex("3C6BFF");               // base blu reale
                    p.baseBlueAmount = 0.10f;
                    p.kelvinCold = 1600f;
                    p.kelvinHot = 2100f;

                    p.flameRate = 26f;
                    p.sparkRate = 0f;                       // NIENTE scintille
                    p.burstSparkCount = 0;
                    p.emberRate = 0f;
                    p.smokeRate = 3f;
                    p.smokeRise = 0.22f;                    // filo sottile
                    p.hasEmbersBed = false;
                    p.bedRate = 0f;

                    p.loopVolume = 0.05f;
                    p.loopPitch = 1.60f;
                    p.crackleVolume = 0f;                   // non scoppietta
                    p.audioRange = 1.2f;
                    break;

                // ------------------------------------------------------------
                // TORCIA
                // Pece o stoffa impregnata: combustione ricca, nervosa, sputa
                // scintille, fuma parecchio, si piega vistosamente col vento.
                // ------------------------------------------------------------
                case FireKind.Torch:
                    p.flameHeight = 0.30f;
                    p.flameWidth = 0.11f;
                    p.fuelRichness = 0.65f;

                    p.intensity = 1.9f;
                    p.range = 6.5f;
                    p.speed = 4.2f;
                    p.flickerAmount = 0.30f;
                    p.rangeAmount = 0.18f;
                    p.wobbleRadius = 0.045f;
                    p.gustChance = 0.55f;
                    p.gustPower = 0.42f;
                    p.saturationSwing = 0.35f;
                    p.windSensitivity = 0.70f;

                    p.cEmber = Hex("7A1D02");
                    p.cBody = Hex("FF5A0F");
                    p.cCore = Hex("FFC46B");
                    p.cBase = Hex("2E5CFF");
                    p.baseBlueAmount = 0.05f;
                    p.kelvinCold = 1400f;
                    p.kelvinHot = 1950f;

                    p.flameRate = 60f;
                    p.sparkRate = 14f;
                    p.burstSparkCount = 8;
                    p.emberRate = 4f;
                    p.smokeRate = 14f;
                    p.smokeRise = 1.1f;
                    p.hasEmbersBed = false;
                    p.bedRate = 0f;

                    p.loopVolume = 0.35f;
                    p.loopPitch = 1.15f;
                    p.crackleVolume = 0.30f;
                    p.audioRange = 8f;
                    break;

                // ------------------------------------------------------------
                // CAMINO
                // Legna: lento, profondo, letto di brace pulsante, scoppiettii
                // forti, colonna di fumo ampia. Riparato dal vento.
                // ------------------------------------------------------------
                case FireKind.Fireplace:
                    p.flameHeight = 0.65f;
                    p.flameWidth = 0.45f;
                    p.fuelRichness = 0.85f;

                    p.intensity = 3.2f;
                    p.range = 11f;
                    p.speed = 1.8f;
                    p.flickerAmount = 0.20f;
                    p.rangeAmount = 0.10f;
                    p.wobbleRadius = 0.085f;
                    p.gustChance = 0.85f;
                    p.gustPower = 0.32f;
                    p.saturationSwing = 0.40f;
                    p.windSensitivity = 0.25f;              // protetto

                    p.cEmber = Hex("8A1F00");
                    p.cBody = Hex("F2530A");
                    p.cCore = Hex("FFB25C");
                    p.cBase = Hex("FF3B00");               // brace, non blu
                    p.baseBlueAmount = 0.0f;
                    p.kelvinCold = 1300f;
                    p.kelvinHot = 1900f;

                    p.flameRate = 110f;
                    p.sparkRate = 10f;
                    p.burstSparkCount = 22;
                    p.emberRate = 12f;
                    p.smokeRate = 24f;
                    p.smokeRise = 1.8f;
                    p.hasEmbersBed = true;                  // unico ad averlo
                    p.bedRate = 30f;

                    p.loopVolume = 0.55f;
                    p.loopPitch = 0.85f;
                    p.crackleVolume = 0.55f;
                    p.audioRange = 14f;
                    break;

                default:
                    goto case FireKind.Candle;
            }

            return p;
        }

        static Color Hex(string h)
        {
            Color c;
            if (!ColorUtility.TryParseHtmlString("#" + h, out c))
                c = Color.white;
            return c;
        }
    }
}
