using Hexa.NET.ImGui;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HexaImGui
{
    public class LogViewer
    {
        public const int MaxLocalStorage = 100;

        public ConcurrentQueue<LogMessage> MessageQueue = new ConcurrentQueue<LogMessage>();

        public List<LogMessage> MessageLocalStorage = new List<LogMessage>();

        public void AddMessage(LogMessage message)
        {
            MessageQueue.Enqueue(message);
        }

        public void AdjustMessage()
        {
            while (MessageQueue.TryDequeue(out var message) == true)
            {
                MessageLocalStorage.Add(message);
            }

            while (MessageLocalStorage.Count > MaxLocalStorage)
            {
                MessageLocalStorage.RemoveAt(0);
            }
        }

        public void Draw()
        {
            AdjustMessage();

            ImGui.Begin("LogViewer");

            foreach (LogMessage message in MessageLocalStorage)
            {
                ImGui.Text(message.Message);
            }

            ImGui.End();
            
        }
    }

    public record LogMessage
    {
        public DateTime DateTime { get; set; } = DateTime.MinValue;
        
        public int Level { get; set; } = 0;

        public string Message { get; set; } = string.Empty;
    }
}
