using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Specialized;
using XenoKit.Engine.Shader;
using XenoKit.Engine.Textures;
using Xv2CoreLib.EMP_NEW;

namespace XenoKit.Engine.Vfx.Particle
{
    public class ParticleEmissionData : EngineObject, IDisposable
    {
        public readonly ParticleNode ParticleNode;

        public SamplerInfo[] Samplers { get; private set; }
        public Xv2Texture[] Textures { get; private set; }
        public Xv2ShaderEffect Material { get; private set; }

        private bool IsTexturesDirty { get; set; }
        private bool IsMaterialsDirty { get; set; }

        private TextureEntry_Ref[] ObservedTextureEntries = Array.Empty<TextureEntry_Ref>();
        private EMP_TextureSamplerDef[] ObservedTextureDefs = Array.Empty<EMP_TextureSamplerDef>();

        public ParticleEmissionData(ParticleNode node)
        {
            ParticleNode = node;
            SetSamplers();
            SetMaterial();

            ParticleNode.EmissionNode.Texture.PropertyChanged += Texture_PropertyChanged;
            ParticleNode.EmissionNode.Texture.TextureEntryRef.CollectionChanged += TextureEntries_CollectionChanged;
        }

        private void TextureEntries_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            IsTexturesDirty = true;
        }

        private void TextureResource_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsTexturesDirty = true;
        }

        private void Texture_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ParticleTexture.MaterialRef))
                IsMaterialsDirty = true;
        }

        public void Dispose()
        {
            ParticleNode.EmissionNode.Texture.PropertyChanged -= Texture_PropertyChanged;
            ParticleNode.EmissionNode.Texture.TextureEntryRef.CollectionChanged -= TextureEntries_CollectionChanged;
            UnsubscribeTextureEntries();
        }

        private void UnsubscribeTextureEntries()
        {
            foreach (TextureEntry_Ref entry in ObservedTextureEntries)
                entry?.PropertyChanged -= TextureResource_PropertyChanged;

            foreach (EMP_TextureSamplerDef textureDef in ObservedTextureDefs)
                textureDef?.PropertyChanged -= TextureResource_PropertyChanged;
        }

        private void SetSamplers()
        {
            int textureCount = ParticleNode.EmissionNode.Texture.TextureEntryRef.Count;
            if (textureCount > 4)
                throw new NotSupportedException($"EMP node '{ParticleNode.Name}' has {textureCount} texture slots; XenoKit supports up to four image samplers.");

            UnsubscribeTextureEntries();

            SamplerInfo[] samplers = new SamplerInfo[textureCount];
            Xv2Texture[] textures = new Xv2Texture[textureCount];
            ObservedTextureEntries = new TextureEntry_Ref[textureCount];
            ObservedTextureDefs = new EMP_TextureSamplerDef[textureCount];

            for (int index = 0; index < textureCount; index++)
            {
                TextureEntry_Ref entry = ParticleNode.EmissionNode.Texture.TextureEntryRef[index];
                if (entry == null)
                    throw new InvalidOperationException($"EMP node '{ParticleNode.Name}' has a null texture entry at slot {index}.");

                ObservedTextureEntries[index] = entry;
                entry.PropertyChanged += TextureResource_PropertyChanged;

                samplers[index].type = SamplerType.Sampler2D;
                samplers[index].textureSlot = index;
                samplers[index].samplerSlot = index;
                samplers[index].name = ShaderManager.GetSamplerName(index);
                samplers[index].state = new SamplerState { Name = samplers[index].name };

                EMP_TextureSamplerDef textureDef = entry.TextureRef;
                ObservedTextureDefs[index] = textureDef;
                if (textureDef == null)
                    continue;

                textureDef.PropertyChanged += TextureResource_PropertyChanged;
                samplers[index].state.AddressU = EmpSamplerSettings.GetAddressMode(textureDef.RepetitionU);
                samplers[index].state.AddressV = EmpSamplerSettings.GetAddressMode(textureDef.RepetitionV);
                samplers[index].state.AddressW = TextureAddressMode.Wrap;
                samplers[index].state.BorderColor = Color.White;
                samplers[index].state.Filter = EmpSamplerSettings.GetFilter(textureDef.FilteringMin, textureDef.FilteringMag);
                samplers[index].state.MaxAnisotropy = 1;
                samplers[index].state.MaxMipLevel = 1;
                samplers[index].parameter = textureDef.EmbIndex;
                textures[index] = CompiledObjectManager.GetCompiledObject<Xv2Texture>(textureDef.TextureRef);
            }

            Samplers = samplers;
            Textures = textures;
            IsTexturesDirty = false;
        }

        private void SetMaterial()
        {
            IsMaterialsDirty = false;

            Xv2ShaderEffect compiledMat = CompiledObjectManager.GetCompiledObject<Xv2ShaderEffect>(ParticleNode.EmissionNode.Texture.MaterialRef);

            if (compiledMat == null)
            {
                //No material was found for this Submesh. Use default.
                compiledMat = DefaultShaders.Red;
            }

            Material = compiledMat;
        }

        public override void Update()
        {
            if (IsMaterialsDirty)
                SetMaterial();

            if (IsTexturesDirty)
                SetSamplers();
        }
    }
}
