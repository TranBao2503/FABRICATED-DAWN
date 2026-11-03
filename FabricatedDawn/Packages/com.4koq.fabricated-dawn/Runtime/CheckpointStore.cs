using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace FabricatedDawn
{
    [Serializable]
    public sealed class SaveEnvelope
    {
        public int formatVersion = 1;
        public string contentId;
        public string savedUtc;
        public string payload;
        public string checksum;
    }

    public sealed class CheckpointStore
    {
        public string FilePath { get; }
        public string ContentId { get; }
        public bool Exists => File.Exists(FilePath);

        public CheckpointStore(string filePath, string contentId)
        {
            if (!Path.IsPathRooted(filePath) || string.IsNullOrWhiteSpace(contentId)) throw new ArgumentException("Absolute save path and content ID required.");
            FilePath = filePath; ContentId = contentId;
        }

        public void Save(AdventureSession session)
        {
            if (session.Content.contentId != ContentId) throw new ArgumentException("Wrong content ID.");
            session.ValidateContentState(session.Story.State);
            string payload = session.Story.CaptureCheckpoint();
            var envelope = new SaveEnvelope { contentId = ContentId, savedUtc = DateTime.UtcNow.ToString("O"),
                payload = payload, checksum = Hash(ContentId + "\n" + payload) };
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
            string staging = FilePath + ".tmp";
            // Complete the staging file before atomically replacing the last valid save.
            using (var stream = new FileStream(staging, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                byte[] bytes = new UTF8Encoding(false).GetBytes(JsonUtility.ToJson(envelope, true));
                stream.Write(bytes, 0, bytes.Length); stream.Flush(true);
            }
            if (File.Exists(FilePath)) File.Replace(staging, FilePath, FilePath + ".bak");
            else File.Move(staging, FilePath);
        }

        public void Load(AdventureSession session)
        {
            if (session.Content.contentId != ContentId) throw new ArgumentException("Wrong content ID.");
            session.Restore(ReadValidated(FilePath));
        }

        public void LoadBackup(AdventureSession session)
        {
            if (session.Content.contentId != ContentId) throw new ArgumentException("Wrong content ID.");
            session.Restore(ReadValidated(FilePath + ".bak"));
        }

        private string ReadValidated(string path)
        {
            if (!File.Exists(path)) throw new IOException("Chưa có checkpoint.");
            if (new FileInfo(path).Length > 4 * 1024 * 1024) throw new IOException("Save is unexpectedly large.");
            var envelope = JsonUtility.FromJson<SaveEnvelope>(File.ReadAllText(path));
            if (envelope == null || envelope.formatVersion != 1 || envelope.contentId != ContentId ||
                string.IsNullOrWhiteSpace(envelope.payload) || envelope.checksum != Hash(ContentId + "\n" + envelope.payload))
                throw new ArgumentException("Checkpoint không hợp lệ hoặc thuộc bản nội dung khác.");
            return envelope.payload;
        }

        private static string Hash(string value)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(value))).Replace("-", "");
        }
    }
}
