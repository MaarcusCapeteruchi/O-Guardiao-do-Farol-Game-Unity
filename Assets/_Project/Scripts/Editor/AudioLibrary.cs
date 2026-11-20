using UnityEditor;
using UnityEngine;

namespace GuardiaoDoFarol.EditorTools
{
    /// <summary>
    /// Configura a importação dos áudios em Assets/_Project/Audio e liga os clipes no prefab do AudioManager.
    /// As músicas e efeitos foram sintetizados (chiptune) especialmente para o jogo.
    /// Menu: Guardiao do Farol > Aplicar Áudio.
    /// </summary>
    public static class AudioLibrary
    {
        private const string MusicDir = "Assets/_Project/Audio/Music";
        private const string SfxDir = "Assets/_Project/Audio/SFX";
        private const string AudioPrefabPath = "Assets/_Project/Prefabs/AudioManager.prefab";

        private static readonly (string field, string file)[] Clips =
        {
            ("menuMusic", MusicDir + "/Musica_Menu.wav"),
            ("tavernMusic", MusicDir + "/Musica_Taverna.wav"),
            ("footstep", SfxDir + "/SFX_Passo.wav"),
            ("pickup", SfxDir + "/SFX_Pegar.wav"),
            ("deliver", SfxDir + "/SFX_Entregar.wav"),
            ("coin", SfxDir + "/SFX_Moeda.wav"),
            ("dialogueOpen", SfxDir + "/SFX_Dialogo.wav"),
            ("customerEnter", SfxDir + "/SFX_Cliente_Entra.wav"),
            ("victory", SfxDir + "/SFX_Vitoria.wav"),
            ("defeat", SfxDir + "/SFX_Derrota.wav"),
            ("buttonClick", SfxDir + "/SFX_Clique.wav"),
            ("wrongItem", SfxDir + "/SFX_Errado.wav"),
            ("customerLeave", SfxDir + "/SFX_Cliente_Sai.wav"),
            ("shiftBell", SfxDir + "/SFX_Sino.wav"),
            ("dayEnd", SfxDir + "/SFX_Fim_Do_Dia.wav")
        };

        [MenuItem("Guardiao do Farol/Aplicar Áudio")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            foreach ((string _, string file) in Clips) ConfigureImporter(file, file.StartsWith(MusicDir));

            GameObject prefab = PrefabUtility.LoadPrefabContents(AudioPrefabPath);
            if (prefab == null) return;
            AudioManager manager = prefab.GetComponent<AudioManager>();
            if (manager != null)
            {
                SerializedObject so = new SerializedObject(manager);
                foreach ((string field, string file) in Clips)
                {
                    SerializedProperty property = so.FindProperty(field);
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(file);
                    if (property != null && clip != null) property.objectReferenceValue = clip;
                }
                so.FindProperty("musicVolume").floatValue = 0.45f;
                so.FindProperty("sfxVolume").floatValue = 0.8f;
                so.FindProperty("footstepVolume").floatValue = 0.35f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            PrefabUtility.SaveAsPrefabAsset(prefab, AudioPrefabPath);
            PrefabUtility.UnloadPrefabContents(prefab);
            AssetDatabase.SaveAssets();
        }

        private static void ConfigureImporter(string path, bool music)
        {
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return;
            importer.forceToMono = true;
            importer.loadInBackground = music;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = music ? AudioCompressionFormat.Vorbis : AudioCompressionFormat.ADPCM;
            settings.quality = 0.6f;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }
    }
}
