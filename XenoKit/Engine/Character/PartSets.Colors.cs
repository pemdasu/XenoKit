using System.Linq;
using Xv2CoreLib.BCS;

namespace XenoKit.Engine
{
    public partial class CharaPartSet
    {
        #region BcsColors
        public void ApplyCustomColors()
        {
            foreach (CharaPart part in Parts)
            {
                if (part != null)
                {
                    part.ResetCustomColors();
                    part.LoadCustomColors(BcsColors);
                }
            }
        }

        private void SetBcsColor(int group, int color)
        {
            CustomColorGroup partColor = BcsColors.FirstOrDefault(x => x.Group == group);

            if(partColor == null)
            {
                BcsColors.Add(new CustomColorGroup(group, color));
            }
            else
            {
                partColor.Color = color;
            }
        }

        public void SetBcsColors(PartSet partSet)
        {
            BcsColors.Clear();
            SetBcsColors(partSet.Boots);
            SetBcsColors(partSet.Bust);
            SetBcsColors(partSet.FaceBase);
            SetBcsColors(partSet.FaceEar);
            SetBcsColors(partSet.FaceEye);
            SetBcsColors(partSet.FaceForehead);
            SetBcsColors(partSet.FaceNose);
            SetBcsColors(partSet.Hair);
            SetBcsColors(partSet.Pants);
            SetBcsColors(partSet.Rist);
        }

        private void SetBcsColors(Part part)
        {
            if (part == null) return;

            foreach(ColorSelector colSel in part.ColorSelectors)
            {
                SetBcsColor(colSel.PartColorGroup, colSel.ColorIndex);
            }
        }

        public void SetCacCustomColors(Xv2CoreLib.SAV.CaC cac, int preset)
        {
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.HAIR_, cac.Appearence.I_44);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.EYE_, cac.Appearence.I_46);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.PAINT_A_, cac.Appearence.I_48);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.PAINT_B_, cac.Appearence.I_50);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.PAINT_C_, cac.Appearence.I_52);

            SetBcsColor((int)CustomColorGroup.PartColorMaterials.SKIN_, cac.Appearence.I_36);
            SetBcsColor(1, cac.Appearence.I_38);
            SetBcsColor(2, cac.Appearence.I_40);
            SetBcsColor(3, cac.Appearence.I_42);

            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC00_BUST_, cac.Presets[preset].I_28);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC01_BUST_, cac.Presets[preset].I_30);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC02_BUST_, cac.Presets[preset].I_32);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC03_BUST_, cac.Presets[preset].I_34);

            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC00_PANTS_, cac.Presets[preset].I_36);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC01_PANTS_, cac.Presets[preset].I_38);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC02_PANTS_, cac.Presets[preset].I_40);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC03_PANTS_, cac.Presets[preset].I_42);

            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC00_RIST_, cac.Presets[preset].I_44);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC01_RIST_, cac.Presets[preset].I_46);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC02_RIST_, cac.Presets[preset].I_48);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC03_RIST_, cac.Presets[preset].I_50);

            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC00_BOOTS_, cac.Presets[preset].I_52);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC01_BOOTS_, cac.Presets[preset].I_54);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC02_BOOTS_, cac.Presets[preset].I_56);
            SetBcsColor((int)CustomColorGroup.PartColorMaterials.CC03_BOOTS_, cac.Presets[preset].I_58);
        }

        #endregion
    }
}
