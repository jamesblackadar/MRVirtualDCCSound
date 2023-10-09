using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace MRVirtualDCCSound.Core
{
    public class StorageRepository<T> : IStorageRepository<T> where T : class, new()
    {
        private ILogger _logger;
        private bool isDebugEnabled;
        private readonly string _filename;
        private readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions() { WriteIndented = true };
        public string FilePath => _filename;
        public StorageRepository(string path) : this(path, null)
        {
        }

        public StorageRepository(string path, ILogger logger)
        {

            _logger = logger;
            isDebugEnabled = logger != null && logger.IsEnabled(LogLevel.Debug);

            var x = typeof(T).GetGenericArguments().FirstOrDefault();
            _filename = System.IO.Path.Combine(path, (x ?? typeof(T)).Name + ".json");
        }
        public void Write(object value)
        {
            try
            {
                string json = JsonSerializer.Serialize(value, jsonSerializerOptions);
                if (isDebugEnabled) _logger.LogDebug($"Writing '{_filename}' with {json}");
                File.WriteAllText(_filename, json);
            }
            catch (Exception e)
            {
                if (_logger.IsEnabled(LogLevel.Error)) _logger.LogError(e, $"Error writing '{_filename}' because {e.Message}");
            }
        }
        public T Read()
        {
            if (File.Exists(_filename))
            {
                string json = File.ReadAllText(_filename);
                File.Copy(_filename, _filename + ".bak", true);
                var obj = JsonSerializer.Deserialize<T>(json);
                return obj;
            }
            else
            {
                return new T();
            }
        }
    }
}