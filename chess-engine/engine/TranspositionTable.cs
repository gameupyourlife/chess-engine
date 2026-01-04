using chess_engine.game;

namespace chess_engine.engine
{
    /// <summary>
    /// Type of bound stored in transposition table entry.
    /// </summary>
    internal enum TTBoundType : byte
    {
        Exact,      // Exact score (PV node)
        LowerBound, // Score is at least this value (fail-high / beta cutoff)
        UpperBound  // Score is at most this value (fail-low / alpha not raised)
    }

    /// <summary>
    /// Entry in the transposition table.
    /// </summary>
    internal struct TTEntry
    {
        public ulong Key;           // Full Zobrist key for collision detection
        public int Score;           // Evaluation score
        public int Depth;           // Search depth when this was stored
        public TTBoundType Bound;   // Type of bound
        public Move? BestMove;      // Best move found (for move ordering)

        public TTEntry(ulong key, int score, int depth, TTBoundType bound, Move? bestMove)
        {
            Key = key;
            Score = score;
            Depth = depth;
            Bound = bound;
            BestMove = bestMove;
        }
    }

    /// <summary>
    /// Transposition table for caching position evaluations.
    /// Uses a fixed-size array with replacement based on depth.
    /// </summary>
    internal class TranspositionTable
    {
        private readonly TTEntry[] _table;
        private readonly int _size;
        private readonly ulong _mask;

        public int Hits { get; private set; }
        public int Stores { get; private set; }
        public int Collisions { get; private set; }

        /// <summary>
        /// Creates a transposition table with the specified number of entries.
        /// Size will be rounded down to nearest power of 2 for efficient indexing.
        /// </summary>
        /// <param name="sizeInEntries">Approximate number of entries (default ~1M entries, ~32MB)</param>
        public TranspositionTable(int sizeInEntries = 1024 * 1024)
        {
            // Round down to power of 2
            _size = 1;
            while (_size * 2 <= sizeInEntries)
            {
                _size *= 2;
            }
            
            _mask = (ulong)(_size - 1);
            _table = new TTEntry[_size];
        }

        /// <summary>
        /// Gets the index in the table for a given hash.
        /// </summary>
        private int GetIndex(ulong hash) => (int)(hash & _mask);

        /// <summary>
        /// Attempts to retrieve an entry from the table.
        /// </summary>
        /// <param name="hash">Position hash</param>
        /// <param name="entry">Retrieved entry if found</param>
        /// <returns>True if a valid entry was found</returns>
        public bool TryGet(ulong hash, out TTEntry entry)
        {
            int index = GetIndex(hash);
            entry = _table[index];

            // Verify the full key matches (collision detection)
            if (entry.Key == hash)
            {
                Hits++;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Stores an entry in the table.
        /// Uses depth-preferred replacement: deeper searches are kept over shallower ones.
        /// </summary>
        public void Store(ulong hash, int score, int depth, TTBoundType bound, Move? bestMove)
        {
            int index = GetIndex(hash);
            ref TTEntry existing = ref _table[index];

            // Replace if: empty, same position, or new search is deeper
            if (existing.Key == 0 || existing.Key == hash || depth >= existing.Depth)
            {
                if (existing.Key != 0 && existing.Key != hash)
                {
                    Collisions++;
                }

                _table[index] = new TTEntry(hash, score, depth, bound, bestMove);
                Stores++;
            }
        }

        /// <summary>
        /// Clears the transposition table.
        /// </summary>
        public void Clear()
        {
            Array.Clear(_table, 0, _table.Length);
            Hits = 0;
            Stores = 0;
            Collisions = 0;
        }

        /// <summary>
        /// Resets statistics only.
        /// </summary>
        public void ResetStats()
        {
            Hits = 0;
            Stores = 0;
            Collisions = 0;
        }
    }
}
