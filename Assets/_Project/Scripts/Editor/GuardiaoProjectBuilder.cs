using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace GuardiaoDoFarol.EditorTools
{
    /// <summary>
    /// Monta automaticamente todo o projeto: placeholders, tiles, campanha, prefabs,
    /// cenas MainMenu e Game com todas as referências ligadas, e Build Settings.
    /// Menu: Guardiao do Farol > Montar Projeto Completo.
    /// ATENÇÃO: recria as cenas MainMenu e Game (o asset da campanha e os sprites existentes são preservados).
    /// </summary>
    public static class GuardiaoProjectBuilder
    {
        private const string Root = "Assets/_Project";
        private const string PlaceholderDir = Root + "/Art/Placeholders";
        private const string TilesDir = Root + "/Art/Tiles";
        private const string PrefabsDir = Root + "/Prefabs";
        private const string ScenesDir = Root + "/Scenes";
        private const string DataDir = Root + "/Data";
        private const string GameScenePath = ScenesDir + "/Game.unity";
        private const string MenuScenePath = ScenesDir + "/MainMenu.unity";

        private const string LayerFloor = "Floor";
        private const string LayerWorld = "World";
        private const string LayerOverhead = "Overhead";

        private static readonly Color TextColor = new Color(0.96f, 0.91f, 0.80f);
        private static readonly Color GoldColor = new Color(0.98f, 0.80f, 0.35f);
        private static readonly Color PanelColor = new Color(0.16f, 0.11f, 0.08f, 0.94f);
        private static readonly Color ButtonColor = new Color(0.55f, 0.36f, 0.18f);
        private static readonly Color HighlightColor = new Color(1f, 0.92f, 0.3f, 1f);
        private static readonly Color InkColor = new Color(0.24f, 0.14f, 0.08f);

        private static Sprite squareSprite;
        private static Sprite circleSprite;
        private static Sprite outlineSprite;

        [MenuItem("Guardiao do Farol/Montar Projeto Completo")]
        public static void BuildFromMenu()
        {
            bool ok = EditorUtility.DisplayDialog("O Guardião do Farol",
                "Isto vai (re)criar as cenas MainMenu e Game, os prefabs e os placeholders.\n\n" +
                "Alterações feitas manualmente nessas cenas serão perdidas. Continuar?",
                "Montar", "Cancelar");
            if (ok) BuildAll();
        }

        public static void BuildAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            EnsureFolder(PlaceholderDir);
            EnsureFolder(TilesDir);
            EnsureFolder(PrefabsDir);
            EnsureFolder(ScenesDir);
            EnsureFolder(DataDir);
            EnsureFolder(Root + "/Art/Characters");
            EnsureFolder(Root + "/Art/Props");
            EnsureFolder(Root + "/Art/Icons");
            EnsureFolder(Root + "/Animations");
            EnsureFolder(Root + "/Audio/Music");
            EnsureFolder(Root + "/Audio/SFX");

            EnsureSortingLayers(LayerFloor, LayerWorld, LayerOverhead);
            ConfigureTopDownSorting();

            squareSprite = CreatePlaceholderSprite("PH_Square", Shape.Square);
            circleSprite = CreatePlaceholderSprite("PH_Circle", Shape.Circle);
            outlineSprite = CreatePlaceholderSprite("PH_Outline", Shape.Outline);

            Tile floorA = CreateTile("Tile_PisoA", squareSprite, new Color(0.56f, 0.39f, 0.25f), Tile.ColliderType.None);
            Tile floorB = CreateTile("Tile_PisoB", squareSprite, new Color(0.51f, 0.35f, 0.22f), Tile.ColliderType.None);
            Tile wall = CreateTile("Tile_Parede", squareSprite, new Color(0.24f, 0.21f, 0.22f), Tile.ColliderType.Grid);
            AssetDatabase.SaveAssets();

            PixelUIStyle.GenerateAll();

            CampaignData campaign = LoadOrCreateCampaign();
            CustomerController customerPrefab = BuildCustomerPrefab();
            GameObject audioPrefab = BuildAudioManagerPrefab();

            BuildMainMenuScene(audioPrefab);
            BuildGameScene(audioPrefab, customerPrefab, campaign, floorA, floorB, wall);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(MenuScenePath, true),
                new EditorBuildSettingsScene(GameScenePath, true)
            };

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            PixelArtLibrary.GenerateAll();
            PixelArtLibrary.ApplyToProject();

            ImportedArt.ConfigureImporters();
            ImportedArt.ApplyToGameScene();
            ImportedArt.ApplyToMenuScene();
            AudioLibrary.Apply();

            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            Debug.Log("O Guardião do Farol: projeto montado com sucesso. Aperte Play para começar pela tela inicial.");
        }

        // =====================================================================
        // Configurações gerais
        // =====================================================================

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

        private static void EnsureSortingLayers(params string[] names)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0) return;

            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty layers = tagManager.FindProperty("m_SortingLayers");
            if (layers == null) return;

            foreach (string layerName in names)
            {
                bool exists = false;
                for (int i = 0; i < layers.arraySize; i++)
                {
                    if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layerName)
                    {
                        exists = true;
                        break;
                    }
                }

                if (exists) continue;
                layers.InsertArrayElementAtIndex(layers.arraySize);
                SerializedProperty element = layers.GetArrayElementAtIndex(layers.arraySize - 1);
                element.FindPropertyRelative("name").stringValue = layerName;
                element.FindPropertyRelative("uniqueID").longValue = (uint)(layerName.GetHashCode() & 0x7FFFFFFF) + 1000u;
                element.FindPropertyRelative("locked").boolValue = false;
            }

            tagManager.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Ordenação top-down: objetos mais abaixo na tela são desenhados na frente.</summary>
        private static void ConfigureTopDownSorting()
        {
            bool configured = false;
            foreach (string guid in AssetDatabase.FindAssets("t:Renderer2DData"))
            {
                Object asset = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null) continue;
                SerializedObject so = new SerializedObject(asset);
                SerializedProperty mode = so.FindProperty("m_TransparencySortMode");
                SerializedProperty axis = so.FindProperty("m_TransparencySortAxis");
                if (mode == null || axis == null) continue;
                mode.intValue = (int)TransparencySortMode.CustomAxis;
                axis.vector3Value = new Vector3(0f, 1f, 0f);
                so.ApplyModifiedPropertiesWithoutUndo();
                configured = true;
            }

            GraphicsSettings_SetCustomAxis();
            if (!configured)
            {
                Debug.LogWarning("Renderer2DData não encontrado: configure Transparency Sort Mode = Custom Axis (0,1,0) manualmente.");
            }
        }

        private static void GraphicsSettings_SetCustomAxis()
        {
            UnityEngine.Rendering.GraphicsSettings.transparencySortMode = TransparencySortMode.CustomAxis;
            UnityEngine.Rendering.GraphicsSettings.transparencySortAxis = new Vector3(0f, 1f, 0f);
        }

        // =====================================================================
        // Placeholders e tiles
        // =====================================================================

        private enum Shape { Square, Circle, Outline }

        private static Sprite CreatePlaceholderSprite(string fileName, Shape shape)
        {
            const int size = 16;
            string path = PlaceholderDir + "/" + fileName + ".png";

            if (!File.Exists(path))
            {
                Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
                float center = (size - 1) / 2f;
                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        bool filled;
                        switch (shape)
                        {
                            case Shape.Circle:
                                float dx = x - center;
                                float dy = y - center;
                                filled = dx * dx + dy * dy <= (size / 2f) * (size / 2f);
                                break;
                            case Shape.Outline:
                                filled = x < 2 || y < 2 || x >= size - 2 || y >= size - 2;
                                break;
                            default:
                                filled = true;
                                break;
                        }
                        texture.SetPixel(x, y, filled ? Color.white : new Color(1f, 1f, 1f, 0f));
                    }
                }
                texture.Apply();
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = size;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void ReloadPlaceholderSprites()
        {
            PixelUIStyle.Load();
            squareSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderDir + "/PH_Square.png");
            circleSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderDir + "/PH_Circle.png");
            outlineSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PlaceholderDir + "/PH_Outline.png");
        }

        private static Tile CreateTile(string name, Sprite sprite, Color color, Tile.ColliderType colliderType)
        {
            string path = TilesDir + "/" + name + ".asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                tile.sprite = sprite;
                tile.color = color;
                AssetDatabase.CreateAsset(tile, path);
            }

            if (tile.sprite == null) tile.sprite = sprite;
            tile.colliderType = colliderType;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static CampaignData LoadOrCreateCampaign()
        {
            string path = DataDir + "/CampanhaGuardiao.asset";
            CampaignData campaign = AssetDatabase.LoadAssetAtPath<CampaignData>(path);
            if (campaign == null)
            {
                campaign = ScriptableObject.CreateInstance<CampaignData>();
                campaign.FillWithDefaultContent();
                AssetDatabase.CreateAsset(campaign, path);
            }
            else if (campaign.DayCount == 0)
            {
                campaign.FillWithDefaultContent();
                EditorUtility.SetDirty(campaign);
            }
            return campaign;
        }

        // =====================================================================
        // Prefabs
        // =====================================================================

        private static CustomerController BuildCustomerPrefab()
        {
            GameObject root = new GameObject("Customer");

            SpriteRenderer body = CreateSprite("Body", root.transform, squareSprite, Color.white,
                new Vector3(0f, 0.45f, 0f), new Vector3(0.75f, 0.9f, 1f), LayerWorld, 0);

            GameObject bubble = new GameObject("OrderBubble");
            bubble.transform.SetParent(root.transform, false);
            bubble.transform.localPosition = new Vector3(0f, 1.55f, 0f);
            CreateSprite("BubbleBackground", bubble.transform, circleSprite, Color.white,
                Vector3.zero, new Vector3(0.75f, 0.75f, 1f), LayerOverhead, 10);
            SpriteRenderer icon = CreateSprite("OrderIcon", bubble.transform, circleSprite, Color.white,
                Vector3.zero, new Vector3(0.5f, 0.5f, 1f), LayerOverhead, 11);

            GameObject bar = new GameObject("PatienceBar");
            bar.transform.SetParent(root.transform, false);
            bar.transform.localPosition = new Vector3(0f, 1.08f, 0f);
            CreateSprite("Background", bar.transform, squareSprite, new Color(0.1f, 0.1f, 0.1f, 0.85f),
                Vector3.zero, new Vector3(0.95f, 0.16f, 1f), LayerOverhead, 10);
            GameObject pivot = new GameObject("FillPivot");
            pivot.transform.SetParent(bar.transform, false);
            pivot.transform.localPosition = new Vector3(-0.45f, 0f, 0f);
            SpriteRenderer fill = CreateSprite("Fill", pivot.transform, squareSprite, new Color(0.35f, 0.85f, 0.35f),
                new Vector3(0.45f, 0f, 0f), new Vector3(0.9f, 0.1f, 1f), LayerOverhead, 11);

            CharacterAnimator animator = root.AddComponent<CharacterAnimator>();
            SetRef(animator, "spriteRenderer", body);

            CustomerController controller = root.AddComponent<CustomerController>();
            SetRef(controller, "bodyRenderer", body);
            SetRef(controller, "characterAnimator", animator);
            SetRef(controller, "orderBubble", bubble);
            SetRef(controller, "orderIconRenderer", icon);
            SetRef(controller, "patienceBarRoot", bar);
            SetRef(controller, "patienceFillPivot", pivot.transform);
            SetRef(controller, "patienceFillRenderer", fill);

            bubble.SetActive(false);
            bar.SetActive(false);

            string path = PrefabsDir + "/Customer.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab.GetComponent<CustomerController>();
        }

        private static GameObject BuildAudioManagerPrefab()
        {
            string path = PrefabsDir + "/AudioManager.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing; // preserva os AudioClips já atribuídos

            GameObject root = new GameObject("AudioManager");
            AudioSource music = root.AddComponent<AudioSource>();
            AudioSource sfx = root.AddComponent<AudioSource>();
            music.playOnAwake = false;
            music.loop = true;
            sfx.playOnAwake = false;
            AudioManager manager = root.AddComponent<AudioManager>();
            SetRef(manager, "musicSource", music);
            SetRef(manager, "sfxSource", sfx);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        // =====================================================================
        // Cena: MainMenu
        // =====================================================================

        private static void BuildMainMenuScene(GameObject audioPrefab)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            audioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/AudioManager.prefab");
            ReloadPlaceholderSprites();

            CreateCamera(new Color(0.07f, 0.08f, 0.13f));
            PrefabUtility.InstantiatePrefab(audioPrefab, scene);
            CreateEventSystem();

            Canvas canvas = CreateCanvas("Canvas");
            RectTransform background = Stretch("Background", canvas.transform, 0, 0, 0, 0);
            Image backgroundImage = AddImage(background, Color.white);
            backgroundImage.sprite = PixelUIStyle.MenuBackground;
            backgroundImage.raycastTarget = false;

            const float contentX = 200f;

            RectTransform title = Rect("Titulo", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(contentX, 330f), new Vector2(1400f, 170f));
            AddText(title, "O GUARDIÃO DO FAROL", 118, TextAlignmentOptions.Center, GoldColor, FontStyles.Bold, true);

            RectTransform subtitle = Rect("Subtitulo", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(contentX, 215f), new Vector2(1400f, 70f));
            AddText(subtitle, "Cinco dias. Heróis aposentados. Uma dívida de 100 moedas.", 40,
                TextAlignmentOptions.Center, TextColor, FontStyles.Normal, true);

            RectTransform menuBox = Rect("PainelMenu", canvas.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(contentX, -120f), new Vector2(560f, 440f));
            AddPanel(menuBox, PixelUIStyle.Panel);

            Button play = CreateButton("BotaoJogar", menuBox, "Jogar", new Vector2(0f, 115f), new Vector2(420f, 100f), out _);
            Button credits = CreateButton("BotaoCreditos", menuBox, "Créditos", new Vector2(0f, 0f), new Vector2(420f, 100f), out _);
            Button quit = CreateButton("BotaoSair", menuBox, "Sair", new Vector2(0f, -115f), new Vector2(420f, 100f), out _);
            SetNavigation(play, credits);
            SetNavigation(credits, quit);
            Navigation creditsNav = credits.navigation;
            creditsNav.selectOnUp = play;
            credits.navigation = creditsNav;

            RectTransform controls = Rect("Controles", canvas.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(1250f, 76f));
            AddPanel(controls, PixelUIStyle.Panel);
            AddText(Stretch("Texto", controls, 30, 30, 8, 8),
                "WASD / Setas: mover   •   E / Espaço: interagir   •   Enter: confirmar", 32,
                TextAlignmentOptions.Center, TextColor);

            // Painel de créditos
            RectTransform creditsPanel = Stretch("Creditos", canvas.transform, 0, 0, 0, 0);
            AddImage(creditsPanel, new Color(0.02f, 0.02f, 0.06f, 0.78f));
            RectTransform creditsBox = Rect("Caixa", creditsPanel, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1320f, 800f));
            AddPanel(creditsBox, PixelUIStyle.Panel);
            AddText(Rect("Titulo", creditsBox, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -45f), new Vector2(-80f, 100f)), "CRÉDITOS", 80, TextAlignmentOptions.Center, GoldColor, FontStyles.Bold, true);

            RectTransform creditsPaper = Stretch("Pergaminho", creditsBox, 70, 70, 160, 180);
            AddPanel(creditsPaper, PixelUIStyle.Parchment);
            AddText(Stretch("Texto", creditsPaper, 50, 50, 30, 30),
                "<i>O Guardião do Farol</i> é um jogo desenvolvido para a matéria de " +
                "<b>Desenvolvimento de Jogos Digitais</b> do professor <b>Luis Filipe Grael Tinós</b>.\n\n" +
                "<color=#8A2A1C>Desenvolvedora:</color> Marcus Vinícius da Silva Capeteruchi\n" +
                "<color=#8A2A1C>Curso:</color> Ciência da Computação\n" +
                "<color=#8A2A1C>Instituição:</color> UNISAGRADO\n" +
                "<color=#8A2A1C>Professor:</color> Luis Filipe Grael Tinós\n" +
                "<color=#8A2A1C>Disciplina:</color> Desenvolvimento de Jogos Digitais",
                36, TextAlignmentOptions.Center, InkColor);
            Button closeCredits = CreateButton("BotaoVoltar", creditsBox, "Voltar", new Vector2(0f, -320f), new Vector2(380f, 96f), out _);
            creditsPanel.gameObject.SetActive(false);

            GameObject menu = new GameObject("MainMenu");
            MainMenuController controller = menu.AddComponent<MainMenuController>();
            SetRef(controller, "playButton", play);
            SetRef(controller, "creditsButton", credits);
            SetRef(controller, "quitButton", quit);
            SetRef(controller, "creditsPanel", creditsPanel.gameObject);
            SetRef(controller, "closeCreditsButton", closeCredits);

            EditorSceneManager.SaveScene(scene, MenuScenePath);
        }

        // =====================================================================
        // Cena: Game
        // =====================================================================

        private static void BuildGameScene(GameObject audioPrefab, CustomerController customerPrefab,
            CampaignData campaign, Tile floorA, Tile floorB, Tile wall)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // NewScene descarrega assets não usados: recarrega os tiles do disco para evitar referências inválidas.
            floorA = AssetDatabase.LoadAssetAtPath<Tile>(TilesDir + "/Tile_PisoA.asset");
            floorB = AssetDatabase.LoadAssetAtPath<Tile>(TilesDir + "/Tile_PisoB.asset");
            wall = AssetDatabase.LoadAssetAtPath<Tile>(TilesDir + "/Tile_Parede.asset");
            audioPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/AudioManager.prefab");
            GameObject customerAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabsDir + "/Customer.prefab");
            customerPrefab = customerAsset != null ? customerAsset.GetComponent<CustomerController>() : null;
            campaign = AssetDatabase.LoadAssetAtPath<CampaignData>(DataDir + "/CampanhaGuardiao.asset");
            ReloadPlaceholderSprites();

            CreateCamera(new Color(0.08f, 0.07f, 0.07f));
            GameObject lightGo = new GameObject("Global Light 2D");
            Light2D light = lightGo.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;

            PrefabUtility.InstantiatePrefab(audioPrefab, scene);
            CreateEventSystem();

            // ---------------- Tilemap ----------------
            GameObject grid = new GameObject("Grid", typeof(Grid));
            Tilemap floor = CreateTilemap("Piso", grid.transform, LayerFloor, 0, false);
            Tilemap walls = CreateTilemap("Paredes", grid.transform, LayerFloor, 1, true);

            for (int x = -10; x <= 9; x++)
            {
                for (int y = -6; y <= 5; y++)
                {
                    floor.SetTile(new Vector3Int(x, y, 0), (y & 1) == 0 ? floorA : floorB);
                }
            }

            for (int x = -11; x <= 10; x++)
            {
                walls.SetTile(new Vector3Int(x, 6, 0), wall);
                bool doorGap = x == -1 || x == 0;
                if (!doorGap) walls.SetTile(new Vector3Int(x, -7, 0), wall);
            }
            for (int y = -7; y <= 6; y++)
            {
                walls.SetTile(new Vector3Int(-11, y, 0), wall);
                walls.SetTile(new Vector3Int(10, y, 0), wall);
            }

            // ---------------- Cenário ----------------
            GameObject scenery = new GameObject("Cenario");

            GameObject door = new GameObject("Porta");
            door.transform.SetParent(scenery.transform);
            door.transform.position = new Vector3(0f, -6.5f, 0f);
            CreateSprite("Visual", door.transform, squareSprite, new Color(0.33f, 0.2f, 0.1f),
                Vector3.zero, new Vector3(2f, 1f, 1f), LayerFloor, 2);
            BoxCollider2D doorBlocker = door.AddComponent<BoxCollider2D>();
            doorBlocker.size = new Vector2(2f, 0.8f);

            GameObject counter = CreateSolidProp("Balcao", scenery.transform, new Vector3(-7f, 2.5f, 0f),
                new Vector2(6f, 0.8f), new Color(0.32f, 0.18f, 0.09f));
            CreateSprite("Tampo", counter.transform, squareSprite, new Color(0.58f, 0.38f, 0.2f),
                new Vector3(0f, 0.25f, 0f), new Vector3(6f, 0.3f, 1f), LayerWorld, 1);

            // Barril (Cerveja)
            GameObject barrel = new GameObject("Barril");
            barrel.transform.SetParent(scenery.transform);
            barrel.transform.position = new Vector3(-2.5f, 5.2f, 0f);
            CreateSprite("Visual", barrel.transform, circleSprite, new Color(0.5f, 0.3f, 0.14f),
                Vector3.zero, new Vector3(1.1f, 1.1f, 1f), LayerWorld, 0);
            CircleCollider2D barrelSolid = barrel.AddComponent<CircleCollider2D>();
            barrelSolid.radius = 0.5f;
            CircleCollider2D barrelTrigger = barrel.AddComponent<CircleCollider2D>();
            barrelTrigger.radius = 0.85f;
            barrelTrigger.isTrigger = true;
            ProductStation barrelStation = barrel.AddComponent<ProductStation>();
            ConfigureStation(barrelStation, ProductType.Cerveja, "Barril", barrel.transform, new Vector2(1.5f, 1.5f));

            // Fogão (Carne e Sopa)
            GameObject stove = CreateSolidProp("Fogao", scenery.transform, new Vector3(-7f, 5.2f, 0f),
                new Vector2(3f, 1f), new Color(0.25f, 0.25f, 0.28f));
            CreateSprite("Brasa", stove.transform, squareSprite, new Color(0.9f, 0.4f, 0.1f),
                new Vector3(0f, 0.15f, 0f), new Vector3(2.6f, 0.2f, 1f), LayerWorld, 1);

            GameObject meatStation = new GameObject("Estacao_Carne");
            meatStation.transform.SetParent(stove.transform, false);
            meatStation.transform.localPosition = new Vector3(-0.8f, -0.1f, 0f);
            CircleCollider2D meatTrigger = meatStation.AddComponent<CircleCollider2D>();
            meatTrigger.radius = 0.6f;
            meatTrigger.isTrigger = true;
            ConfigureStation(meatStation.AddComponent<ProductStation>(), ProductType.Carne, "Fogão",
                meatStation.transform, new Vector2(1.3f, 1.2f));

            GameObject soupStation = new GameObject("Estacao_Sopa");
            soupStation.transform.SetParent(stove.transform, false);
            soupStation.transform.localPosition = new Vector3(0.8f, -0.1f, 0f);
            CircleCollider2D soupTrigger = soupStation.AddComponent<CircleCollider2D>();
            soupTrigger.radius = 0.6f;
            soupTrigger.isTrigger = true;
            ConfigureStation(soupStation.AddComponent<ProductStation>(), ProductType.Sopa, "Fogão",
                soupStation.transform, new Vector2(1.3f, 1.2f));

            // ---------------- Waypoints e mesas ----------------
            GameObject waypoints = new GameObject("Waypoints");
            Transform doorPoint = CreatePoint("Porta_Spawn", waypoints.transform, new Vector3(0f, -6.4f, 0f));
            Transform entry = CreatePoint("Entrada", waypoints.transform, new Vector3(0f, -4.5f, 0f));
            Transform aisle = CreatePoint("Corredor", waypoints.transform, new Vector3(0f, 0.2f, 0f));

            GameObject tablesRoot = new GameObject("Mesas");
            Vector3[] tablePositions =
            {
                new Vector3(3f, 2.5f, 0f),
                new Vector3(7f, 2.5f, 0f),
                new Vector3(3f, -2.5f, 0f),
                new Vector3(7f, -2.5f, 0f),
                new Vector3(-6f, -2.5f, 0f)
            };

            List<Table> tables = new List<Table>();
            for (int i = 0; i < tablePositions.Length; i++)
            {
                tables.Add(CreateTable(i + 1, tablePositions[i], tablesRoot.transform, waypoints.transform, entry, aisle));
            }

            // ---------------- Jogador ----------------
            GameObject player = new GameObject("Taverneiro");
            player.transform.position = new Vector3(-1f, -1f, 0f);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            BoxCollider2D feet = player.AddComponent<BoxCollider2D>();
            feet.size = new Vector2(0.7f, 0.5f);
            feet.offset = new Vector2(0f, 0.25f);
            SpriteRenderer playerBody = CreateSprite("Body", player.transform, squareSprite, new Color(0.3f, 0.45f, 0.9f),
                new Vector3(0f, 0.48f, 0f), new Vector3(0.75f, 0.95f, 1f), LayerWorld, 0);
            CreateSprite("Avental", player.transform, squareSprite, new Color(0.95f, 0.93f, 0.85f),
                new Vector3(0f, 0.32f, 0f), new Vector3(0.55f, 0.45f, 1f), LayerWorld, 1);
            SpriteRenderer carried = CreateSprite("ItemCarregado", player.transform, circleSprite, Color.white,
                new Vector3(0f, 1.3f, 0f), new Vector3(0.55f, 0.55f, 1f), LayerOverhead, 20);

            CharacterAnimator playerAnimator = player.AddComponent<CharacterAnimator>();
            SetRef(playerAnimator, "spriteRenderer", playerBody);
            PlayerController controller = player.AddComponent<PlayerController>();
            SetRef(controller, "characterAnimator", playerAnimator);
            PlayerInteraction interaction = player.AddComponent<PlayerInteraction>();
            SetRef(interaction, "carriedItemRenderer", carried);

            GameObject customersRoot = new GameObject("Clientes");

            // ---------------- Sistemas ----------------
            GameObject systems = new GameObject("Sistemas");
            GameManager gameManager = AddSystem<GameManager>(systems.transform, "GameManager");
            DayManager dayManager = AddSystem<DayManager>(systems.transform, "DayManager");
            AddSystem<EconomyManager>(systems.transform, "EconomyManager");
            OrderManager orderManager = AddSystem<OrderManager>(systems.transform, "OrderManager");
            CustomerManager customerManager = AddSystem<CustomerManager>(systems.transform, "CustomerManager");
            DialogueManager dialogueManager = AddSystem<DialogueManager>(systems.transform, "DialogueManager");

            ConfigureProducts(orderManager);

            SetRef(gameManager, "dayManager", dayManager);
            SetRef(dayManager, "campaign", campaign);
            SetRef(dayManager, "customerManager", customerManager);
            SetRef(dayManager, "dialogueManager", dialogueManager);
            SetRef(customerManager, "customerPrefab", customerPrefab);
            SetRef(customerManager, "doorPoint", doorPoint);
            SetRef(customerManager, "customersParent", customersRoot.transform);
            SetRefList(customerManager, "tables", tables.ConvertAll(t => (Object)t));

            // ---------------- Interface ----------------
            BuildGameUI(dialogueManager);

            EditorSceneManager.SaveScene(scene, GameScenePath);
        }

        private static Table CreateTable(int number, Vector3 position, Transform parent, Transform waypointsRoot,
            Transform entry, Transform aisle)
        {
            GameObject tableGo = new GameObject("Mesa_" + number);
            tableGo.transform.SetParent(parent);
            tableGo.transform.position = position;

            CreateSprite("Visual", tableGo.transform, squareSprite, new Color(0.34f, 0.2f, 0.1f),
                Vector3.zero, new Vector3(1.6f, 1f, 1f), LayerWorld, 0);
            CreateSprite("Caneca", tableGo.transform, circleSprite, new Color(0.75f, 0.7f, 0.6f),
                new Vector3(0.4f, 0.15f, 0f), new Vector3(0.25f, 0.25f, 1f), LayerWorld, 1);

            BoxCollider2D solid = tableGo.AddComponent<BoxCollider2D>();
            solid.size = new Vector2(1.6f, 1f);
            BoxCollider2D trigger = tableGo.AddComponent<BoxCollider2D>();
            trigger.size = new Vector2(2.6f, 2f);
            trigger.isTrigger = true;

            Vector3 seatWorld = position + new Vector3(-1.2f, 0.1f, 0f);
            Transform seat = CreatePoint("Assento", tableGo.transform, seatWorld);
            CreateSprite("Cadeira", tableGo.transform, squareSprite, new Color(0.25f, 0.15f, 0.08f),
                new Vector3(-1.2f, 0.05f, 0f), new Vector3(0.6f, 0.6f, 1f), LayerFloor, 3);

            Transform lane = CreatePoint("Corredor_Mesa_" + number, waypointsRoot, new Vector3(seatWorld.x, 0.2f, 0f));

            GameObject highlight = CreateSprite("Destaque", tableGo.transform, outlineSprite, HighlightColor,
                Vector3.zero, new Vector3(1.9f, 1.3f, 1f), LayerOverhead, 0).gameObject;
            highlight.SetActive(false);

            Table table = tableGo.AddComponent<Table>();
            SetRef(table, "highlight", highlight);
            SetRef(table, "seatPoint", seat);
            SetRefList(table, "pathFromDoor", new List<Object> { entry, aisle, lane });
            return table;
        }

        private static void ConfigureStation(ProductStation station, ProductType product, string stationName,
            Transform parent, Vector2 highlightSize)
        {
            SpriteRenderer icon = CreateSprite("IconeProduto", parent, circleSprite, Color.white,
                new Vector3(0f, 0.75f, 0f), new Vector3(0.45f, 0.45f, 1f), LayerOverhead, 5);
            GameObject highlight = CreateSprite("Destaque", parent, outlineSprite, HighlightColor,
                Vector3.zero, new Vector3(highlightSize.x, highlightSize.y, 1f), LayerOverhead, 0).gameObject;
            highlight.SetActive(false);

            SerializedObject so = new SerializedObject(station);
            so.FindProperty("product").intValue = (int)product;
            so.FindProperty("stationName").stringValue = stationName;
            so.FindProperty("productIconRenderer").objectReferenceValue = icon;
            so.FindProperty("highlight").objectReferenceValue = highlight;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureProducts(OrderManager orderManager)
        {
            SerializedObject so = new SerializedObject(orderManager);
            SerializedProperty list = so.FindProperty("products");
            list.arraySize = 3;
            SetProduct(list.GetArrayElementAtIndex(0), ProductType.Cerveja, "Cerveja", 5, new Color(0.98f, 0.78f, 0.2f));
            SetProduct(list.GetArrayElementAtIndex(1), ProductType.Carne, "Carne", 8, new Color(0.8f, 0.25f, 0.2f));
            SetProduct(list.GetArrayElementAtIndex(2), ProductType.Sopa, "Sopa", 6, new Color(0.45f, 0.8f, 0.3f));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetProduct(SerializedProperty element, ProductType type, string displayName, int price, Color color)
        {
            element.FindPropertyRelative("type").intValue = (int)type;
            element.FindPropertyRelative("displayName").stringValue = displayName;
            element.FindPropertyRelative("icon").objectReferenceValue = circleSprite;
            element.FindPropertyRelative("iconColor").colorValue = color;
            element.FindPropertyRelative("price").intValue = price;
        }

        // =====================================================================
        // Interface do jogo
        // =====================================================================

        private static void BuildGameUI(DialogueManager dialogueManager)
        {
            Canvas canvas = CreateCanvas("Canvas");
            UIManager ui = canvas.gameObject.AddComponent<UIManager>();

            // HUD
            RectTransform hud = Stretch("HUD", canvas.transform, 0, 0, 0, 0);
            RectTransform topBar = Rect("BarraSuperior", hud, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(-24f, 96f));
            AddPanel(topBar, PixelUIStyle.Panel);
            HorizontalLayoutGroup layout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(48, 48, 14, 14);
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            TextMeshProUGUI dayText = AddText(Rect("Dia", topBar, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero),
                "Dia 1/5", 42, TextAlignmentOptions.Left, GoldColor, FontStyles.Bold, true);
            TextMeshProUGUI coinsText = AddText(Rect("Moedas", topBar, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero),
                "Moedas: 0/100", 42, TextAlignmentOptions.Center, new Color(1f, 0.86f, 0.4f), FontStyles.Bold, true);
            TextMeshProUGUI timerText = AddText(Rect("Tempo", topBar, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero),
                "Expediente: 0:00", 42, TextAlignmentOptions.Center, TextColor, FontStyles.Normal, true);
            TextMeshProUGUI servedText = AddText(Rect("Atendidos", topBar, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero),
                "Atendidos: 0/0", 42, TextAlignmentOptions.Right, TextColor, FontStyles.Normal, true);

            RectTransform carriedPanel = Rect("ItemCarregado", hud, Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(20f, 20f), new Vector2(460f, 130f));
            AddPanel(carriedPanel, PixelUIStyle.Panel);
            RectTransform slot = Rect("Slot", carriedPanel, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(84f, 84f));
            AddPanel(slot, PixelUIStyle.Slot);
            RectTransform carriedIconRect = Stretch("Icone", slot, 14, 14, 14, 14);
            Image carriedIcon = AddImage(carriedIconRect, Color.white);
            carriedIcon.preserveAspect = true;
            TextMeshProUGUI carriedLabel = AddText(Stretch("Texto", carriedPanel, 128, 24, 12, 12),
                "Mãos vazias", 34, TextAlignmentOptions.Left, TextColor, FontStyles.Normal, true);

            RectTransform prompt = Rect("IndicadorInteracao", hud, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 170f), new Vector2(1060f, 84f));
            AddPanel(prompt, PixelUIStyle.Panel);
            TextMeshProUGUI promptText = AddText(Stretch("Texto", prompt, 30, 30, 8, 8), "[E] Interagir", 36,
                TextAlignmentOptions.Center, GoldColor, FontStyles.Bold, true);

            RectTransform toast = Rect("Aviso", hud, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -122f), new Vector2(1240f, 84f));
            AddPanel(toast, PixelUIStyle.Parchment);
            TextMeshProUGUI toastText = AddText(Stretch("Texto", toast, 30, 30, 8, 8), "", 36,
                TextAlignmentOptions.Center, InkColor, FontStyles.Bold);

            // Diálogo (pergaminho)
            RectTransform dialogue = Rect("Dialogo", canvas.transform, new Vector2(0.08f, 0f), new Vector2(0.92f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(0f, 300f));
            AddPanel(dialogue, PixelUIStyle.Parchment);
            RectTransform namePlate = Rect("Placa", dialogue, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(620f, 84f));
            AddPanel(namePlate, PixelUIStyle.Panel);
            TextMeshProUGUI speaker = AddText(Stretch("Nome", namePlate, 30, 30, 8, 8), "Nome", 40,
                TextAlignmentOptions.Left, GoldColor, FontStyles.Bold, true);
            TextMeshProUGUI dialogueBody = AddText(Stretch("Fala", dialogue, 48, 48, 66, 56), "Fala", 40,
                TextAlignmentOptions.TopLeft, InkColor);
            TextMeshProUGUI hint = AddText(Rect("Dica", dialogue, new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(-96f, 40f)), "[E] continuar", 28,
                TextAlignmentOptions.Right, new Color(InkColor.r, InkColor.g, InkColor.b, 0.75f));

            SerializedObject dialogueSo = new SerializedObject(dialogueManager);
            dialogueSo.FindProperty("dialoguePanel").objectReferenceValue = dialogue.gameObject;
            dialogueSo.FindProperty("speakerNameText").objectReferenceValue = speaker;
            dialogueSo.FindProperty("bodyText").objectReferenceValue = dialogueBody;
            dialogueSo.FindProperty("continueHintText").objectReferenceValue = hint;
            dialogueSo.ApplyModifiedPropertiesWithoutUndo();

            // Resumo do dia
            RectTransform summary = Stretch("ResumoDoDia", canvas.transform, 0, 0, 0, 0);
            AddImage(summary, new Color(0.02f, 0.02f, 0.06f, 0.75f));
            RectTransform summaryBox = Rect("Caixa", summary, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 760f));
            AddPanel(summaryBox, PixelUIStyle.Panel);
            TextMeshProUGUI summaryTitle = AddText(Rect("Titulo", summaryBox, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(-80f, 90f)), "Dia 1/5 concluído", 60,
                TextAlignmentOptions.Center, GoldColor, FontStyles.Bold, true);
            RectTransform summaryPaper = Stretch("Pergaminho", summaryBox, 70, 70, 150, 190);
            AddPanel(summaryPaper, PixelUIStyle.Parchment);
            TextMeshProUGUI summaryBody = AddText(Stretch("Texto", summaryPaper, 40, 40, 30, 30), "", 40,
                TextAlignmentOptions.Center, InkColor);
            Button nextDay = CreateButton("BotaoProximoDia", summaryBox, "Próximo Dia", new Vector2(0f, -290f),
                new Vector2(460f, 100f), out TextMeshProUGUI nextDayLabel);

            // Vitória e derrota
            BuildEndingPanel(canvas.transform, "Vitoria", new Color(0.03f, 0.10f, 0.06f, 0.88f),
                "Jogar Novamente", out GameObject victoryPanel, out TextMeshProUGUI victoryTitle,
                out TextMeshProUGUI victoryBody, out Button victoryAgain, out Button victoryMenu);
            BuildEndingPanel(canvas.transform, "Derrota", new Color(0.12f, 0.03f, 0.04f, 0.88f),
                "Tentar Novamente", out GameObject defeatPanel, out TextMeshProUGUI defeatTitle,
                out TextMeshProUGUI defeatBody, out Button defeatAgain, out Button defeatMenu);

            SerializedObject so = new SerializedObject(ui);
            so.FindProperty("hudRoot").objectReferenceValue = hud.gameObject;
            so.FindProperty("dayText").objectReferenceValue = dayText;
            so.FindProperty("coinsText").objectReferenceValue = coinsText;
            so.FindProperty("timerText").objectReferenceValue = timerText;
            so.FindProperty("servedText").objectReferenceValue = servedText;
            so.FindProperty("carriedIcon").objectReferenceValue = carriedIcon;
            so.FindProperty("carriedLabel").objectReferenceValue = carriedLabel;
            so.FindProperty("interactionPromptRoot").objectReferenceValue = prompt.gameObject;
            so.FindProperty("interactionPromptText").objectReferenceValue = promptText;
            so.FindProperty("toastRoot").objectReferenceValue = toast.gameObject;
            so.FindProperty("toastText").objectReferenceValue = toastText;
            so.FindProperty("daySummaryPanel").objectReferenceValue = summary.gameObject;
            so.FindProperty("summaryTitleText").objectReferenceValue = summaryTitle;
            so.FindProperty("summaryBodyText").objectReferenceValue = summaryBody;
            so.FindProperty("nextDayButton").objectReferenceValue = nextDay;
            so.FindProperty("nextDayButtonLabel").objectReferenceValue = nextDayLabel;
            so.FindProperty("victoryPanel").objectReferenceValue = victoryPanel;
            so.FindProperty("victoryTitleText").objectReferenceValue = victoryTitle;
            so.FindProperty("victoryBodyText").objectReferenceValue = victoryBody;
            so.FindProperty("victoryPlayAgainButton").objectReferenceValue = victoryAgain;
            so.FindProperty("victoryMenuButton").objectReferenceValue = victoryMenu;
            so.FindProperty("defeatPanel").objectReferenceValue = defeatPanel;
            so.FindProperty("defeatTitleText").objectReferenceValue = defeatTitle;
            so.FindProperty("defeatBodyText").objectReferenceValue = defeatBody;
            so.FindProperty("defeatTryAgainButton").objectReferenceValue = defeatAgain;
            so.FindProperty("defeatMenuButton").objectReferenceValue = defeatMenu;
            so.ApplyModifiedPropertiesWithoutUndo();

            dialogue.gameObject.SetActive(false);
            summary.gameObject.SetActive(false);
            victoryPanel.SetActive(false);
            defeatPanel.SetActive(false);
            toast.gameObject.SetActive(false);
            prompt.gameObject.SetActive(false);
        }

        private static void BuildEndingPanel(Transform parent, string name, Color background, string againLabel,
            out GameObject panel, out TextMeshProUGUI title, out TextMeshProUGUI body, out Button again, out Button menu)
        {
            RectTransform root = Stretch(name, parent, 0, 0, 0, 0);
            AddImage(root, background);
            panel = root.gameObject;

            RectTransform box = Rect("Caixa", root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1480f, 940f));
            AddPanel(box, PixelUIStyle.Panel);

            title = AddText(Rect("Titulo", box, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -45f), new Vector2(-100f, 120f)), name, 84, TextAlignmentOptions.Center, GoldColor, FontStyles.Bold, true);

            RectTransform paper = Stretch("Pergaminho", box, 70, 70, 180, 200);
            AddPanel(paper, PixelUIStyle.Parchment);
            body = AddText(Stretch("Texto", paper, 50, 50, 36, 36), "", 36, TextAlignmentOptions.Center, InkColor);

            again = CreateButton("BotaoJogarNovamente", box, againLabel, new Vector2(-270f, 0f), new Vector2(480f, 100f), out _);
            menu = CreateButton("BotaoMenu", box, "Menu Inicial", new Vector2(270f, 0f), new Vector2(480f, 100f), out _);
            foreach (Button button in new[] { again, menu })
            {
                RectTransform rt = (RectTransform)button.transform;
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 100f);
            }

            Navigation againNav = again.navigation;
            againNav.mode = Navigation.Mode.Explicit;
            againNav.selectOnRight = menu;
            again.navigation = againNav;
            Navigation menuNav = menu.navigation;
            menuNav.mode = Navigation.Mode.Explicit;
            menuNav.selectOnLeft = again;
            menu.navigation = menuNav;
        }

        // =====================================================================
        // Auxiliares de cena
        // =====================================================================

        private static Camera CreateCamera(Color background)
        {
            GameObject go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            go.transform.position = new Vector3(0f, 0f, -10f);
            Camera camera = go.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 7.5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 100f;
            go.AddComponent<AudioListener>();
            go.AddComponent<UniversalAdditionalCameraData>();
            return camera;
        }

        private static void CreateEventSystem()
        {
            GameObject go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            InputSystemUIInputModule module = go.AddComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject go = new GameObject(name);
            go.layer = 5;
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static Tilemap CreateTilemap(string name, Transform grid, string sortingLayer, int order, bool withCollider)
        {
            GameObject go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid, false);
            TilemapRenderer renderer = go.GetComponent<TilemapRenderer>();
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            if (withCollider) go.AddComponent<TilemapCollider2D>();
            return go.GetComponent<Tilemap>();
        }

        private static GameObject CreateSolidProp(string name, Transform parent, Vector3 position, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            go.transform.position = position;
            CreateSprite("Visual", go.transform, squareSprite, color, Vector3.zero, new Vector3(size.x, size.y, 1f), LayerWorld, 0);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = size;
            return go;
        }

        private static SpriteRenderer CreateSprite(string name, Transform parent, Sprite sprite, Color color,
            Vector3 localPosition, Vector3 localScale, string sortingLayer, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingLayerName = sortingLayer;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Transform CreatePoint(string name, Transform parent, Vector3 worldPosition)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, true);
            go.transform.position = worldPosition;
            return go.transform;
        }

        private static T AddSystem<T>(Transform parent, string name) where T : Component
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            return go.AddComponent<T>();
        }

        // =====================================================================
        // Auxiliares de UI
        // =====================================================================

        private static RectTransform Rect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.layer = 5;
            RectTransform rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = sizeDelta;
            return rt;
        }

        private static RectTransform Stretch(string name, Transform parent, float left, float right, float top, float bottom)
        {
            RectTransform rt = Rect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        private static Image AddImage(RectTransform rt, Color color)
        {
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        /// <summary>Imagem 9-slice em pixel art (moldura de madeira, pergaminho, slot...).</summary>
        private static Image AddPanel(RectTransform rt, Sprite sprite)
        {
            Image image = rt.gameObject.AddComponent<Image>();
            image.color = sprite != null ? Color.white : PanelColor;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 100f / 16f / PixelUIStyle.UIPixelScale;
            return image;
        }

        private static TextMeshProUGUI AddText(RectTransform rt, string text, float size, TextAlignmentOptions alignment,
            Color color, FontStyles style = FontStyles.Normal, bool shadow = false)
        {
            TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (PixelUIStyle.Font != null)
            {
                tmp.font = PixelUIStyle.Font;
                if (shadow && PixelUIStyle.ShadowMaterial != null) tmp.fontSharedMaterial = PixelUIStyle.ShadowMaterial;
            }
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.raycastTarget = false;
            tmp.richText = true;
            return tmp;
        }

        private static Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition,
            Vector2 size, out TextMeshProUGUI labelText)
        {
            RectTransform rt = Rect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), anchoredPosition, size);
            Image image = AddPanel(rt, PixelUIStyle.Button);
            Button button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            if (PixelUIStyle.Button != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                SpriteState state = new SpriteState
                {
                    highlightedSprite = PixelUIStyle.ButtonHighlight,
                    selectedSprite = PixelUIStyle.ButtonHighlight,
                    pressedSprite = PixelUIStyle.ButtonPressed,
                    disabledSprite = PixelUIStyle.ButtonPressed
                };
                button.spriteState = state;
            }
            else
            {
                ColorBlock colors = button.colors;
                colors.normalColor = ButtonColor;
                colors.highlightedColor = new Color(0.75f, 0.52f, 0.26f);
                colors.selectedColor = new Color(0.82f, 0.58f, 0.28f);
                colors.pressedColor = new Color(0.4f, 0.25f, 0.12f);
                button.colors = colors;
            }

            labelText = AddText(Stretch("Texto", rt, 20, 20, 8, 14), label, 46, TextAlignmentOptions.Center,
                new Color(1f, 0.95f, 0.82f), FontStyles.Bold, true);
            labelText.textWrappingMode = TextWrappingModes.NoWrap;
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 26f;
            labelText.fontSizeMax = 46f;
            return button;
        }

        private static void SetNavigation(Button upper, Button lower)
        {
            Navigation up = upper.navigation;
            up.mode = Navigation.Mode.Explicit;
            up.selectOnDown = lower;
            upper.navigation = up;
            Navigation down = lower.navigation;
            down.mode = Navigation.Mode.Explicit;
            down.selectOnUp = upper;
            lower.navigation = down;
        }

        // =====================================================================
        // Auxiliares de SerializedObject
        // =====================================================================

        private static void SetRef(Object target, string propertyName, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError("Campo '" + propertyName + "' não encontrado em " + target.GetType().Name);
                return;
            }
            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefList(Object target, string propertyName, IList<Object> values)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty property = so.FindProperty(propertyName);
            if (property == null)
            {
                Debug.LogError("Lista '" + propertyName + "' não encontrada em " + target.GetType().Name);
                return;
            }
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
