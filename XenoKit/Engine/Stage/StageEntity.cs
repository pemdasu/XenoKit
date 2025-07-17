using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Matrix4x4 = System.Numerics.Matrix4x4;

namespace XenoKit.Engine.Stage
{
    public class StageEntity
    {
        public Matrix4x4 Transform { get; set; }
        public StageVisual Visual { get; set; }

        public void Draw(Matrix4x4 world)
        {
            Visual?.Draw(world * Transform);
        }

        public void DrawSimple(Matrix4x4 world)
        {
            Visual?.DrawSimple(world * Transform);
        }
    }
}
