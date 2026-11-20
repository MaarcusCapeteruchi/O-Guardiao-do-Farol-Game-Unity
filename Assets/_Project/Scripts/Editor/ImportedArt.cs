using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace GuardiaoDoFarol.EditorTools
{
    /// <summary>
    /// Aplica a arte importada (Assets/_Project/Art/Imported):
    /// - Taverna_Interior.png: fundo pintado da taverna (substitui o visual do tilemap e dos móveis fixos);
    /// - Tela_Titulo.png: arte da tela inicial (já contém o título);
    /// - Taverneira/*.png: quadros de animação da personagem jogável.
    /// Cada parte só é aplicada se o arquivo existir, então o projeto continua funcionando sem ela.
    /// </summary>
    public static class ImportedArt
    {
        private const string Dir = "Assets/_Project/Art/Imported";
        private const string InteriorPath = Dir + "/Taverna_Interior.png";
        private const string TitlePath = Dir + "/Tela_Titulo.png";
        private const string CharacterDir = Dir + "/Taverneira";
        private const string GameScenePath = "Assets/_Project/Scenes/Game.unity";
        private const string MenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";

        // Fundo: 76 px por unidade. O piso da imagem (x 40..1557, y 210..905 px) cobre x -10..9 e y -6..3,1 no mundo.
        private const float InteriorPixelsPerUnit = 76f;
        private static readonly Vector3 InteriorCenter = new Vector3(-0.5f, -0.57f, 0f);
        private const float FloorTopY = 3.15f;
        private const float CharacterPixelsPerUnit = 80f;

        [MenuItem("Guardiao do Farol/Aplicar Arte Importada")]
        public static void ApplyAll()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ConfigureImporters();
            ApplyToGameScene();
            ApplyToMenuScene();
            AssetDatabase.SaveAssets();
        }

        public static void ConfigureImporters()
        {
            AssetDatabase.Refresh();
            Configure(InteriorPath, InteriorPixelsPerUnit, new Vector2(0.5f, 0.5f), FilterMode.Point);
            Configure(TitlePath, 100f, new Vector2(0.5f, 0.5f), FilterMode.Bilinear);
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { CharacterDir }))
            {
                Configure(AssetDatabase.GUIDToAssetPath(guid), CharacterPixelsPerUnit, new Vector2(0.5f, 2f / 140f), FilterMode.Point);
            }
        }

        private static void Configure(string path, float ppu, Vector2 pivot, FilterMode filter)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.filterMode = filter;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.SaveAndReimport();
        }

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static Sprite[] LoadFrames(string animation)
        {
            List<Sprite> frames = new List<Sprite>();
            for (int i = 0; i < 32; i++)
            {
                Sprite sprite = LoadSprite($"{CharacterDir}/Taverneira_{animation}_{i}.png");
                if (sprite == null) break;
                frames.Add(sprite);
            }
            return frames.ToArray();
        }

        // ------------------------------------------------------------------ Jogo

        public static void ApplyToGameScene()
        {
            EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

            Sprite interior = LoadSprite(InteriorPath);
            if (interior != null) ApplyInterior(interior);

            Sprite[] walkDown = LoadFrames("Andar_Baixo");
            if (walkDown.Length > 0) ApplyCharacter(walkDown);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void ApplyInterior(Sprite interior)
        {
            // Fundo pintado
            GameObject background = GameObject.Find("FundoTaverna") ?? new GameObject("FundoTaverna");
            background.transform.position = InteriorCenter;
            SpriteRenderer renderer = background.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = background.AddComponent<SpriteRenderer>();
            renderer.sprite = interior;
            renderer.sortingLayerName = "Floor";
            renderer.sortingOrder = -50;

            // Câmera centralizada na sala pintada (a faixa escura de cima fica sob o HUD).
            Camera camera = Camera.main;
            if (camera != null) camera.transform.position = new Vector3(-0.5f, -0.35f, -10f);

            // Tilemap continua com colisão, mas não aparece mais.
            foreach (TilemapRenderer tilemapRenderer in Object.FindObjectsByType<TilemapRenderer>())
            {
                tilemapRenderer.enabled = false;
            }

            // Parede do fundo da imagem é mais alta que a do tilemap: bloqueia acima do piso pintado.
            GameObject backWall = GameObject.Find("Cenario/ParedeFundo");
            if (backWall == null)
            {
                backWall = new GameObject("ParedeFundo");
                backWall.transform.SetParent(GameObject.Find("Cenario")?.transform);
            }
            backWall.transform.position = new Vector3(-0.5f, (FloorTopY + 6f) * 0.5f, 0f);
            BoxCollider2D wallCollider = backWall.GetComponent<BoxCollider2D>();
            if (wallCollider == null) wallCollider = backWall.AddComponent<BoxCollider2D>();
            wallCollider.size = new Vector2(21f, 6f - FloorTopY);

            // Móveis fixos passam para onde estão na pintura; os visuais antigos somem.
            PlaceProp("Cenario/Balcao", new Vector3(-6.56f, 0.3f, 0f), new Vector2(6.6f, 1.4f));
            PlaceProp("Cenario/Fogao", new Vector3(-8.45f, 2.95f, 0f), new Vector2(2.9f, 0.8f));
            PlaceProp("Cenario/Barril", new Vector3(0.54f, 3.05f, 0f), null);
            HideVisual("Cenario/Porta");
            SetLocal("Cenario/Fogao/Estacao_Carne", new Vector3(-1.0f, -0.35f, 0f));
            SetLocal("Cenario/Fogao/Estacao_Sopa", new Vector3(0.5f, -0.35f, 0f));

            GameObject bucket = GameObject.Find("Cenario/Balde") ?? new GameObject("Balde");
            bucket.transform.SetParent(GameObject.Find("Cenario")?.transform);
            bucket.transform.position = new Vector3(-1.98f, 0.45f, 0f);
            BoxCollider2D bucketCollider = bucket.GetComponent<BoxCollider2D>();
            if (bucketCollider == null) bucketCollider = bucket.AddComponent<BoxCollider2D>();
            bucketCollider.size = new Vector2(1.1f, 0.7f);

            // Mesas do fundo descem para não ficarem dentro da parede pintada;
            // os corredores dos clientes passam abaixo do balcão.
            MoveY("Mesas/Mesa_1", 1.0f);
            MoveY("Mesas/Mesa_2", 1.0f);
            MoveY("Waypoints/Corredor", -0.9f);
            for (int i = 1; i <= 5; i++) MoveY("Waypoints/Corredor_Mesa_" + i, -0.9f);
        }

        private static void PlaceProp(string path, Vector3 position, Vector2? colliderSize)
        {
            GameObject go = GameObject.Find(path);
            if (go == null) return;
            go.transform.position = position;
            if (colliderSize.HasValue)
            {
                foreach (BoxCollider2D box in go.GetComponents<BoxCollider2D>())
                {
                    if (box.isTrigger) continue;
                    box.size = colliderSize.Value;
                    box.offset = Vector2.zero;
                }
            }
            HideVisual(path);
        }

        private static void HideVisual(string path)
        {
            GameObject go = GameObject.Find(path);
            if (go == null) return;
            foreach (SpriteRenderer sr in go.GetComponentsInChildren<SpriteRenderer>(true))
            {
                string n = sr.gameObject.name;
                if (n == "IconeProduto" || n == "Destaque") continue; // ícone e destaque da estação continuam
                sr.enabled = false;
            }
        }

        private static void SetLocal(string path, Vector3 local)
        {
            GameObject go = GameObject.Find(path);
            if (go != null) go.transform.localPosition = local;
        }

        private static void MoveY(string path, float y)
        {
            GameObject go = GameObject.Find(path);
            if (go == null) return;
            Vector3 p = go.transform.position;
            go.transform.position = new Vector3(p.x, y, p.z);
        }

        private static void ApplyCharacter(Sprite[] walkDown)
        {
            GameObject player = GameObject.Find("Taverneiro");
            if (player == null) return;

            Transform body = player.transform.Find("Body");
            SpriteRenderer bodyRenderer = body != null ? body.GetComponent<SpriteRenderer>() : null;
            Sprite[] idle = LoadFrames("Parada");
            if (bodyRenderer != null)
            {
                body.localPosition = Vector3.zero;
                body.localScale = Vector3.one;
                bodyRenderer.sprite = idle.Length > 0 ? idle[0] : walkDown[0];
                bodyRenderer.color = Color.white;
                bodyRenderer.flipX = false;
            }

            Transform carried = player.transform.Find("ItemCarregado");
            if (carried != null) carried.localPosition = new Vector3(0f, 2.05f, 0f);

            CharacterAnimator animator = player.GetComponent<CharacterAnimator>();
            if (animator == null) return;
            SerializedObject so = new SerializedObject(animator);
            so.FindProperty("spriteRenderer").objectReferenceValue = bodyRenderer;
            so.FindProperty("flipWhenMovingLeft").boolValue = false;
            SetArray(so, "idleFrames", idle);
            SetArray(so, "walkDownFrames", walkDown);
            SetArray(so, "walkUpFrames", LoadFrames("Andar_Cima"));
            SetArray(so, "walkLeftFrames", LoadFrames("Andar_Esquerda"));
            SetArray(so, "walkRightFrames", LoadFrames("Andar_Direita"));
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(SerializedObject so, string property, Sprite[] sprites)
        {
            SerializedProperty array = so.FindProperty(property);
            array.arraySize = sprites.Length;
            for (int i = 0; i < sprites.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
            }
        }

        // ------------------------------------------------------------------ Menu

        public static void ApplyToMenuScene()
        {
            Sprite title = LoadSprite(TitlePath);
            if (title == null) return;

            EditorSceneManager.OpenScene(MenuScenePath, OpenSceneMode.Single);
            GameObject background = GameObject.Find("Canvas/Background");
            if (background != null)
            {
                Image image = background.GetComponent<Image>();
                image.sprite = title;
                image.type = Image.Type.Simple;
                image.preserveAspect = false;
            }

            // A arte já traz o título na placa da taverna.
            SetActive("Canvas/Titulo", false);
            SetActive("Canvas/Subtitulo", false);

            // Botões sobre o mar, à direita, sem cobrir a placa nem o farol.
            GameObject panel = GameObject.Find("Canvas/PainelMenu");
            if (panel != null) ((RectTransform)panel.transform).anchoredPosition = new Vector2(560f, -170f);

            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void SetActive(string path, bool active)
        {
            GameObject go = GameObject.Find(path);
            if (go != null) go.SetActive(active);
        }
    }
}
