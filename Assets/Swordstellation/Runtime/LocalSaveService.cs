using System;
using System.IO;
using Swordstellation.Core;
using UnityEngine;

namespace Swordstellation.Runtime
{
    public sealed class LocalSaveService
    {
        private const string SaveFilename = "player-save-v1.json";
        private const string BackupFilename = "player-save-v1.backup.json";

        private readonly string _directory;

        private string SavePath =>
            Path.Combine(_directory, SaveFilename);
        private string BackupPath =>
            Path.Combine(_directory, BackupFilename);

        public LocalSaveService(string directory = null)
        {
            _directory =
                string.IsNullOrWhiteSpace(directory)
                    ? Application.persistentDataPath
                    : Path.GetFullPath(directory);
        }

        public PlayerSaveData Load()
        {
            PlayerSaveData loaded = TryLoad(SavePath) ?? TryLoad(BackupPath);
            return loaded ?? PlayerSaveData.CreateNew();
        }

        public void Save(PlayerSaveData data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            data.Normalize();
            Directory.CreateDirectory(_directory);

            string temporaryPath = SavePath + ".tmp";
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(temporaryPath, json);

            if (File.Exists(SavePath))
            {
                File.Copy(SavePath, BackupPath, true);
                File.Delete(SavePath);
            }

            File.Move(temporaryPath, SavePath);
        }

        private static PlayerSaveData TryLoad(string path)
        {
            try
            {
                if (!File.Exists(path))
                {
                    return null;
                }

                string json = File.ReadAllText(path);
                PlayerSaveData data = JsonUtility.FromJson<PlayerSaveData>(json);
                if (data == null || data.SchemaVersion > PlayerSaveData.CurrentSchemaVersion)
                {
                    return null;
                }

                int loadedSchemaVersion = data.SchemaVersion;
                if (loadedSchemaVersion < 3)
                {
                    data.MusicVolume = PlayerSaveData.DefaultMusicVolume;
                    data.SfxVolume = PlayerSaveData.DefaultSfxVolume;
                    data.SelectedUltimateId = "blade_rain";
                }

                data.Normalize();
                return data;
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"Save load failed at {path}: {exception.Message}");
                return null;
            }
        }
    }
}
