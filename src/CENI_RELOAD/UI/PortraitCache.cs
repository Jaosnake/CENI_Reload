using System;
using System.Collections.Generic;
using RoR2;
using UnityEngine;

namespace Jaosnake.CeniReload.UI
{
    internal static class PortraitCache
    {
        private static readonly Dictionary<string, Texture> Cache =
            new Dictionary<string, Texture>(StringComparer.Ordinal);

        public static Texture GetPortrait(string bodyName)
        {
            if (string.IsNullOrEmpty(bodyName)) return null;

            bool bodyReady = BodyCatalog.availability.available;
            bool survivorReady = SurvivorCatalog.availability.available;
            if (!bodyReady && !survivorReady) return null;

            if (Cache.TryGetValue(bodyName, out Texture cached))
            {
                if (cached != null) return cached;
                if (bodyReady && survivorReady) return null;
            }

            Texture portrait = null;

            try
            {
                BodyIndex bodyIdx = BodyIndex.None;
                if (bodyReady)
                {
                    bodyIdx = BodyCatalog.FindBodyIndex(bodyName);
                    if (bodyIdx == BodyIndex.None)
                    {
                        bodyIdx = BodyCatalog.FindBodyIndexCaseInsensitive(bodyName);
                    }
                }

                if (bodyIdx != BodyIndex.None && survivorReady)
                {
                    SurvivorIndex survIdx = SurvivorCatalog.GetSurvivorIndexFromBodyIndex(bodyIdx);
                    if (survIdx != SurvivorIndex.None)
                    {
                        portrait = SurvivorCatalog.GetSurvivorPortrait(survIdx);
                    }
                }

                if (portrait == null && bodyReady)
                {
                    GameObject prefab = BodyCatalog.FindBodyPrefab(bodyName);
                    if (prefab == null && bodyIdx != BodyIndex.None)
                    {
                        prefab = BodyCatalog.GetBodyPrefab(bodyIdx);
                    }

                    if (prefab != null)
                    {
                        var body = prefab.GetComponent<CharacterBody>();
                        if (body != null)
                        {
                            portrait = body.portraitIcon;
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            if (portrait != null || (bodyReady && survivorReady))
            {
                Cache[bodyName] = portrait;
            }

            return portrait;
        }

        public static void Clear()
        {
            Cache.Clear();
        }
    }
}
