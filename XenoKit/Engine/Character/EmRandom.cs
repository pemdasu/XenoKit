namespace XenoKit.Engine.Character
{
    public class EmRandom
    {
        private uint state;

        public void Seed(uint seed)
        {
            state = seed;
        }

        public uint GetInt()
        {
            uint next = unchecked(state * 0x19660Du + 0x3C6EF35Fu);
            state = (next << 16) | (next >> 16);
            return state;
        }
    }
}
