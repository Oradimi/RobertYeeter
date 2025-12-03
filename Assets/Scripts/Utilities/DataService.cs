using System;
using System.IO;
using MessagePack;
using MessagePack.Resolvers;
using UnityEngine;

namespace Utilities
{
    public static class DataService
    {
        public static bool SaveData<T>(string relativePath, T data)
        {
            var path = Application.persistentDataPath + relativePath;

            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                using var stream = new FileStream(path, FileMode.Create);
                stream.Close();
                var bytes = MessagePackSerializer.Serialize(data, ContractlessStandardResolver.Options);
                File.WriteAllBytes(path, bytes);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"Unable to save data due to: {e.Message} {e.StackTrace}");
                return false;
            }
        }

        public static T LoadData<T>(string relativePath)
        {
            var path = Application.persistentDataPath + relativePath;

            if (!File.Exists(path))
                throw new FileNotFoundException($"{path} does not exist.");

            try
            {
                var data = MessagePackSerializer.Deserialize<T>(File.ReadAllBytes(path), ContractlessStandardResolver.Options);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"Unable to load data due to: {e.Message} {e.StackTrace}");
                throw;
            }
        }
    }
}