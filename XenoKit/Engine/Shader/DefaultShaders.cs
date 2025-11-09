using System;
using XenoKit.Editor;
using Xv2CoreLib.EMM;

namespace XenoKit.Engine.Shader
{
    public static class DefaultShaders
    {
        public static Xv2ShaderEffect VertexColor_W { get; private set; }
        public static Xv2ShaderEffect Red { get; private set; }
        public static Xv2ShaderEffect RedWireframe { get; private set; }
        public static Xv2ShaderEffect BlueWireframe { get; private set; }
        public static Xv2ShaderEffect WhiteWireframe { get; private set; }

        public static void InitDefaultShaders()
        {
            EmmMaterial material = new EmmMaterial();
            material.Name = "default";
            material.DecompileParameters();
            material.DecompiledParameters.BackFace = 1;
            material.DecompiledParameters.ForceWireframeMode = true;

            //Red
            material.ShaderProgram = "Red";
            RedWireframe = new Xv2ShaderEffect(material, ShaderType.Chara);

            //Blue
            EmmMaterial blueMat = material.Copy();
            blueMat.ShaderProgram = "Blue";
            BlueWireframe = new Xv2ShaderEffect(blueMat, ShaderType.Chara);

            //White
            EmmMaterial whiteMat = material.Copy();
            whiteMat.ShaderProgram = "White";
            WhiteWireframe = new Xv2ShaderEffect(whiteMat, ShaderType.Chara);

            //Skinned default shader for characters/stages/emos
            EmmMaterial normalMat = material.Copy();
            normalMat.ShaderProgram = "VertexColor_W";
            if (!LocalSettings.Instance.UseWireframeMissingMaterial)
                normalMat.DecompiledParameters.ForceWireframeMode = false;

            VertexColor_W = new Xv2ShaderEffect(normalMat, ShaderType.Chara);

            //No wireframe
            EmmMaterial redNoWireframeMat = material.Copy();
            redNoWireframeMat.DecompiledParameters.ForceWireframeMode = false;
            Red = new Xv2ShaderEffect(redNoWireframeMat, ShaderType.Chara);
        }
    }
}