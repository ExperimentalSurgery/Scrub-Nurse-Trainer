using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Google.Cloud.TextToSpeech.V1Beta1;
using Newtonsoft.Json;
using UnityEngine.Localization;
using Object = UnityEngine.Object;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

#if UNITY_EDITOR
using UnityEditor.Localization;
using UnityEditor;
#endif

namespace NMY.GoogleCloudTextToSpeech
{
    /// <summary>
    /// https://www.cloudskillsboost.google/focuses/2178?parent=catalog
    /// https://cloud.google.com/dotnet/docs/reference/Google.Cloud.TextToSpeech.V1/latest
    /// https://cloud.google.com/text-to-speech/docs/reference/rpc/google.cloud.texttospeech.v1beta1#google.cloud.texttospeech.v1beta1.SynthesizeSpeechResponse
    /// https://cloud.google.com/text-to-speech/docs/reference/rest/v1beta1/text/synthesize?hl=de
    /// </summary>
    [CreateAssetMenu(fileName = "GoogleTextToSpeech", menuName = "TTS/GoogleTextToSpeech", order = 0)]
    public class GoogleTextToSpeech : ScriptableObject
    {
    #region Serialized Fields

        [Tooltip("Authenticate the API calls with this service account JSON file.")]
        [SerializeField] private TextAsset _credentials;

        [Header("Tables")]
        [Tooltip("The string table from which all keys are synthesized to audio.")]
        [SerializeField] private LocalizedStringTable _stringTable;

        [Tooltip("The asset table to which all synthesized assets are stored.")]
        [SerializeField] private LocalizedAssetTable _assetTable;

        [Header("Output")]
        [Tooltip("The output folder for all synthesized assets.")]
        [SerializeField] private Object _outputFolder;

        [Tooltip("The pattern for the output files. \n\nAvailable patterns: [KEY], [LOCALE_CODE]")]
        [SerializeField] private string _outputFilePattern = "[LOCALE_CODE]_[KEY]";
        
        [Tooltip("A valid date time format used in the meta data in each asset entry.")]
        [SerializeField] private string _dateTimeFormat    = "yyyy-MM-dd HH:mm:ss";
        
        [Tooltip("If enabled, create for each string table key a new folder within the OutputFolder. Otherwise, all asset are created in the OutputFolder.")]
        [SerializeField] private bool   _createSubFolders  = true;

        [Space]
        [Tooltip("List of all TTS Configurations")]
        [SerializeField] private List<TextToSpeechConfiguration> _textToSpeechConfigurations;

    #endregion

        private struct TextToSpeechResults
        {
            public readonly string           key;
            public readonly LocaleIdentifier identifier;
            public readonly string           spokenText;
            public readonly string           audioClipPath;
            public readonly string           timestampPath;

            public TextToSpeechResults(string key, LocaleIdentifier identifier, string spokenText, string audioClipPath,
                                       string timestampPath)
            {
                this.key           = key;
                this.identifier    = identifier;
                this.spokenText    = spokenText;
                this.audioClipPath = audioClipPath;
                this.timestampPath = timestampPath;
            }
        }


        private TextToSpeechClient _client;

        private TextToSpeechClient client
        {
            get
            {
                if (_client is not null) return _client;

                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);
                _client = TextToSpeechClient.Create();
                return _client;
            }
        }

        private string outputFolderPath => _outputFolder is not null
            ? Path.GetFullPath(AssetDatabase.GetAssetPath(_outputFolder.GetInstanceID()))
            : string.Empty;

        private string credentialPath => _credentials is not null
            ? Path.GetFullPath(AssetDatabase.GetAssetPath(_credentials))
            : string.Empty;


        public async Task Start()
        {
            Assert.IsNotNull(_credentials, "No credentials file found. Please provide a valid credentials file!");
            Assert.IsNotNull(_stringTable, "No String Table found. Please select a string table.");
            Assert.IsNotNull(_assetTable, "No Asset Table found. Please select a asset table.");
            Assert.IsNotNull(_outputFolder, "No Output folder found. Please select an output folder.");

            var timer = new Stopwatch();
            timer.Start();
            Debug.Log($"{GetType()}: Start processing {_stringTable.TableReference.TableCollectionName} ...", this);

            var resultList = await ProcessStringTable();
            CreateTtsItems(resultList);

            timer.Stop();
            Debug.Log($"{GetType()}: Finished processing in {timer.Elapsed}.", this);
        }

#if UNITY_EDITOR
        public void ClearAssetTableMetadata()
        {
            Assert.IsNotNull(_assetTable);

            var assetTableCollection = LocalizationEditorSettings.GetAssetTableCollection(_assetTable.TableReference);
            
            foreach (var row in assetTableCollection.GetRowEnumerator())
            {
                foreach (var assetTableEntry in row.TableEntries)
                {
                    try
                    {
                        var metadata = assetTableEntry.GetMetadata<LocalizedTextToSpeechMetadata>();
                        if (metadata != null) assetTableEntry.RemoveMetadata(metadata);
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }

            EditorUtility.SetDirty(assetTableCollection);
            EditorUtility.SetDirty(assetTableCollection.SharedData);

            Debug.Log($"{GetType()}: Finished cleaning!", this);
        }

        private async Task<List<TextToSpeechResults>> ProcessStringTable()
        {
            var stringTableCollection = LocalizationEditorSettings.GetStringTableCollection(_stringTable.TableReference);
            var assetTableCollection  = LocalizationEditorSettings.GetAssetTableCollection(_assetTable.TableReference);

            var stringTableCollectionSize = stringTableCollection.GetRowEnumerator().Count();

            var rowCount   = 0;
            var resultList = new List<TextToSpeechResults>();

            foreach (var row in stringTableCollection.GetRowEnumerator())
            {
                ++rowCount;

                var key = row.KeyEntry.Key;
                for (var i = 0; i < row.TableEntries.Length; i++)
                {
                    var stringTableEntry = row.TableEntries[i];

                    // If no content was found in the string table, continue with the next entry
                    if (stringTableEntry is null) continue;

                    var localeIdentifier = stringTableEntry.Table.LocaleIdentifier;
                    var text             = stringTableEntry.Value;

                    if (WasAssetAlreadyCreatedWithSameContent(assetTableCollection, localeIdentifier, key, text)) continue;

                    Debug.Log(
                        $"{GetType()}: Synthesize Entry '{key}' for {localeIdentifier.ToString()} (Row: {rowCount}/{stringTableCollectionSize} | Column: {i + 1}/{row.TableEntries.Length})",
                        this);
                    var result = await SynthesizeSpeech(key, localeIdentifier, text);
                    resultList.Add(result);
                }
            }

            AssetDatabase.Refresh();

            return resultList;
        }
#endif

        private bool WasAssetAlreadyCreatedWithSameContent(AssetTableCollection assetTableCollection,
                                                           LocaleIdentifier localeIdentifier, string key, string text)
        {
            var table = assetTableCollection.AssetTables.First(x => x.LocaleIdentifier == localeIdentifier);
            var entry = table.GetEntry(key);

            // No entry was found or data in entry is missing. We will generate it again!
            if (entry == null || entry.IsEmpty) return false;

            // Asset was not created if no meta data was found.
            // If asset content does not match, create asset again.
            var metadata = entry.GetMetadata<LocalizedTextToSpeechMetadata>();
            if (metadata == null || metadata.spokenText == null || !metadata.spokenText.Equals(text)) return false;

            Debug.Log(
                $"{GetType()}: Audio file for '{key}' in '{localeIdentifier}' already created with same content! Skipped.",
                this);
            return true;
        }


        private async Task<TextToSpeechResults> SynthesizeSpeech(string key, LocaleIdentifier localeIdentifier, string text)
        {
            var config = _textToSpeechConfigurations.Find(x => x.locale.Identifier == localeIdentifier);

            if (config != null) return await SynthesizeSpeech(key, localeIdentifier, text, config);
            throw new Exception($"TTS Configuration for locale identifier '{localeIdentifier}' could not be found!");
        }


        private async Task<TextToSpeechResults> SynthesizeSpeech(string key, LocaleIdentifier localeIdentifier, string text,
                                                                 TextToSpeechConfiguration config)
        {
            var localeCode = localeIdentifier.Code;

            var response      = await client.SynthesizeSpeechAsync(CreateSpeechRequest(text, config));
            var audioClipPath = await ProcessAudioFile(key, localeCode, config, response);
            var timestampPath = await ProcessTimestampFile(key, localeCode, response);

            return new TextToSpeechResults(key, localeIdentifier, text, audioClipPath, timestampPath);
        }

        private static SynthesizeSpeechRequest CreateSpeechRequest(string text, TextToSpeechConfiguration config)
        {
            var input = new SynthesisInput { Ssml = CreateSsmlText(text) };

            var request = new SynthesizeSpeechRequest
            {
                Input       = input,
                AudioConfig = config.GetAudioConfig(),
                Voice       = config.GetVoiceSelectionParams()
                    
            };
            request.EnableTimePointing.Add(config.timepoint);
            return request;
        }


        private async Task<string> ProcessAudioFile(string key, string localeCode, TextToSpeechConfiguration config,
                                                    SynthesizeSpeechResponse response)
        {
            GetPaths(GetAudioFileName, key, localeCode, config, out var relativePath, out var fullPath);

            await using (Stream output = File.Create(fullPath))
            {
                // response.AudioContent is a ByteString. This can easily be converted into
                // a byte array or written to a stream.
                response.AudioContent.WriteTo(output);
            }

            return relativePath;
        }


        private async Task<string> ProcessTimestampFile(string key, string localeCode, SynthesizeSpeechResponse response)
        {
            GetPaths(GetTimestampsFileName, key, localeCode, out var relativePath, out var fullPath);

            await using (var output = new StreamWriter(fullPath, false))
            {
                await output.WriteAsync(JsonConvert.SerializeObject(response.Timepoints, Formatting.Indented));
            }

            return relativePath;
        }

        private string CreateSubfolderIfNeeded(string key)
        {
            var subFolderPath = Path.Combine(outputFolderPath, _createSubFolders ? key : "");
            if (!AssetDatabase.IsValidFolder(RelativePathToAssets(subFolderPath)))
                AssetDatabase.CreateFolder(RelativePathToAssets(outputFolderPath), key);

            return subFolderPath;
        }

        private void CreateTtsItems(IReadOnlyList<TextToSpeechResults> resultList)
        {
            for (var i = 0; i < resultList.Count; i++)
            {
                var result = resultList[i];
                Debug.Log($"{GetType()}: Create TTS Item: {i + 1}/{resultList.Count}", this);
                var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(result.audioClipPath);
                var timestamp = AssetDatabase.LoadAssetAtPath<TextAsset>(result.timestampPath);
                var ttsItem   = CreateTtsItem(result.key, result.identifier.Code, audioClip, timestamp);
                SaveToAssetTable(result, ttsItem);
            }

            AssetDatabase.Refresh();
        }


        private LocalizedTextToSpeechItem CreateTtsItem(string key, string localeCode, AudioClip clip, TextAsset timestamp)
        {
            var fileName      = GetTtsItemFileName(key, localeCode);
            var subfolderPath = CreateSubfolderIfNeeded(key);
            var fullPath      = Path.Combine(subfolderPath, fileName);
            var relativePath  = RelativePathToAssets(fullPath);

            var item = LocalizedTextToSpeechItem.Create(clip, timestamp);

            AssetDatabase.CreateAsset(item, relativePath);
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();

            return item;
        }

        private void SaveToAssetTable(TextToSpeechResults result, LocalizedTextToSpeechItem textToSpeechItem)
        {
            var collection = LocalizationEditorSettings.GetAssetTableCollection(_assetTable.TableReference);
            collection.AddAssetToTable(result.identifier, result.key, textToSpeechItem);

            AddMetadataToEntry(result, collection);

            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(collection.SharedData);
        }

        private void AddMetadataToEntry(TextToSpeechResults result, AssetTableCollection collection)
        {
            var table = collection.AssetTables.First(x => x.LocaleIdentifier == result.identifier);
            var entry = table.GetEntry(result.key);

            // Add the spoken text to the meta data, to check if the content did change the last time it was created
            var metadata = entry.GetMetadata<LocalizedTextToSpeechMetadata>();
            if (metadata == null)
                entry.AddMetadata(new LocalizedTextToSpeechMetadata()
                {
                    created      = DateTime.Now.ToString(_dateTimeFormat),
                    lastModified = DateTime.Now.ToString(_dateTimeFormat),
                    spokenText   = result.spokenText
                });
            else
            {
                metadata.spokenText   = result.spokenText;
                metadata.lastModified = DateTime.Now.ToString(_dateTimeFormat);
            }
        }


    #region Helpers

        private void GetPaths(Func<string, string, string> fileNameGenerator, string key, string localeCode,
                              out string relativePath, out string fullPath)
        {
            var fileName      = fileNameGenerator.Invoke(key, localeCode);
            var subfolderPath = CreateSubfolderIfNeeded(key);
            fullPath     = Path.Combine(subfolderPath, fileName);
            relativePath = RelativePathToAssets(fullPath);
        }


        private void GetPaths(Func<string, string, TextToSpeechConfiguration, string> fileNameGenerator, string key,
                              string localeCode, TextToSpeechConfiguration configuration, out string relativePath,
                              out string fullPath)
        {
            var fileName      = fileNameGenerator.Invoke(key, localeCode, configuration);
            var subfolderPath = CreateSubfolderIfNeeded(key);
            fullPath     = Path.Combine(subfolderPath, fileName);
            relativePath = RelativePathToAssets(fullPath);
        }


        private string CreateOutputFileName(string key, string localeCode) =>
            _outputFilePattern.Replace("[KEY]", key).Replace("[LOCALE_CODE]", localeCode);


        private string GetAudioFileName(string key, string localeCode,
                                        TextToSpeechConfiguration textToSpeechConfiguration) =>
            $"{CreateOutputFileName(key, localeCode)}{textToSpeechConfiguration.GetAudioExtension()}";


        private string GetTimestampsFileName(string key, string localeCode) =>
            $"{CreateOutputFileName(key, localeCode)}.json";


        private string GetTtsItemFileName(string key, string localeCode) =>
            $"{CreateOutputFileName(key, localeCode)}.asset";

        private string RelativePathToAssets(string fullPath) =>
            $"Assets/{Path.GetRelativePath(Application.dataPath, fullPath)}";

        private static string CreateSsmlText(string text) => $"<speak>{text}</speak>";

    #endregion
    }
}

namespace System
{
    public enum IAsyncDisposable
    {
    }
}
