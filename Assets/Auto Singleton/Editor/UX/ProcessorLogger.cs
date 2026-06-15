using System.Collections.Generic;
using UnityEngine;

namespace AutoSingletonEditor
{
    class ProcessorLogger
    {
        const string LogPrefix = "\t";

        static readonly IReadOnlyDictionary<LogLevel, Color> LogColors = new Dictionary<LogLevel, Color>()
        {
            { LogLevel.Warning, Color.Lerp(Color.yellow, Color.red, 0.35f) },
            { LogLevel.Error, Color.Lerp(Color.red, Color.black, 0.35f) },
        };

        readonly string title;

        List<string> textsList = new List<string>();

        public ProcessorLogger(string title)
        {
            this.title = title;
        }

        public void Append(string text, LogLevel logLevel)
        {
            text = LogPrefix + text;

            if (LogColors.TryGetValue(logLevel, out Color color))
                textsList.Add(ToColor(text, color));
            else
                textsList.Add(text);
        }

        public void Dump()
        {
            if (textsList.Count > 0)
                Debug.Log($"{title}\n{string.Join("\n", textsList)}");
        }

        string ToColor(string text, Color color) => $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }
}
