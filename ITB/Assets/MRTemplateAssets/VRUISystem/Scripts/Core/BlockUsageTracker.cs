using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MRTemplateAssets.Scripts
{
    /// <summary>
    /// Tracks block usage statistics for the stats panel and recents system
    /// </summary>
    public class BlockUsageTracker : MonoBehaviour
    {
        public static BlockUsageTracker Instance { get; private set; }

        [System.Serializable]
        public class BlockUsage
        {
            public string blockId;
            public string blockName;
            public Color color;
            public int count;

            public string GetColorName()
            {
                // Simple color name approximation
                if (color == Color.red) return "Red";
                if (color == Color.blue) return "Blue";
                if (color == Color.yellow) return "Yellow";
                if (color == Color.green) return "Green";
                if (color == Color.white) return "White";
                if (color == Color.black) return "Black";
                return "Custom";
            }
        }

        private Dictionary<string, BlockUsage> usageStats = new Dictionary<string, BlockUsage>();
        private List<string> recentBlocks = new List<string>();
        private const int MAX_RECENTS = 6;

        // Events
        public event System.Action OnUsageStatsUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Record that a block was placed
        /// </summary>
        public void RecordBlockPlacement(string blockId, string blockName, Color color)
        {
            string key = $"{blockId}_{ColorToString(color)}";

            if (usageStats.ContainsKey(key))
            {
                usageStats[key].count++;
            }
            else
            {
                usageStats[key] = new BlockUsage
                {
                    blockId = blockId,
                    blockName = blockName,
                    color = color,
                    count = 1
                };
            }

            // Update recents
            if (recentBlocks.Contains(key))
            {
                recentBlocks.Remove(key);
            }
            recentBlocks.Insert(0, key);

            if (recentBlocks.Count > MAX_RECENTS)
            {
                recentBlocks.RemoveAt(recentBlocks.Count - 1);
            }

            OnUsageStatsUpdated?.Invoke();
        }

        /// <summary>
        /// Get the top N most used blocks
        /// </summary>
        public List<BlockUsage> GetTopUsedBlocks(int count = 3)
        {
            return usageStats.Values
                .OrderByDescending(usage => usage.count)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Get all usage statistics sorted by count
        /// </summary>
        public List<BlockUsage> GetAllUsageStats()
        {
            return usageStats.Values
                .OrderByDescending(usage => usage.count)
                .ToList();
        }

        /// <summary>
        /// Get recent blocks for the hotbar
        /// </summary>
        public List<BlockUsage> GetRecentBlocks()
        {
            List<BlockUsage> recents = new List<BlockUsage>();
            foreach (string key in recentBlocks)
            {
                if (usageStats.ContainsKey(key))
                {
                    recents.Add(usageStats[key]);
                }
            }
            return recents;
        }

        /// <summary>
        /// Get total number of blocks placed
        /// </summary>
        public int GetTotalBlockCount()
        {
            return usageStats.Values.Sum(usage => usage.count);
        }

        private string ColorToString(Color color)
        {
            return $"{(int)(color.r * 255)}_{(int)(color.g * 255)}_{(int)(color.b * 255)}";
        }
    }
}
