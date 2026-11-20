using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace GuardiaoDoFarol.EditorTools
{
    /// <summary>
    /// Estilo visual pixel da interface: molduras 9-slice, botões, pergaminho, fundo do menu
    /// e a fonte Pixelify Sans (TextMeshPro). Usado pelo GuardiaoProjectBuilder.
    /// </summary>
    public static class PixelUIStyle
    {
        public const string UIDir = "Assets/_Project/Art/UI";
        public const string FontDir = "Assets/_Project/Fonts";
        public const string FontTtfPath = FontDir + "/PixelifySans.ttf";
        public const string FontAssetPath = FontDir + "/PixelifySans SDF.asset";
        public const string ShadowMaterialPath = FontDir + "/PixelifySans SDF - Sombra.mat";

        /// <summary>Quantos pixels de tela (na resolução 1080p) cada pixel da arte de UI ocupa.</summary>
        public const float UIPixelScale = 5f;

        public static Sprite Panel { get; private set; }
        public static Sprite Parchment { get; private set; }
        public static Sprite Button { get; private set; }
        public static Sprite ButtonHighlight { get; private set; }
        public static Sprite ButtonPressed { get; private set; }
        public static Sprite Slot { get; private set; }
        public static Sprite Coin { get; private set; }
        public static Sprite MenuBackground { get; private set; }
        public static TMP_FontAsset Font { get; private set; }
        public static Material ShadowMaterial { get; private set; }

        private static readonly Color K = new Color(0.10f, 0.06f, 0.05f);

        // =====================================================================

        public static void GenerateAll()
        {
            EnsureFolder(UIDir);

            SaveSliced(PanelTex(), "UI_Painel", 5);
            SaveSliced(ParchmentTex(), "UI_Pergaminho", 5);
            SaveSliced(ButtonTex(false, false), "UI_Botao", 5);
            SaveSliced(ButtonTex(true, false), "UI_Botao_Destaque", 5);
            SaveSliced(ButtonTex(false, true), "UI_Botao_Pressionado", 5);
            SaveSliced(SlotTex(), "UI_Slot", 4);
            SaveSliced(CoinTex(), "UI_Moeda", 0);
            SaveSliced(MenuBackgroundTex(), "UI_FundoMenu", 0);

            EnsureFontAsset();
            AssetDatabase.SaveAssets();
            Load();
        }

        /// <summary>Recarrega as referências do disco (necessário após EditorSceneManager.NewScene).</summary>
        public static void Load()
        {
            Panel = LoadSprite("UI_Painel");
            Parchment = LoadSprite("UI_Pergaminho");
            Button = LoadSprite("UI_Botao");
            ButtonHighlight = LoadSprite("UI_Botao_Destaque");
            ButtonPressed = LoadSprite("UI_Botao_Pressionado");
            Slot = LoadSprite("UI_Slot");
            Coin = LoadSprite("UI_Moeda");
            MenuBackground = LoadSprite("UI_FundoMenu");
            Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            ShadowMaterial = AssetDatabase.LoadAssetAtPath<Material>(ShadowMaterialPath);
        }

        private static Sprite LoadSprite(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(UIDir + "/" + name + ".png");
        }

        // =====================================================================
        // Fonte
        // =====================================================================

        private static void EnsureFontAsset()
        {
            Font ttf = AssetDatabase.LoadAssetAtPath<Font>(FontTtfPath);
            if (ttf == null)
            {
                Debug.LogWarning("PixelifySans.ttf não encontrado em " + FontDir + ". A fonte padrão do TMP será usada.");
                return;
            }

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                fontAsset = TMP_FontAsset.CreateFontAsset(ttf, 64, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, true);
                fontAsset.name = "PixelifySans SDF";
                AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

                Texture2D atlas = fontAsset.atlasTextures[0];
                atlas.name = "PixelifySans SDF Atlas";
                AssetDatabase.AddObjectToAsset(atlas, fontAsset);

                Material material = fontAsset.material;
                material.name = "PixelifySans SDF Material";
                AssetDatabase.AddObjectToAsset(material, fontAsset);

                // Garante acentos do português no atlas.
                fontAsset.TryAddCharacters(
                    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
                    "ÁÀÂÃÉÊÍÓÔÕÚÇáàâãéêíóôõúç.,;:!?¡¿'\"()[]{}<>+-*/=%$#@&_|\\~^°ºª•·…–— ", out _);

                EditorUtility.SetDirty(fontAsset);
                AssetDatabase.SaveAssets();
            }

            Material shadow = AssetDatabase.LoadAssetAtPath<Material>(ShadowMaterialPath);
            if (shadow == null)
            {
                shadow = new Material(fontAsset.material) { name = "PixelifySans SDF - Sombra" };
                AssetDatabase.CreateAsset(shadow, ShadowMaterialPath);
            }

            shadow.shader = fontAsset.material.shader;
            shadow.SetTexture(ShaderUtilities.ID_MainTex, fontAsset.atlasTextures[0]);
            shadow.EnableKeyword(ShaderUtilities.Keyword_Underlay);
            shadow.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0.05f, 0.03f, 0.03f, 1f));
            shadow.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.6f);
            shadow.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
            shadow.SetFloat(ShaderUtilities.ID_UnderlayDilate, 0.2f);
            shadow.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0f);
            EditorUtility.SetDirty(shadow);
        }

        // =====================================================================
        // Sprites de UI (16x16, 9-slice)
        // =====================================================================

        private static Texture2D PanelTex()
        {
            Texture2D t = New(16, 16);
            Color woodLight = new Color(0.66f, 0.43f, 0.22f);
            Color wood = new Color(0.50f, 0.31f, 0.15f);
            Color woodDark = new Color(0.33f, 0.20f, 0.10f);
            Color inner = new Color(0.17f, 0.11f, 0.08f, 1f);
            Color gold = new Color(0.98f, 0.78f, 0.32f);

            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(15 - x, 15 - y));
                    Color c;
                    if (d == 0) c = K;
                    else if (d == 1) c = (y == 1 || x == 1) ? woodLight : wood;
                    else if (d == 2) c = wood;
                    else if (d == 3) c = woodDark;
                    else if (d == 4) c = K;
                    else c = inner;
                    Put(t, x, y, c);
                }

            // cantos sem pixel externo (arredondado) e rebites dourados
            Put(t, 0, 0, Clear); Put(t, 15, 0, Clear); Put(t, 0, 15, Clear); Put(t, 15, 15, Clear);
            Put(t, 2, 2, gold); Put(t, 13, 2, gold); Put(t, 2, 13, gold); Put(t, 13, 13, gold);
            t.Apply();
            return t;
        }

        private static Texture2D ParchmentTex()
        {
            Texture2D t = New(16, 16);
            Color paper = new Color(0.93f, 0.85f, 0.66f);
            Color paperDark = new Color(0.80f, 0.68f, 0.46f);
            Color edge = new Color(0.55f, 0.40f, 0.24f);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(15 - x, 15 - y));
                    Color c = d == 0 ? K : d == 1 ? edge : d == 2 ? paperDark : paper;
                    Put(t, x, y, c);
                }
            Put(t, 0, 0, Clear); Put(t, 15, 0, Clear); Put(t, 0, 15, Clear); Put(t, 15, 15, Clear);
            Put(t, 1, 1, K); Put(t, 14, 1, K); Put(t, 1, 14, K); Put(t, 14, 14, K);
            t.Apply();
            return t;
        }

        private static Texture2D ButtonTex(bool highlight, bool pressed)
        {
            Texture2D t = New(16, 16);
            Color top = pressed ? new Color(0.36f, 0.22f, 0.10f) : new Color(0.78f, 0.53f, 0.27f);
            Color body = pressed ? new Color(0.45f, 0.28f, 0.13f) : new Color(0.60f, 0.38f, 0.18f);
            Color shadow = new Color(0.32f, 0.19f, 0.09f);
            Color frame = highlight ? new Color(1f, 0.84f, 0.35f) : K;
            Color glow = new Color(1f, 0.93f, 0.6f);

            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(15 - x, 15 - y));
                    Color c;
                    if (d == 0) c = K;
                    else if (d == 1 && highlight) c = frame;
                    else if (!pressed && (y >= 13)) c = shadow;
                    else if (pressed && y <= 2) c = shadow;
                    else if (!pressed && y <= 2) c = top;
                    else c = body;
                    if (highlight && d == 2 && y <= 3) c = glow;
                    Put(t, x, y, c);
                }
            Put(t, 0, 0, Clear); Put(t, 15, 0, Clear); Put(t, 0, 15, Clear); Put(t, 15, 15, Clear);
            t.Apply();
            return t;
        }

        private static Texture2D SlotTex()
        {
            Texture2D t = New(16, 16);
            Color dark = new Color(0.09f, 0.06f, 0.05f, 0.95f);
            Color rim = new Color(0.38f, 0.25f, 0.14f);
            Color light = new Color(0.62f, 0.44f, 0.25f);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    int d = Mathf.Min(Mathf.Min(x, y), Mathf.Min(15 - x, 15 - y));
                    Color c = d == 0 ? K : d == 1 ? ((y == 14 || x == 14) ? light : rim) : d == 2 ? K : dark;
                    Put(t, x, y, c);
                }
            Put(t, 0, 0, Clear); Put(t, 15, 0, Clear); Put(t, 0, 15, Clear); Put(t, 15, 15, Clear);
            t.Apply();
            return t;
        }

        private static Texture2D CoinTex()
        {
            string[] rows =
            {
                "...KKKK...",
                "..KYYYYK..",
                ".KYWYYYOK.",
                "KYWYYYYYOK",
                "KYYYOOYYOK",
                "KYYYOOYYOK",
                "KYYYYYYYOK",
                ".KYYYYYOK.",
                "..KOOOOK..",
                "...KKKK..."
            };
            Texture2D t = New(10, 10);
            for (int y = 0; y < 10; y++)
                for (int x = 0; x < 10; x++)
                {
                    char ch = rows[y][x];
                    Color c = ch == 'K' ? K : ch == 'Y' ? new Color(1f, 0.82f, 0.25f) : ch == 'O' ? new Color(0.85f, 0.55f, 0.12f) :
                        ch == 'W' ? new Color(1f, 0.97f, 0.8f) : Clear;
                    Put(t, x, y, c);
                }
            t.Apply();
            return t;
        }

        // =====================================================================
        // Fundo do menu (320x180): noite, mar, falésia, farol e taverna
        // =====================================================================

        private static Texture2D MenuBackgroundTex()
        {
            const int W = 320;
            const int H = 180;
            Texture2D t = New(W, H);

            Color[] sky =
            {
                new Color(0.04f, 0.05f, 0.13f), new Color(0.06f, 0.07f, 0.18f), new Color(0.09f, 0.09f, 0.23f),
                new Color(0.13f, 0.10f, 0.27f), new Color(0.19f, 0.12f, 0.30f), new Color(0.27f, 0.15f, 0.32f),
                new Color(0.36f, 0.19f, 0.32f)
            };

            const int horizon = 124;
            for (int y = 0; y < horizon; y++)
            {
                float f = y / (float)horizon * (sky.Length - 1);
                int band = Mathf.FloorToInt(f);
                float frac = f - band;
                for (int x = 0; x < W; x++)
                {
                    // dithering em xadrez na transição entre faixas
                    bool next = frac > 0.66f || (frac > 0.33f && ((x + y) & 1) == 0);
                    Color c = sky[Mathf.Min(sky.Length - 1, band + (next ? 1 : 0))];
                    Put(t, x, y, c);
                }
            }

            // estrelas
            for (int i = 0; i < 140; i++)
            {
                int x = Mathf.FloorToInt(Hash(i, 1) * W);
                int y = Mathf.FloorToInt(Hash(i, 2) * (horizon - 30));
                bool bright = Hash(i, 3) > 0.85f;
                Color star = bright ? new Color(1f, 0.97f, 0.85f) : new Color(0.75f, 0.78f, 0.95f);
                Put(t, x, y, star);
                if (bright)
                {
                    Put(t, x + 1, y, star * 0.6f); Put(t, x - 1, y, star * 0.6f);
                    Put(t, x, y + 1, star * 0.6f); Put(t, x, y - 1, star * 0.6f);
                }
            }

            // lua
            Vector2 moon = new Vector2(284f, 82f);
            for (int y = 66; y < 100; y++)
                for (int x = 266; x < 304; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), moon);
                    if (dist <= 11.5f)
                    {
                        Color m = new Color(0.98f, 0.94f, 0.78f);
                        if (Vector2.Distance(new Vector2(x, y), moon + new Vector2(-4, -2)) < 2.6f ||
                            Vector2.Distance(new Vector2(x, y), moon + new Vector2(3, 4)) < 2f ||
                            Vector2.Distance(new Vector2(x, y), moon + new Vector2(5, -5)) < 1.5f)
                            m = new Color(0.88f, 0.82f, 0.64f);
                        Put(t, x, y, m);
                    }
                    else if (dist <= 14f && ((x + y) & 1) == 0)
                    {
                        Blend(t, x, y, new Color(1f, 0.95f, 0.75f, 0.25f));
                    }
                }

            // mar
            Color[] sea = { new Color(0.10f, 0.16f, 0.33f), new Color(0.07f, 0.12f, 0.27f), new Color(0.05f, 0.09f, 0.21f) };
            for (int y = horizon; y < H; y++)
            {
                int band = Mathf.Min(sea.Length - 1, (y - horizon) / 18);
                for (int x = 0; x < W; x++)
                {
                    Color c = sea[band];
                    int phase = (x + y * 7) % 23;
                    if ((y - horizon) % 6 == 2 && phase < 5) c = new Color(0.28f, 0.38f, 0.62f);
                    // reflexo da lua
                    if (Mathf.Abs(x - 284) < 9 - (y - horizon) / 8 && (y - horizon) % 3 != 0 && Hash(x, y) > 0.35f)
                        c = new Color(0.95f, 0.88f, 0.65f);
                    Put(t, x, y, c);
                }
            }
            for (int x = 0; x < W; x++) Put(t, x, horizon, new Color(0.40f, 0.30f, 0.45f));

            // falésia à esquerda
            for (int x = 0; x < 150; x++)
            {
                float edge = 98f + 6f * Mathf.Sin(x * 0.08f) + 3f * Hash(x, 9) + Mathf.Max(0, x - 110) * 1.4f;
                int top = Mathf.RoundToInt(edge);
                for (int y = top; y < H; y++)
                {
                    Color c;
                    if (y == top) c = new Color(0.22f, 0.42f, 0.25f);
                    else if (y == top + 1) c = new Color(0.15f, 0.30f, 0.18f);
                    else
                    {
                        float n = Hash(x / 3, y / 3);
                        c = n > 0.8f ? new Color(0.20f, 0.18f, 0.24f) : new Color(0.13f, 0.12f, 0.17f);
                        if (x == 149 || (x > 110 && y == top + 2)) c = K;
                    }
                    Put(t, x, y, c);
                }
            }

            // feixe de luz do farol
            Vector2 lamp = new Vector2(52f, 46f);
            for (int y = 0; y < horizon; y++)
                for (int x = 58; x < W; x++)
                {
                    float dx = x - lamp.x;
                    float dy = y - lamp.y;
                    float spread = 0.10f + dx * 0.0008f;
                    if (Mathf.Abs(dy / dx) < spread && dx > 0)
                    {
                        float alpha = Mathf.Lerp(0.38f, 0.04f, dx / (W - lamp.x));
                        if (((x + y) & 1) == 0 || alpha > 0.25f) Blend(t, x, y, new Color(1f, 0.92f, 0.55f, alpha));
                    }
                }

            // farol
            int baseY = 100;
            for (int y = 50; y < baseY + 2; y++)
            {
                float k = (y - 50) / (float)(baseY - 50);
                int half = Mathf.RoundToInt(4f + 4f * k);
                for (int x = 52 - half; x <= 52 + half; x++)
                {
                    bool border = x == 52 - half || x == 52 + half;
                    Color stripe = ((y - 50) / 8) % 2 == 0 ? new Color(0.95f, 0.93f, 0.88f) : new Color(0.78f, 0.18f, 0.16f);
                    if (x > 52 + half - 3 && !border) stripe *= 0.8f;
                    Put(t, x, y, border ? K : Opaque(stripe));
                }
            }
            for (int y = 40; y < 50; y++)
                for (int x = 46; x <= 58; x++)
                {
                    bool border = x == 46 || x == 58 || y == 40 || y == 49;
                    Put(t, x, y, border ? K : (y < 43 ? new Color(0.78f, 0.18f, 0.16f) : new Color(1f, 0.88f, 0.40f)));
                }
            for (int y = 33; y < 40; y++)
                for (int x = 52 - (y - 33); x <= 52 + (y - 33); x++)
                    Put(t, x, y, (x == 52 - (y - 33) || x == 52 + (y - 33)) ? K : new Color(0.62f, 0.14f, 0.12f));
            for (int x = 44; x <= 60; x++) Put(t, x, 50, K);
            Put(t, 51, 92, K); Put(t, 52, 92, new Color(1f, 0.85f, 0.4f)); Put(t, 53, 92, K);

            // taverna
            int hx = 76, hy = 84, hw = 34, hh = 18;
            for (int y = hy; y < hy + hh; y++)
                for (int x = hx; x < hx + hw; x++)
                {
                    bool border = x == hx || x == hx + hw - 1 || y == hy + hh - 1;
                    Color wall = ((y - hy) % 4 == 3) ? new Color(0.35f, 0.22f, 0.13f) : new Color(0.48f, 0.31f, 0.17f);
                    Put(t, x, y, border ? K : wall);
                }
            for (int i = 0; i < 12; i++)
                for (int x = hx - 3 + i; x <= hx + hw + 2 - i; x++)
                {
                    int y = hy - i;
                    bool border = x == hx - 3 + i || x == hx + hw + 2 - i || i == 0;
                    Put(t, x, y, border ? K : (i % 3 == 0 ? new Color(0.45f, 0.13f, 0.12f) : new Color(0.60f, 0.18f, 0.15f)));
                }
            DrawWindow(t, hx + 5, hy + 5);
            DrawWindow(t, hx + 24, hy + 5);
            for (int y = hy + 8; y < hy + hh - 1; y++)
                for (int x = hx + 14; x < hx + 20; x++)
                    Put(t, x, y, (x == hx + 14 || x == hx + 19 || y == hy + 8) ? K : new Color(0.30f, 0.18f, 0.09f));
            // placa
            for (int x = hx + 12; x < hx + 22; x++) { Put(t, x, hy + 2, K); Put(t, x, hy + 5, K); }
            for (int x = hx + 13; x < hx + 21; x++) { Put(t, x, hy + 3, new Color(0.85f, 0.65f, 0.3f)); Put(t, x, hy + 4, new Color(0.85f, 0.65f, 0.3f)); }
            Put(t, hx + 12, hy + 3, K); Put(t, hx + 12, hy + 4, K); Put(t, hx + 21, hy + 3, K); Put(t, hx + 21, hy + 4, K);
            // fumaça da chaminé
            for (int y = hy - 16; y < hy - 8; y++)
                for (int x = hx + 26; x < hx + 30; x++)
                    Put(t, x, y, (x == hx + 26 || x == hx + 29) ? K : new Color(0.35f, 0.33f, 0.36f));
            for (int i = 0; i < 6; i++)
            {
                int sx = hx + 27 + (i % 2 == 0 ? 0 : 2) + i;
                int sy = hy - 18 - i * 3;
                Blend(t, sx, sy, new Color(0.7f, 0.7f, 0.78f, 0.5f));
                Blend(t, sx + 1, sy, new Color(0.7f, 0.7f, 0.78f, 0.35f));
            }

            t.Apply();
            return t;
        }

        private static void DrawWindow(Texture2D t, int x0, int y0)
        {
            for (int y = y0; y < y0 + 6; y++)
                for (int x = x0; x < x0 + 6; x++)
                {
                    bool border = x == x0 || x == x0 + 5 || y == y0 || y == y0 + 5;
                    bool cross = x == x0 + 2 || y == y0 + 2;
                    Put(t, x, y, border ? K : cross ? new Color(0.5f, 0.3f, 0.12f) : new Color(1f, 0.82f, 0.38f));
                }
            for (int y = y0 + 6; y < y0 + 10; y++)
                for (int x = x0 - 1; x < x0 + 7; x++)
                    if (((x + y) & 1) == 0) Blend(t, x, y, new Color(1f, 0.8f, 0.3f, 0.25f));
        }

        // =====================================================================
        // Utilitários
        // =====================================================================

        private static readonly Color Clear = new Color(0f, 0f, 0f, 0f);

        private static Texture2D New(int w, int h)
        {
            Texture2D t = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Clear;
            t.SetPixels(pixels);
            return t;
        }

        private static void Put(Texture2D t, int x, int yTop, Color c)
        {
            if (x < 0 || yTop < 0 || x >= t.width || yTop >= t.height) return;
            t.SetPixel(x, t.height - 1 - yTop, c);
        }

        private static void Blend(Texture2D t, int x, int yTop, Color c)
        {
            if (x < 0 || yTop < 0 || x >= t.width || yTop >= t.height) return;
            Color under = t.GetPixel(x, t.height - 1 - yTop);
            Color result = Color.Lerp(under, new Color(c.r, c.g, c.b, 1f), c.a);
            result.a = Mathf.Max(under.a, c.a);
            t.SetPixel(x, t.height - 1 - yTop, result);
        }

        private static Color Opaque(Color c)
        {
            c.a = 1f;
            return c;
        }

        private static float Hash(int x, int y)
        {
            float n = Mathf.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
            return n - Mathf.Floor(n);
        }

        private static void SaveSliced(Texture2D tex, string name, int border)
        {
            string path = UIDir + "/" + name + ".png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string path)
        {
            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
