using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace GuardiaoDoFarol.EditorTools
{
    /// <summary>
    /// Gera a pixel art do jogo (16 PPU) como arquivos PNG em Assets/_Project/Art/Generated
    /// e aplica nas cenas, no prefab de cliente, nos tiles, na campanha e nos ícones.
    /// Menu: Guardiao do Farol > Gerar e Aplicar Pixel Art.
    /// Para usar sua própria arte depois, basta substituir os PNGs (mesmo nome) ou trocar os sprites no Inspector.
    /// </summary>
    public static class PixelArtLibrary
    {
        private const string ArtDir = "Assets/_Project/Art/Generated";
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string MenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";
        private const string CustomerPrefabPath = "Assets/_Project/Prefabs/Customer.prefab";
        private const string CampaignPath = "Assets/_Project/Data/CampanhaGuardiao.asset";
        private const string TilesDir = "Assets/_Project/Art/Tiles";

        private static readonly Color Outline = C(0.13f, 0.09f, 0.08f);
        private static readonly Color Eye = C(0.08f, 0.08f, 0.12f);
        private static readonly Color Skin = C(0.95f, 0.76f, 0.60f);
        private static readonly Color Boots = C(0.25f, 0.15f, 0.08f);

        [MenuItem("Guardiao do Farol/Gerar e Aplicar Pixel Art")]
        public static void GenerateAndApply()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            GenerateAll();
            ApplyToProject();
            Debug.Log("O Guardião do Farol: pixel art gerada e aplicada.");
        }

        // =====================================================================
        // Geração
        // =====================================================================

        public static void GenerateAll()
        {
            EnsureFolder(ArtDir);

            Save(FloorTile(0, 1f), "Tile_Madeira_A", new Vector2(0.5f, 0.5f));
            Save(FloorTile(7, 0.94f), "Tile_Madeira_B", new Vector2(0.5f, 0.5f));
            Save(WallTile(), "Tile_Pedra", new Vector2(0.5f, 0.5f));

            Save(Table(), "Mesa", new Vector2(0.5f, 0.5f));
            Save(Chair(), "Cadeira", new Vector2(0.5f, 0.5f));
            Save(Counter(), "Balcao", new Vector2(0.5f, 0.5f));
            Save(Barrel(), "Barril", new Vector2(0.5f, 0.5f));
            Save(Stove(), "Fogao", new Vector2(0.5f, 0.5f));
            Save(Door(), "Porta", new Vector2(0.5f, 0.5f));
            Save(Lighthouse(), "Farol", new Vector2(0.5f, 0.5f));

            Save(FromRows(BeerRows, new Dictionary<char, Color>
            {
                { 'K', Outline }, { 'W', C(0.98f, 0.97f, 0.92f) }, { 'Y', C(0.95f, 0.70f, 0.18f) },
                { 'A', C(1f, 0.88f, 0.45f) }
            }), "Icone_Cerveja", new Vector2(0.5f, 0.5f));
            Save(FromRows(MeatRows, new Dictionary<char, Color>
            {
                { 'K', Outline }, { 'R', C(0.72f, 0.28f, 0.18f) }, { 'r', C(0.88f, 0.45f, 0.30f) },
                { 'W', C(0.96f, 0.93f, 0.85f) }
            }), "Icone_Carne", new Vector2(0.5f, 0.5f));
            Save(FromRows(SoupRows, new Dictionary<char, Color>
            {
                { 'K', Outline }, { 's', C(0.9f, 0.9f, 0.95f, 0.8f) }, { 'G', C(0.45f, 0.72f, 0.28f) },
                { 'g', C(0.65f, 0.88f, 0.40f) }, { 'B', C(0.55f, 0.35f, 0.18f) }
            }), "Icone_Sopa", new Vector2(0.5f, 0.5f));
            Save(FromRows(BubbleRows, new Dictionary<char, Color>
            {
                { 'K', Outline }, { 'W', C(0.98f, 0.97f, 0.93f) }
            }), "Balao", new Vector2(0.5f, 0.5f));

            Vector2 feet = new Vector2(0.5f, 0f);
            SaveWalker(HeadBasic, Pal(C(0.40f, 0.25f, 0.12f), Skin, C(0.25f, 0.42f, 0.72f), C(0.95f, 0.92f, 0.84f), C(0.30f, 0.20f, 0.12f)), "Taverneiro", feet);
            SaveWalker(HeadBasic, Pal(C(0.55f, 0.45f, 0.35f), Skin, C(0.55f, 0.52f, 0.48f), C(0.70f, 0.62f, 0.45f), C(0.30f, 0.25f, 0.20f)), "Heroi_Generico", feet);
            SaveWalker(HeadWarrior, Pal(C(0.60f, 0.62f, 0.66f), Skin, C(0.68f, 0.20f, 0.18f), C(0.72f, 0.72f, 0.78f), C(0.35f, 0.22f, 0.15f),
                C(0.96f, 0.92f, 0.80f), C(0.72f, 0.32f, 0.15f)), "Heroi_Borin", feet);
            SaveWalker(HeadMage, Pal(C(0.42f, 0.24f, 0.68f), Skin, C(0.50f, 0.30f, 0.78f), C(0.98f, 0.85f, 0.30f), C(0.28f, 0.17f, 0.48f),
                C(1f, 1f, 1f), C(0.90f, 0.90f, 0.95f)), "Heroi_Lyra", feet);
            SaveWalker(HeadHood, Pal(C(0.22f, 0.52f, 0.24f), Skin, C(0.34f, 0.60f, 0.30f), C(0.58f, 0.40f, 0.20f), C(0.30f, 0.22f, 0.12f)), "Heroi_Tavio", feet);
            SaveWalker(HeadKnight, Pal(C(0.80f, 0.82f, 0.86f), Skin, C(0.84f, 0.86f, 0.90f), C(0.95f, 0.78f, 0.25f), C(0.50f, 0.42f, 0.22f)), "Heroi_Aldric", feet);
            SaveWalker(HeadRogue, Pal(C(0.16f, 0.36f, 0.42f), Skin, C(0.20f, 0.25f, 0.30f), C(0.62f, 0.15f, 0.20f), C(0.12f, 0.12f, 0.15f)), "Heroi_Mirela", feet);
            SaveWalker(HeadBarbarian, Pal(C(0.88f, 0.45f, 0.15f), C(0.85f, 0.62f, 0.45f), C(0.55f, 0.38f, 0.22f), C(0.78f, 0.62f, 0.40f), C(0.35f, 0.22f, 0.12f)), "Heroi_Grom", feet);
            SaveWalker(HeadCleric, Pal(C(0.97f, 0.97f, 0.98f), Skin, C(0.92f, 0.92f, 0.96f), C(0.95f, 0.80f, 0.30f), C(0.68f, 0.68f, 0.80f)), "Heroi_Celestia", feet);

            AssetDatabase.Refresh();
        }

        // =====================================================================
        // Aplicação no projeto
        // =====================================================================

        public static void ApplyToProject()
        {
            Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + name + ".png");

            // Tiles
            SetTile("Tile_PisoA", Load("Tile_Madeira_A"));
            SetTile("Tile_PisoB", Load("Tile_Madeira_B"));
            SetTile("Tile_Parede", Load("Tile_Pedra"));

            // Campanha: um sprite por herói
            CampaignData campaign = AssetDatabase.LoadAssetAtPath<CampaignData>(CampaignPath);
            if (campaign != null)
            {
                foreach (DayConfig day in campaign.days)
                {
                    foreach (CustomerProfile hero in day.customers)
                    {
                        Sprite sprite = HeroSprite(hero.heroName, Load);
                        if (sprite == null) continue;
                        Sprite step1 = Load(sprite.name + "_Passo1");
                        Sprite step2 = Load(sprite.name + "_Passo2");
                        hero.walkFrames = step1 != null && step2 != null
                            ? new[] { step1, sprite, step2, sprite }
                            : new Sprite[0];
                        hero.sprite = sprite;
                        hero.tint = Color.white;
                    }
                }
                EditorUtility.SetDirty(campaign);
            }

            // Prefab de cliente
            GameObject customer = PrefabUtility.LoadPrefabContents(CustomerPrefabPath);
            SetVisual(customer.transform.Find("Body"), Load("Heroi_Generico"), Vector3.zero, true);
            Transform bubble = customer.transform.Find("OrderBubble");
            if (bubble != null)
            {
                bubble.localPosition = new Vector3(0f, 2.15f, 0f);
                SetVisual(bubble.Find("BubbleBackground"), Load("Balao"), Vector3.zero, false);
                Transform icon = bubble.Find("OrderIcon");
                if (icon != null)
                {
                    icon.localPosition = new Vector3(0f, 0.1f, 0f);
                    icon.localScale = new Vector3(0.8f, 0.8f, 1f);
                    SpriteRenderer iconRenderer = icon.GetComponent<SpriteRenderer>();
                    iconRenderer.sprite = Load("Icone_Cerveja");
                    iconRenderer.color = Color.white;
                }
            }
            Transform bar = customer.transform.Find("PatienceBar");
            if (bar != null) bar.localPosition = new Vector3(0f, 1.65f, 0f);
            PrefabUtility.SaveAsPrefabAsset(customer, CustomerPrefabPath);
            PrefabUtility.UnloadPrefabContents(customer);

            // Cena do jogo
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            OrderManager orders = Object.FindAnyObjectByType<OrderManager>();
            if (orders != null)
            {
                SerializedObject so = new SerializedObject(orders);
                SerializedProperty list = so.FindProperty("products");
                for (int i = 0; i < list.arraySize; i++)
                {
                    SerializedProperty element = list.GetArrayElementAtIndex(i);
                    ProductType type = (ProductType)element.FindPropertyRelative("type").intValue;
                    string iconName = type == ProductType.Carne ? "Icone_Carne" : type == ProductType.Sopa ? "Icone_Sopa" : "Icone_Cerveja";
                    element.FindPropertyRelative("icon").objectReferenceValue = Load(iconName);
                    element.FindPropertyRelative("iconColor").colorValue = Color.white;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            GameObject player = GameObject.Find("Taverneiro");
            if (player != null)
            {
                SetVisual(player.transform.Find("Body"), Load("Taverneiro"), Vector3.zero, true);
                Transform apron = player.transform.Find("Avental");
                if (apron != null) Object.DestroyImmediate(apron.gameObject);
                Transform carried = player.transform.Find("ItemCarregado");
                if (carried != null)
                {
                    carried.localPosition = new Vector3(0f, 1.85f, 0f);
                    carried.localScale = new Vector3(0.9f, 0.9f, 1f);
                }
            }

            foreach (Table table in Object.FindObjectsByType<Table>())
            {
                SetVisual(table.transform.Find("Visual"), Load("Mesa"), Vector3.zero, false);
                Transform mug = table.transform.Find("Caneca");
                if (mug != null) Object.DestroyImmediate(mug.gameObject);
                Transform chair = table.transform.Find("Cadeira");
                if (chair != null) SetVisual(chair, Load("Cadeira"), chair.localPosition, false);
                Transform highlight = table.transform.Find("Destaque");
                if (highlight != null) highlight.localScale = new Vector3(2.2f, 1.5f, 1f);
            }

            ReplaceProp("Cenario/Balcao", "Tampo", Load("Balcao"));
            ReplaceProp("Cenario/Fogao", "Brasa", Load("Fogao"));
            ReplaceProp("Cenario/Barril", null, Load("Barril"));
            ReplaceProp("Cenario/Porta", null, Load("Porta"));

            foreach (ProductStation station in Object.FindObjectsByType<ProductStation>())
            {
                Transform icon = station.transform.Find("IconeProduto");
                if (icon == null) continue;
                string iconName = station.Product == ProductType.Carne ? "Icone_Carne" : station.Product == ProductType.Sopa ? "Icone_Sopa" : "Icone_Cerveja";
                SpriteRenderer renderer = icon.GetComponent<SpriteRenderer>();
                renderer.sprite = Load(iconName);
                renderer.color = Color.white;
                icon.localScale = new Vector3(0.8f, 0.8f, 1f);
                icon.localPosition = new Vector3(0f, 0.95f, 0f);
            }

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            // Menu: farol no lugar do círculo
            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            GameObject lighthouse = GameObject.Find("Canvas/Farol");
            if (lighthouse != null)
            {
                Image image = lighthouse.GetComponent<Image>();
                image.sprite = Load("Farol");
                image.color = Color.white;
                image.preserveAspect = true;
                RectTransform rt = (RectTransform)lighthouse.transform;
                rt.sizeDelta = new Vector2(150f, 300f);
                rt.anchoredPosition = new Vector2(-720f, 120f);
            }
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            AssetDatabase.SaveAssets();
        }

        private static Sprite HeroSprite(string heroName, System.Func<string, Sprite> load)
        {
            if (string.IsNullOrEmpty(heroName)) return load("Heroi_Generico");
            string[] keys = { "Borin", "Lyra", "Tavio", "Aldric", "Mirela", "Grom", "Celestia" };
            foreach (string key in keys)
            {
                if (heroName.Contains(key)) return load("Heroi_" + key);
            }
            return load("Heroi_Generico");
        }

        private static void SetTile(string tileName, Sprite sprite)
        {
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(TilesDir + "/" + tileName + ".asset");
            if (tile == null || sprite == null) return;
            tile.sprite = sprite;
            tile.color = Color.white;
            EditorUtility.SetDirty(tile);
        }

        private static void SetVisual(Transform target, Sprite sprite, Vector3 localPosition, bool sortByPivot)
        {
            if (target == null || sprite == null) return;
            target.localPosition = localPosition;
            target.localScale = Vector3.one;
            SpriteRenderer renderer = target.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            if (sortByPivot) renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        }

        private static void ReplaceProp(string path, string childToRemove, Sprite sprite)
        {
            GameObject prop = GameObject.Find(path);
            if (prop == null) return;
            SetVisual(prop.transform.Find("Visual"), sprite, Vector3.zero, false);
            if (!string.IsNullOrEmpty(childToRemove))
            {
                Transform extra = prop.transform.Find(childToRemove);
                if (extra != null) Object.DestroyImmediate(extra.gameObject);
            }
        }

        // =====================================================================
        // Personagens (16x24, pivô nos pés)
        // =====================================================================

        private static Dictionary<char, Color> Pal(Color head, Color skin, Color outfit, Color accent, Color dark,
            Color? extra1 = null, Color? extra2 = null)
        {
            Dictionary<char, Color> palette = new Dictionary<char, Color>
            {
                { 'K', Outline }, { 'E', Eye }, { 'H', head }, { 'S', skin }, { 'O', outfit },
                { 'A', accent }, { 'D', dark }, { 'B', Boots }, { 'M', C(0.65f, 0.30f, 0.30f) },
                { 'W', extra1 ?? accent }, { 'R', extra2 ?? head }
            };
            return palette;
        }

        private static Texture2D Character(string[] head, Dictionary<char, Color> palette, string[] legs = null)
        {
            string[] rows = new string[24];
            for (int i = 0; i < 10; i++) rows[i] = head[i];
            for (int i = 0; i < 14; i++) rows[10 + i] = Body[i];
            if (legs != null)
            {
                for (int i = 0; i < legs.Length; i++) rows[24 - legs.Length + i] = legs[i];
            }
            return FromRows(rows, palette);
        }

        /// <summary>Salva o personagem parado e dois quadros de passo (Nome_Passo1, Nome_Passo2).</summary>
        private static void SaveWalker(string[] head, Dictionary<char, Color> palette, string name, Vector2 pivot)
        {
            Save(Character(head, palette), name, pivot);
            Save(Character(head, palette, StepLeft), name + "_Passo1", pivot);
            Save(Character(head, palette, StepRight), name + "_Passo2", pivot);
        }

        // Pernas alternando (substituem as 6 últimas linhas do corpo).
        private static readonly string[] StepLeft =
        {
            "....KDDKKDDK....",
            "....KDDKKDDK....",
            "....KBBKKDDK....",
            "...KBBBKKDDK....",
            "...KKKKKKBBK....",
            ".........KKK...."
        };

        private static readonly string[] StepRight =
        {
            "....KDDKKDDK....",
            "....KDDKKDDK....",
            "....KDDKKBBK....",
            "....KDDKKBBBK...",
            "....KBBKKKKKK...",
            "....KKK........."
        };

        private static readonly string[] Body =
        {
            "....KOOOOOOK....",
            "...KOOOAAOOOK...",
            "..KOOOOAAOOOOK..",
            "..KSKOOAAOOKSK..",
            "..KSKOOAAOOKSK..",
            "..KKKDDDDDDKKK..",
            "....KOOAAOOK....",
            "....KOOOOOOK....",
            "....KDDKKDDK....",
            "....KDDKKDDK....",
            "....KDDKKDDK....",
            "....KBBKKBBK....",
            "...KBBBKKBBBK...",
            "...KKKKKKKKKK..."
        };

        private static readonly string[] HeadBasic =
        {
            "................",
            "................",
            ".....KKKKKK.....",
            "....KHHHHHHK....",
            "...KHHHHHHHHK...",
            "...KHSSSSSSHK...",
            "...KSSESSESSK...",
            "...KSSSSSSSSK...",
            "....KSSMMSSK....",
            ".....KKSSKK....."
        };

        private static readonly string[] HeadWarrior =
        {
            "................",
            "..KW........WK..",
            "..KWKKKKKKKKWK..",
            "...KHHHHHHHHK...",
            "...KHHHHHHHHK...",
            "...KHSSSSSSHK...",
            "...KSSESSESSK...",
            "...KRRSSSSRRK...",
            "....KRRRRRRK....",
            ".....KRRRRK....."
        };

        private static readonly string[] HeadMage =
        {
            ".......KK.......",
            "......KHHK......",
            ".....KHHWHK.....",
            "....KHHHHHHK....",
            "..KKHHHHHHHHKK..",
            "...KRSSSSSSRK...",
            "...KRSESSESRK...",
            "...KRSSSSSSRK...",
            "...KRKSSSSKRK...",
            "...KR.KSSK.RK..."
        };

        private static readonly string[] HeadHood =
        {
            "................",
            "......KKKK......",
            ".....KHHHHK.....",
            "....KHHHHHHK....",
            "...KHHHHHHHHK...",
            "...KHHSSSSHHK...",
            "...KHSESSESHK...",
            "...KHSSSSSSHK...",
            "....KHSSSSHK....",
            ".....KKSSKK....."
        };

        private static readonly string[] HeadKnight =
        {
            "................",
            "......KKKK......",
            ".....KAAAAK.....",
            "....KHHHHHHK....",
            "...KHHHHHHHHK...",
            "...KHHHHHHHHK...",
            "...KHKKKKKKHK...",
            "...KHHHAHHHHK...",
            "....KHHHHHHK....",
            ".....KKHHKK....."
        };

        private static readonly string[] HeadRogue =
        {
            "................",
            "......KKKK......",
            ".....KHHHHK.....",
            "....KHHHHHHK....",
            "...KHHHHHHHHK...",
            "...KHSSSSSSHK...",
            "...KHSESSESHK...",
            "...KHKKKKKKHK...",
            "....KHKKKKHK....",
            ".....KKKKKK....."
        };

        private static readonly string[] HeadBarbarian =
        {
            "...K.K..K.K.....",
            "..KHKHKKHKHK....",
            "..KHHHHHHHHHK...",
            "..KHHHHHHHHHHK..",
            "..KHHSSSSSSHHK..",
            "..KHSSSSSSSSHK..",
            "...KSKESSEKSK...",
            "...KSSSSSSSSK...",
            "....KSKKKKSK....",
            ".....KKSSKK....."
        };

        private static readonly string[] HeadCleric =
        {
            "................",
            "......KKKK......",
            ".....KHHHHK.....",
            "....KHAAAAHK....",
            "...KHHHHHHHHK...",
            "...KHSSSSSSHK...",
            "...KHSESSESHK...",
            "...KHSSSSSSHK...",
            "...KHHSSSSHHK...",
            "...KHHKSSKHHK..."
        };

        // =====================================================================
        // Ícones (12x12) e balão (16x16)
        // =====================================================================

        private static readonly string[] BeerRows =
        {
            "............",
            "..WWWWWW....",
            ".WWWWWWWW...",
            ".KYYYYYYK...",
            ".KYYYYYYKKK.",
            ".KYAYYYYK.K.",
            ".KYAYYYYK.K.",
            ".KYAYYYYKKK.",
            ".KYYYYYYK...",
            ".KYYYYYYK...",
            "..KKKKKK....",
            "............"
        };

        private static readonly string[] MeatRows =
        {
            "............",
            "....KKKK....",
            "...KRRRRK...",
            "..KRRrRRRK..",
            "..KRRRRRRK..",
            "..KRRRrRRK..",
            "...KRRRRK...",
            "....KKRK....",
            ".....KWK....",
            "....KWWWK...",
            "....KWKWK...",
            ".....K.K...."
        };

        private static readonly string[] SoupRows =
        {
            "............",
            "....s..s....",
            ".....s..s...",
            "....s..s....",
            "............",
            ".KKKKKKKKKK.",
            ".KGGGgGGGGK.",
            ".KBBBBBBBBK.",
            "..KBBBBBBK..",
            "...KBBBBK...",
            "....KKKK....",
            "............"
        };

        private static readonly string[] BubbleRows =
        {
            "....KKKKKKKK....",
            "..KKWWWWWWWWKK..",
            ".KWWWWWWWWWWWWK.",
            ".KWWWWWWWWWWWWK.",
            "KWWWWWWWWWWWWWWK",
            "KWWWWWWWWWWWWWWK",
            "KWWWWWWWWWWWWWWK",
            "KWWWWWWWWWWWWWWK",
            "KWWWWWWWWWWWWWWK",
            "KWWWWWWWWWWWWWWK",
            ".KWWWWWWWWWWWWK.",
            ".KWWWWWWWWWWWWK.",
            "..KKWWWWWWWWKK..",
            "....KKKKWWKKK...",
            "........KWK.....",
            ".........K......"
        };

        // =====================================================================
        // Cenário procedural
        // =====================================================================

        private static Texture2D FloorTile(int seed, float brightness)
        {
            Texture2D tex = NewTex(16, 16);
            Color wood = C(0.56f, 0.38f, 0.23f) * brightness;
            Color seam = C(0.30f, 0.19f, 0.11f);
            for (int y = 0; y < 16; y++)
            {
                int plank = y / 4;
                int joint = (plank * 7 + 5 + seed) % 16;
                for (int x = 0; x < 16; x++)
                {
                    Color c;
                    if (y % 4 == 3 || x == joint) c = seam;
                    else
                    {
                        float shade = 0.9f + 0.1f * Noise(plank, seed, 3);
                        if (Noise(x, y, seed + 11) > 0.86f) shade -= 0.08f;
                        if (y % 4 == 0) shade += 0.06f;
                        c = wood * shade;
                    }
                    Put(tex, x, y, Opaque(c));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D WallTile()
        {
            Texture2D tex = NewTex(16, 16);
            Color mortar = C(0.17f, 0.15f, 0.16f);
            Color stone = C(0.42f, 0.39f, 0.41f);
            for (int y = 0; y < 16; y++)
            {
                int row = y / 4;
                int offset = (row % 2) * 4;
                for (int x = 0; x < 16; x++)
                {
                    bool isMortar = y % 4 == 3 || (x + offset) % 8 == 7;
                    Color c;
                    if (isMortar) c = mortar;
                    else
                    {
                        int brick = (x + offset) / 8 + row * 3;
                        float shade = 0.85f + 0.2f * Noise(brick, 1, 5);
                        if (y % 4 == 0) shade += 0.12f;
                        if (Noise(x, y, 9) > 0.9f) shade -= 0.1f;
                        c = stone * shade;
                    }
                    Put(tex, x, y, Opaque(c));
                }
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D Table()
        {
            Texture2D tex = NewTex(32, 20);
            Color top = C(0.62f, 0.42f, 0.24f);
            Color edge = C(0.40f, 0.25f, 0.13f);
            Rect(tex, 0, 0, 32, 16, Outline);
            for (int y = 1; y < 15; y++)
            {
                for (int x = 1; x < 31; x++)
                {
                    float shade = (y % 5 == 0) ? 0.82f : 1f - 0.06f * Noise(x / 6, y / 5, 2);
                    Put(tex, x, y, Opaque(top * shade));
                }
            }
            for (int x = 1; x < 31; x++) { Put(tex, x, 13, edge); Put(tex, x, 14, edge); }
            Rect(tex, 2, 16, 3, 4, Outline);
            Rect(tex, 27, 16, 3, 4, Outline);
            Put(tex, 3, 16, edge); Put(tex, 3, 17, edge); Put(tex, 3, 18, edge);
            Put(tex, 28, 16, edge); Put(tex, 28, 17, edge); Put(tex, 28, 18, edge);
            // caneca e prato decorativos
            Rect(tex, 21, 3, 5, 5, Outline);
            Fill(tex, 22, 4, 3, 3, C(0.85f, 0.82f, 0.75f));
            Rect(tex, 6, 4, 7, 5, Outline);
            Fill(tex, 7, 5, 5, 3, C(0.92f, 0.90f, 0.85f));
            tex.Apply();
            return tex;
        }

        private static Texture2D Chair()
        {
            Texture2D tex = NewTex(12, 12);
            Color wood = C(0.48f, 0.30f, 0.16f);
            Rect(tex, 0, 0, 12, 4, Outline);
            Fill(tex, 1, 1, 10, 2, wood * 0.85f);
            Rect(tex, 1, 3, 10, 6, Outline);
            Fill(tex, 2, 4, 8, 4, wood);
            Rect(tex, 1, 9, 2, 3, Outline);
            Rect(tex, 9, 9, 2, 3, Outline);
            tex.Apply();
            return tex;
        }

        private static Texture2D Counter()
        {
            Texture2D tex = NewTex(96, 20);
            Color top = C(0.64f, 0.44f, 0.25f);
            Color front = C(0.36f, 0.22f, 0.11f);
            Rect(tex, 0, 0, 96, 20, Outline);
            for (int y = 1; y < 7; y++)
                for (int x = 1; x < 95; x++)
                    Put(tex, x, y, Opaque(top * (y == 1 ? 1.1f : 1f - 0.05f * Noise(x / 8, y, 4))));
            for (int x = 1; x < 95; x++) Put(tex, x, 7, Outline);
            for (int y = 8; y < 19; y++)
                for (int x = 1; x < 95; x++)
                {
                    bool panelLine = x % 12 == 0 || y == 8 || y == 18;
                    Put(tex, x, y, panelLine ? front * 0.75f : Opaque(front * (1f - 0.05f * Noise(x / 4, y / 3, 8))));
                }
            // canecas sobre o balcão
            for (int i = 0; i < 4; i++)
            {
                int x0 = 10 + i * 22;
                Rect(tex, x0, 2, 4, 5, Outline);
                Fill(tex, x0 + 1, 3, 2, 3, C(0.95f, 0.72f, 0.2f));
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D Barrel()
        {
            Texture2D tex = NewTex(18, 20);
            Color wood = C(0.55f, 0.33f, 0.16f);
            Color hoop = C(0.35f, 0.35f, 0.38f);
            for (int y = 0; y < 20; y++)
            {
                float t = (y - 9.5f) / 9.5f;
                int half = Mathf.RoundToInt(8.5f - 2.5f * t * t);
                int x0 = 9 - half;
                int x1 = 8 + half;
                for (int x = x0; x <= x1; x++)
                {
                    Color c;
                    if (x == x0 || x == x1 || y == 0 || y == 19) c = Outline;
                    else if (y <= 3) c = (y == 3) ? Outline : C(0.68f, 0.45f, 0.24f);
                    else if (y == 6 || y == 15) c = hoop;
                    else c = wood * ((x / 3) % 2 == 0 ? 1f : 0.88f);
                    Put(tex, x, y, Opaque(c));
                }
            }
            // torneira
            Put(tex, 9, 11, C(0.8f, 0.7f, 0.3f));
            Put(tex, 9, 12, C(0.8f, 0.7f, 0.3f));
            tex.Apply();
            return tex;
        }

        private static Texture2D Stove()
        {
            Texture2D tex = NewTex(48, 24);
            Color stone = C(0.40f, 0.38f, 0.40f);
            Color iron = C(0.20f, 0.20f, 0.23f);
            Rect(tex, 0, 0, 48, 24, Outline);
            Fill(tex, 1, 1, 46, 7, iron);
            for (int y = 8; y < 23; y++)
                for (int x = 1; x < 47; x++)
                {
                    int row = (y - 8) / 4;
                    bool mortar = (y - 8) % 4 == 3 || (x + (row % 2) * 4) % 8 == 7;
                    Put(tex, x, y, mortar ? C(0.2f, 0.18f, 0.19f) : Opaque(stone * (0.88f + 0.15f * Noise((x + row * 4) / 8, row, 3))));
                }
            // grelha com carne (esquerda)
            Rect(tex, 5, 1, 14, 7, Outline);
            Fill(tex, 6, 2, 12, 5, C(0.3f, 0.3f, 0.32f));
            Fill(tex, 8, 3, 8, 3, C(0.72f, 0.28f, 0.18f));
            Fill(tex, 9, 3, 3, 1, C(0.88f, 0.45f, 0.30f));
            // panela de sopa (direita)
            Rect(tex, 29, 0, 14, 8, Outline);
            Fill(tex, 30, 1, 12, 6, C(0.28f, 0.28f, 0.30f));
            Fill(tex, 31, 2, 10, 3, C(0.45f, 0.72f, 0.28f));
            Put(tex, 34, 3, C(0.65f, 0.88f, 0.40f));
            Put(tex, 38, 2, C(0.65f, 0.88f, 0.40f));
            // fogo
            Rect(tex, 18, 13, 12, 9, Outline);
            Fill(tex, 19, 14, 10, 7, C(0.15f, 0.08f, 0.05f));
            Fill(tex, 20, 17, 8, 4, C(0.95f, 0.45f, 0.10f));
            Fill(tex, 22, 16, 4, 4, C(1f, 0.80f, 0.25f));
            tex.Apply();
            return tex;
        }

        private static Texture2D Door()
        {
            Texture2D tex = NewTex(32, 16);
            Color wood = C(0.42f, 0.26f, 0.12f);
            Rect(tex, 0, 0, 32, 16, Outline);
            for (int y = 1; y < 15; y++)
                for (int x = 1; x < 31; x++)
                    Put(tex, x, y, x % 6 == 0 ? wood * 0.7f : Opaque(wood * (1f - 0.06f * Noise(x / 6, y, 6))));
            Fill(tex, 1, 4, 30, 1, C(0.3f, 0.3f, 0.33f));
            Fill(tex, 1, 11, 30, 1, C(0.3f, 0.3f, 0.33f));
            Fill(tex, 14, 7, 2, 2, C(0.9f, 0.75f, 0.3f));
            Fill(tex, 17, 7, 2, 2, C(0.9f, 0.75f, 0.3f));
            tex.Apply();
            return tex;
        }

        private static Texture2D Lighthouse()
        {
            Texture2D tex = NewTex(16, 32);
            Color red = C(0.78f, 0.18f, 0.16f);
            Color white = C(0.95f, 0.93f, 0.88f);
            Color light = C(1f, 0.86f, 0.35f);
            // telhado
            for (int y = 0; y < 4; y++)
                for (int x = 7 - y; x <= 8 + y; x++)
                    Put(tex, x, y, (x == 7 - y || x == 8 + y) ? Outline : red);
            // lanterna
            Rect(tex, 4, 4, 8, 5, Outline);
            Fill(tex, 5, 5, 6, 3, light);
            Fill(tex, 3, 9, 10, 1, Outline);
            // torre listrada
            for (int y = 10; y < 32; y++)
            {
                float t = (y - 10) / 21f;
                int half = Mathf.RoundToInt(3f + 3f * t);
                for (int x = 8 - half; x < 8 + half; x++)
                {
                    bool border = x == 8 - half || x == 8 + half - 1 || y == 31;
                    Color band = ((y - 10) / 4) % 2 == 0 ? white : red;
                    Put(tex, x, y, border ? Outline : band);
                }
            }
            Fill(tex, 7, 26, 2, 5, C(0.35f, 0.22f, 0.12f));
            tex.Apply();
            return tex;
        }

        // =====================================================================
        // Utilitários de desenho (coordenadas com origem no topo)
        // =====================================================================

        private static Texture2D NewTex(int w, int h)
        {
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color[] clear = new Color[w * h];
            for (int i = 0; i < clear.Length; i++) clear[i] = new Color(0f, 0f, 0f, 0f);
            tex.SetPixels(clear);
            return tex;
        }

        private static void Put(Texture2D tex, int x, int yTop, Color c)
        {
            if (x < 0 || yTop < 0 || x >= tex.width || yTop >= tex.height) return;
            tex.SetPixel(x, tex.height - 1 - yTop, c);
        }

        private static void Fill(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            for (int j = y; j < y + h; j++)
                for (int i = x; i < x + w; i++)
                    Put(tex, i, j, c);
        }

        /// <summary>Retângulo preenchido com a cor (usado como contorno; o interior é sobrescrito depois).</summary>
        private static void Rect(Texture2D tex, int x, int y, int w, int h, Color c)
        {
            Fill(tex, x, y, w, h, c);
        }

        private static Texture2D FromRows(string[] rows, Dictionary<char, Color> palette)
        {
            int h = rows.Length;
            int w = rows[0].Length;
            Texture2D tex = NewTex(w, h);
            for (int y = 0; y < h; y++)
            {
                string row = rows[y];
                for (int x = 0; x < w && x < row.Length; x++)
                {
                    if (palette.TryGetValue(row[x], out Color c)) Put(tex, x, y, c);
                }
            }
            tex.Apply();
            return tex;
        }

        private static float Noise(int x, int y, int seed)
        {
            float n = Mathf.Sin(x * 12.9898f + y * 78.233f + seed * 37.719f) * 43758.5453f;
            return n - Mathf.Floor(n);
        }

        private static Color C(float r, float g, float b, float a = 1f) => new Color(r, g, b, a);

        private static Color Opaque(Color c)
        {
            c.a = 1f;
            return c;
        }

        private static void Save(Texture2D tex, string name, Vector2 pivot)
        {
            string path = ArtDir + "/" + name + ".png";
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

            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
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
