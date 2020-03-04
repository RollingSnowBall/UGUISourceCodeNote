using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.UI
{
    /// <summary>
    ///   Utility class that is used to help with Text update.
    /// </summary>
    /// <remarks>
    /// When Unity rebuilds a font atlas a callback is sent to the font. Using this class you can register your text as needing to be rebuilt if the font atlas is updated.
    /// </remarks>
    public static class FontUpdateTracker
    {
        static Dictionary<Font, HashSet<Text>> m_Tracked = new Dictionary<Font, HashSet<Text>>();

        // 维护了一个字典，font为key，使用font的所有Text为value。
        // 当rebuild时会根据font调用接口
        public static void TrackText(Text t)
        {
            if (t.font == null)
                return;

            HashSet<Text> exists;
            m_Tracked.TryGetValue(t.font, out exists);
            if (exists == null)
            {
                // The textureRebuilt event is global for all fonts, so we add our delegate the first time we register *any* Text
                if (m_Tracked.Count == 0)
                    Font.textureRebuilt += RebuildForFont;

                exists = new HashSet<Text>();
                m_Tracked.Add(t.font, exists);
            }

            if (!exists.Contains(t))
                exists.Add(t);
        }

        private static void RebuildForFont(Font f)
        {
            HashSet<Text> texts;
            m_Tracked.TryGetValue(f, out texts);

            if (texts == null)
                return;

            foreach (var text in texts)
                text.FontTextureChanged();
        }

        // Deregister a Text element from receiving texture atlas rebuild calls.
        // 维护了一个字典，font为key，使用font的所有Text为value。
        // 当rebuild时会根据font调用接口
        public static void UntrackText(Text t)
        {
            if (t.font == null)
                return;

            HashSet<Text> texts;
            m_Tracked.TryGetValue(t.font, out texts);

            if (texts == null)
                return;

            texts.Remove(t);

            if (texts.Count == 0)
            {
                m_Tracked.Remove(t.font);

                // There is a global textureRebuilt event for all fonts, so once the last Text reference goes away, remove our delegate
                if (m_Tracked.Count == 0)
                    Font.textureRebuilt -= RebuildForFont;
            }
        }
    }
}
