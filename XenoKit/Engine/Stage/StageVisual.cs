using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XenoKit.Engine.Model;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Stage
{
    public class StageVisual
    {
        public LodGroup LodGroup { get; set; }

        public void DrawReflection(Matrix4x4 world)
        {
            LodGroup.DrawReflection(world);
        }

        public void Draw(Matrix4x4 world)
        {
            LodGroup.Draw(world);
        }

        public void DrawSimple(Matrix4x4 world)
        {
            LodGroup.DrawSimple(world);
        }
    }
}
