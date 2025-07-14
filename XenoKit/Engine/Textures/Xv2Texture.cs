using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Xv2CoreLib.EMB_CLASS;

namespace XenoKit.Engine.Textures
{
    [Serializable]
    public class Xv2Texture : Entity
    {
        #region DefaultTexture
        public static Xv2Texture DefaultTexture { get; private set; }

        public static void InitDefaultTexture(GameBase game)
        {
            if(DefaultTexture == null)
            {
                EMB_File defaultEmb = EMB_File.LoadEmb(Properties.Resources.DefaultEmb);
                DefaultTexture = new Xv2Texture(defaultEmb.Entry[0], game, false);
            }
        }
        #endregion

        [field: NonSerialized]
        private Texture2D _texture = null;

        public Texture2D Texture
        {
            get
            {
                if ((_texture == null || IsDirty) && EmbEntry != null)
                {
                    _texture = TextureLoader.ConvertToTexture2D(EmbEntry, null, GameBase.GraphicsDevice);
                    IsDirty = false;
                }
                return _texture;
            }
            private set
            {
                IsDirty = false;
                _texture = value;
            }
        }
        public EmbEntry EmbEntry { get; private set; }

        //TODO: logic for this
        public bool IsDirty { get; set; }

        public Xv2Texture(EmbEntry embEntry, GameBase gameBase, bool autoUpdate = true) : base(gameBase)
        {
            GameBase = gameBase;
            EmbEntry = embEntry;
            Texture = TextureLoader.ConvertToTexture2D(embEntry, null, GameBase.GraphicsDevice);

            if (autoUpdate)
                EmbEntry.PropertyChanged += EmbEntry_PropertyChanged;
        }

        private void EmbEntry_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(EmbEntry.Data))
            {
                IsDirty = true;
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            if (EmbEntry != null)
                EmbEntry.PropertyChanged -= EmbEntry_PropertyChanged;
        }

        public Xv2Texture HardCopy()
        {
            return new Xv2Texture(EmbEntry.Copy(), GameBase, false);
        }

        public static Xv2Texture[] LoadTextureArray(EMB_File embFile, GameBase gameBase)
        {
            Xv2Texture[] textures = new Xv2Texture[embFile.Entry.Count];

            for (int i = 0; i < textures.Length; i++)
            {
                textures[i] = gameBase.CompiledObjectManager.GetCompiledObject<Xv2Texture>(embFile.Entry[i], gameBase);
            }

            return textures;
        }

        private static Xv2Texture[] LoadTextureArray2(EMB_File embFile, GameBase gameBase)
        {
            //Alternative loader method that can handle arbitary indexing
            int maxId = embFile.Entry.Max(x => x.ID);
            Xv2Texture[] textures = new Xv2Texture[maxId + 1];

            for (int i = 0; i < textures.Length; i++)
            {
                //TODO: GetEntryWithID needs to be optimized, as EmbEntry internally uses a string for its ID and its converting to int all the time
                EmbEntry entry = embFile.GetEntryWithID(i);

                if(entry != null)
                {
                    textures[i] = gameBase.CompiledObjectManager.GetCompiledObject<Xv2Texture>(entry, gameBase);
                }
                else
                {
                    textures[i] = DefaultTexture;
                }
            }

            return textures;
        }
    }
}
