namespace XenoKit.Engine
{
    public class CustomColorGroup
    {
        //For CAC reference only. Get the actual material prefix from the BCS file, as it can change!
        public enum PartColorMaterials
        {
            SKIN_ = 0,
            SKIN2 = 1,
            SKIN3 = 2,
            SKIN4 = 3,
            HAIR_ = 4,
            EYE_ = 5,
            CC00_BUST_ = 6,
            CC01_BUST_ = 7,
            CC02_BUST_ = 8,
            CC03_BUST_ = 9,
            CC00_PANTS_ = 10,
            CC01_PANTS_ = 11,
            CC02_PANTS_ = 12,
            CC03_PANTS_ = 13,
            CC00_RIST_ = 14,
            CC01_RIST_ = 15,
            CC02_RIST_ = 16,
            CC03_RIST_ = 17,
            CC00_BOOTS_ = 18,
            CC01_BOOTS_ = 19,
            CC02_BOOTS_ = 20,
            CC03_BOOTS_ = 21,
            PAINT_A_ = 22,
            PAINT_B_ = 23,
            PAINT_C_ = 24
        }

        public int Group { get; set; }
        public int Color { get; set; }

        public CustomColorGroup(int group, int color)
        {
            Group = group;
            Color = color;
        }

        public void Reset()
        {
            Group = -1;
            Color = -1;
        }
    }
}
